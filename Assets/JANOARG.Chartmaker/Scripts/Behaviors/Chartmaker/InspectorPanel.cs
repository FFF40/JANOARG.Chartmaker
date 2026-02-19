using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using JANOARG.Chartmaker.Data.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker.Actions;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Form;
using JANOARG.Chartmaker.UI.Form.FormTypes;
using JANOARG.Chartmaker.UI.Inspector;
using JANOARG.Chartmaker.UI.Pickers.ObjectPicker;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class InspectorPanel : MonoBehaviour
    {
        public static InspectorPanel main;

        public InspectorMode CurrentMode;

        [NonSerialized]
        public object CurrentHierarchyObject = null;
        public object          CurrentObject;
        public List<Timestamp> CurrentTimestamp;

        public TMP_Text      FormTitle;
        public RectTransform FormHolder;
        public RectTransform OffsetFieldHolder;

        [Space]
        public Button PropertiesButton;
        public Button StatisticsButton;
        [Space]
        public GameObject Collapser;
        [Space]
        public Button     ExtraModesButton;
        [Space]
        public Button BackButton;
        [Space]
        public FieldInfo CurrentMultiField;
        public ChartmakerMultiHandler                   MultiHandler;
        public Dictionary<Type, ChartmakerMultiHandler> MultiHandlers = new ();
        [Space]
        public DebugStatsInspector DebugStatsSample;
        public LaneStatsInspector  LaneStatsSample;
        public LaneGroupStatsInspector LaneGroupStatsSample;
        [Space]
        public Button EaseCopyToRightButtonSample;
        public EaseCopyToBottomItem EaseCopyToButtomItemSample;
        [Space]
        public bool IsCollapsed;
        public bool          IsCoverDirty;
        public RectTransform PanelHolder;

        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            UpdateForm();
            PropertiesButton.onClick.AddListener(() => SetMode(InspectorMode.Properties));
            StatisticsButton.onClick.AddListener(() => SetMode(InspectorMode.Statistics));
        }

        public void OnObjectChange()
        {
            UpdateButtons();
            UpdateForm();
        
            TimelinePanel.main.UpdateTabs();
            TimelinePanel.main.UpdateItems();
        
            Chartmaker.main.OnClipboardUpdate();
        
            PlayerView.main.UpdateHandles();
        }

        public void UnsetObject()
        {
            if (CurrentTimestamp?.Count > 0)
                CurrentTimestamp = new ();
            else
                CurrentObject = null;
        
            OnObjectChange();
        }

        public void SetMode(InspectorMode mode)
        {
            CurrentMode = mode;
        
            if (IsCollapsed) 
                Restore();
        
            OnObjectChange();
        }

        public void SetObject(object obj, bool? forceAdd = null)
        {
            if (forceAdd ?? (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                try 
                {
                    // Convert obj to a list based on its type
                    IList listIn = obj switch
                    {
                        List<Timestamp> listTimeStamp => listTimeStamp,
                        IList list                    => list,
                        Timestamp timestamp           => new List<Timestamp> { timestamp },
                        _                             => new List<object> { obj }
                    };

                    // Get the target list (prefer CurrentTimestamp if it has items)
                    IList listTarget = CurrentTimestamp?.Count > 0 
                        ? CurrentTimestamp 
                        : CurrentObject as IList ?? new List<object> { CurrentObject };

                    // Merge lists if they contain the same type
                    if (listIn.Count > 0 && listTarget.Count > 0 && listIn[0]?.GetType() == listTarget[0]?.GetType())
                    {
                        foreach (object item in listTarget)
                        {
                            if (!listIn.Contains(item))
                                listIn.Add(item);
                            else
                                listIn.Remove(item);
                        }
                    }

                    obj = listIn;
                } 
                catch (NotSupportedException e)
                {
                    UnityEngine.Debug.Log(e);
                    UnityEngine.Debug.Log("Force add is " + forceAdd);
                }
            }
            switch (obj)
            {
                case Timestamp ts:
                    CurrentTimestamp = new () { ts }; break;
            
                case List<Timestamp> timestampList:
                    CurrentTimestamp = timestampList; break;
            
                case IList list when list.Count > 0 && list[0] is Timestamp:
                {
                    CurrentTimestamp = new ();
                    foreach (object item in list) 
                        CurrentTimestamp.Add((Timestamp)item);

                    break;
                }
            
                case IList list:
                {
                    obj = list.Count switch
                    {
                        0 => null,
                        1 => list[0],
                        _ => obj
                    };

                    CurrentObject = obj;
                    CurrentTimestamp = new ();
                
                    if (Helper.IsHierarchyObject(obj)) 
                    {
                        CurrentHierarchyObject = obj;
                        HierarchyPanel.main.UpdateHolderSelection();
                    }

                    break;
                }
                default:
                {
                    CurrentObject = obj;
                    CurrentTimestamp = new ();
                
                    if (Helper.IsHierarchyObject(obj)) 
                    {
                        CurrentHierarchyObject = obj;
                        HierarchyPanel.main.UpdateHolderSelection();
                    }

                    break;
                }
            }
            OnObjectChange();
        }

        public void ClearForm()
        {
            foreach (RectTransform rt in FormHolder)
                Destroy(rt.gameObject);
        
            foreach (RectTransform rt in OffsetFieldHolder)
                Destroy(rt.gameObject);
        }

        public void UpdateButtons()
        {
            BackButton.gameObject.SetActive(CurrentObject != null);

            PropertiesButton.interactable = IsCollapsed || CurrentMode != InspectorMode.Properties;
            StatisticsButton.interactable = IsCollapsed || CurrentMode != InspectorMode.Statistics;
            Collapser.transform.SetParent((CurrentMode switch
            {
                InspectorMode.Properties => PropertiesButton,
                InspectorMode.Statistics => StatisticsButton,
                _ => PropertiesButton,
            }).transform.parent);
            ((RectTransform)Collapser.transform).anchoredPosition = Vector2.zero;
        }

        public void EventCollapsible()
        {
            if (IsCollapsed)
                Restore();
            else
                Collapse();
        }
        
        public void UpdateForm()
        {
            ClearForm();

            switch (CurrentMode)
            {
                case InspectorMode.Properties or InspectorMode.Statistics when CurrentObject == null:
                    Collapser.SetActive(true);
                    FormTitle.text = "No object selected";
                    SpawnForm<FormEntryLabel>("Select an item to get started.");
                    break;
            
                case InspectorMode.Properties:
                    
                    Collapser.SetActive(true);

                    switch (CurrentTimestamp.Count)
                    {
                        case 1:
                            {
                                FormTitle.text = "Timestamp";

                                Timestamp ts = CurrentTimestamp[0];
                                MakeOffsetEntry(() => ts.Offset, x => Chartmaker.main.SetItem(ts, "Offset", x));

                                SpawnForm<FormEntryHeader>("General");
                                SpawnForm<FormEntryFloat, float>("Duration", () => ts.Duration, x => Chartmaker.main.SetItem(ts, "Duration", x));
                                SpawnForm<FormEntryToggleFloat, float>("From", () => ts.From, x => Chartmaker.main.SetItem(ts, "From", x));
                                SpawnForm<FormEntryFloat, float>("To", () => ts.Target, x => Chartmaker.main.SetItem(ts, "Target", x));
                                SpawnForm<FormEntryEasing, IEaseDirective>("Easing", () => ts.Easing, x => Chartmaker.main.SetItem(ts, "Easing", x));
                                break;
                            }
                        case >= 1:
                            FormTitle.text = "Multi-select";

                            MakeMultiEditForm(CurrentTimestamp);
                            break;

                        default:
                            {
                                switch (CurrentObject)
                                {
                                    case IList list when CurrentObject != Chartmaker.main.CurrentChart?.Groups:
                                        FormTitle.text = "Multi-select";

                                        MakeMultiEditForm(list);
                                        break;

                                    case PlayableSong song when song != Chartmaker.main.CurrentSong:
                                        SetObject(null);
                                        return;

                                    case PlayableSong song:
                                        FormTitle.text = "Playable Song";

                                        SpawnForm<FormEntryHeader>("Metadata");

                                        SpawnForm<FormEntryString, string>("Song Name", () => song.SongName, x => Chartmaker.main.SetItem(song, "SongName", x));
                                        SpawnForm<FormEntryString, string>("Alt. Name", () => song.AltSongName, x => Chartmaker.main.SetItem(song, "AltSongName", x));
                                        SpawnForm<FormEntrySpace>("");
                                        SpawnForm<FormEntryString, string>("Song Artist", () => song.SongArtist, x => Chartmaker.main.SetItem(song, "SongArtist", x));
                                        SpawnForm<FormEntryString, string>("Alt. Artist", () => song.AltSongArtist, x => Chartmaker.main.SetItem(song, "AltSongArtist", x));
                                        SpawnForm<FormEntrySpace>("");
                                        SpawnForm<FormEntryString, string>("Genre", () => song.Genre, x => Chartmaker.main.SetItem(song, "Genre", x));
                                        SpawnForm<FormEntryString, string>("Location", () => song.Location, x => Chartmaker.main.SetItem(song, "Location", x));
                                        SpawnForm<FormEntrySpace>("");
                                        SpawnForm<FormEntryTimeRange, Vector2>("Preview Range", () => song.PreviewRange, x => Chartmaker.main.SetItem(song, "PreviewRange", x));

                                        SpawnForm<FormEntryHeader>("Accent Colors");
                                        SpawnForm<FormEntryColor, Color>("Background", () => song.BackgroundColor, x => Chartmaker.main.SetItem(song, "BackgroundColor", x));
                                        SpawnForm<FormEntryColor, Color>("Interface", () => song.InterfaceColor, x => Chartmaker.main.SetItem(song, "InterfaceColor", x));
                                        break;

                                    case Cover cover when cover != Chartmaker.main.CurrentSong.Cover:
                                        SetObject(null);
                                        return;

                                    case Cover cover:
                                        {
                                            FormTitle.text = "Cover";

                                            SpawnForm<FormEntryHeader>("Metadata");

                                            SpawnForm<FormEntryString, string>("Artist Name", () => cover.ArtistName,
                                                x => Chartmaker.main.SetItem(cover, "ArtistName", x));
                                            SpawnForm<FormEntryString, string>("Alt. Name", () => cover.AltArtistName,
                                                x => Chartmaker.main.SetItem(cover, "AltArtistName", x));

                                            SpawnForm<FormEntryHeader>("Colors");
                                            FormEntryColor bgColor = SpawnForm<FormEntryColor, Color>("Background", () => cover.BackgroundColor, x =>
                                            {
                                                Chartmaker.main.SetItem(cover, "BackgroundColor", x); IsCoverDirty = true;
                                            });
                                            FormEntryButton copy = SpawnForm<FormEntryButton>("Copy from Playable Song");
                                            copy.Button.onClick.AddListener(() =>
                                            {
                                                Chartmaker.main.SetItem(cover, "BackgroundColor", Chartmaker.main.CurrentSong.BackgroundColor);
                                                bgColor.Start();
                                            });

                                            SpawnForm<FormEntryHeader>("Icon");
                                            SpawnForm<FormEntryString, string>("Save Target", () => cover.IconTarget, x =>
                                            {
                                                Chartmaker.main.SetItem(cover, "IconTarget", x); IsCoverDirty = true;
                                            });
                                            SpawnForm<FormEntryVector2, Vector2>("Center", () => cover.IconCenter, x =>
                                            {
                                                Chartmaker.main.SetItem(cover, "IconCenter", x); IsCoverDirty = true;
                                            });
                                            SpawnForm<FormEntryFloat, float>("Size", () => cover.IconSize, x =>
                                            {
                                                Chartmaker.main.SetItem(cover, "IconSize", x); IsCoverDirty = true;
                                            });
                                            FormEntryButton update = SpawnForm<FormEntryButton>("Update Icon File");
                                            update.Button.onClick.AddListener(() =>
                                            {
                                                PlayerView.main.UpdateIconFile();
                                            });
                                            break;
                                        }

                                    case CoverLayer layer when !Chartmaker.main.CurrentSong.Cover.Layers.Contains(layer):
                                        SetObject(null);
                                        return;

                                    case CoverLayer layer:
                                        FormTitle.text = "Cover Layer";

                                        SpawnForm<FormEntryHeader>("Transform");
                                        SpawnForm<FormEntryVector2, Vector2>("Position", () => layer.Position, x =>
                                        {
                                            Chartmaker.main.SetItem(layer, "Position", x); IsCoverDirty = true;
                                        });
                                        SpawnForm<FormEntryFloat, float>("Scale", () => layer.Scale, x =>
                                        {
                                            Chartmaker.main.SetItem(layer, "Scale", x); IsCoverDirty = true;
                                        });
                                        SpawnForm<FormEntryFloat, float>("Parallax Z", () => layer.ParallaxFactor, x =>
                                        {
                                            Chartmaker.main.SetItem(layer, "ParallaxFactor", x); IsCoverDirty = true;
                                        });
                                        SpawnForm<FormEntryBool, bool>("Tiling", () => layer.Tiling, x =>
                                        {
                                            Chartmaker.main.SetItem(layer, "Tiling", x); IsCoverDirty = true;
                                        });
                                        break;

                                    case BPMStop stop when !Chartmaker.main.CurrentSong.Timing.Stops.Contains(stop):
                                        SetObject(null);
                                        return;

                                    case BPMStop stop:
                                        FormTitle.text = "BPM Stop";
                                        MakeOffsetEntry(() => stop.Offset, x => Chartmaker.main.SetItem(stop, "Offset", x));

                                        SpawnForm<FormEntryHeader>("Properties");
                                        SpawnForm<FormEntryFloat, float>("BPM", () => stop.BPM, x => Chartmaker.main.SetItem(stop, "BPM", x));
                                        SpawnForm<FormEntryInt, int>("Signature", () => stop.Signature, x => Chartmaker.main.SetItem(stop, "Signature", x));
                                        SpawnForm<FormEntryHeader>("Flags");
                                        SpawnForm<FormEntryBool, bool>("Significant", () => stop.Significant, x => Chartmaker.main.SetItem(stop, "Significant", x));
                                        break;

                                    case Chart chart when chart != Chartmaker.main.CurrentChart:
                                        SetObject(null);
                                        return;

                                    case Chart chart:
                                        {
                                            ExternalChartMeta meta = Chartmaker.main.CurrentChartMeta;
                                            FormTitle.text = "Chart";

                                            SpawnForm<FormEntryHeader>("Metadata");

                                            SpawnForm<FormEntryString, string>("Chart Name", () => chart.DifficultyName,
                                                x => Chartmaker.main.SetItem(chart, "DifficultyName", meta.DifficultyName = x));
                                            SpawnForm<FormEntryInt, int>("Sorting Index", () => chart.DifficultyIndex,
                                                x => Chartmaker.main.SetItem(chart, "DifficultyIndex", meta.DifficultyIndex = x));
                                            SpawnForm<FormEntrySpace>("");
                                            SpawnForm<FormEntryString, string>("Charter Name", () => chart.CharterName,
                                                x => Chartmaker.main.SetItem(chart, "CharterName", meta.CharterName = x));
                                            SpawnForm<FormEntryString, string>("Alt C. Name", () => chart.AltCharterName,
                                                x => Chartmaker.main.SetItem(chart, "AltCharterName", meta.AltCharterName = x));
                                            SpawnForm<FormEntrySpace>("");
                                            SpawnForm<FormEntryString, string>("Difficulty", () => chart.DifficultyLevel,
                                                x => Chartmaker.main.SetItem(chart, "DifficultyLevel", meta.DifficultyLevel = x));
                                            SpawnForm<FormEntryFloat, float>("Chart Constant", () => chart.ChartConstant,
                                                x => Chartmaker.main.SetItem(chart, "ChartConstant", meta.ChartConstant = x));

                                            break;
                                        }

                                    case Palette pallete when pallete != Chartmaker.main.CurrentChart?.Palette:
                                        SetObject(null);
                                        return;

                                    case Palette pallete:
                                        {
                                            FormTitle.text = "Palette";

                                            SpawnForm<FormEntryHeader>("Colors");
                                            FormEntryColor bgColor = SpawnForm<FormEntryColor, Color>("Background", () => pallete.BackgroundColor, x => Chartmaker.main.SetItem(pallete, "BackgroundColor", x));
                                            FormEntryColor fgColor = SpawnForm<FormEntryColor, Color>("Interface", () => pallete.InterfaceColor, x => Chartmaker.main.SetItem(pallete, "InterfaceColor", x));

                                            FormEntryButton copy = SpawnForm<FormEntryButton>("Copy from Playable Song");
                                            copy.Button.onClick.AddListener(() =>
                                            {
                                                Chartmaker.main.SetItem(pallete, "BackgroundColor", Chartmaker.main.CurrentSong.BackgroundColor);
                                                bgColor.Start();
                                                Chartmaker.main.SetItem(pallete, "InterfaceColor", Chartmaker.main.CurrentSong.InterfaceColor);
                                                fgColor.Start();
                                            });

                                            break;
                                        }

                                    case LaneStyle laneStyle when Chartmaker.main.CurrentChart?.Palette.LaneStyles.Contains(laneStyle) != true:
                                        SetObject(null);
                                        return;

                                    case LaneStyle laneStyle:
                                        FormTitle.text = "Lane Style";

                                        SpawnForm<FormEntryHeader>("Lane");
                                        SpawnForm<FormEntryColor, Color>("Color", () => laneStyle.LaneColor, x => Chartmaker.main.SetItem(laneStyle, "LaneColor", x));

                                        SpawnForm<FormEntryHeader>("Judge");
                                        SpawnForm<FormEntryColor, Color>("Color", () => laneStyle.JudgeColor, x => Chartmaker.main.SetItem(laneStyle, "JudgeColor", x));
                                        break;

                                    case HitStyle hitStyle when Chartmaker.main.CurrentChart?.Palette.HitStyles.Contains(hitStyle) != true:
                                        SetObject(null);
                                        return;

                                    case HitStyle hitStyle:
                                        FormTitle.text = "Hit Style";

                                        SpawnForm<FormEntryHeader>("Hit Body");
                                        SpawnForm<FormEntryColor, Color>("Normal Color", () => hitStyle.NormalColor, x => Chartmaker.main.SetItem(hitStyle, "NormalColor", x));
                                        SpawnForm<FormEntryColor, Color>("Catch Color", () => hitStyle.CatchColor, x => Chartmaker.main.SetItem(hitStyle, "CatchColor", x));

                                        SpawnForm<FormEntryHeader>("Hold Tail");
                                        SpawnForm<FormEntryColor, Color>("Color", () => hitStyle.HoldTailColor, x => Chartmaker.main.SetItem(hitStyle, "HoldTailColor", x));
                                        break;

                                    default:
                                        {
                                            if (CurrentObject == Chartmaker.main.CurrentChart?.Groups)
                                            {
                                                FormTitle.text = "Lane Groups";

                                                List<LaneGroup> groups = Chartmaker.main.CurrentChart?.Groups;

                                                var listHeader = SpawnForm<FormEntryListHeader>("Groups");
                                                listHeader.Button.onClick.AddListener(() =>
                                                {
                                                    LaneGroup group = new();
                                                    int index = 1;
                                                    while (Chartmaker.main.CurrentChart.Groups.FindIndex(x => x.Name == "Group " + index) >= 0) index++;
                                                    group.Name = "Group " + index;
                                                    Chartmaker.main.AddItem(group);
                                                });
                                                int index = 0;
                                                foreach (LaneGroup group in groups)
                                                {
                                                    LaneGroup Group = group;
                                                    var button = SpawnForm<FormEntryListItem>(group.Name);
                                                    button.Button.onClick.AddListener(() =>
                                                    {
                                                        SetObject(Group);
                                                    });
                                                    button.RemoveButton.onClick.AddListener(() =>
                                                    {
                                                        Chartmaker.main.DeleteItem(Group, false);
                                                    });
                                                    index++;
                                                }
                                            }
                                            else switch (CurrentObject)
                                            {
                                                case LaneGroup group when Chartmaker.main.CurrentChart?.Groups!.Contains(group) != true:
                                                    SetObject(null);
                                                    return;

                                                case LaneGroup group:
                                                    FormTitle.text = "Lane Group";

                                                    SpawnForm<FormEntryHeader>("Transform");
                                                    SpawnForm<FormEntryVector3, Vector3>("Position", () => group.Position, x => Chartmaker.main.SetItem(group, "Position", x));
                                                    SpawnForm<FormEntryVector3, Vector3>("Rotation", () => group.Rotation, x => Chartmaker.main.SetItem(group, "Rotation", x));
                                                    SpawnForm<FormEntryHeader>("Statistics");
                                                    LaneGroupStatsSample.HightlightedLaneGroup = group;
                                                    Instantiate(LaneGroupStatsSample, FormHolder);
                                                    break;

                                                case CameraController camera when camera != Chartmaker.main.CurrentChart?.Camera:
                                                    SetObject(null);
                                                    return;

                                                case CameraController camera:
                                                    FormTitle.text = "Camera Controller";

                                                    SpawnForm<FormEntryHeader>("Pivot");
                                                    SpawnForm<FormEntryVector3, Vector3>("Position", () => camera.CameraPivot, x => Chartmaker.main.SetItem(camera, "CameraPivot", x));
                                                    SpawnForm<FormEntryVector3, Vector3>("Rotation", () => camera.CameraRotation, x => Chartmaker.main.SetItem(camera, "CameraRotation", x));
                                                    SpawnForm<FormEntryFloat, float>("Distance", () => camera.PivotDistance, x => Chartmaker.main.SetItem(camera, "PivotDistance", x));
                                                    break;

                                                case Lane lane when Chartmaker.main.CurrentChart?.Lanes.Contains(lane) != true:
                                                    SetObject(null);
                                                    return;

                                                case Lane lane:
                                                    FormTitle.text = "Lane";

                                                    SpawnForm<FormEntryHeader>("Transform");
                                                    SpawnForm<FormEntryVector3, Vector3>("Position", () => lane.Position, x => Chartmaker.main.SetItem(lane, "Position", x));
                                                    SpawnForm<FormEntryVector3, Vector3>("Rotation", () => lane.Rotation, x => Chartmaker.main.SetItem(lane, "Rotation", x));
                                                    SpawnForm<FormEntryHeader>("Appearance");
                                                    MakeLaneStyleEntry(lane);

                                                    break;

                                                case LaneStep step when CurrentHierarchyObject is not Lane l || !l.LaneSteps.Contains(step):
                                                    SetObject(null);
                                                    return;

                                                case LaneStep step:
                                                {
                                                    FormTitle.text = "Lane Step";
                                                    MakeOffsetEntry(() => step.Offset, x => Chartmaker.main.SetItem(step, "Offset", x));

                                                    SpawnForm<FormEntryHeader>("Transform");
                                                    SpawnForm<FormEntryVector2, Vector2>("Start Pos", () => step.StartPointPosition, x => Chartmaker.main.SetItem(step, "StartPointPosition", x));
                                                    SpawnForm<FormEntryVector2, Vector2>("End Pos", () => step.EndPointPosition, x => Chartmaker.main.SetItem(step, "EndPointPosition", x));

                                                    var easeHeader = SpawnForm<FormEntryLabel>("Easings");

                                                    easeHeader.TitleLabel.margin -= new Vector4(0, 0, 0, 4);

                                                    FormEntryEasing startX, startY, endX, endY;

                                                    SetEase2(
                                                        startX = SpawnForm<FormEntryEasing, IEaseDirective>("Start Ease", () => step.StartEaseX,
                                                            x => Chartmaker.main.SetItem(step, "StartEaseX", x)
                                                        ),
                                                        startY = SpawnForm<FormEntryEasing, IEaseDirective>("", () => step.StartEaseY,
                                                            x => Chartmaker.main.SetItem(step, "StartEaseY", x)
                                                        )
                                                    );

                                                    EaseCopyToBottomItem copyPasteToBottom = Instantiate(EaseCopyToButtomItemSample, FormHolder);
                                                    SetEase2(
                                                        endX = SpawnForm<FormEntryEasing, IEaseDirective>("End Ease", () => step.EndEaseX,
                                                            x => Chartmaker.main.SetItem(step, "EndEaseX", x)
                                                        ),
                                                        endY = SpawnForm<FormEntryEasing, IEaseDirective>("", () => step.EndEaseY,
                                                            x => Chartmaker.main.SetItem(step, "EndEaseY", x)
                                                        )
                                                    );

                                                    copyPasteToBottom.SetFormItems(startX, startY, endX, endY);
                                                    SpawnForm<FormEntrySpace>();
                                                    SpawnForm<FormEntryFloat, float>("Speed", () => step.Speed, x => Chartmaker.main.SetItem(step, "Speed", x));
                                                    break;
                                                }

                                                case HitObject hit when CurrentHierarchyObject is not Lane l || !l.Objects.Contains(hit):
                                                    SetObject(null);
                                                    return;

                                                case HitObject hit:
                                                {
                                                    FormTitle.text = "Hit Object";
                                                    MakeOffsetEntry(() => hit.Offset, x => Chartmaker.main.SetItem(hit, "Offset", x));

                                                    SpawnForm<FormEntryHeader>("Type");

                                                    FormEntryDropdown typeDropdown = SpawnForm<FormEntryDropdown, object>("", () => hit.Type, x => hit.Type = (HitObject.HitType)x);
                                                    typeDropdown.TargetEnum(typeof(HitObject.HitType));
                                                    typeDropdown.TitleLabel.gameObject.SetActive(false);
                                                    typeDropdown.GetComponent<HorizontalLayoutGroup>().padding.left = 10;

                                                    SpawnForm<FormEntryHeader>("Transform");
                                                    SpawnForm<FormEntryFloat, float>("Position", () => hit.Position, x => Chartmaker.main.SetItem(hit, "Position", x));
                                                    SpawnForm<FormEntryFloat, float>("Width", () => hit.Length, x => Chartmaker.main.SetItem(hit, "Length", x));
                                                    SpawnForm<FormEntryFloat, float>("Hold Length", () => hit.HoldLength, x => Chartmaker.main.SetItem(hit, "HoldLength", x));

                                                    SpawnForm<FormEntryHeader>("Appearance");
                                                    MakeHitStyleEntry(hit);

                                                    FormEntryToggleFloat dirField = null;
                                                    SpawnForm<FormEntryHeader>("Behavior");
                                                    SpawnForm<FormEntryBool, bool>("Flickable", () => hit.Flickable, x =>
                                                    {
                                                        Chartmaker.main.SetItem(hit, "Flickable", x);
                                                        dirField?.gameObject.SetActive(x);
                                                    });
                                                    dirField = SpawnForm<FormEntryToggleFloat, float>("Direction", () => hit.FlickDirection, x => Chartmaker.main.SetItem(hit, "FlickDirection", x));
                                                    dirField.gameObject.SetActive(hit.Flickable);

                                                    SpawnForm<FormEntryBool, bool>("Fake", () => hit.IsFake, x => Chartmaker.main.SetItem(hit, "IsFake", x));
                                                    break;
                                                }

                                                default:
                                                    FormTitle.text = Chartmaker.GetItemName(CurrentObject);

                                                    SpawnForm<FormEntryLabel>("Unsupported object " + CurrentObject.GetType());

                                                    break;
                                            }

                                            break;
                                        }
                                }

                                break;
                            }
                    }

                    break;
                    
                case InspectorMode.Statistics:
                    switch (CurrentObject)
                    {
                        case Lane lane:
                            FormTitle.text = "Statistics of Lane";

                            var statsHolder = Instantiate(LaneStatsSample, FormHolder);
                            statsHolder.HightlightedLane = lane;

                            break;

                        default:
                            FormTitle.text = "Statistics of " + Chartmaker.GetItemName(CurrentObject);

                            SpawnForm<FormEntryLabel>("This object does not keep any statistics.");

                            break;
                    }
                    break;
            

                case InspectorMode.DebugStats:
                    FormTitle.text = "Debug Stats";
                    Collapser.SetActive(false);

                    Instantiate(DebugStatsSample, FormHolder);

                    break;
            }
        }

        private void MakeOffsetEntry(Func<float> get, Action<float> set)
        {
            var field = SpawnForm<FormEntryFloat, float>("", get, set);
            field.transform.SetParent(OffsetFieldHolder);
            field.TitleLabel.gameObject.SetActive(false);
        }

        private void MakeOffsetEntry(Func<BeatPosition> get, Action<BeatPosition> set)
        {
            var field = SpawnForm<FormEntryBeatPosition, BeatPosition>("", get, set);
            field.transform.SetParent(OffsetFieldHolder);
            field.TitleLabel.gameObject.SetActive(false);
        }

        private void MakeLaneStyleEntry(Lane lane)
        {
            var list = Chartmaker.main.CurrentChart.Palette.LaneStyles;
            var dropdown = SpawnForm<FormEntryObject, object>("Style", 
                () => lane.StyleIndex < 0 || lane.StyleIndex >= list.Count ? null : list[lane.StyleIndex], 
                (x) => Chartmaker.main.SetItem(lane, "StyleIndex", list.IndexOf((LaneStyle)x))
            );
            dropdown.CurrentIndex = lane.StyleIndex;
            dropdown.SetType(ObjectPickerType.LaneStyle);
        }

        private void MakeHitStyleEntry(HitObject hit)
        {
            var list = Chartmaker.main.CurrentChart.Palette.HitStyles;
            var dropdown = SpawnForm<FormEntryObject, object>("Style", 
                () => hit.StyleIndex < 0 || hit.StyleIndex >= list.Count ? null : list[hit.StyleIndex], 
                (x) => Chartmaker.main.SetItem(hit, "StyleIndex", list.IndexOf((HitStyle)x))
            );
            dropdown.CurrentIndex = hit.StyleIndex;
            dropdown.SetType(ObjectPickerType.HitStyle);
        }

        private void MakeMultiEditForm(IList thing)
        {
            Dictionary<string, string> nameOverrides = new() 
            {
                ["StartPointPosition"] = "Start Position",
                ["EndPointPosition"] = "End Position",
                ["StartEaseX"] = "Start Ease X",
                ["StartEaseY"] = "Start Ease Y",
                ["EndEaseX"] = "End Ease X",
                ["EndEaseY"] = "End Ease Y",
                
                // Note
                ["Length"] = "Note width",
                ["HoldLength"] = "Note hold length",
                ["FlickDirection"] = "Flick direction",
                ["IsFake"] = "Fake",
                
                ["StyleIndex"] = "Style Index",
            };

            string[] variablesToHide = new[]
            {
                "IsDirty", "IsSimultaneous"
            };
            SpawnForm<FormEntrySpace>("");
            SpawnForm<FormEntryLabel>(Chartmaker.GetItemName(thing));

            SpawnForm<FormEntryHeader>("Multi-edit");

            var fields = Array.FindAll(thing[0].GetType().GetFields(), field => !(
                typeof(IEnumerable).IsAssignableFrom(field.FieldType)
                || typeof(Storyboard) == field.FieldType
                || field.IsStatic || field.IsLiteral || !field.IsPublic
            )).ToList();
            if (fields.IndexOf(CurrentMultiField) < 0) SetMultiField(fields[0]);
            
            // Filter internal fields
            fields.RemoveAll(field => variablesToHide.Contains(field.Name));

            var dropdown = SpawnForm<FormEntryDropdown, object>("Target", () => CurrentMultiField, x => {
                SetMultiField((FieldInfo)x);
                UpdateForm();
            });
            foreach (FieldInfo field in fields)
            {
                string name = nameOverrides.TryGetValue(field.Name, out var @override) ? @override : field.Name;
                dropdown.ValidValues.Add(field, name);
            }

            SpawnForm<FormEntrySpace>("");
        
            void MakeLerpableEditor<T>(LerpableMultiHandler<T> lerpHandler)
            {
                bool advanced = float.IsFinite(lerpHandler.From);
                SpawnForm<FormEntryBool, bool>("Advanced", () => advanced, x => { 
                    lerpHandler.From = x ? lerpHandler.To : float.NaN; 
                    if (x) lerpHandler.SetLerp(thing);
                    UpdateForm();
                });
                if (advanced) SpawnForm<FormEntryFloat, float>("From", () => lerpHandler.From, x => { lerpHandler.From = x; });
                SpawnForm<FormEntryFloat, float>("To", () => lerpHandler.To, x => { lerpHandler.To = x; });
            
                if (advanced) 
                {
                    var lerpDropdown = SpawnForm<FormEntryDropdown, object>("Lerp Source", () => lerpHandler.LerpSource, x => {
                        lerpHandler.LerpSource = (string)x; lerpHandler.SetLerp(thing);
                    });
                    foreach (FieldInfo field in fields)
                    {
                        if (typeof(float).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(BeatPosition))
                            lerpDropdown.ValidValues.Add(field.Name, field.Name);
                    }
                    SpawnForm<FormEntryEasing, IEaseDirective>("Lerp Easing", () => lerpHandler.LerpEasing, x => lerpHandler.LerpEasing = x);
                }
            
                SpawnForm<FormEntryDropdown, object>("Operation", () => lerpHandler.Operation, 
                    x => { lerpHandler.Operation = (LerpableOperation)x; }
                ).TargetEnum(typeof(LerpableOperation));
            }
            void MakeBeatPositionEditor(ChartmakerMultiHandlerBeatPosition beatHandler)
            {
                bool advanced = !BeatPosition.IsNaN(beatHandler.From);
                SpawnForm<FormEntryBool, bool>("Advanced", () => advanced, x => { 
                    beatHandler.From = x ? beatHandler.To : BeatPosition.NaN; 
                    if (x) beatHandler.SetLerp(thing);
                    UpdateForm();
                });
                if (advanced)
                    SpawnForm<FormEntryBeatPosition, BeatPosition>("From", () => beatHandler.From, x => { beatHandler.From = x; });
                
                SpawnForm<FormEntryBeatPosition, BeatPosition>("To", () => beatHandler.To, x => { beatHandler.To = x; });
            
                if (advanced) 
                {
                    var lerpDropdown = SpawnForm<FormEntryDropdown, object>("Lerp Source", () => beatHandler.LerpSource, x => {
                        beatHandler.LerpSource = (string)x; beatHandler.SetLerp(thing);
                    });
                    foreach (FieldInfo field in fields)
                    {
                        if (typeof(float).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(BeatPosition))
                            lerpDropdown.ValidValues.Add(field.Name, field.Name);
                    }
                    SpawnForm<FormEntryEasing, IEaseDirective>("Lerp Easing", () => beatHandler.LerpEasing, x => beatHandler.LerpEasing = x);
                }
            
                SpawnForm<FormEntryDropdown, object>("Operation", () => beatHandler.Operation, 
                    x => { beatHandler.Operation = (BeatPositionOperation)x; }
                ).TargetEnum(typeof(BeatPositionOperation));
            }

            switch (MultiHandler)
            {
                case ChartmakerMultiHandlerBoolean boolHandler:
                    SpawnForm<FormEntryDropdown, object>("To", () => boolHandler.To == null ? 2 : (bool)boolHandler.To ? 1 : 0, x => 
                    {
                        boolHandler.To = new bool?[] {true, false, null}[(int)x];
                    }).TargetList("False", "True", "Toggle");

                    break;
                case ChartmakerMultiHandlerBeatPosition beatHandler:
                    MakeBeatPositionEditor(beatHandler);
    
                    break;
                case ChartmakerMultiHandlerFloat floatHandler:
                    MakeLerpableEditor(floatHandler);

                    break;
                case ChartmakerMultiHandlerVector2 v2Handler:
                    SpawnForm<FormEntryDropdown, object>("Axis", () => v2Handler.Axis, x => { v2Handler.Axis = (int)x; }).TargetList("X", "Y");
                    SpawnForm<FormEntrySpace>("");
                    MakeLerpableEditor(v2Handler);

                    break;
                case ChartmakerMultiHandlerVector3 v3Handler:
                    SpawnForm<FormEntryDropdown, object>("Axis", () => v3Handler.Axis, x => { v3Handler.Axis = (int)x; }).TargetList("X", "Y", "Z");
                    SpawnForm<FormEntrySpace>("");
                    MakeLerpableEditor(v3Handler);

                    break;
                case ChartmakerMultiHandler<int> intHandler:
                    MultiHandler.To ??= 0;
                    SpawnForm<FormEntryInt, int>("To", () => (int)intHandler.To, x => { intHandler.To = x; });

                    break;
                case ChartmakerMultiHandler<string> stringHandler:
                    MultiHandler.To ??= "";
                    SpawnForm<FormEntryString, string>("To", () => (string)stringHandler.To, x => { stringHandler.To = x; });

                    break;
                case ChartmakerMultiHandler<IEaseDirective> easeHandler:
                    MultiHandler.To ??= new BasicEaseDirective();
                    SpawnForm<FormEntryEasing, IEaseDirective>("To", () => (IEaseDirective)easeHandler.To, x => { easeHandler.To = x; });

                    break;
                default:
                {
                    if (MultiHandler.TargetType.IsEnum) {
                        MultiHandler.To ??= MultiHandler.TargetType.GetEnumValues().GetValue(0);
                        SpawnForm<FormEntryDropdown, object>("To", () => MultiHandler.To, x => {
                            MultiHandler.To = x;
                        }).TargetEnum(MultiHandler.TargetType);
                    } else {
                        SpawnForm<FormEntryLabel>("Unknown field type " + CurrentMultiField?.FieldType);
                    }

                    break;
                }
            }

            SpawnForm<FormEntrySpace>("");

            var button = SpawnForm<FormEntryButton>("Execute");
            button.Button.onClick.AddListener(() => {
                ExecuteMulti(thing, Chartmaker.main.History);
            });
        }

        private void ExecuteMulti(IList items, ChartmakerHistory history) {

            ChartmakerMultiEditAction action = new ChartmakerMultiEditAction() 
            { 
                Keyword = CurrentMultiField.Name 
            };

            foreach(object obj in items) {
                ChartmakerMultiEditActionItem item = new ChartmakerMultiEditActionItem
                {
                    Target = obj,
                    From = CurrentMultiField.GetValue(obj),
                };
                item.To = MultiHandler.Get(item.From, obj);
                action.Targets.Add(item);
            }
            action.Redo();
            history.ActionsBehind.Push(action);
            history.ActionsAhead.Clear();
            Chartmaker.main.OnHistoryDo();
            Chartmaker.main.OnHistoryUpdate();
        }


        private void SetMultiField(FieldInfo field)
        {
            MultiHandler = MultiHandlers.ContainsKey(field.FieldType) ? MultiHandlers[field.FieldType] : MakeNewHandler(field.FieldType);
            CurrentMultiField = field;
        }

        private ChartmakerMultiHandler MakeNewHandler(Type type)
        {
        
            if (type ==  typeof(bool))
                return new ChartmakerMultiHandlerBoolean();
        
            if (type == typeof(BeatPosition))
                return new ChartmakerMultiHandlerBeatPosition();
        
            if (type == typeof(float))
                return new ChartmakerMultiHandlerFloat();
        
            if (type == typeof(Vector2))
                return new ChartmakerMultiHandlerVector2();
        
            if (type == typeof(Vector3))
                return new ChartmakerMultiHandlerVector3();
        
            return Activator.CreateInstance(typeof(ChartmakerMultiHandler<>).MakeGenericType(type)) as ChartmakerMultiHandler;
        }

        public void GoBack()
        {
            if (CurrentTimestamp.Count > 0) 
                SetObject(CurrentObject);
        
            if (CurrentObject is LaneStyle or HitStyle)
                SetObject(Chartmaker.main.CurrentChart?.Palette);
            else 
                SetObject(null);
        }

        public bool IsSelected(object obj)
        {
            return obj is Timestamp ts 
                ? CurrentTimestamp?.Contains(ts) == true : CurrentObject is IList list 
                    ? list.Contains(obj) : CurrentObject == obj;
        }

        T SpawnForm<T>(string title = "") where T : FormEntry
            => Formmaker.main.Spawn<T>(FormHolder, title);

        T SpawnForm<T, U>(string title, Func<U> get, Action<U> set) where T : FormEntry<U>
            => Formmaker.main.Spawn<T, U>(FormHolder, title, get, set);

        void SetEase2(FormEntryEasing easeX, FormEntryEasing easeY)
        {
            var button = Instantiate(EaseCopyToRightButtonSample, easeX.transform);
            button.onClick.AddListener(() => {
                easeY.SetValue(easeX.CurrentValue);
                easeY.Reset();
            });

            easeX.TitleLabel.gameObject.SetActive(false);
            easeY.TitleLabel.gameObject.SetActive(false);
        
            easeY.ValueLabel.transform.parent.SetParent(easeX.transform);
        
            easeY.GetComponent<LayoutElement>().minHeight = 0;
            easeY.GetComponent<LayoutElement>().ignoreLayout = true;
        
            easeY.transform.SetAsFirstSibling();

            var layoutGroup = easeX.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.padding.left = 10;
        }

        public void OpenExtraModesMenu()
        {
            ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                new ContextMenuListAction("Debug Stats", () => SetMode(InspectorMode.DebugStats), _checked: !IsCollapsed && CurrentMode == InspectorMode.DebugStats)
            ), (RectTransform)ExtraModesButton.transform, ContextMenuDirection.Left); 
        }

        public string GetNewGroupName(string name, LaneGroup exclude = null) 
        {
            int index = 0;
            name = name.Trim();
            Match match = Regex.Match(name, @"^(.*) (\d+)$");
            if (match is { Success: true })
            {
                name = match.Groups[1].Value;
                index = int.Parse(match.Groups[2].Value);
            }

            string newName() => index > 0 ? name + " " + index : name;

            int foundIndex = 0;
            while (
                (foundIndex = Chartmaker.main.CurrentChart.Groups.FindIndex(x => x.Name == newName())) >= 0
                && (foundIndex < 0 || Chartmaker.main.CurrentChart.Groups[foundIndex] != exclude)
            ) index++;
            return newName();
        }
    

        public void OnResizerDrag()
        {
            ResizeInspector(Screen.width - Input.mousePosition.x, false);
        }
        public void OnResizerEndDrag()
        {
            ResizeInspector(Screen.width - Input.mousePosition.x);
        }
    
        public void ResizeInspector(float width, bool snap = true)
        {
            if (snap)
                width = width < 172 
                    ? 27 : 320;
            else
                width = Mathf.Clamp(width, 27, 320);

            PanelHolder.anchoredPosition = new(320 - width, PanelHolder.anchoredPosition.y);
        
            Chartmaker.main.PlayerViewHolder.sizeDelta = new(
                -Chartmaker.main.HierarchyHolder.sizeDelta.x - Chartmaker.main.HierarchyHolder.anchoredPosition.x - width, 
                Chartmaker.main.PlayerViewHolder.sizeDelta.y
            );

            PlayerView.main.Update();
            PlayerView.main.UpdateObjects();

            if (snap) 
            {
                IsCollapsed = width < 172;
                UpdateButtons();
            }
        }
    
        public void Collapse()
        {
            ResizeInspector(27, true);
            Collapser.gameObject.SetActive(false);
        }
    
        public void Restore()
        {
            ResizeInspector(320, true);
            Collapser.gameObject.SetActive(true);
            PlayerView.main.IsMaximised = false;
        }
    }

    public enum InspectorMode
    {
        Properties,
        Statistics,
        DebugStats,
    }
}