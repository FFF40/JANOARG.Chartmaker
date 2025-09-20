using System.Collections.Generic;
using JANOARG.Chartmaker.UI.Modal;
using JANOARG.Chartmaker.UI.Modal.ModalTypes;
using JANOARG.Chartmaker.UI.Tooltip;
using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class PickerPanel : MonoBehaviour
    {
        public static PickerPanel main;

        public TimelinePickerMode CurrentTimelinePickerMode;
        public List<Button>       HierarchyButtons;
        public List<Button>       TimelineButtons;
    
        public GameObject HierarchySongItems;
        public GameObject HierarchyChartItems;
    
        // Funni thing
        public GameObject hmmm;
        public GameObject hmmm2;

        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            for (int a = 0; a < TimelineButtons.Count; a++)
            {
                TimelinePickerMode mode = (TimelinePickerMode)a;
                TimelineButtons[a].onClick.AddListener(() => SetTimelinePickerMode(mode));
            
                TooltipTarget tooltip = TimelineButtons[a].gameObject.AddComponent<TooltipTarget>();
                tooltip.Text = TimelineButtons[a].name;
                tooltip.PositionMode = TooltipPositionMode.Right;
            }
        
            for (int a = 0; a < HierarchyButtons.Count; a++)
            {
                HierarchyPickerItem mode = (HierarchyPickerItem)a;
                HierarchyButtons[a].onClick.AddListener(() => ClickHierarchyPickerItem(mode));
          
                TooltipTarget tooltip = HierarchyButtons[a].gameObject.AddComponent<TooltipTarget>();
                tooltip.Text = HierarchyButtons[a].name;
                tooltip.PositionMode = TooltipPositionMode.Right;
            }
        }

        public void SetTabMode(int mode) => SetTimelinePickerMode((TimelinePickerMode)mode);

        public void SetTimelinePickerMode(TimelinePickerMode mode)
        {
            CurrentTimelinePickerMode = mode;
            UpdateButtons();
        }

        public void ClickHierarchyPickerItem(HierarchyPickerItem item) 
        {
            Chart chart = Chartmaker.main.CurrentChart;
            PlayableSong song = Chartmaker.main.CurrentSong;
        
            switch (item)
            {
                case HierarchyPickerItem.CoverLayer:
                    ModalHolder.main.Spawn<NewCoverLayerModal>(); break;
            
                case HierarchyPickerItem.LaneStyle:
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
                    break;
                }
                case HierarchyPickerItem.HitStyle:
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
                    break;
                }
                case HierarchyPickerItem.Lane:
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
                    break;
                }
                case HierarchyPickerItem.LaneGroup:
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

                    break;
                }
            }
        }
    

        public void UpdateButtons()
        {
            TimelineMode tMode = TimelinePanel.main.CurrentMode;
            TimelineButtons[3].gameObject.SetActive(tMode == TimelineMode.Storyboard);
            TimelineButtons[4].gameObject.SetActive(tMode == TimelineMode.Timing);
            TimelineButtons[5].gameObject.SetActive(tMode == TimelineMode.Lanes);
            TimelineButtons[6].gameObject.SetActive(tMode == TimelineMode.LaneSteps);
            TimelineButtons[7].gameObject.SetActive(tMode == TimelineMode.HitObjects);
            TimelineButtons[8].gameObject.SetActive(tMode == TimelineMode.HitObjects);

            HierarchyMode hMode = HierarchyPanel.main.CurrentMode;
            HierarchySongItems.gameObject.SetActive(hMode == HierarchyMode.PlayableSong);
            HierarchyChartItems.gameObject.SetActive(hMode == HierarchyMode.Chart);

            bool isTimelinePickable = false;
        
            for (int a = 0; a < TimelineButtons.Count; a++)
            {
                TimelineButtons[a].interactable = CurrentTimelinePickerMode != (TimelinePickerMode)a;
                if (TimelineButtons[a].gameObject.activeSelf && !TimelineButtons[a].interactable) 
                    isTimelinePickable = true;
            }

            if (!isTimelinePickable) 
                SetTimelinePickerMode(TimelinePickerMode.Cursor);

            bool hmm = Random.value < 0.005;
            hmmm.SetActive(hmm);
            hmmm2.SetActive(hmm);
        }

        public void DoTheFunnyThing()
        {
            Application.OpenURL("https://file.garden/X9Xrm_GIBmpbTDCZ/omnicharting");
            hmmm.SetActive(false);
            hmmm2.SetActive(false);
        }
    }

    public enum TimelinePickerMode
    {
        Cursor, Select, Delete,
        Timestamp, BPMStop, Lane, LaneStep, NormalHit, CatchHit
    }

    public enum HierarchyPickerItem
    {
        CoverLayer,
        Lane, LaneGroup, LaneStyle, HitStyle
    }
}