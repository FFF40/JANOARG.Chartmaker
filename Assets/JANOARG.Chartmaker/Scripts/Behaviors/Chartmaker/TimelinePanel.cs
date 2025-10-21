using System;
using System.Collections;
using System.Collections.Generic;
using JANOARG.Chartmaker.Constants;
using JANOARG.Chartmaker.Data.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker.Actions;
using JANOARG.Chartmaker.UI;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Themeable;
using JANOARG.Chartmaker.UI.Timeline;
using JANOARG.Chartmaker.Utils;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using JANOARG.Shared.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class TimelinePanel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public static           TimelinePanel main;
        private static readonly int           OutlineColor = Shader.PropertyToID("_OutlineColor");

        [Header("Data")]
        public TimelineMode CurrentMode;
        [Space]
        public Vector2 PeekRange;
        public Vector2 PeekLimit;
        [Space]
        public int ScrollOffset;
        public int            SeparationFactor;
        public LaneFilterMode LaneFilterMode;
        public float          VerticalScale;
        public float          VerticalOffset;
        public float          ResizeVelocity;

        [Header("Objects")]
        public Button StoryboardTab;
        [HideInInspector] public RectTransform StoryboardTabRect;
        public Button TimingTab;
        [HideInInspector] public RectTransform TimingTabRect;
        public Button LaneTab;
        [HideInInspector] public RectTransform LaneTabRect;
        public Button LaneStepTab;
        [HideInInspector] public RectTransform LaneStepTabRect;
        public Button HitObjectTab;
        [HideInInspector] public RectTransform HitObjectTabRect;
        [Space]
        public RectTransform CollapserTransform;
        public Button Collapser;
        [Space]
        public RectTransform TimeSliderHolder;
        public RectTransform CurrentTimeSlider;
        public RectTransform PeekRangeSlider;
        public RectTransform PeekStartSlider;
        public RectTransform PeekEndSlider;
        public RectTransform SongStartRect;
        public RectTransform SongEndRect;
        public RectTransform LaneStartRect;
        public RectTransform LaneEndRect;
        public RectTransform TicksHolder;
        public RectTransform ItemsHolder;
        public RectTransform TailsHolder;
        public RectTransform LabelsHolder;
        public RectTransform GraphsHolder;
        public RectTransform StoryboardEntryHolder;
        public RectTransform CurrentTimeTick;
        public RectTransform CurrentTimeConnector;
        public RectTransform SelectionRect;
        [Space]
        public RawImage WaveformImage;
        [Space]
        public Scrollbar VerticalScrollbar;
        public GameObject  Blocker;
        public TMP_Text    BlockerLabel;
        public CanvasGroup CurrentTimeCoonectorGroup;
        public CanvasGroup PeekSliderGroup;
        public CanvasGroup BlockerTextGroup;
        [Space]
        public Button UndoButton;
        public Button        RedoButton;
        public RectTransform EditHistoryHolder;
        public CanvasGroup   UndoButtonGroup;
        public CanvasGroup   RedoButtonGroup;
        public TMP_Text      ActionsBehindCounter;
        public TMP_Text      ActionsAheadCounter;
        [Space]
        public Button CutButton;
        public Button      CopyButton;
        public Button      PasteButton;
        public CanvasGroup CutButtonGroup;
        public CanvasGroup CopyButtonGroup;
        public CanvasGroup PasteButtonGroup;
        [Space]
        public GameObject LaneOptionsHolder;
        public GameObject HitObjectOptionsHolder;
        [Space]
        public TimelineOptionsPanel Options;

        [Space]
        public TimelineItem Previewer;
        public TimelineItem PreviewerTail;

        [Header("Samples")]
        public TimelineTick TickSample;
        [HideInInspector]
        public List<TimelineTick> Ticks;

        public TimelineItem ItemSample;
        [HideInInspector]
        public List<TimelineItem> Items;

        public Image ItemTailSample;
        [HideInInspector]
        public List<Image> ItemTails;

        public TMP_Text LabelSample;
        [HideInInspector]
        public List<TMP_Text> Labels;

        public LineGraph GraphSample;
        [HideInInspector]
        public List<LineGraph> Graphs;
    
        public TMP_Text StoryboardEntrySample;
        public Material StoryboardEntryMaterial;
        [HideInInspector]
        public List<TMP_Text> StoryboardEntries;
    
        [Header("Icons")]
        public Sprite LineIcon;
        public Sprite BehindIcon;
        public Sprite NormalHitIcon;
        public Sprite CatchHitIcon;

        public int TimelineHeight { get; private set; } = 8;
        int          TimelineExpandHeight  = 8;
        int          TimelineRestoreHeight = 8;
        int          ItemHeight            = 0;
        bool         lastPlayed;
        public IList DraggingItem;
        float        DraggingItemOffset;

        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            UpdateTabs();
            UpdateScrollbar();
            Options.OnEnable();
        }

        public void UpdatePeekLimit()
        {
            PeekLimit.x = -5;
            PeekLimit.y = Chartmaker.main.CurrentSong.Clip.length + 5;
        }

        public void Update()
        {
            Vector2 limit = new(
                Mathf.Min(PeekRange.x, PeekLimit.x),
                Mathf.Max(PeekRange.y, PeekLimit.y)
            );

            float time = Chartmaker.main.SongSource.time;

            if (isDragged && (int)dragMode % 2 == 1 && dragMode != TimelineDragMode.TimelineDrag)
            {
                float density = (PeekRange.y - PeekRange.x) / TicksHolder.rect.width;
                float offset = 0;

                if (dragEnd.x < 50)
                    offset = -Mathf.Pow(50 - dragEnd.x, 2f) * density;
            
                if (dragEnd.x > TicksHolder.rect.width - 50)
                    offset = Mathf.Pow(dragEnd.x - TicksHolder.rect.width + 50, 2f) * density;
            
                if (offset != 0)
                {
                    offset = Mathf.Clamp(offset * Time.deltaTime, limit.x - PeekRange.x, limit.y - PeekRange.y);
                    PeekRange.x += offset;
                    PeekRange.y += offset;
                    OnDrag(lastDrag);
                }
            } 
            else if (Options.FollowSeekLine && Chartmaker.main.SongSource.isPlaying)
            {
                float mid = (PeekRange.x + PeekRange.y) / 2;
                float offset = Mathf.Clamp(time - mid, limit.x - PeekRange.x, limit.y - PeekRange.y);
           
                PeekRange.x += offset;
                PeekRange.y += offset;
            }

            CurrentTimeSlider.anchorMin = CurrentTimeSlider.anchorMax
                = new(Mathf.InverseLerp(limit.x, limit.y, time), .5f);
            PeekStartSlider.anchorMin = PeekStartSlider.anchorMax = PeekRangeSlider.anchorMin
                = new(Mathf.InverseLerp(limit.x, limit.y, PeekRange.x), .5f);
            PeekEndSlider.anchorMin = PeekEndSlider.anchorMax = PeekRangeSlider.anchorMax
                = new(Mathf.InverseLerp(limit.x, limit.y, PeekRange.y), .5f);
            
            if (Mathf.Approximately(PeekRange.x, PeekRange.y))
            {
                CurrentTimeTick.anchorMin = new(-1, 0);
                CurrentTimeTick.anchorMax = new(-1, 1);
                CurrentTimeConnector.anchorMin = new(-1, 0);
                CurrentTimeConnector.anchorMax = new(-1, 0);
            }
            else
            {
                float timePos = Mathf.Clamp(InverseLerpUnclamped(PeekRange.x, PeekRange.y, time), -1, 2);
                float timeCOffset = 40 / TimeSliderHolder.rect.width;
                float timeCPos = InverseLerpUnclamped(limit.x, limit.y, time) / (1 + timeCOffset) + timeCOffset / 2;

                CurrentTimeTick.anchorMin = new(timePos, 0);
                CurrentTimeTick.anchorMax = new(timePos, 1);
            
                CurrentTimeConnector.anchorMin = new(Mathf.Min(timePos, timeCPos), 0);
                CurrentTimeConnector.anchorMax = new(Mathf.Max(timePos, timeCPos), 0);
            }
            
            UpdateTimeline();
        }

        public void EventCollapsible()
        {
            if (TimelineHeight <= 0)
                Restore();
            else
                Collapse();
        }
        
        public void UpdateTabs()
        {

            TimingTab.gameObject.SetActive(HierarchyPanel.main.CurrentMode == HierarchyMode.PlayableSong);
            TimingTab.interactable = TimelineHeight <= 0 || CurrentMode != TimelineMode.Timing;
        
            StoryboardTab.gameObject.SetActive(HierarchyPanel.main.CurrentMode == HierarchyMode.Chart);
            StoryboardTab.interactable = TimelineHeight <= 0 || CurrentMode != TimelineMode.Storyboard;
        
            LaneTab.gameObject.SetActive(HierarchyPanel.main.CurrentMode == HierarchyMode.Chart);
            LaneTab.interactable = TimelineHeight <= 0 || CurrentMode != TimelineMode.Lanes;
        
            LaneStepTab.gameObject.SetActive(HierarchyPanel.main.CurrentMode == HierarchyMode.Chart && InspectorPanel.main.CurrentHierarchyObject is Lane);
            LaneStepTab.interactable = TimelineHeight <= 0 || CurrentMode != TimelineMode.LaneSteps;
        
            HitObjectTab.gameObject.SetActive(HierarchyPanel.main.CurrentMode == HierarchyMode.Chart && InspectorPanel.main.CurrentHierarchyObject is Lane);
            HitObjectTab.interactable = TimelineHeight <= 0 || CurrentMode != TimelineMode.HitObjects;

            switch (CurrentMode)
            {
                case TimelineMode.Timing:
                    CollapserTransform.anchoredPosition = TimingTabRect.anchoredPosition;
                    break;
                case TimelineMode.Storyboard:
                    CollapserTransform.anchoredPosition = StoryboardTabRect.anchoredPosition;
                    break;
                case TimelineMode.Lanes:
                    CollapserTransform.anchoredPosition = LaneTabRect.anchoredPosition;
                    break;
                case TimelineMode.LaneSteps:
                    CollapserTransform.anchoredPosition = LaneStepTabRect.anchoredPosition;
                    break;
                case TimelineMode.HitObjects:
                    CollapserTransform.anchoredPosition = HitObjectTabRect.anchoredPosition;
                    break;
            }
            Collapser.gameObject.SetActive(TimelineHeight > 0);
            Collapser.interactable = TimelineHeight > 0;
        
            LaneOptionsHolder.SetActive(CurrentMode == TimelineMode.Lanes);
            HitObjectOptionsHolder.SetActive(CurrentMode == TimelineMode.HitObjects);

            PickerPanel.main.UpdateButtons();
        }

        public void SetTabMode(int mode) => SetTabMode((TimelineMode)mode);

        public void SetTabMode(TimelineMode mode)
        {
            CurrentMode = mode;
            
            if (TimelineExpandHeight != TimelineHeight) 
            {
                ResizeTimeline(TimelineExpandHeight * 24 + 80);
            }
            else 
            {
                UpdateTabs();
                UpdateItems();
            }

        }

        public void UpdateTimeline(bool forced = false)
        {
            if (lastLimit != PeekRange || lastPlayed != Chartmaker.main.SongSource.isPlaying || forced)
            {
                lastLimit = PeekRange;
                lastPlayed = Chartmaker.main.SongSource.isPlaying;
                Metronome metronome = Chartmaker.main.CurrentSong.Timing;

                int count = 0;

                if (metronome.Stops.Count > 0)
                {
                    float density = (PeekRange.y - PeekRange.x) * metronome.GetStop(PeekRange.x, out _).BPM / TicksHolder.rect.width / 8;

                    Color color = Themer.main.Keys["TimelineTickMain"];

                    if (density != 0)
                    {
                        float factor = Mathf.Log(density, SeparationFactor);
                    
                        BeatPosition beat = BeatFloor(metronome.ToBeat(PeekRange.x), Mathf.FloorToInt(factor), SeparationFactor);
                        BeatPosition interval = BeatInterval(Mathf.FloorToInt(factor), SeparationFactor);
                    
                        float end = metronome.ToBeat(PeekRange.y);
                   
                        while (beat < end)
                        {
                            TimelineTick tick;
                            if (Ticks.Count <= count) 
                                Ticks.Add(tick = Instantiate(TickSample, TicksHolder));
                            else 
                                tick = Ticks[count];

                            float beatDensity = GetSeparationFactor(beat, SeparationFactor) - factor;

                            RectTransform rt = (RectTransform)tick.transform;
                            rt.anchorMin = new (
                                (metronome.ToSeconds(beat) - PeekRange.x) / (PeekRange.y - PeekRange.x),
                                0f
                            );
                            rt.anchorMax = new(rt.anchorMin.x, 1);

                            tick.Image.color = GetBeatColor(beat) * new Color(1, 1, 1, Mathf.Clamp01((Mathf.Pow(1.5f, beatDensity) - 1) / (Mathf.Pow(1.5f, 3) - 1)) * .5f);
                            tick.Label.color = color;
                            tick.Label.alpha = Mathf.Clamp01(beatDensity - 2.5f) * .5f;
                            if (tick.Label.alpha > 0) 
                                tick.Label.text = beat.ToString();

                            beat += interval;
                            count++;

                            if (count > 1000)
                                break;
                        }
                    }
                }
            
                while (Ticks.Count > count)
                {
                    Destroy(Ticks[^1].gameObject);
                    Ticks.RemoveAt(Ticks.Count - 1);
                }

                // Update border rects
                SongStartRect.anchorMax = new (
                    Mathf.InverseLerp(PeekRange.x, PeekRange.y, 0), 
                    SongStartRect.anchorMax.y
                );
                SongEndRect.anchorMin = new (
                    Mathf.InverseLerp(PeekRange.x, PeekRange.y, Chartmaker.main.CurrentSong.Clip.length), 
                    SongEndRect.anchorMin.y
                );

                UpdateItems();
                UpdateWaveform();
            }
            else if (waveTimeouted) 
            {
                UpdateWaveform();
            }
        }

        TimelineItem GetTimelineItem(int index)
        {
            TimelineItem item;
        
            if (Items.Count <= index)
                Items.Add(item = Instantiate(ItemSample, ItemsHolder));
            else 
                item = Items[index];
        
            return item;
        }
        Image GetItemTail(int index)
        {
            Image item;
        
            // Special case for previewer 
            if (index == -3280)
                return Instantiate(ItemTailSample, TailsHolder);
            
            if (ItemTails.Count <= index) 
                ItemTails.Add(item = Instantiate(ItemTailSample, TailsHolder));
            else 
                item = ItemTails[index];
        
            return item;
        }
        TMP_Text GetItemLabel(int index)
        {
            TMP_Text item;
        
            if (Labels.Count <= index)
                Labels.Add(item = Instantiate(LabelSample, LabelsHolder));
            else
                item = Labels[index];
        
            return item;
        }
        LineGraph GetItemGraph(int index)
        {
            LineGraph item;
        
            if (Graphs.Count <= index)
                Graphs.Add(item = Instantiate(GraphSample, GraphsHolder));
            else 
                item = Graphs[index];
        
            return item;
        }
        TMP_Text GetStoryboardEntry(int index)
        {
            TMP_Text item;
        
            if (StoryboardEntries.Count <= index)
                StoryboardEntries.Add(item = Instantiate(StoryboardEntrySample, StoryboardEntryHolder));
            else
                item = StoryboardEntries[index];
        
            return item;
        }

        public void UpdateItems()
        {
            if (TimelineHeight <= 0) 
                return;

            int count = 0, tailCount = 0, labelCount = 0, graphCount = 0, sbcount = 0;
            Metronome metronome = Chartmaker.main.CurrentSong.Timing;
        
            float density = (PeekRange.y - PeekRange.x) / TicksHolder.rect.width;
            List<float> times = new();

            Blocker.SetActive(false);

            TimelineItem AddItem(object obj, float time)
            {
                var item = GetTimelineItem(count);
            
                RectTransform rt = (RectTransform)item.transform;
                rt.anchorMin = rt.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, time), 1);
            
                item.SetItem(obj);
                item.Icon.sprite = LineIcon;
            
                count++;
                return item;
            }
            int AddTime(float time, float size = 24)
            {
                int pos;
                size *= density;

                for (pos = 0; pos < times.Count; pos++)
                    if (times[pos] < time - size) break;

                if (pos < times.Count) 
                    times[pos] = time;
                else
                    times.Add(time);

                return pos;
            }
            TimelineItem AddItemNormal(object obj, float time, float size = 20)
            {
                TimelineItem item = null;
                int pos = AddTime(time, size + 2) - ScrollOffset;
                float dOffset = size * density / 2;
                if (time >= PeekRange.x - dOffset && time <= PeekRange.y + dOffset && pos >= -1 && pos < TimelineHeight + 1)
                {
                    item = AddItem(obj, time);
                    RectTransform rt = (RectTransform)item.transform;
                    rt.anchoredPosition = new(0, -24 * pos - 6);
                    rt.sizeDelta = new(size, 20);
                }
                return item;
            }
            TMP_Text AddLabel(string text)
            {
                var label = GetItemLabel(labelCount);
                label.text = text;
                label.overflowMode = TextOverflowModes.Truncate;
                label.alignment = TextAlignmentOptions.CaplineLeft;
                return label;
            }

            if (Mathf.Approximately(PeekRange.x, PeekRange.y))
            {
                /* Do nothing */
            }
        
            else switch (CurrentMode)
            {
                case TimelineMode.Storyboard when InspectorPanel.main.CurrentObject is Storyboardable thing:
                {
                    TimestampType[] types = (TimestampType[])thing.timestampTypes;
                    Storyboard storyboard = thing.Storyboard;

                    StoryboardEntryMaterial.SetColor(OutlineColor, Themer.main.Keys["Background0"] + new Color(0, 0, 0, 1));

                    for (int a = 0; a < types.Length; a++)
                    {
                        int index = a - ScrollOffset;
                        times.Add(0);
                        if (index >= 0 && a - ScrollOffset < TimelineHeight)
                        {
                            TMP_Text label = GetStoryboardEntry(index);
                            RectTransform rt = label.rectTransform;
                        
                            label.text = types[a].Name;
                            rt.anchoredPosition = new(0, -24 * index - 5);
                        
                            sbcount++;
                        }
                    }

                    float dOffset = 4 * density;
                    foreach (Timestamp timestamp in storyboard.Timestamps)
                    {
                        float time = metronome.ToSeconds(timestamp.Offset);
                        float timeEndPoint = metronome.ToSeconds(timestamp.Offset + timestamp.Duration);
                        if (timeEndPoint < PeekRange.x - dOffset || time > PeekRange.y + dOffset)
                            continue;

                        float index = Array.FindIndex(types, x => x.ID == timestamp.ID) - ScrollOffset;
                        if (index < -1 || index >= TimelineHeight + 1)
                            continue;

                        float posX = InverseLerpUnclamped(PeekRange.x, PeekRange.y, time);
                        Image tail;
                        if (!Mathf.Approximately(time, timeEndPoint))
                        {
                            tail = GetItemTail(tailCount);
                            RectTransform tailRectTransform = tail.rectTransform;
                            tailRectTransform.anchorMin = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, time), 1);
                            tailRectTransform.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, timeEndPoint), 1);
                            tailRectTransform.anchoredPosition = new(0, -24 * index - 6);
                            tailRectTransform.sizeDelta = new(0, 20);
                        
                            posX = Mathf.Max(posX, Mathf.Min(8 / ItemsHolder.rect.width, Mathf.Max(tailRectTransform ? tailRectTransform.anchorMax.x - 4 / ItemsHolder.rect.width : posX, posX)));
                            tailCount++;

                            TMP_Text endLabel = AddLabel("");
                            endLabel.alignment = TextAlignmentOptions.CaplineRight;
                        
                            RectTransform labelRectTransform = endLabel.rectTransform;
                            labelRectTransform.anchorMin = tailRectTransform.anchorMin;
                            labelRectTransform.anchorMax = tailRectTransform.anchorMax;
                            labelRectTransform.anchoredPosition = new(0, -24 * index - 5);
                            labelCount++;

                            TMP_Text startLabel = null;

                            if (timeEndPoint - time > 8 * density) 
                            {
                                var graph = GetItemGraph(graphCount);
                            
                                graph.rectTransform.anchorMin = tailRectTransform.anchorMin;
                                graph.rectTransform.anchorMax = tailRectTransform.anchorMax;
                                graph.rectTransform.anchoredPosition = tailRectTransform.anchoredPosition;
                                graph.rectTransform.sizeDelta = tailRectTransform.sizeDelta;
                            
                                graph.Values = GetEaseGraphValues(timestamp.Easing);
                            
                                graphCount++;
                            }
                            if (!float.IsNaN(timestamp.From))
                            {

                                startLabel = AddLabel("");
                            
                                RectTransform startLabelRectTransform = startLabel.rectTransform;
                                startLabelRectTransform.anchorMin = tailRectTransform.anchorMin;
                                startLabelRectTransform.anchorMax = tailRectTransform.anchorMax;
                                startLabelRectTransform.anchoredPosition = new(0, -24 * index - 5);
                                labelCount++;
                            }

                            if (startLabel)
                                NumberLabelTest(timestamp.From, startLabel, timestamp.Target, endLabel, labelRectTransform.rect.width - startLabel.margin.x * 2.5f);
                            else 
                                NumberLabelTest(timestamp.Target, endLabel, labelRectTransform.rect.width - endLabel.margin.x * 3f);
                        
                            if (string.IsNullOrEmpty(endLabel.text)) 
                                labelCount -= startLabel 
                                    ? 2 : 1;
                        }
                        else 
                        {

                            TMP_Text endLabel = AddLabel(timestamp.Target.ToString("0.##"));
                            endLabel.alignment = TextAlignmentOptions.CaplineRight;
                            endLabel.overflowMode = TextOverflowModes.Overflow;
                        
                            RectTransform labelRectTransform = endLabel.rectTransform;
                            labelRectTransform.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, time), 1);
                            labelRectTransform.anchorMin = labelRectTransform.anchorMax - new Vector2(1, 0);
                            labelRectTransform.anchoredPosition = new(0, -24 * index - 5);
                            labelCount++;
                        }

                        var item = GetTimelineItem(count);
                        item.Icon.sprite = LineIcon;
                    
                        RectTransform itemRectTransform = (RectTransform)item.transform;
                        itemRectTransform.anchorMin = itemRectTransform.anchorMax = new (posX, 1);
                        itemRectTransform.anchoredPosition = new(0, -24 * index - 6);
                        itemRectTransform.sizeDelta = new(6, 20);
                        item.SetItem(timestamp);

                        count++;
                    }

                    break;
                }
            
                case TimelineMode.Storyboard when InspectorPanel.main.CurrentObject is IList:
                    Blocker.SetActive(true);
                    BlockerLabel.text = "Storyboard editing of multiple objects is not supported.";

                    break;
                case TimelineMode.Storyboard when InspectorPanel.main.CurrentObject == null:
                    Blocker.SetActive(true);
                    BlockerLabel.text = "No object selected - Please select an object first to view its Storyboard.";

                    break;
                case TimelineMode.Storyboard:
                    Blocker.SetActive(true);
                    BlockerLabel.text = "This object is not storyboardable.";

                    break;
                case TimelineMode.Timing:
                {
                    if (Chartmaker.main.CurrentSong?.Timing.Stops == null)
                        return;
                
                    foreach (BPMStop stop in Chartmaker.main.CurrentSong?.Timing.Stops!)
                        AddItemNormal(stop, stop.Offset);

                    break;
                }
                case TimelineMode.Lanes when Chartmaker.main.CurrentChart?.Lanes != null:
                {
                    float dOffset = 11 * density;

                    List<Lane> lanes = GetLanesInTimeline();

                    if (lanes.Count == 0 && Chartmaker.main.CurrentChart.Lanes.Count > 0)
                    {
                        Blocker.SetActive(true);
                        BlockerLabel.text = 
                            "Your Lane filter settings are filtering all Lanes in your Chart."
                            + "\nTry adjusting your Lane filter settings.";
                    }
                    else foreach (Lane lane in lanes)
                    {
                        float time = metronome.ToSeconds(lane.LaneSteps[0].Offset);
                        float timeEndPoint = metronome.ToSeconds(lane.LaneSteps[^1].Offset);

                        int zPosition = AddTime(time, 24) - ScrollOffset;
                        times[zPosition + ScrollOffset] = Mathf.Max(time, timeEndPoint - 7 * density);

                        if (zPosition < -1 || zPosition >= TimelineHeight + 1) 
                            continue;
                        if (timeEndPoint < PeekRange.x - dOffset || time > PeekRange.y + dOffset) 
                            continue;

                        for (int a = 1; a < lane.LaneSteps.Count; a++)
                        {
                            LaneStep step = lane.LaneSteps[a];
                            float stepTime = metronome.ToSeconds(step.Offset);
                           
                            if (stepTime < PeekRange.x - dOffset || stepTime > PeekRange.y + dOffset)
                                continue;

                            float spritePositionX = InverseLerpUnclamped(PeekRange.x, PeekRange.y, stepTime);
                          
                            var spriteItem = GetTimelineItem(count);
                            
                            spriteItem.Icon.sprite = LineIcon;
                            
                            RectTransform spriteRectTransform = (RectTransform)spriteItem.transform;
                            
                            spriteRectTransform.anchorMin = spriteRectTransform.anchorMax = new (spritePositionX, 1);
                            spriteRectTransform.anchoredPosition = new(0, -24 * zPosition - 6);
                            spriteRectTransform.sizeDelta = new(6, 20);
                            
                            spriteItem.SetItem(step, lane);
                        
                            count++;
                        }

                        float positionX = InverseLerpUnclamped(PeekRange.x, PeekRange.y, time);
                        float posX2 = positionX;
                        if (time != timeEndPoint)
                        {
                            var tail = GetItemTail(tailCount);
                            RectTransform tailRectTransform = tail.rectTransform;
                            tailRectTransform.anchorMin = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, time), 1);
                            tailRectTransform.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, timeEndPoint), 1);
                            tailRectTransform.anchoredPosition = new(0, -24 * zPosition - 6);
                            tailRectTransform.sizeDelta = new(0, 20);
                            positionX = Mathf.Max(positionX, Mathf.Min(15 / ItemsHolder.rect.width, Mathf.Max(tailRectTransform ? tailRectTransform.anchorMax.x - 16 / ItemsHolder.rect.width : positionX, positionX)));
                            tailCount++;

                            if (!String.IsNullOrWhiteSpace(lane.Name))
                            {
                                TMP_Text label = AddLabel(lane.Name);
                                label.overflowMode = TextOverflowModes.Ellipsis;
                                RectTransform labelRectTransform = label.rectTransform;
                                labelRectTransform.anchorMin = new (Math.Max(15 / TicksHolder.rect.width, tailRectTransform.anchorMin.x), 1);
                                labelRectTransform.anchorMax = tailRectTransform.anchorMax;
                                labelRectTransform.anchoredPosition = new(8, -24 * zPosition - 5);
                                labelCount++;
                            }
                        }

                        var item = GetTimelineItem(count);
                        item.Icon.sprite = !Mathf.Approximately(positionX, posX2)
                            ? BehindIcon : LineIcon;
                    
                        RectTransform rt = (RectTransform)item.transform;
                        rt.anchorMin = rt.anchorMax = new (positionX, 1);
                        rt.anchoredPosition = new(0, -24 * zPosition - 6);
                        rt.sizeDelta = new(20, 20);
                    
                        item.SetItem(lane);
                    
                        count++;
                    }

                    break;
                }
                case TimelineMode.Lanes:
                    Blocker.SetActive(true);
                    BlockerLabel.text = "No chart loaded - Load a chart first to view its Lanes.";
                    break;
                case TimelineMode.LaneSteps when InspectorPanel.main.CurrentHierarchyObject is Lane lane:
                {
                    foreach (LaneStep step in lane.LaneSteps)
                        AddItemNormal(step, metronome.ToSeconds(step.Offset));
                    break;
                }
                case TimelineMode.LaneSteps:
                    Blocker.SetActive(true);
                    BlockerLabel.text = "No lane selected - Select a lane first to view its Lane Steps.";
                    break;
                case TimelineMode.HitObjects when InspectorPanel.main.CurrentHierarchyObject is Lane lane:
                {
                    float height = ItemsHolder.rect.height - 8;
                    float dOffset = 4 * density;

                    float vpStart = .5f - VerticalScale * .5f + VerticalOffset;
                    float vpEnd = .5f + VerticalScale * .5f + VerticalOffset;

                    foreach (HitObject hit in lane.Objects)
                    {
                        float time = metronome.ToSeconds(hit.Offset);
                        float timeEndPoint = metronome.ToSeconds(hit.Offset + hit.HoldLength);
                        if (timeEndPoint < PeekRange.x - dOffset || time > PeekRange.y + dOffset) 
                            continue;

                        float start = InverseLerpUnclamped(vpStart, vpEnd, hit.Position);
                        float end = InverseLerpUnclamped(vpStart, vpEnd, hit.Position + hit.Length);
                        float position = Mathf.Floor(-start * height) - 3;
                        float length = Mathf.Floor((end - start) * height) + 2;

                        if (!Mathf.Approximately(time, timeEndPoint))
                        {
                            var tail = GetItemTail(tailCount);
                            RectTransform tailRectTransform = tail.rectTransform;
                            tailRectTransform.anchorMin = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, time), 1);
                            tailRectTransform.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, timeEndPoint), 1);
                            tailRectTransform.anchoredPosition = new Vector2(0, position);
                            tailRectTransform.sizeDelta = new Vector2(0, length);
                            tailCount++;
                        }
                        if (time < PeekRange.x - dOffset || time > PeekRange.y + dOffset) 
                            continue;

                        var item = GetTimelineItem(count);
                        item.Icon.sprite = hit.Type == HitObject.HitType.Normal 
                            ? NormalHitIcon : CatchHitIcon;
                    
                        RectTransform rt = (RectTransform)item.transform;
                        rt.anchorMin = rt.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, time), 1);
                        rt.anchoredPosition = new Vector2(0, position);
                        rt.sizeDelta = new Vector2(6, length);

                        item.SetItem(hit);

                        count++;
                    }

                    break;
                }
                case TimelineMode.HitObjects:
                    Blocker.SetActive(true);
                    BlockerLabel.text = "No lane selected - Select a lane first to view its Hit Objects.";
                    break;
            }

            while (Items.Count > count)
            {
                Destroy(Items[^1].gameObject);
                Items.RemoveAt(Items.Count - 1);
            }
        
            while (ItemTails.Count > tailCount)
            {
                Destroy(ItemTails[^1].gameObject);
                ItemTails.RemoveAt(ItemTails.Count - 1);
            }
        
            while (Labels.Count > labelCount)
            {
                Destroy(Labels[^1].gameObject);
                Labels.RemoveAt(Labels.Count - 1);
            }
        
            while (Graphs.Count > graphCount)
            {
                Destroy(Graphs[^1].gameObject);
                Graphs.RemoveAt(Graphs.Count - 1);
            }
        
            while (StoryboardEntries.Count > sbcount)
            {
                Destroy(StoryboardEntries[^1].gameObject);
                StoryboardEntries.RemoveAt(StoryboardEntries.Count - 1);
            }
        
            if (ItemHeight != times.Count)
            {
                ItemHeight = times.Count;
                UpdateScrollbar();
            }

            if (InspectorPanel.main.CurrentHierarchyObject is Lane activeLane &&
                (CurrentMode is TimelineMode.LaneSteps or TimelineMode.HitObjects))
            {
                LaneStartRect.anchorMax = new (
                    Mathf.InverseLerp(PeekRange.x, PeekRange.y, metronome.ToSeconds(activeLane.LaneSteps[0].Offset)), 
                    LaneStartRect.anchorMax.y
                );
                LaneEndRect.anchorMin = new (
                    Mathf.InverseLerp(PeekRange.x, PeekRange.y, metronome.ToSeconds(activeLane.LaneSteps[^1].Offset)), 
                    LaneEndRect.anchorMin.y
                );
            }
            else 
            {
                LaneStartRect.anchorMax = SongStartRect.anchorMax;
                LaneEndRect.anchorMin = SongEndRect.anchorMin;
            }
        }

        private float[] GetEaseGraphValues(IEaseDirective easing)
        {
            float[] values = new float[64];
            float interval = 1f / (values.Length - 1);
        
            for (int i = 0; i < values.Length; i++) 
                values[i] = easing.Get(i * interval);
        
            return values;
        }

        private List<Lane> GetLanesInTimeline()
        {
            switch (LaneFilterMode)
            {
                case LaneFilterMode.All:
                    return Chartmaker.main.CurrentChart.Lanes;
                case LaneFilterMode.HierarchyVisible:
                {
                    List<Lane> lanes = new();
                    foreach(HierarchyItemHolder item in HierarchyPanel.main.Holders)
                        if (item.Target.Target is Lane lane) 
                            lanes.Add(lane);
                
                    lanes.Sort((x, y) => x.LaneSteps[0].Offset.CompareTo(y.LaneSteps[0].Offset));
                    return lanes;
                }
                default:
                    return null;
            }
        }

        int    waveOffset                = 0;
        float  waveTime, waveLastDensity = 0;
        bool   waveTimeouted             = false;
        bool[] waveBaked;

        public void UpdateWaveform()
        {
            if (
                TimelineHeight <= 0
                || Mathf.Approximately(PeekRange.y, PeekRange.x)
                || Options.WaveformMode == 0
                || Options.WaveformIdle < (Chartmaker.main.SongSource.isPlaying ? 1 : 0)
            )
            {
                WaveformImage.enabled = false;
                return;
            }
        
            if (!WaveformImage.enabled)
                WaveformImage.enabled = true;
        
            Color color = Themer.main.Keys["TimelineTickMain"];

            Texture2D texture = null;
        
            if (WaveformImage.texture is Texture2D imageTexture)
                texture = imageTexture;
        
            RectTransform waveRT = WaveformImage.rectTransform;
        
            if (!texture || texture.width != (int)waveRT.rect.width || texture.height != (int)waveRT.rect.height)
            {
                Destroy(WaveformImage.texture);
                WaveformImage.texture = texture = new Texture2D((int)waveRT.rect.width, (int)waveRT.rect.height);
                waveBaked = new bool[(int)waveRT.rect.width];
                waveOffset = 0;
            }

            AudioClip clip = Chartmaker.main.SongSource.clip;

            float density = clip.frequency * (PeekRange.y - PeekRange.x) / texture.width;

            float step = (PeekRange.y - PeekRange.x) / texture.width;
            float sec = Mathf.Floor(PeekRange.x / step - 1) * step;
            int waveNewOffset = (int)((sec - waveTime) / step);

            if (!(Math.Abs(waveLastDensity / density - 1) < 0.0001f) || Mathf.Abs(waveOffset - waveNewOffset) >= texture.width) {
                Destroy(WaveformImage.texture);
                WaveformImage.texture = texture = new Texture2D((int)waveRT.rect.width, (int)waveRT.rect.height);
                waveBaked = new bool[(int)waveRT.rect.width];
                waveLastDensity = density;
                waveTime = sec;
                waveOffset = waveNewOffset = 0;
            }

            Color[] lineBuffer = new Color[texture.height];
            while (waveOffset < waveNewOffset) 
            {
                int sLine = (waveOffset % texture.width + texture.width) % texture.width;
                waveBaked[sLine] = false;
                texture.SetPixels(sLine, 0, 1, texture.height, lineBuffer);
                waveOffset++;
            }
            while (waveOffset > waveNewOffset) 
            {
                int sLine = (waveOffset % texture.width + texture.width) % texture.width;
                waveBaked[sLine] = false;
                texture.SetPixels(sLine, 0, 1, texture.height, lineBuffer);
                waveOffset--;
            }

            WaveformImage.uvRect = new Rect(waveOffset / (float)texture.width, 0, 1, 1);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool Timeout() => stopwatch.ElapsedMilliseconds >= 15;
            waveTimeouted = false;

            switch (Options.WaveformMode) 
            {
                // Waveform
                case 1: {
                    float[] data = new float[Mathf.CeilToInt(Math.Min(density, 1024) / clip.channels) * clip.channels];
                    float denY = 1f / texture.height * clip.channels;
                    float[] lastMin = new float[clip.channels], lastMax = new float[clip.channels];

                    float darkAlpha = Mathf.Clamp(Mathf.Sqrt(5 / density), 0.5f, 0.8f);

                    for (int a = 0; a < clip.channels; a++)
                    {
                        lastMin[a] = -1;
                        lastMax[a] = 1;
                    }

                    for (int x = 0; x < texture.width; x++) 
                    {
                        int sampleLine = ((x + waveOffset) % texture.width + texture.width) % texture.width;
                        if (waveBaked[sampleLine])
                            continue;
                   
                        sec = waveTime + (x + waveOffset) * step;
                        float[] min = new float[clip.channels], max = new float[clip.channels];
                        float[] rms = new float[clip.channels];
                        int position = (int)(sec * clip.frequency);
                    
                        if (position >= 0 && position < clip.samples - data.Length)
                        {
                            clip.GetData(data, position);
                            for (int a = 0; a < clip.channels; a++)
                            {
                                min[a] = 1;
                                max[a] = -1;
                            }
                            for (int a = 0; a < data.Length; a++)
                            {
                                int channels = (a) % clip.channels;
                                min[channels] = Math.Min(min[channels], data[a]);
                                max[channels] = Math.Max(max[channels], data[a]);
                                rms[channels] += data[a] * data[a];
                            }
                        
                            for (int a = 0; a < clip.channels; a++)
                            {
                                float temp;
                                min[a] = Math.Min(lastMax[a], temp = min[a]) * .8f;
                                max[a] = Math.Max(lastMin[a], lastMax[a] = max[a]) * .8f;
                                rms[a] = Mathf.Sqrt(rms[a] / data.Length * clip.channels) * .8f;
                                lastMin[a] = temp;
                            }
                        }
                        float samplePos = 0;
                        for (int y = 0; y < texture.height; y++) 
                        {
                            int channel = Mathf.FloorToInt(samplePos);
                            float window = 1 - (samplePos % 1) * 2f;
                            color.a = window >= min[channel] - denY && window <= max[channel] + denY ? (
                                Mathf.Abs(window) < rms[channel] - denY ? 0.8f : darkAlpha
                            ) : 0;
                            lineBuffer[y] = color;
                            samplePos += denY;
                        }
                    
                        texture.SetPixels(sampleLine, 0, 1, texture.height, lineBuffer);
                        waveBaked[sampleLine] = true;
                    
                        if (Timeout())
                        {
                            waveTimeouted = true;
                            break;
                        }
                    }
                } break;

                // Spectrogram
                case 2: {
                    int resolution = 512;

                    float[] data = new float[resolution * clip.channels];
                    float[][] fft = new float[clip.channels][];
                    float denY = 1f / texture.height * clip.channels;

                    for (int i = 0; i < clip.channels; i++) 
                        fft[i] = new float[resolution];

                    FrequencyScaling.GetScalingFunctions(Chartmaker.Preferences.FrequencyScale, out var scale, out var unscale);

                    float minScale = scale(Chartmaker.Preferences.FrequencyMin);
                    float maxScale = scale(Chartmaker.Preferences.FrequencyMax);
                
                    for (int x = 0; x < texture.width; x++) 
                    {
                        int sampleLine = ((x + waveOffset) % texture.width + texture.width) % texture.width;
                        if (waveBaked[sampleLine]) continue;
                        sec = waveTime + (x + waveOffset) * step;
                        int position = (int)(sec * clip.frequency - resolution / 2);
                        if (position >= 0 && position < clip.samples - data.Length)
                        {
                            clip.GetData(data, position);
                        
                            for (int y = 0; y < data.Length; y++) 
                            {
                                int chan = (position + y) % clip.channels;
                                int p = y / clip.channels;
                                fft[chan][p] = data[y];
                            }
                        
                            foreach (var t in fft)
                                FFT.Transform(t, Chartmaker.Preferences.FFTWindow);
                        }
                    
                        float sPos = 0;
                        for (int y = 0; y < texture.height; y++) 
                        {
                            int channel = Mathf.FloorToInt(sPos);
                            float cPos = Mathf.Clamp(unscale(Mathf.Lerp(minScale, maxScale, sPos % 1)) / clip.frequency * resolution, 0, resolution - 1);
                            float value = Mathf.Sqrt(Mathf.Lerp(fft[channel][Mathf.FloorToInt(cPos)], fft[channel][Mathf.CeilToInt(cPos)], cPos % 1) / resolution * cPos) / 4;
                            lineBuffer[y] = color * new Color(1, 1, 1, value);
                            sPos += denY;
                        }
                        texture.SetPixels(sampleLine, 0, 1, texture.height, lineBuffer);
                        waveBaked[sampleLine] = true;
                    
                        if (Timeout())
                        {
                            waveTimeouted = true;
                            break;
                        }
                    }
                } break;
            }

            texture.Apply();
        }

        public void DiscardWaveform() 
        {
            var waveRT = WaveformImage.rectTransform;
       
            Destroy(WaveformImage.texture);
        
            WaveformImage.texture = new Texture2D((int)waveRT.rect.width, (int)waveRT.rect.height);
        
            if (waveBaked != null)
                waveBaked = new bool[waveBaked.Length];
            waveTimeouted = true;
        }

        string FormatNumber(float number, int type) 
        {
            return type switch
            {
                0 => number.ToString("0.###"),
                1 => number.ToString("0.##"),
                2 => number.ToString("0.#"),
                3 => number.ToString("0"),
                4 => number.ToString("0.###e0"),
                5 => number.ToString("0.##e0"),
                6 => number.ToString("0.#e0"),
                7 => number.ToString("0e0"),
                _ => "…",
            };
        }

        private void NumberLabelTest(float number, TMP_Text label, float targetSize) 
        {
            int type = 0;
            label.text = FormatNumber(number, type);
            label.ForceMeshUpdate();
        
            while (true) 
            {
                if (label.textBounds.size.x <= targetSize) 
                    break;
            
                if (type >= 7)
                {
                    label.text = "…";
                    label.ForceMeshUpdate();
                
                    if (label.textBounds.size.x > targetSize)
                        label.text = "";
                    break;
                }
                type++;
                label.text = FormatNumber(number, type);
                label.ForceMeshUpdate();
            }
        }

        private void NumberLabelTest(float number1, TMP_Text label1, float number2, TMP_Text label2, float targetSize) 
        {
            int type1 = 0, type2 = 0;
        
            label1.text = FormatNumber(number1, type1);
            label1.ForceMeshUpdate();
        
            label2.text = FormatNumber(number2, type2);
            label2.ForceMeshUpdate();
       
            while (true) 
            {
                float width1 = label1.textBounds.size.x;
                float width2 = label2.textBounds.size.x;
           
                if (width1 + width2 <= targetSize)
                    break;
            
                if (type1 >= 7 && type2 >= 7)
                {
                    label1.text = label2.text = "…";
                    label1.ForceMeshUpdate(); label2.ForceMeshUpdate();
                    if (label1.textBounds.size.x + label2.textBounds.size.x > targetSize) 
                        label1.text = label2.text = "";
                    break;
                }
            
                if ((width1 > width2 && type1 < 7) || type2 >= 7) 
                {
                    type1++;
                    label1.text = FormatNumber(number1, type1);
                    label1.ForceMeshUpdate();
                }
                else
                {
                    type2++;
                    label2.text = FormatNumber(number2, type2);
                    label2.ForceMeshUpdate();
                }
            }
        }

        private void UpdateScrollbar()
        {
            if (ItemHeight > TimelineHeight)
            {
                if (ScrollOffset > ItemHeight - TimelineHeight)
                {
                    ScrollOffset = ItemHeight - TimelineHeight;
                    UpdateItems();
                }
                VerticalScrollbar.gameObject.SetActive(true);
                VerticalScrollbar.value = ScrollOffset / (float)(ItemHeight - TimelineHeight);
                VerticalScrollbar.size = TimelineHeight / (float)ItemHeight;
            }
            else
            {
                ScrollOffset = 0;
                VerticalScrollbar.gameObject.SetActive(false);
            }
        }

        public void SetScrollbar(float value)
        {
            int offset = Mathf.RoundToInt(value * (ItemHeight - TimelineHeight));
            if (ScrollOffset != offset)
            {
                ScrollOffset = offset;
                UpdateItems();
            }
            UpdateScrollbar();
        }

        private float RoundBeat(float time) 
        {
            Metronome metronome = Chartmaker.main.CurrentSong.Timing;
        
            float density = (PeekRange.y - PeekRange.x) * metronome.GetStop(time, out _).BPM / TicksHolder.rect.width / 8;
            float factor = Mathf.Floor(Mathf.Log(density, SeparationFactor));
            float step = Mathf.Pow(SeparationFactor, factor + 1);
        
            return Mathf.Round(metronome.ToBeat(time) / step) * step;
        }

        public BeatPosition ToRoundedBeat(float beat) 
        {
            Metronome metronome = Chartmaker.main.CurrentSong.Timing;
       
            float density = (PeekRange.y - PeekRange.x) * metronome.GetStop(beat, out _).BPM / TicksHolder.rect.width / 8;
            float factor = Mathf.Floor(Mathf.Log(density, SeparationFactor));
            float step = Mathf.Pow(SeparationFactor, -1 - factor);
        
            if (step > 1) 
            {
                return new BeatPosition(
                    Mathf.FloorToInt(Mathf.Abs(beat)) * Math.Sign(beat),
                    Mathf.RoundToInt(Mathf.Abs(beat % 1) * step) * Math.Sign(beat),
                    (int)step);
            } 
            else 
            {
                return new BeatPosition((int)(Mathf.Floor(beat * step) / step));
            }
        }

        BeatPosition BeatFloor(float time, int factor, int sep) 
        {
            int fMin = (int)Math.Pow(sep, Math.Max(factor, 0));
            int fMax = (int)Math.Pow(sep, Math.Max(-factor, 0));
    
            return new(
                (int)(Mathf.Floor(time / fMin) * fMin),
                fMax == 1 ? 0 : (int)(Mathf.Floor(time % 1 * fMax)),
                fMax
            );
        }
        BeatPosition BeatInterval(int factor, int sep) 
        {
            int fMin = (int)Math.Pow(sep, Math.Max(factor, 0));
            int fMax = (int)Math.Pow(sep, Math.Max(-factor, 0));
     
            return new(0, fMin, fMax);
        }

        static int GetSeparationFactor(BeatPosition time, int sep) 
        {
            if (time.Denominator == 1) 
            {
                if (time.Number == 0) return int.MaxValue;
                int s = 0;
                while (time.Number % Mathf.Pow(sep, s + 1) == 0) s++;
                return s;
            }
            else 
            {
                return -Mathf.RoundToInt(Mathf.Log(time.Denominator, sep));
            }
        }

        Color GetBeatColor(BeatPosition time)
        {
            switch (time.Denominator)
            {
                case 1:      return Themer.main.Keys["TimelineTickMain"];
                case 2:      return Themer.main.Keys["TimelineTick2"];
                case 4:      return Themer.main.Keys["TimelineTick4"];
                case 8:      return Themer.main.Keys["TimelineTick8"];
                case 3:      return Themer.main.Keys["TimelineTick3"];
                case 6:      return Themer.main.Keys["TimelineTick6"];
                default:     return Themer.main.Keys["TimelineTickOther"];
            }
        }

        float InverseLerpUnclamped(float start, float end, float value)
        {
            return (value - start) / (end - start);
        }

        Vector2 lastLimit;
    
        TimelineDragMode dragMode;
        Vector2          dragStart, dragEnd;
        float            timeStart, timeEnd, beatStart, beatEnd;
        public bool isDragged { get; private set; }

        PointerEventData      lastDrag;
        private Vector2       initialPreviewersPosition;
        private RectTransform hitobjectRect;
        
        public void OnPointerDown(PointerEventData eventData)
        {
            bool contains(RectTransform rt)                  => RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.pressPosition, eventData.pressEventCamera);
            bool localPos(RectTransform rt, out Vector2 pos) => RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.pressPosition, eventData.pressEventCamera, out pos);
         
            isDragged = false;
            lastDrag = eventData;
            DraggingItem = null;
            
            if (contains(ItemsHolder))
            {
                if (eventData.button == PointerEventData.InputButton.Middle)
                    dragMode = TimelineDragMode.TimelineDrag;
                else if (eventData.button == PointerEventData.InputButton.Right || PickerPanel.main.CurrentTimelinePickerMode == TimelinePickerMode.Select)
                    dragMode = TimelineDragMode.Select;
                else
                    dragMode = TimelineDragMode.Timeline;

                localPos(ItemsHolder, out dragStart);

                dragEnd = dragStart;
                timeStart = timeEnd = Mathf.Lerp(PeekRange.x, PeekRange.y, dragStart.x / ItemsHolder.rect.width);

                Metronome metronome = Chartmaker.main.CurrentSong.Timing;
                beatStart = RoundBeat(timeStart);

                if (eventData.button != PointerEventData.InputButton.Left) 
                    return;

                if (eventData.button != PointerEventData.InputButton.Left)
                    return;

                TimelinePickerMode mode = PickerPanel.main.CurrentTimelinePickerMode;

                if (mode is TimelinePickerMode.Lane or TimelinePickerMode.LaneStep or TimelinePickerMode.BPMStop)
                {
                    Previewer.gameObject.SetActive(true);
                    Previewer.gameObject.transform.position = eventData.pressPosition;
                    initialPreviewersPosition = Previewer.gameObject.transform.position;
                }
                else if (mode is TimelinePickerMode.CatchHit or TimelinePickerMode.NormalHit or TimelinePickerMode.Timestamp)
                {
                    PreviewerTail.gameObject.SetActive(true);
    
                    if (mode is TimelinePickerMode.CatchHit or TimelinePickerMode.NormalHit)
                    {
                        hitobjectRect = PreviewerTail.GetComponent<RectTransform>();
        
                        float vpStart = .5f - VerticalScale * .5f + VerticalOffset;
                        float vpEnd = .5f + VerticalScale * .5f + VerticalOffset;
                        float height = ItemsHolder.rect.height - 8;
        
                        float start = InverseLerpUnclamped(vpStart, vpEnd, eventData.position.y);
                        float end = InverseLerpUnclamped(vpStart, vpEnd, eventData.position.y + Options.NewHitObjectLength);
                        float length = Mathf.Floor((end - start) * height) + 2;
        
                        hitobjectRect.sizeDelta = new Vector2(6, length);
                    }
                    else if (InspectorPanel.main.CurrentObject is Storyboardable thing)
                    {
                        TimestampType[] types = thing.timestampTypes;
                        int index = Math.Clamp(
                            Mathf.FloorToInt((ItemsHolder.rect.height - eventData.pressPosition.y - 3) / 24) + ScrollOffset, 
                            0, 
                            types.Length - 1
                        );

                        // Snap PreviewerTail to the center of the selected row
                        float yPosition = ItemsHolder.rect.height - (index - ScrollOffset) * 24 - 3 - 12; // -12 to center in 24px row
    
                        PreviewerTail.gameObject.transform.position *= new Vector2Frag(y: yPosition);
                    }

                    PreviewerTail.gameObject.transform.position = eventData.pressPosition;
                    initialPreviewersPosition = PreviewerTail.gameObject.transform.position;
                }
                
                AudioSource source = Chartmaker.main.SongSource;
                float time = Mathf.Clamp(metronome.ToSeconds(beatStart), 0, Chartmaker.main.SongSource.clip.length);

                if (source.time == 0)
                {
                    source.Play();
                    source.Pause();
                }
            
                source.time = time;

            }
            else if (contains(PeekStartSlider))
            {
                dragMode = TimelineDragMode.PeekStart;
                localPos(PeekStartSlider, out dragStart);
            }
            else if (contains(PeekEndSlider))
            {
                dragMode = TimelineDragMode.PeekEnd;
                localPos(PeekEndSlider, out dragStart);
            }
            else if (contains(CurrentTimeSlider))
            {
                dragMode = TimelineDragMode.CurrentTime;
                localPos(CurrentTimeSlider, out dragStart);
            }
            else if (contains(PeekRangeSlider))
            {
                dragMode = TimelineDragMode.PeekRange;
                localPos(PeekRangeSlider, out dragStart);
            }
            else 
                dragMode = TimelineDragMode.None;
        }

        public void BeginDragItem(IList items, PointerEventData eventData) 
        {
            DraggingItem = items;
            dragMode = TimelineDragMode.ItemDrag;
        
            bool localPos(RectTransform rt, out Vector2 pos) => RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.pressPosition, eventData.pressEventCamera, out pos);
            localPos(ItemsHolder, out dragStart);

            dragEnd = dragStart;
            timeStart = timeEnd = Mathf.Lerp(PeekRange.x, PeekRange.y, dragStart.x / ItemsHolder.rect.width);
        
            Metronome metronome = Chartmaker.main.CurrentSong.Timing;
            beatStart = RoundBeat(timeStart);
        
            ChartmakerHistory history = Chartmaker.main.History;
            IChartmakerAction last = history.ActionsBehind.Count == 0 ? null : history.ActionsBehind.Peek();
            if (last is ChartmakerTimelineDragFloatAction lastMove && Equals(lastMove.Targets, DraggingItem))
                DraggingItemOffset = lastMove.Value;
            else 
                DraggingItemOffset = 0;
        }

        private Image pseudoTail = null;
        public void OnDrag(PointerEventData eventData)
        {
            isDragged = true;
            if (dragMode == TimelineDragMode.None) 
                return;

            Chartmaker chartmaker = Chartmaker.main;
            Vector2 limit = new(
                Mathf.Min(PeekRange.x, PeekLimit.x),
                Mathf.Max(PeekRange.y, PeekLimit.y)
            );
        
            float width = limit.y - limit.x;
            float time;
        
            bool localPos(RectTransform rt, out Vector2 pos) => RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, eventData.position, eventData.pressEventCamera, out pos);
        
            // Timeline dragging

            if ((int)dragMode % 2 == 1)
            {

                localPos(ItemsHolder, out dragEnd);
                timeEnd = Mathf.Lerp(PeekRange.x, PeekRange.y, dragEnd.x / ItemsHolder.rect.width);

                Metronome metronome = Chartmaker.main.CurrentSong.Timing;
                beatEnd = RoundBeat(timeEnd);

                if (DraggingItem != null)
                {
                    if (DraggingItem.Count <= 0) return;

                    ChartmakerHistory history = Chartmaker.main.History;
                    switch (DraggingItem[0])
                    {
                        case Lane:
                        {
                            ChartmakerTimelineDragLaneAction action;
                            IChartmakerAction last = history.ActionsBehind.Count == 0 ? null : history.ActionsBehind.Peek();
                            
                            if (last is ChartmakerTimelineDragLaneAction lastMove && lastMove.Targets == DraggingItem)
                            {
                                action = lastMove;
                                action.Undo();
                            } 
                            else 
                            {
                                action = new ChartmakerTimelineDragLaneAction
                                {
                                    Targets = DraggingItem,
                                };
                                
                                history.ActionsBehind.Push(action);
                            }
                            action.Value = ToRoundedBeat(beatEnd - beatStart + DraggingItemOffset);
                            action.Redo();

                            break;
                        }
                        case BPMStop:
                        {
                            ChartmakerTimelineDragFloatAction action;
                            IChartmakerAction last = history.ActionsBehind.Count == 0 ? null : history.ActionsBehind.Peek();
                            if (last is ChartmakerTimelineDragFloatAction lastMove && Equals(lastMove.Targets, DraggingItem))
                            {
                                action = lastMove;
                                action.Undo();
                            } 
                            else 
                            {
                                action = new ChartmakerTimelineDragFloatAction {
                                    Targets = DraggingItem,
                                };
                                history.ActionsBehind.Push(action);
                            }
                            action.Value = timeEnd - timeStart + DraggingItemOffset;
                            action.Redo();

                            break;
                        }
                        default:
                        {
                            ChartmakerTimelineDragBeatPositionAction action;
                            var last = history.ActionsBehind.Count == 0 ? null : history.ActionsBehind.Peek();
                            if (last is ChartmakerTimelineDragBeatPositionAction lastMove && Equals(lastMove.Targets, DraggingItem))
                            {
                                action = lastMove;
                                action.Undo();
                            } 
                            else 
                            {
                                action = new ChartmakerTimelineDragBeatPositionAction {
                                    Targets = DraggingItem,
                                };
                                history.ActionsBehind.Push(action);
                            }
                            action.Value = ToRoundedBeat(beatEnd - beatStart + DraggingItemOffset);
                            action.Redo();

                            break;
                        }
                    }
                    history.ActionsAhead.Clear();
                    Chartmaker.main.OnHistoryDo();
                    Chartmaker.main.OnHistoryUpdate();
                } 
                else switch (dragMode)
                {
                    case TimelineDragMode.TimelineDrag:
                    {
                        float offset = Mathf.Clamp(-eventData.delta.x * (PeekRange.y - PeekRange.x) / TicksHolder.rect.width, limit.x - PeekRange.x, limit.y - PeekRange.y);
                        PeekRange.x += offset;
                        PeekRange.y += offset;

                        break;
                    }
                    case TimelineDragMode.Select:
                    {
                        SelectionRect.gameObject.SetActive(true);
                
                        if (CurrentMode is TimelineMode.Storyboard or TimelineMode.HitObjects)
                        {
                            SelectionRect.anchorMin = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, Mathf.Min(timeStart, timeEnd)), 0);
                            SelectionRect.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, Mathf.Max(timeStart, timeEnd)), 0);
                            SelectionRect.anchoredPosition = new (0, Mathf.Round(Mathf.Min(dragStart.y, dragEnd.y)));
                            SelectionRect.sizeDelta = new (0, Mathf.Round(Mathf.Abs(dragStart.y - dragEnd.y)));
                        }
                        else
                        {
                            SelectionRect.anchorMin = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, Mathf.Min(timeStart, timeEnd)), 0);
                            SelectionRect.anchorMax = new (InverseLerpUnclamped(PeekRange.x, PeekRange.y, Mathf.Max(timeStart, timeEnd)), 1);
                            SelectionRect.anchoredPosition = SelectionRect.sizeDelta = new (0, 0);
                        }

                        break;
                    }
                    case TimelineDragMode.Timeline:
                        Chartmaker.main.SongSource.time = Mathf.Clamp(metronome.ToSeconds(beatEnd), 0, Chartmaker.main.SongSource.clip.length);

                        if (PickerPanel.main.CurrentTimelinePickerMode is TimelinePickerMode.Cursor or TimelinePickerMode.Select or TimelinePickerMode.Delete)
                            break;
                        
                        switch (CurrentMode)
                        {
                            case TimelineMode.LaneSteps:
                            case TimelineMode.Timing:
                                if (isDragged)
                                    Previewer.gameObject.SetActive(false);

                                break;
                            case TimelineMode.Lanes:
                                // Only get a new tail if we don't have one
                                if (pseudoTail == null)
                                    pseudoTail = GetItemTail(-3280);
                                
                                PreviewerTail.gameObject.SetActive(true);

                                RectTransform tailRectTransform = pseudoTail.rectTransform;

                                // Convert world positions to local positions in ItemsHolder for normalization
                                Vector2 previewerLocalPos = ItemsHolder.InverseTransformPoint(initialPreviewersPosition);
                                Vector2 pointerLocalPos;

                                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                    ItemsHolder,
                                    eventData.position,
                                    eventData.pressEventCamera,
                                    out pointerLocalPos
                                );

                                // Get normalized positions
                                float previewerNormalizedX = Mathf.InverseLerp(0, ItemsHolder.rect.width, previewerLocalPos.x);
                                float pointerNormalizedX = Mathf.InverseLerp(0, ItemsHolder.rect.width, pointerLocalPos.x);

                                // Set anchors to stretch between the two points
                                float minX = Mathf.Min(previewerNormalizedX, pointerNormalizedX);
                                float maxX = Mathf.Max(previewerNormalizedX, pointerNormalizedX);

                                tailRectTransform.anchorMin = new Vector2(minX, 1);
                                tailRectTransform.anchorMax = new Vector2(maxX, 1);
                                tailRectTransform.sizeDelta = new Vector2(0, 20);
                                tailRectTransform.position  = new Vector3(tailRectTransform.position.x, Previewer.gameObject.transform.position.y, tailRectTransform.position.z);

                                if (eventData.position.x < initialPreviewersPosition.x)
                                {
                                    Previewer.gameObject.transform.position = new Vector3(eventData.position.x, Previewer.gameObject.transform.position.y, Previewer.gameObject.transform.position.z);
                                    PreviewerTail.gameObject.transform.position = initialPreviewersPosition;
                                }
                                else
                                {
                                    Previewer.gameObject.transform.position = initialPreviewersPosition;
                                    PreviewerTail.gameObject.transform.position = new Vector3(eventData.position.x, Previewer.gameObject.transform.position.y, Previewer.gameObject.transform.position.z); // Make sure it's aligned with Previewer
                                }
                                break;
                            
                            case TimelineMode.HitObjects:
                            case TimelineMode.Storyboard:
                                // Only get a new tail if we don't have one
                                if (pseudoTail == null)
                                    pseudoTail = GetItemTail(-3280);
                                
                                RectTransform hitTailRectTransform = pseudoTail.rectTransform;

                                // Convert world positions to local positions in ItemsHolder for normalization
                                Vector2 hitPreviewerLocalPos = ItemsHolder.InverseTransformPoint(initialPreviewersPosition);

                                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                    ItemsHolder,
                                    eventData.position,
                                    eventData.pressEventCamera,
                                    out pointerLocalPos
                                );

                                // Get normalized positions
                                float hitPreviewerNormalizedX = Mathf.InverseLerp(0, ItemsHolder.rect.width, hitPreviewerLocalPos.x);
                                float hitPointerNormalizedX = Mathf.InverseLerp(0, ItemsHolder.rect.width, pointerLocalPos.x);

                                // Set anchors to stretch between the two points
                                float hitMinX = Mathf.Min(hitPreviewerNormalizedX, hitPointerNormalizedX);
                                float hitMaxX = Mathf.Max(hitPreviewerNormalizedX, hitPointerNormalizedX);

                                hitTailRectTransform.anchorMin = new Vector2(hitMinX, 1);
                                hitTailRectTransform.anchorMax = new Vector2(hitMaxX, 1);
                                hitTailRectTransform.sizeDelta = new Vector2(0, PreviewerTail.GetComponent<RectTransform>().rect.height);;
                                hitTailRectTransform.position  *= new Vector3Frag(y: PreviewerTail.gameObject.transform.position.y);
                                
                                if (eventData.position.x < initialPreviewersPosition.x)
                                    PreviewerTail.gameObject.transform.position *= new Vector3Frag(x: eventData.position.x);
                                break;
                        }
                        
                        break;
                }
                return;
            }

            // Slider dragging

            if (localPos(TimeSliderHolder, out Vector2 localMousePos))
            {
                float sliderWidth = TimeSliderHolder.rect.width;
                time = ((localMousePos - dragStart).x / sliderWidth + TimeSliderHolder.pivot.x) * width + limit.x;
            }
            else
                return;
        
            switch (dragMode)
            {
                case TimelineDragMode.CurrentTime:
                {
                    if (chartmaker.SongSource.time == 0 && !chartmaker.SongSource.isPlaying)
                    {
                        chartmaker.SongSource.Play();
                        chartmaker.SongSource.Pause();
                    }
                    chartmaker.SongSource.timeSamples = (int)Mathf.Clamp(time * chartmaker.SongSource.clip.frequency, 0, chartmaker.SongSource.clip.samples - 1);
                    break;
                }
                case TimelineDragMode.PeekRange:
                {
                    float mid = (PeekRange.x + PeekRange.y) / 2;
                    float offset = Mathf.Clamp(time - mid, limit.x - PeekRange.x, limit.y - PeekRange.y);
                    PeekRange.x += offset;
                    PeekRange.y += offset;
                    break;
                }
                case TimelineDragMode.PeekStart:
                    PeekRange.x = Mathf.Clamp(time, limit.x, PeekRange.y); break;
                case TimelineDragMode.PeekEnd:
                    PeekRange.y = Mathf.Clamp(time, PeekRange.x, limit.y); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isDragged)
                return;

            Metronome metronome = Chartmaker.main.CurrentSong.Timing;
            if (eventData.button == PointerEventData.InputButton.Right)
                Chartmaker.main.SongSource.time = Mathf.Clamp(metronome.ToSeconds(beatStart), 0, Chartmaker.main.SongSource.clip.length);
            
            OnEndDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (DraggingItem != null) 
            {
                DraggingItem = null;
            }
            else
            {
                Previewer.gameObject.SetActive(false);
                PreviewerTail.gameObject.SetActive(false);

                if (pseudoTail != null)
                {
                    Destroy(pseudoTail.gameObject);
                    pseudoTail = null;
                }
                
                switch (dragMode)
                {
                    case TimelineDragMode.Select:
                    {
                        Metronome metronome = Chartmaker.main.CurrentSong.Timing;
                        float beatStart = metronome.ToBeat(Mathf.Min(timeStart, timeEnd));
                        float beatEnd = metronome.ToBeat(Mathf.Max(timeEnd, timeStart));

                        IList list = null;

                        switch (CurrentMode)
                        {
                            case TimelineMode.Storyboard:
                            {
                                if (InspectorPanel.main.CurrentObject is not Storyboardable thing)
                                    break;

                                TimestampType[] types = thing.timestampTypes;
                                Storyboard storyboard = thing.Storyboard;

                                int yStart = Mathf.FloorToInt(Mathf.Clamp((ItemsHolder.rect.height - Mathf.Max(dragStart.y, dragEnd.y) - 3) / 24, 0, TimelineHeight - 1)) + ScrollOffset;
                                int yEnd = Mathf.FloorToInt(Mathf.Clamp((ItemsHolder.rect.height - Mathf.Min(dragStart.y, dragEnd.y) - 3) / 24, 0, TimelineHeight - 1)) + ScrollOffset;

                                list = storyboard.Timestamps.FindAll(x =>
                                {
                                    int index = Array.FindIndex(types, y => x.ID == y.ID);
                                    return x.Offset >= beatStart && x.Offset <= beatEnd && index >= yStart && index <= yEnd;
                                });
                            }
                                break;

                            case TimelineMode.Timing:
                                list = Chartmaker.main.CurrentSong.Timing.Stops.FindAll(x => x.Offset >= timeStart && x.Offset <= timeEnd); break;
                            case TimelineMode.Lanes:
                            {
                                if (Chartmaker.main.CurrentChart == null)
                                    break;

                                list = GetLanesInTimeline().FindAll(x => x.LaneSteps[0].Offset >= beatStart && x.LaneSteps[0].Offset <= beatEnd);

                                break;
                            }

                            case TimelineMode.LaneSteps:
                            {
                                if (InspectorPanel.main.CurrentHierarchyObject is not Lane lane) break;

                                list = lane.LaneSteps.FindAll(x => x.Offset >= beatStart && x.Offset <= beatEnd);
                                break;
                            }

                            case TimelineMode.HitObjects:
                            {
                                if (InspectorPanel.main.CurrentHierarchyObject is not Lane lane)
                                    break;

                                float vpStart = .5f - VerticalScale * .5f + VerticalOffset;
                                float vpEnd = .5f + VerticalScale * .5f + VerticalOffset;

                                float yStart = Mathf.Lerp(vpStart, vpEnd, Mathf.Clamp01(1 - (Mathf.Max(dragStart.y, dragEnd.y) - 4) / (ItemsHolder.rect.height - 8)));
                                float yEnd = Mathf.Lerp(vpStart, vpEnd, Mathf.Clamp01(1 - (Mathf.Min(dragStart.y, dragEnd.y) - 4) / (ItemsHolder.rect.height - 8)));

                                list = lane.Objects.FindAll(x => x.Offset >= beatStart && x.Offset <= beatEnd && x.Position <= yEnd && x.Position + x.Length >= yStart);

                                break;
                            }
                        }

                        if (list?.Count >= 2)
                            InspectorPanel.main.SetObject(list);
                        else if (list?.Count == 1)
                            InspectorPanel.main.SetObject(list[0]);
                        break;
                    }
                    case TimelineDragMode.Timeline:
                    {
                        if (!Chartmaker.main.SongSource.isPlaying)
                        {
                            TimelinePickerMode pickMode = PickerPanel.main.CurrentTimelinePickerMode;
                    
                            Metronome metronome = Chartmaker.main.CurrentSong.Timing;

                            float density = (PeekRange.y - PeekRange.x) * metronome.GetStop(timeEnd, out _).BPM / TicksHolder.rect.width / 8;
                            float factor = Mathf.Floor(Mathf.Log(density, SeparationFactor));
                            float step = Mathf.Pow(SeparationFactor, factor + 1);
                            float beat = Mathf.Round(metronome.ToBeat(timeEnd) / step) * step;

                            switch (pickMode) 
                            {
                                case TimelinePickerMode.Timestamp:
                                {
                                    if (InspectorPanel.main.CurrentObject is not Storyboardable thing) 
                                        break;

                                    TimestampType[] types = thing.timestampTypes;
                                    Storyboard storyboard = thing.Storyboard;
                                    TimestampType type = types[Math.Clamp(Mathf.FloorToInt((ItemsHolder.rect.height - dragEnd.y - 3) / 24) + ScrollOffset, 0, types.Length - 1)];
                            
                                    Timestamp ts = new Timestamp {
                                        ID = type.ID,
                                        Offset = (BeatPosition)(isDragged ? Mathf.Min(beatStart, beatEnd) : beatStart),
                                        Duration = isDragged ? Mathf.Abs(beatStart - beatEnd) : 0,
                                        Target = type.StoryboardGetter(thing.GetStoryboardableObject(isDragged ? Mathf.Min(beatStart, beatEnd) : beatStart)),
                                    };
                                    if (storyboard.Timestamps.FindIndex(
                                            x => x.ID == ts.ID && (
                                                (x.Offset < ts.Offset + ts.Duration && ts.Offset < x.Offset + x.Duration)
                                                || (x.Duration == 0 && ts.Duration == 0 && Mathf.Approximately(x.Offset, ts.Offset))
                                            )
                                        ) < 0) Chartmaker.main.AddItem(ts);
                                }
                                    break;
                                case TimelinePickerMode.BPMStop:
                                {
                                    if (isDragged) break;

                                    BPMStop baseStop = Chartmaker.main.CurrentSong.Timing.GetStop(timeStart, out _);

                                    Chartmaker.main.AddItem(new BPMStop(baseStop.BPM, timeStart) { Signature = baseStop.Signature });

                                    break;
                                }
                                case TimelinePickerMode.Lane:
                                {
                                    Lane lane = new Lane
                                    {
                                        Position = new(0, -4, 0)
                                    };

                                    lane.LaneSteps.Add(new LaneStep
                                    {
                                        StartPointPosition = new(-8, 0),
                                        EndPointPosition = new(8, 0),
                                        Offset = (BeatPosition)(isDragged ? Math.Min(beatStart, beatEnd) : beatStart)
                                    });

                                    lane.LaneSteps.Add(new LaneStep
                                    {
                                        StartPointPosition = new(-8, 0),
                                        EndPointPosition = new(8, 0),
                                        Offset = (BeatPosition)(isDragged ? Math.Max(beatStart, beatEnd) : beatStart + 1),
                                    });

                                    Chartmaker.main.AddItem(lane);
                                    break;
                                }
                                case TimelinePickerMode.LaneStep:
                                {
                                    if (isDragged) break;

                                    if (InspectorPanel.main.CurrentHierarchyObject is not Lane lane) break;

                                    LanePosition basePos = ((Lane)lane.GetStoryboardableObject(timeEnd)).GetLanePosition(timeStart, timeStart, metronome);

                                    LaneStep baseStep = new()
                                    {
                                        StartPointPosition = basePos.StartPosition,
                                        EndPointPosition = basePos.EndPosition,
                                        Offset = (BeatPosition)(isDragged ? beatEnd : beatStart),
                                    };

                                    Chartmaker.main.AddItem(baseStep);
                                    break;
                                }
                                case TimelinePickerMode.NormalHit or TimelinePickerMode.CatchHit:
                                {
                                    HitObject hit = new();

                                    if (!isDragged)
                                    {
                                        dragEnd = dragStart;
                                        beatEnd = beatStart;
                                    }

                                    float vpStart = .5f - VerticalScale * .5f + VerticalOffset;
                                    float vpEnd = .5f + VerticalScale * .5f + VerticalOffset;
                                    float yStart = Mathf.Lerp(vpStart, vpEnd, Mathf.Round(Mathf.Clamp01(1 - (Mathf.Max(dragStart.y, dragEnd.y) - 4) / (ItemsHolder.rect.height - 8)) / .05f) * .05f);
                                    float yEnd = Mathf.Lerp(vpStart, vpEnd, Mathf.Round(Mathf.Clamp01(1 - (Mathf.Min(dragStart.y, dragEnd.y) - 4) / (ItemsHolder.rect.height - 8)) / .05f) * .05f);

                                    hit.Offset = (BeatPosition)Math.Min(beatStart, beatEnd);
                                    hit.HoldLength = Math.Abs(beatStart - beatEnd);
                                    hit.Length = Options.NewHitObjectLength;
                                    hit.Position = yEnd - hit.Length / 2;

                                    hit.Type = PickerPanel.main.CurrentTimelinePickerMode == TimelinePickerMode.CatchHit 
                                        ? HitObject.HitType.Catch : HitObject.HitType.Normal;

                                    Chartmaker.main.AddItem(hit);

                                    break;
                                }
                            }
                        }

                        break;
                    }
                }
                
            }

            isDragged = false;
            dragMode = TimelineDragMode.None;
            SelectionRect.gameObject.SetActive(false);
        }

        public void ShowEditHistory()
        {
            ChartmakerHistory history = Chartmaker.main.History;
            ContextMenuList list = new();
            IChartmakerAction[] ahead = history.ActionsAhead.ToArray();
            IChartmakerAction[] behind = history.ActionsBehind.ToArray();

            if (ahead.Length == 0 && behind.Length == 0)
            {
                list.Items.Add(new ContextMenuListAction("No edit history", () => {}, _enabled: false));
            }
            else 
            {
                if (ahead.Length != 0) for (int a = Mathf.Min(ahead.Length, 10) - 1; a >= 0; a--)
                {
                    int A = a;
                    list.Items.Add(new ContextMenuListAction(ahead[a].GetName(), () => Chartmaker.main.Redo(A + 1), icon: "Redo"));
                }
                else 
                    list.Items.Add(new ContextMenuListAction("Nothing to Redo", () => {}, _enabled: false));

                list.Items.Add(new ContextMenuListSeparator());

                if (behind.Length != 0) for (int a = 0; a < Mathf.Min(behind.Length, 10); a++)
                {
                    int A = a;
                    list.Items.Add(new ContextMenuListAction(behind[a].GetName(), () => Chartmaker.main.Undo(A + 1), icon: "Undo"));
                }
                else 
                    list.Items.Add(new ContextMenuListAction("Nothing to Undo", () => {}, _enabled: false));
            }

            ContextMenuHolder.main.OpenRoot(list, EditHistoryHolder, ContextMenuDirection.Up);
        }

        public void OnResizerDrag()
        {
            ResizeTimeline(Input.mousePosition.y, false);
        }
        public void OnResizerEndDrag()
        {
            ResizeTimeline(Input.mousePosition.y);
        }

        public float SnapTimeline(float height)
        {
            return height < 72 ? 40 : Mathf.Max(Mathf.Round((height - 80) / 24) * 24 + 80, 104);
        }
    
        public void ResizeTimeline(float height, bool snap = true)
        {
            float maxHeight = SnapTimeline(Screen.height * 0.5f);
            height = Mathf.Round(Mathf.Clamp(height, 40, maxHeight));
        
            if (snap)
                height = SnapTimeline(height);
       
            Chartmaker.main.TimelineHolder.anchoredPosition = new(
                Chartmaker.main.TimelineHolder.sizeDelta.x, 
                -Mathf.Pow(Mathf.Max(106 - height, 0) / 64, 2) * 32
            );
       
            Chartmaker.main.TimelineHolder.sizeDelta = new (
                Chartmaker.main.TimelineHolder.sizeDelta.x, 
                height - Chartmaker.main.TimelineHolder.anchoredPosition.y
            );
       
            Chartmaker.main.MainViewHolder.sizeDelta = new (Chartmaker.main.MainViewHolder.sizeDelta.x, - 33 - height);
        
            Chartmaker.main.PickerHolder.sizeDelta = new (
                Chartmaker.main.PickerHolder.sizeDelta.x, 
                -32 - Chartmaker.main.TimelineHolder.anchoredPosition.y + Chartmaker.main.PickerHolder.anchoredPosition.y
            );
      
            CurrentTimeCoonectorGroup.alpha = PeekSliderGroup.alpha = BlockerTextGroup.alpha =
                1 + Chartmaker.main.TimelineHolder.anchoredPosition.y / 32;
       
            TimelineHeight = height <= 40 ? 0 : Mathf.Max(Mathf.RoundToInt((height - 80) / 24), 1);
      
            if (snap) 
            {
                if (TimelineHeight > 0) TimelineExpandHeight = TimelineHeight;
                UpdateTabs();
            }
      
            UpdateTimeline(true);
            UpdateScrollbar();
      
            PlayerView.main.Update();
            PlayerView.main.UpdateObjects();
        }
    
        public void Collapse()
        {
            TimelineRestoreHeight = TimelineHeight;
            ResizeTimeline(40);
        }
    
        public void Restore()
        {
            if (TimelineHeight <= 0) ResizeTimeline(TimelineRestoreHeight * 24 + 80);
            PlayerView.main.IsMaximised = false;
        }

        public void OnScroll(PointerEventData eventData)
        {
            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool isCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool isAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            Chartmaker chartmaker = Chartmaker.main;

            // Ctrl+Shift modifier = Vertical zoom
            if (isCtrl && isShift)
            {
                if (CurrentMode == TimelineMode.HitObjects)
                {
                    float zoom = Mathf.Pow(2, ResizeVelocity * -eventData.scrollDelta.y / 10f);
                    Options.VerticalScale = VerticalScale *= zoom;
                    Options.UpdateFields();
                }
                UpdateTimeline(true);
            }
            // Shift modifier = Vertical scroll
            else if (isShift)
            {
                if (CurrentMode == TimelineMode.HitObjects)
                {
                    Options.VerticalOffset = VerticalOffset += -eventData.scrollDelta.y * VerticalScale / 10;
                    Options.UpdateFields();
                }
                else 
                {
                    ScrollOffset = Mathf.Max(Mathf.Min(ScrollOffset + (int)Mathf.Sign(-eventData.scrollDelta.y), ItemHeight - TimelineHeight), 0);
                }
                UpdateTimeline(true);
                UpdateScrollbar();
            }
            // Ctrl modifier = Horizontal zoom
            else if (isCtrl)
            {
                float zoom = Mathf.Pow(2, ResizeVelocity * -eventData.scrollDelta.y / 10f);
                float center = GetPointerTimeAtTimeline(eventData);
                float currentXRange = PeekRange.x - (center - PeekRange.x) * (zoom - 1);
                float currentYRange = PeekRange.y - (center - PeekRange.y) * (zoom - 1);

                UnityEngine.Debug.Log($"{PeekRange.x} -> {currentXRange}, {PeekRange.y} -> {currentYRange}");

                PeekRange.x = Mathf.Clamp(currentXRange, PeekLimit.x, PeekRange.y);
                PeekRange.y = Mathf.Clamp(currentYRange, PeekRange.x, PeekLimit.y);
            }
            // Alt modifier = Seek current time
            else if (isAlt || (Options.FollowSeekLine && chartmaker.SongSource.isPlaying))
            {

                Metronome metronome = chartmaker.CurrentSong.Timing;
                float bpm = metronome.GetStop(chartmaker.SongSource.time, out _).BPM;
                float density = (PeekRange.y - PeekRange.x) * bpm / TicksHolder.rect.width / 8;
                float factor = Mathf.Floor(Mathf.Log(density, SeparationFactor));
                float step = Mathf.Pow(SeparationFactor, factor + 1);

                float time = chartmaker.SongSource.time + (-eventData.scrollDelta.y * step / bpm * 240);
                if (chartmaker.SongSource.time == 0 && !chartmaker.SongSource.isPlaying)
                {
                    chartmaker.SongSource.Play();
                    chartmaker.SongSource.Pause();
                }
                chartmaker.SongSource.time = Mathf.Clamp(time, 0, chartmaker.SongSource.clip.length);
            }
            // No modifier = Horizontal scroll
            else
            {
                float offset = Mathf.Clamp(
                    (PeekRange.y - PeekRange.x) / TicksHolder.rect.width * 50 * -eventData.scrollDelta.y,
                    PeekLimit.x - PeekRange.x,
                    PeekLimit.y - PeekRange.y
                );

                PeekRange.x += offset;
                PeekRange.y += offset;
            }
        }

        public float GetPointerTimeAtTimeline(PointerEventData eventData)
        {
            Chartmaker chartmaker = Chartmaker.main;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(TimeSliderHolder, eventData.position, eventData.pressEventCamera, out Vector2 mousePosition))
            {
                return Mathf.Lerp(PeekRange.x, PeekRange.y, mousePosition.x / ItemsHolder.rect.width + .5f);
            }
            else
            {
                return chartmaker.SongSource.time;
            }
        }

        public void RightClickItem(TimelineItem item)
        {
            static string KeyOf(string id) => KeyboardHandler.main.Keybindings[id].Keybind.ToString();

            if (item.Lane != null) InspectorPanel.main.SetObject(item.Lane);
            InspectorPanel.main.SetObject(item.Item);
            ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                new ContextMenuListAction("Cut", Chartmaker.main.Cut, KeyOf("ED:Cut"), 
                    icon: "Cut", _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListAction("Copy", Chartmaker.main.Copy, KeyOf("ED:Copy"), 
                    icon: "Copy", _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListAction("Paste <i>" + (Chartmaker.main.CanPaste() ? Chartmaker.GetItemName(Chartmaker.main.ClipboardItem) : ""), Chartmaker.main.Paste, KeyOf("ED:Paste"), 
                    icon: "Paste", _enabled: Chartmaker.main.CanPaste()),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Delete", () => KeyboardHandler.main.Keybindings["ED:Delete"].Invoke(), KeyOf("ED:Delete"), 
                    _enabled: Chartmaker.main.CanCopy())
            ), (RectTransform)item.transform, ContextMenuDirection.Cursor);
        }

        public void SelectAdjacent(int direction)
        {
            IList targetList = InspectorPanel.main.CurrentTimestamp?.Count > 0 
                ? ((Storyboardable)InspectorPanel.main.CurrentObject).Storyboard.Timestamps : InspectorPanel.main.CurrentObject switch 
                {
                    BPMStop => Chartmaker.main.CurrentSong.Timing.Stops,
                    Lane => GetLanesInTimeline(),
                    LaneStep => ((Lane)InspectorPanel.main.CurrentHierarchyObject).LaneSteps,
                    HitObject => ((Lane)InspectorPanel.main.CurrentHierarchyObject).Objects,
                    _ => null,
                };
        
            if (targetList == null) 
                return;

            object currentObj = InspectorPanel.main.CurrentTimestamp?.Count > 0 
                ? InspectorPanel.main.CurrentTimestamp : InspectorPanel.main.CurrentObject;
      
            if (currentObj is IList curObjList) 
                currentObj = curObjList[direction > 0 ? ^1 : 0];

            int index = targetList.IndexOf(currentObj) + direction;
        
            if (index >= 0 && index < targetList.Count) 
                InspectorPanel.main.SetObject(targetList[index]);
        }
    }

    public enum TimelineMode
    {
        Storyboard,
        Timing,
        Lanes,
        LaneSteps,
        HitObjects,
    }

    public enum TimelineDragMode
    {
        None = 0,

        CurrentTime = 2,
        PeekRange   = 4,
        PeekStart   = 6,
        PeekEnd     = 8,

        TimelineDrag = 1,
        Timeline     = 3,
        Select       = 5,
        ItemDrag     = 7,
    }

    public enum FrequencyScale
    {
        Linear,
        Logarithmic,
        Mel,
        Bark,
    }

    public enum LaneFilterMode 
    {
        All,
        HierarchyVisible,
    }
}