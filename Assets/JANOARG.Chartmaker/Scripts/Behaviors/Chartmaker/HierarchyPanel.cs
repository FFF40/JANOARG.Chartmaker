using System;
using System.Collections;
using System.Collections.Generic;
using JANOARG.Chartmaker.Data.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker.Actions;
using JANOARG.Chartmaker.UI;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Cursor;
using JANOARG.Chartmaker.UI.Modal;
using JANOARG.Chartmaker.UI.Modal.ModalTypes;
using JANOARG.Chartmaker.UI.NativeUI;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class HierarchyPanel : MonoBehaviour
    {
        public static HierarchyPanel main;

        [Space]
        public bool IsCollapsed;
        public HierarchyMode CurrentMode;
        public RectTransform PanelHolder;
        [Space]
        public Button HierarchyTabButton;
        public Button CollapserButton;
        [Space]
        public Sprite[] Icons;
        [Space]
        public HierarchyItemHolder HolderSample;
        public RectTransform             HolderParent;
        public CanvasGroup               HolderGroup;
        public LayoutGroup               HolderLayoutGroup;
        List<HierarchyItem>              Items   = new();
        public List<HierarchyItemHolder> Holders = new();
        [Space]
        public RectTransform DragIntoIndicator;
        public RectTransform DragBetweenIndicator;
        [Space]
        public TMP_InputField SearchField;
        public Button SearchButton;
        public Image  SearchIcon;
        public Image  SearchClearIcon;

        Dictionary<string, HierarchyItem> GroupItems = new();

        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            DragIntoIndicator.gameObject.SetActive(false);
            DragBetweenIndicator.gameObject.SetActive(false);
        }

        public void SetMode(HierarchyMode mode)
        {
            if (mode == HierarchyMode.Chart && Chartmaker.main.CurrentChart == null) return;
            CurrentMode = mode;
            InspectorPanel.main.SetObject(null);
            InformationBar.main.UpdateButtonActivity();
            PlayerView.main.UpdateObjects();
            InitHierarchy();
        }

        public void InitHierarchy() 
        {
            Items.Clear();
            foreach (var item in Holders) Destroy(item.gameObject);
            Holders.Clear();
            GroupItems.Clear();

            switch (CurrentMode)
            {
                case HierarchyMode.PlayableSong:
                {
                    PlayableSong song = Chartmaker.main.CurrentSong;
                    HierarchyItem songItem;
                    Items.Add(songItem = new () 
                    {
                        Name = "Playable Song",
                        Type = HierarchyItemType.PlayableSong,
                        Target = song,
                        Expanded = true,
                    });
            
                    HierarchyItem coverItem;
                    songItem.Children.Add(coverItem = new HierarchyItem 
                    {
                        Name = "Cover",
                        Type = HierarchyItemType.Cover,
                        Target = song.Cover,
                    });

                    break;
                }
            
                case HierarchyMode.Chart:
                {
                    if (Chartmaker.main.CurrentChart != null) 
                    {
                        Chart chart = Chartmaker.main.CurrentChart;
                        HierarchyItem chartItem;
                        Items.Add(chartItem = new () 
                        {
                            Name = "Chart",
                            Type = HierarchyItemType.Chart,
                            Target = chart,
                            Expanded = true,
                        });

                        chartItem.Children.Add(new HierarchyItem 
                        {
                            Name = "Camera",
                            Type = HierarchyItemType.Camera,
                            Target = chart.Camera
                        });

                        HierarchyItem paletteItem;
                        chartItem.Children.Add(paletteItem = new () 
                        {
                            Name = "Palette",
                            Type = HierarchyItemType.Palette,
                            Target = chart.Palette
                        });

                        HierarchyItem worldItem;
                        chartItem.Children.Add(worldItem = new ()
                        {
                            Name = "World",
                            Type = HierarchyItemType.World,
                        });
                    }

                    break;
                }
            }
            UpdateHierarchy();
        }

        public void UpdateHierarchy()
        {
            switch (CurrentMode)
            {
                case HierarchyMode.PlayableSong:
                {
                    PlayableSong song = Chartmaker.main.CurrentSong;

                    HierarchyItem coverItem = Items[0].Children[0];
                    coverItem.Children.Clear();
        
                    foreach (CoverLayer layer in song.Cover.Layers)
                    {
                        HierarchyItem item = new ()
                        {
                            Name = layer.Target,
                            Type = HierarchyItemType.CoverLayer,
                            Target = layer,
                        };

                        coverItem.Children.Add(item);
                    }

                    break;
                }
            
                case HierarchyMode.Chart:
                {
                    Chart chart = Chartmaker.main.CurrentChart;

                    if (chart != null)
                    {
                        HierarchyItem paletteItem = Items[0].Children[1];
                        paletteItem.Children.Clear();
                    
                        int index = 0;
                    
                        foreach (LaneStyle style in chart.Palette.LaneStyles)
                        {
                            HierarchyItem item = new ()
                            {
                                Name = string.IsNullOrWhiteSpace(style.Name) ? ("Lane Style " + index) : style.Name,
                                Type = HierarchyItemType.LaneStyle,
                                Target = style,
                            };
                            paletteItem.Children.Add(item);
                            index++;
                        }
                    
                        index = 0;
                        foreach (HitStyle style in chart.Palette.HitStyles)
                        {
                            HierarchyItem item = new () {
                                Name = string.IsNullOrWhiteSpace(style.Name) ? ("Hit Style " + index) : style.Name,
                                Type = HierarchyItemType.HitStyle,
                                Target = style,
                            };
                            paletteItem.Children.Add(item);
                            index++;
                        }

                        HierarchyItem worldItem = Items[0].Children[2];
                        worldItem.Children.Clear();

                        // Add lane groups
                        Dictionary<string, HierarchyItem> newGroupItems = new ();
                        foreach (LaneGroup group in chart.Groups)
                        {
                            HierarchyItem item = new () 
                            {
                                Name = group.Name,
                                Type = HierarchyItemType.LaneGroup,
                                Target = group,
                                Expanded = GroupItems.ContainsKey(group.Name) ? GroupItems[group.Name].Expanded : false,
                            };
                            newGroupItems[group.Name] = item;
                        }
                    
                        GroupItems = newGroupItems;
                        Dictionary<string, HierarchyItem>.KeyCollection keys = GroupItems.Keys;
                        int keyindex = 0;
                    
                        foreach (string key in keys)
                        {
                            LaneGroup data = (LaneGroup)GroupItems[key].Target;
                            if (!string.IsNullOrEmpty(data.Group) && GroupItems.ContainsKey(data.Group)) GroupItems[data.Group].Children.Add(GroupItems[key]);
                            else worldItem.Children.Add(GroupItems[key]);
                            keyindex++;
                        }
                    
                        // Add lanes
                        foreach (Lane lane in chart.Lanes)
                        {
                            HierarchyItem item = new () {
                                Name = string.IsNullOrWhiteSpace(lane.Name) ? "Lane" : lane.Name,
                                Subname = lane.LaneSteps[0].Offset + "~" + lane.LaneSteps[^1].Offset,
                                Type = HierarchyItemType.Lane,
                                Target = lane,
                            };

                            if (!string.IsNullOrEmpty(lane.Group) && GroupItems.ContainsKey(lane.Group))
                                GroupItems[lane.Group].Children.Add(item);
                            else 
                                worldItem.Children.Add(item);
                        }
                    }
                    break;
                }
            }

            UpdateHolders();
        }

        public void UpdateHolders () 
        {
            int count = 0;

            if (string.IsNullOrWhiteSpace(SearchField.text)) 
            {
                void AddHolder(HierarchyItem item, int indent)
                {
                    if (!HierarchyFiltersPanel.main.GetVisibility(item.Type, HierarchyContext.Hierarchy))
                        return;

                    HierarchyItemHolder holder;
                    
                    if (count >= Holders.Count) 
                    {
                        holder = Instantiate(HolderSample, HolderParent);
                        Holders.Add(holder);
                    }
                    else 
                        holder = Holders[count];
                    
                    count++;

                    holder.SetItem(item, indent);
                    holder.Icon.sprite = Icons[(int)item.Type];
                    holder.ExpandButton.gameObject.SetActive(item.Children.Count > 0);

                    if (item.Expanded)
                        foreach (var child in item.Children) 
                            AddHolder(child, indent + 1);
                }

                foreach (var item in Items) 
                    AddHolder(item, 0);
            }
            else 
            {
                void AddHolder(HierarchyItem item) 
                {
                    if (item.Name.ContainsInsensitive(SearchField.text) 
                        && HierarchyFiltersPanel.main.GetVisibility(item.Type, HierarchyContext.SearchResult)
                       )
                    {
                        HierarchyItemHolder holder;
                        if (count >= Holders.Count) 
                        {
                            holder = Instantiate(HolderSample, HolderParent);
                            Holders.Add(holder);
                        }
                        else
                            holder = Holders[count];
                        
                        count++;

                        holder.SetItem(item, 0);
                        holder.Icon.sprite = Icons[(int)item.Type];
                        holder.ExpandButton.gameObject.SetActive(false);
                    }

                    foreach (var child in item.Children) AddHolder(child);
                }

                foreach (var item in Items)
                    AddHolder(item);
            }

            while (count < Holders.Count) 
            {
                Destroy(Holders[count].gameObject);
                Holders.RemoveAt(count);
            }

            UpdateHolderSelection();
        }

        public void UpdateHolderSelection()
        {
            foreach (var holder in Holders)
            {
                bool hasTarget       = holder.Target.Target != null;
                bool isInList        = InspectorPanel.main.CurrentHierarchyObject is IList list && list.Contains(holder.Target.Target);
                bool isDirectMatch   = holder.Target.Target == InspectorPanel.main.CurrentHierarchyObject;

                holder.SelectedBackground.SetActive(hasTarget && (isInList || isDirectMatch));;
            }
        }

        private float         lastClickTime        = 0f;
        private float         doubleClickThreshold = 0.3f; // 300ms threshold
        private HierarchyItem lastClickedItem      = null;

        public void Select(HierarchyItem item) 
        {
            if (isDragging) return;

            float timeSinceLastClick = Time.unscaledTime - lastClickTime;
    
            // Check if it's a double-click on the same item
            if (timeSinceLastClick <= doubleClickThreshold && lastClickedItem == item)
            {
                OnDoubleClick(item);
                lastClickTime = 0f; // Reset to prevent triple-click registering as another double-click
                lastClickedItem = null;
            }
            else
            {
                // Single click
                if (item.Target != null) InspectorPanel.main.SetObject(item.Target);
        
                lastClickTime = Time.unscaledTime;
                lastClickedItem = item;
            }
        }

        private void OnDoubleClick(HierarchyItem item)
        {
            UnityEngine.Debug.Log("Double Click " + item.Target);

            float? targetTime = null;
            object targetObject = null;

            switch (item.Target)
            {
                case Lane lane when lane.LaneSteps == null || lane.LaneSteps.Count == 0:
                    UnityEngine.Debug.LogWarning($"Lane {lane.Name} has no steps to seek to");
                    return;
                
                case Lane lane:
                    targetTime = Chartmaker.main.CurrentSong.Timing.ToSeconds(lane.LaneSteps[0].Offset);
                    targetObject = item.Target;

                    break;
                
                case LaneGroup group:
                {
                    Lane[] lanes = Chartmaker.main.CurrentChart.Lanes.FindAll(qLane => qLane.Group == group.Name).ToArray();
                    float lowestSec = float.MaxValue;
                    Lane candidateLane = null;
                
                    foreach (Lane childLane in lanes)
                    {
                        if (childLane.LaneSteps == null || childLane.LaneSteps.Count == 0)
                            continue;
                        float head = Chartmaker.main.CurrentSong.Timing.ToSeconds(childLane.LaneSteps[0].Offset);

                        if (!(head < lowestSec)) 
                            continue;

                        lowestSec = head;
                        candidateLane = childLane;
                    }
                
                    if (candidateLane == null)
                    {
                        UnityEngine.Debug.LogWarning($"LaneGroup {group.Name} has no valid lanes to seek to");
                        return;
                    }
                
                    targetTime = lowestSec;
                    targetObject = candidateLane;

                    break;
                }
            }

            // Common seeking logic
            if (targetTime.HasValue)
            {
                SeekToTime(targetTime.Value, targetObject);
            }
        }

        private void SeekToTime(float time, object inspectorTarget)
        {
            Chartmaker.main.SongSource.time = Mathf.Clamp(time, 0, Chartmaker.main.SongSource.clip.length);
    
            if (Chartmaker.main.SongSource.time == 0 && !Chartmaker.main.SongSource.isPlaying)
            {
                Chartmaker.main.SongSource.Play();
                Chartmaker.main.SongSource.Pause();
            }

            InspectorPanel.main.SetObject(inspectorTarget);
    
            float viewWidth = TimelinePanel.main.PeekRange.y - TimelinePanel.main.PeekRange.x;
            float newStart = time - (viewWidth / 2f);
            float offset = newStart - TimelinePanel.main.PeekRange.x;
            offset = Mathf.Clamp(
                offset,
                TimelinePanel.main.PeekLimit.x - TimelinePanel.main.PeekRange.x,
                TimelinePanel.main.PeekLimit.y - TimelinePanel.main.PeekRange.y
            );

            TimelinePanel.main.PeekRange.x += offset;
            TimelinePanel.main.PeekRange.y += offset;
            TimelinePanel.main.UpdateTimeline(true);
    
            // Force update on next frame
            StartCoroutine(UpdatePlayerViewNextFrame());
    
            UnityEngine.Debug.Log($"Seeked to {time:F2}s");
        }

        private IEnumerator UpdatePlayerViewNextFrame()
        {
            yield return null; // Wait one frame
            PlayerView.main.Update();
        }
        
        public void SelectAdjacent(int direction)
        {
            if (isDragging) return;

            int i = Holders.FindIndex(x => x.SelectedBackground.activeSelf);
            if (i < 0) return;
            var a = Holders[i];
            for (i += direction; i >= 0 && i < Holders.Count; i += direction)
            {
                if (Holders[i].Target.Type == a.Target.Type) 
                {
                    InspectorPanel.main.SetObject(Holders[i].Target.Target);
                    break;
                }
            }
        }

        public void RightClickSelect(HierarchyItem item, HierarchyItemHolder holder) 
        {
            static string KeyOf(string id) => KeyboardHandler.main.Keybindings[id].Keybind.ToString();

            UnityEngine.Debug.Log("Right Click Select " + item.Target + " " + holder.Target);;;
            
            ContextMenuListSublist addHierarchyList = new ContextMenuListSublist("New", GetItems(item.Target));
            
            InspectorPanel.main.SetObject(item.Target);
            ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                addHierarchyList,
                new ContextMenuListAction("Cut", Chartmaker.main.Cut, KeyOf("ED:Cut"), 
                    icon: "Cut", _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListAction("Copy", Chartmaker.main.Copy, KeyOf("ED:Copy"), 
                    icon: "Copy", _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListAction("Paste <i>" + (Chartmaker.main.CanPaste() ? Chartmaker.GetItemName(Chartmaker.main.ClipboardItem) : ""), Chartmaker.main.Paste, KeyOf("ED:Paste"), 
                    icon: "Paste", _enabled: Chartmaker.main.CanPaste()),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Rename", () => Rename(holder), KeyOf("ED:Rename"),
                    _enabled: Chartmaker.main.CanRename()),
                new ContextMenuListAction("Delete", () => KeyboardHandler.main.Keybindings["ED:Delete"].Invoke(), KeyOf("ED:Delete"), 
                    _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Expand Recursively", () => {ExpandRecursively(item); UpdateHolders(); },
                    _enabled: item.Children.Count > 0)
            ), (RectTransform)holder.transform, ContextMenuDirection.Cursor);

            ContextMenuListItem[] GetItems(object itemType)
            {
                Chart chart = Chartmaker.main.CurrentChart;
                PlayableSong song = Chartmaker.main.CurrentSong;
                switch (itemType)
                {
                    case LaneStyle:
                    case HitStyle:
                    case Palette:
                        return new ContextMenuListItem[]
                        {
                            new ContextMenuListAction("Lane Style", () =>
                            {
                                LaneStyle target;

                                if (chart.Palette.LaneStyles.Count > 0)
                                    target = chart.Palette.LaneStyles[0];
                                else
                                    target = new LaneStyle()
                                    {
                                        LaneColor = song.InterfaceColor * new Color(1, 1, 1, .35f),
                                        JudgeColor = song.InterfaceColor,
                                    };

                                target = InspectorPanel.main.CurrentObject switch
                                {
                                    LaneStyle laneStyle => laneStyle,
                                    _ => target
                                };

                                Chartmaker.main.AddItem(target.DeepClone());
                            }),
                            
                            new ContextMenuListAction("Hit Style", () =>
                            {
                                HitStyle target;

                                if (chart.Palette.HitStyles.Count > 0)
                                    target = chart.Palette.HitStyles[0];
                                else
                                    target = new HitStyle()
                                    {
                                        NormalColor = song.InterfaceColor,
                                        CatchColor = Color.Lerp(song.InterfaceColor, song.BackgroundColor, .35f),
                                        HoldTailColor = song.InterfaceColor * new Color(1, 1, 1, .35f),
                                    };

                                target = InspectorPanel.main.CurrentObject switch
                                {
                                    HitStyle hitStyle => hitStyle,
                                    _ => target
                                };

                                Chartmaker.main.AddItem(target.DeepClone());
                            }),
                        };
                    
                    case PlayableSong:
                    case Cover:
                        return new ContextMenuListItem[]
                        {
                            new ContextMenuListAction("Cover Layer", () => ModalHolder.main.Spawn<NewCoverLayerModal>()),
                        };
                    
                    case HierarchyItemType.World:
                    case Lane:
                    case LaneGroup:
                        return new ContextMenuListItem[]
                        {
                            new ContextMenuListAction("Lane", () =>
                            {
                                string group = InspectorPanel.main.CurrentObject switch
                                {
                                    Lane laneCurrentObject => laneCurrentObject.Group,
                                    LaneGroup laneGroupCurrentObject => laneGroupCurrentObject.Group,
                                    _ => ""
                                };

                                Lane lane = new Lane
                                {
                                    Position = new(0, -4, 0),
                                    Group = group,
                                };

                                lane.LaneSteps.Add(new LaneStep
                                {
                                    StartPointPosition = new(-8, 0),
                                    EndPointPosition = new(8, 0),
                                    Offset = (BeatPosition)InformationBar.main.beat
                                });

                                lane.LaneSteps.Add(new LaneStep
                                {
                                    StartPointPosition = new(-8, 0),
                                    EndPointPosition = new(8, 0),
                                    Offset = (BeatPosition)(InformationBar.main.beat + 1),
                                });

                                Chartmaker.main.AddItem(lane);
                            }),
                            new ContextMenuListAction("Lane Group", () =>
                            {
                                string parent = InspectorPanel.main.CurrentObject switch
                                {
                                    Lane laneCurrentObject => laneCurrentObject.Group,
                                    LaneGroup laneGroupCurrentObject => laneGroupCurrentObject.Group,
                                    _ => ""
                                };

                                LaneGroup group = new LaneGroup {
                                    Group = parent,
                                    Name = InspectorPanel.main.GetNewGroupName("Group 1"),
                                };
                                Chartmaker.main.AddItem(group);
                            }),
                        };
                    
                    case HierarchyItemType.Chart:
                    case HierarchyItemType.Camera:
                    default:
                        return new ContextMenuListItem[]
                        {
                            new ContextMenuListAction("None...", () => { }, _enabled:false)
                        };
                }

                return null;
            }
        }

        public void RenameCurrent() 
        {
            HierarchyItemHolder holder = Holders.Find(x => x.SelectedBackground.activeSelf);
            if (holder) holder.Rename();
        }

        public void Rename(HierarchyItemHolder holder) 
        {
            holder.Rename();
        }

        public void ExpandRecursively(HierarchyItem item) 
        {
            item.Expanded = true;
            foreach (HierarchyItem child in item.Children) if (child.Children.Count > 0) ExpandRecursively(child);
        }


        public void OnSearchFieldUpdate() 
        {
            UpdateHolders();
            bool active = !string.IsNullOrEmpty(SearchField.text);
            SearchButton.interactable = active;
            SearchIcon.gameObject.SetActive(!active);
            SearchClearIcon.gameObject.SetActive(active);
        }

        public void ClearSearch() 
        {
            SearchField.text = "";
        }

        public void ToggleExpand(HierarchyItem item) 
        {
            item.Expanded = !item.Expanded;
            UpdateHolders();
            if (TimelinePanel.main.LaneFilterMode == LaneFilterMode.HierarchyVisible) TimelinePanel.main.UpdateItems();
        }
    
        public void OnResizerDrag()
        {
            ResizeHierarchy(Input.mousePosition.x, false);
        }
        public void OnResizerEndDrag()
        {
            ResizeHierarchy(Input.mousePosition.x);
        }
    
        public void ResizeHierarchy(float width, bool snap = true)
        {
            if (snap) width = width < 168 ? 69 : 268;
            else width = Mathf.Clamp(Mathf.Floor(width), 69, 268);

            PanelHolder.anchoredPosition = new(width - 226, PanelHolder.anchoredPosition.y);
            Chartmaker.main.PlayerViewHolder.anchoredPosition = new(
                width, 
                Chartmaker.main.PlayerViewHolder.anchoredPosition.y
            );
            Chartmaker.main.PlayerViewHolder.sizeDelta = new(
                Chartmaker.main.InspectorHolder.anchoredPosition.x - Chartmaker.main.InspectorHolder.sizeDelta.x - width, 
                Chartmaker.main.PlayerViewHolder.sizeDelta.y
            );

            PlayerView.main.Update();
            PlayerView.main.UpdateObjects();

            if (snap)
            {
                IsCollapsed = width < 168;
                HierarchyTabButton.interactable = IsCollapsed;
                CollapserButton.gameObject.SetActive(!IsCollapsed);
            }
        }
    
        public void Collapse()
        {
            ResizeHierarchy(0, true);
        }
    
        public void Restore()
        {
            ResizeHierarchy(240, true);
        }

        // -------------------------------------------------- Dragging

        bool isDragging = false;

        bool                isDragInto;
        HierarchyItemHolder item1 = null, item2 = null;

        bool CanDrag(HierarchyItemHolder item) 
        {
            return item.Target.Type is HierarchyItemType.CoverLayer or HierarchyItemType.LaneGroup or HierarchyItemType.Lane;
        }

        bool CanDragInto(HierarchyItemHolder item, HierarchyItemHolder target) 
        {
            return item != target 
                   && (target.Target.Type is HierarchyItemType.LaneGroup && item.Target.Type is HierarchyItemType.Lane or HierarchyItemType.LaneGroup);
        }

        bool CanDragBetween(HierarchyItemHolder item, HierarchyItemHolder before, HierarchyItemHolder after) 
        {
            return (item.Target.Type is HierarchyItemType.Lane && (before.Target.Type is HierarchyItemType.World or HierarchyItemType.Lane || after.Target.Type is HierarchyItemType.Lane))
                   || (item.Target.Type is HierarchyItemType.LaneGroup && (before.Target.Type is HierarchyItemType.World or HierarchyItemType.LaneGroup));
        }

        bool IsLaneGroupDraggingIntoSelf(LaneGroup dragging, string target)
        {
            if (dragging.Name == target) return true;
            while (!String.IsNullOrEmpty(target))
            {
                LaneGroup group = PlayerView.main.Manager.Groups[target].CurrentGroup;
                if (group.Group == dragging.Name) return true;
                if (group.Group == dragging.Group) return false;
                target = group.Group;
            }
            return false;
        }

        IChartmakerAction GetDragAction(HierarchyItemHolder item)
        {
            if (isDragInto)
            {
                if (item.Target.Target is Lane lane)
                {
                    List<Lane> list = Chartmaker.main.CurrentChart.Lanes;
                    int index = list.IndexOf(lane);

                    ChartmakerArrangeLaneAction rearrangeLaneAction(Lane adjacent, string group) => new () {
                        Target = lane,
                        BeforeAdjacent = index > 0 ? list[index - 1] : null,
                        BeforeGroup = lane.Group,
                        AfterAdjacent = adjacent,
                        AfterGroup = group
                    };

                    if (item1.Target.Target is LaneGroup group1)
                    {
                        return rearrangeLaneAction(list[^1], group1.Name);
                    }
                }
                else if (item.Target.Target is LaneGroup group) 
                {
                    List<LaneGroup> list = Chartmaker.main.CurrentChart.Groups;
                    int index = list.IndexOf(group);

                    ChartmakerArrangeLaneGroupAction rearrangeGroupAction(LaneGroup adjacent, string parent) => new () {
                        Target = group,
                        BeforeAdjacent = index > 0 ? list[index - 1] : null,
                        BeforeGroup = group.Group,
                        AfterAdjacent = adjacent,
                        AfterGroup = parent
                    };


                    if (item1.Target.Target is LaneGroup group1)
                    {
                        if (IsLaneGroupDraggingIntoSelf(group, group1.Group)) return null;
                        return rearrangeGroupAction(list[^1], group1.Name);
                    }
                }
            }
            else
            {
                if (item1 == item || item2 == item) return null;

                if (item.Target.Target is Lane lane)
                {
                    List<Lane> list = Chartmaker.main.CurrentChart.Lanes;
                    int index = list.IndexOf(lane);

                    ChartmakerArrangeLaneAction rearrangeLaneAction(Lane adjacent, string group) => new () {
                        Target = lane,
                        BeforeAdjacent = index > 0 ? list[index - 1] : null,
                        BeforeGroup = lane.Group,
                        AfterAdjacent = adjacent,
                        AfterGroup = group
                    };

                    if (item1.Target.Type == HierarchyItemType.World)
                    {
                        return rearrangeLaneAction(list[^1], "");
                    }
                    if (item1.Target.Target is Lane lane1)
                    {
                        return rearrangeLaneAction(lane1, lane1.Group);
                    }
                    if (item1.Target.Target is LaneGroup group1)
                    {
                        if (item1.Target.Children.Count > 0 && item1.Target.Expanded)
                            return rearrangeLaneAction(null, group1.Name);
                        else 
                            return rearrangeLaneAction(null, group1.Group);
                    }
                }
                else if (item.Target.Target is LaneGroup group) 
                {
                    List<LaneGroup> list = Chartmaker.main.CurrentChart.Groups;
                    int index = list.IndexOf(group);

                    ChartmakerArrangeLaneGroupAction rearrangeGroupAction(LaneGroup adjacent, string parent) => 
                        IsLaneGroupDraggingIntoSelf(group, parent) ? null : new () {
                            Target = group,
                            BeforeAdjacent = index > 0 ? list[index - 1] : null,
                            BeforeGroup = group.Group,
                            AfterAdjacent = adjacent,
                            AfterGroup = parent
                        };


                    if (item1.Target.Type == HierarchyItemType.World)
                    {
                        return rearrangeGroupAction(list[^1], "");
                    }
                    if (item1.Target.Target is LaneGroup group1)
                    {
                        if (item1.Target.Children.Count > 0 && item1.Target.Expanded)
                            return rearrangeGroupAction(null, group1.Name);
                        else 
                            return rearrangeGroupAction(group1, group1.Group);
                    }
                }
            }

            return null;
        }

        public void OnItemBeginDrag(HierarchyItemHolder item, PointerEventData eventData)
        {
            foreach (var i in Holders) i.SelectedBackground.SetActive(i == item);
            HolderGroup.blocksRaycasts = false;
            isDragging = true;
        }

        public void OnItemDrag(HierarchyItemHolder item, PointerEventData eventData)
        {
            if (CanDrag(item))
            {
                DragIntoIndicator.gameObject.SetActive(false);
                DragBetweenIndicator.gameObject.SetActive(false);
                item1 = null; item2 = null;

                float itemHeight = ((RectTransform)Holders[0].transform).rect.height;
                RectOffset padding = HolderLayoutGroup.padding;
                Vector3[] corners = new Vector3[4];
                HolderParent.GetWorldCorners(corners);
                float pos = (corners[2].y - eventData.position.y - padding.top) / itemHeight;
                pos = Mathf.Clamp(pos, 0, Holders.Count) - 0.5f;

                int intPos = Mathf.RoundToInt(pos);
                float snapDist = Math.Abs(pos - Mathf.Round(pos));
                var intItem = intPos > Holders.Count - 1 ? null : Holders[intPos];
                bool canDragInto = intItem && CanDragInto(item, intItem);

                float snapDistThres = 0.25f;
            
                dragInto:
                if (snapDist < snapDistThres && canDragInto) 
                {
                    isDragInto = true; 
                    item1 = intItem; 
                    item2 = null;
                
                    DragIntoIndicator.gameObject.SetActive(true);
                    DragIntoIndicator.anchoredPosition = new Vector2(
                        intItem.IndentBox.minWidth + padding.left - 24,
                        -intPos * itemHeight - padding.top
                    );
                
                    UpdateCursor(CursorType.Grabbing);
                }
                else 
                {
                    int beforePos = Mathf.RoundToInt(pos - 0.5f);
                    HierarchyItemHolder before = beforePos < 0 
                        ? null : Holders[beforePos];
                
                    var afterPos = Mathf.RoundToInt(pos + 0.5f);
                    HierarchyItemHolder after = afterPos > Holders.Count - 1 
                        ? null : Holders[afterPos];

                    if (CanDragBetween(item, before, after)) 
                    {
                        isDragInto = false;
                        item1 = before; 
                        item2 = after;
                    
                        DragBetweenIndicator.gameObject.SetActive(true);
                        DragBetweenIndicator.anchoredPosition = new Vector2(
                            Math.Max(after ? after.IndentBox.minWidth : 0, before ? before.IndentBox.minWidth : 0) + padding.left - 24,
                            -Mathf.Round(pos + 0.5f) * itemHeight - padding.top
                        );
                    
                        UpdateCursor(CursorType.Grabbing);
                    }
                    else if (canDragInto)
                    {
                        snapDistThres = 1;
                    
                        // gaming
                        goto dragInto;
                    }
                    else
                        UpdateCursor(CursorType.GrabbingBlocked);
                }
            }
            else 
                UpdateCursor(CursorType.GrabbingBlocked);
        }

        public void OnItemEndDrag(HierarchyItemHolder item, PointerEventData eventData)
        {
            IChartmakerAction action = GetDragAction(item);
            if (action != null) 
                Chartmaker.main.DoAction(action);

            UpdateHolders();
            UpdateCursor(0);
        
            isDragging = false;
        
            DragIntoIndicator.gameObject.SetActive(false);
            DragBetweenIndicator.gameObject.SetActive(false);
        
            HolderGroup.blocksRaycasts = true;
        }

        CursorType CurrentCursor = 0;

        public void UpdateCursor(CursorType cursor)
        {
            if (CurrentCursor != cursor)
            {
                if (CurrentCursor != 0)
                    CursorChanger.PopCursor();
            
                if (cursor != 0) 
                    CursorChanger.PushCursor(cursor);
            
                CurrentCursor = cursor;
            
                BorderlessWindow.UpdateCursor();
            }
        }
    }

    public enum HierarchyMode 
    {
        PlayableSong,
        Chart,
    }

    public class HierarchyItem
    {
        public string              Name;
        public string              Subname;
        public HierarchyItemType   Type;
        public object              Target;
        public List<HierarchyItem> Children = new();
    
        public bool Expanded;
    }

    public enum HierarchyContext 
    {
        Hierarchy,
        SearchResult
    }

    public enum HierarchyItemType 
    {
        Chart        = 0,
        Camera       = 1,
        Palette      = 2,
        LaneStyle    = 3,
        HitStyle     = 4,
        World        = 5,
        LaneGroup    = 6,
        Lane         = 7,
        PlayableSong = 8,
        Cover        = 9,
        CoverLayer   = 10,
    }
}