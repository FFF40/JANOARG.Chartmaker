using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JANOARG.Chartmaker.Constants;
using JANOARG.Chartmaker.Data.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker.Actions;
using JANOARG.Chartmaker.UI;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Themeable;
using JANOARG.Chartmaker.UI.Timeline;
using JANOARG.Chartmaker.Utils;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Shared.Utils;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using FFTWindow = JANOARG.Chartmaker.Utils.FFTWindow;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class TimelinePanel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {

        #region Fields

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
        public RawImage TicksImage;
        [Space]
        public RawImage WaveformImage;
        public RawImage DensityGraphImage;
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
        [HideInInspector] public List<TimelineTick> Ticks;
        public TimelineItem ItemSample;
        [HideInInspector] public List<TimelineItem> Items;
        public Image ItemTailSample;
        [HideInInspector] public List<Image> ItemTails;
        public TMP_Text LabelSample;
        [HideInInspector] public List<TMP_Text> Labels;
        public LineGraph GraphSample;
        [HideInInspector] public List<LineGraph> Graphs;
    
        public TMP_Text StoryboardEntrySample;
        public Material StoryboardEntryMaterial;
        public Material WaveformMaterial;
        
        [HideInInspector] public List<TMP_Text> StoryboardEntries;

        [HideInInspector] [SerializeField] private TMP_Text StoryboardText;
    
        [Header("Icons")]
        public Sprite LineIcon;
        public Sprite BehindIcon;
        public Sprite NormalHitIcon;
        public Sprite CatchHitIcon;

        public int TimelineHeight { get; private set; } = 8;
        int          TimelineRestoreHeight = 8;
        int          ItemHeight            = 0;
        bool         lastPlayed;
        public IList DraggingItem;
        float        DraggingItemOffset;
        

        #endregion

        #region Unity Events

        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            UpdateTabs();
            UpdateScrollbar();
            Options.OnEnable();
            UpdateTimeline(true);
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
            
            // Upload completed bake result to GPU on the main thread
            if (_bakeReady && _bakeDstTexture != null)
            {
                _bakeReady = false;
                _bakeDstTexture.SetPixels(_bakeResultBuffer);
                _bakeDstTexture.Apply(false, false);
                // Destroy old texture and swap in the freshly baked one
                if (WaveformImage.texture != _bakeDstTexture)
                {
                    Destroy(WaveformImage.texture);
                    WaveformImage.texture = _bakeDstTexture;
                }
                _bakeDstTexture = null;
            }

            UpdateTimeline();
        }

        #endregion

        #region Tabs

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
            
            if (TimelineRestoreHeight != TimelineHeight) 
            {
                ResizeTimeline(TimelineRestoreHeight * 24 + 80);
            }
            else 
            {
                UpdateTabs();
                UpdateItems();
            }

        }

        #endregion

        #region Update Timeline

        public void UpdateTimeline(bool forced = false)
        {

            if (
                densityGraphDirtyTimer <= 0 
                    && DensityGraphImage.texture 
                    && DensityGraphImage.texture.width != (int)DensityGraphImage.rectTransform.rect.width
            )
            {
                // If I comment this line the waveform discards when resized 
                // in the Unity editor (desired behavior) but not in the build
                // TODO research
                DiscardWaveform();
                SetDensityGraphDirty(0.1f);
            }

            if (lastLimit != PeekRange || lastPlayed != Chartmaker.main.SongSource.isPlaying || forced)
            {
                lastLimit = PeekRange;
                lastPlayed = Chartmaker.main.SongSource.isPlaying;
                Metronome metronome = Chartmaker.main.CurrentSong.Timing;

                UpdateTickTexture(metronome);

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

            if (densityGraphDirty)
            {
                densityGraphDirtyTimer -= Time.deltaTime;
                if (densityGraphDirtyTimer <= 0)
                {
                    UpdateDensityGraph();
                }
            }
        }

        #endregion

        #region Object Pool
        TimelineItem GetTimelineItem(int index)
        {
            TimelineItem item;
        
            if (Items.Count <= index)
                Items.Add(item = Instantiate(ItemSample, ItemsHolder));
            else
            {
                item = Items[index];
                if (!item.gameObject.activeSelf)
                    item.gameObject.SetActive(true);
            }
        
            return item;
        }
        Image GetItemTail(int index)
        {
            // Special case for previewer
            if (index == -3280)
                return Instantiate(ItemTailSample, TailsHolder);

            Image item;

            if (ItemTails.Count <= index)
                ItemTails.Add(item = Instantiate(ItemTailSample, TailsHolder));
            else
            {
                item = ItemTails[index];
                if (!item.gameObject.activeSelf)
                    item.gameObject.SetActive(true);
            }
        
            return item;
        }
        TMP_Text GetItemLabel(int index)
        {
            TMP_Text item;
        
            if (Labels.Count <= index)
                Labels.Add(item = Instantiate(LabelSample, LabelsHolder));
            else
            {
                item = Labels[index];
                if (!item.gameObject.activeSelf)
                    item.gameObject.SetActive(true);
            }
        
            return item;
        }
        LineGraph GetItemGraph(int index)
        {
            LineGraph item;
        
            if (Graphs.Count <= index)
                Graphs.Add(item = Instantiate(GraphSample, GraphsHolder));
            else
            {
                item = Graphs[index];
                if (!item.gameObject.activeSelf)
                    item.gameObject.SetActive(true);
            }
        
            return item;
        }
        TMP_Text GetStoryboardEntry(int index)
        {
            TMP_Text item;
        
            if (StoryboardEntries.Count <= index)
                StoryboardEntries.Add(item = Instantiate(StoryboardEntrySample, StoryboardEntryHolder));
            else
            {
                item = StoryboardEntries[index];
                if (!item.gameObject.activeSelf)
                    item.gameObject.SetActive(true);
            }
        
            return item;
        }

        #endregion

        #region Items

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

            StoryboardText.alpha = InspectorPanel.main.CurrentObject == null || InspectorPanel.main.CurrentHierarchyObject is not Storyboardable ? 0.5f : 1f;

            for (int i = count; i < Items.Count; i++)
                if (Items[i].gameObject.activeSelf)
                    Items[i].gameObject.SetActive(false);

            for (int i = tailCount; i < ItemTails.Count; i++)
                if (ItemTails[i].gameObject.activeSelf)
                    ItemTails[i].gameObject.SetActive(false);

            for (int i = labelCount; i < Labels.Count; i++)
                if (Labels[i].gameObject.activeSelf)
                    Labels[i].gameObject.SetActive(false);

            for (int i = graphCount; i < Graphs.Count; i++)
                if (Graphs[i].gameObject.activeSelf)
                    Graphs[i].gameObject.SetActive(false);

            for (int i = sbcount; i < StoryboardEntries.Count; i++)
                if (StoryboardEntries[i].gameObject.activeSelf)
                    StoryboardEntries[i].gameObject.SetActive(false);
        
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

        // tickTime describes the left edge of the buffer in seconds.
        // tickViewportWidth is the visible pixel width; texWidth = 9 * tickViewportWidth.
        // Reconstruction triggers when the viewport has consumed more than 62% of
        // the available margin on either side (i.e. drifted > 2.17× viewport widths
        // from the buffer centre).
        const int   TickBufferMultiplier    = 9;   // total buffer = this × viewport
        const int   TickBufferHalfPad       = 4;   // padding on each side in viewport widths
        const float TickReconstructThreshold = 0.62f;
        const int   TickGradientHeight      = 128;  // texture rows; gradient fades bottom→top

        int    tickViewportWidth = 0;
        float  tickTime, tickLastDensity = 0;

        void UpdateTickTexture(Metronome metronome)
        {
            if (TicksImage == null || Mathf.Approximately(PeekRange.x, PeekRange.y) || metronome.Stops.Count == 0)
            {
                if (TicksImage != null) TicksImage.enabled = false;
                HideAllTicks();
                return;
            }
            
            if (TicksHolder.rect.width <= 0) return;

            if (!TicksImage.enabled)
                TicksImage.enabled = true;

            int vpWidth  = Mathf.Max(1, (int)TicksHolder.rect.width);
            int texWidth = Mathf.Min(vpWidth * TickBufferMultiplier, SystemInfo.maxTextureSize);

            float density = (PeekRange.y - PeekRange.x) * metronome.GetStop(PeekRange.x, out _).BPM / vpWidth / 8;

            // step = seconds per viewport pixel
            float step = (PeekRange.y - PeekRange.x) / vpWidth;

            // Left edge of the viewport in buffer-column space
            float viewportLeftSec = PeekRange.x;

            Texture2D texture = TicksImage.texture as Texture2D;

            // Viewport pixel width changed → full rebuild
            if (tickViewportWidth != vpWidth)
            {
                texture = null;
                tickViewportWidth = vpWidth;
            }

            // Density changed significantly → full rebuild
            if (density == 0 || !(Math.Abs(tickLastDensity / density - 1) < 0.0001f))
                texture = null;

            if (!texture || texture.width != texWidth)
            {
                Destroy(TicksImage.texture);
                TicksImage.texture = texture = new Texture2D(texWidth, TickGradientHeight, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode   = TextureWrapMode.Repeat,
                };
                tickLastDensity = density;
                // Centre the buffer on the current viewport
                tickTime   = viewportLeftSec - step * vpWidth * TickBufferHalfPad;
                tickBakeAll(texture, texWidth, step, metronome);
                UpdateTickLabels(metronome, Mathf.Log(density, SeparationFactor), Themer.main.Keys["TimelineTickMain"]);
                return;
            }

            // How many buffer columns does the viewport left edge currently occupy?
            int viewportLeftCol = Mathf.RoundToInt((viewportLeftSec - tickTime) / step);

            // Reconstruct if viewport has drifted past the threshold on either side.
            // Available margin on the left  = viewportLeftCol columns
            // Available margin on the right = texWidth - (viewportLeftCol + vpWidth) columns
            float reconstructMargin = TickBufferHalfPad * vpWidth * TickReconstructThreshold;
            if (viewportLeftCol < reconstructMargin || viewportLeftCol + vpWidth > texWidth - reconstructMargin)
            {
                // Recentre buffer on current viewport
                tickTime   = viewportLeftSec - step * vpWidth * TickBufferHalfPad;
                tickBakeAll(texture, texWidth, step, metronome);
                UpdateTickLabels(metronome, Mathf.Log(density, SeparationFactor), Themer.main.Keys["TimelineTickMain"]);
                return;
            }

            // Viewport is well inside the buffer — just update the UV rect, no texture writes needed.
            float uvLeft = (float)viewportLeftCol / texWidth;
            float uvSize = (float)vpWidth / texWidth;
            TicksImage.uvRect = new Rect(uvLeft, 0f, uvSize, 1f);

            UpdateTickLabels(metronome, Mathf.Log(density, SeparationFactor), Themer.main.Keys["TimelineTickMain"]);
        }

        Color[] _tickPixelBuffer;

        // Full bake of the entire buffer texture. Called on construction or recentre.
        void tickBakeAll(Texture2D texture, int texWidth, float step, Metronome metronome)
        {
            int   vpWidth = tickViewportWidth;
            float density = tickLastDensity;
            float factor  = Mathf.Log(density, SeparationFactor);
            int   texHeight = TickGradientHeight;

            int needed = texWidth * texHeight;
            if (_tickPixelBuffer == null || _tickPixelBuffer.Length != needed)
                _tickPixelBuffer = new Color[needed];
            Color[] pixels = _tickPixelBuffer;
            System.Array.Clear(pixels, 0, needed);

            float bufferStartSec = tickTime;
            float bufferEndSec   = tickTime + texWidth * step;

            BeatPosition beat     = BeatFloor(metronome.ToBeat(bufferStartSec), Mathf.FloorToInt(factor), SeparationFactor);
            BeatPosition interval = BeatInterval(Mathf.FloorToInt(factor), SeparationFactor);
            float        end      = metronome.ToBeat(bufferEndSec);
            int          drawn    = 0;

            while (beat < end && drawn <= 9000)
            {
                float beatSec = metronome.ToSeconds(beat);
                int   col     = Mathf.RoundToInt((beatSec - tickTime) / step);

                if (col >= 0 && col < texWidth)
                {
                    float beatDensity = GetSeparationFactor(beat, SeparationFactor) - factor;
                    float alpha       = Mathf.Clamp01((Mathf.Pow(1.5f, beatDensity) - 1) / (Mathf.Pow(1.5f, 3) - 1)) * .5f;
                    Color baseColor   = GetBeatColor(beat) * new Color(1, 1, 1, alpha);

                    // Gradient simulation in legacy tick renderer
                    for (int y = 0; y < texHeight; y++)
                    {
                        float t = (float)y / (texHeight - 1);
                        pixels[y * texWidth + col] = Color.Lerp(baseColor, Color.clear, t * t);
                    }
                }

                beat += interval;
                drawn++;
            }

            texture.SetPixels(0, 0, texWidth, texHeight, pixels);
            texture.Apply(false, false);

            // Set UV to current viewport position within the freshly baked buffer
            int   viewportLeftCol = Mathf.RoundToInt((PeekRange.x - tickTime) / step);
            float uvLeft = (float)viewportLeftCol / texWidth;
            float uvSize = (float)vpWidth / texWidth;
            TicksImage.uvRect = new Rect(uvLeft, 0f, uvSize, 1f);
        }

        const int MaxTickLabels = 50;

        readonly Dictionary<BeatPosition, string> _beatStringCache = new();

        string GetBeatString(BeatPosition beat)
        {
            if (!_beatStringCache.TryGetValue(beat, out string s))
                _beatStringCache[beat] = s = beat.ToString();
            return s;
        }

        void UpdateTickLabels(Metronome metronome, float factor, Color labelColor)
        {
            int labelCount = 0;

            BeatPosition beat     = BeatFloor(metronome.ToBeat(PeekRange.x), Mathf.FloorToInt(factor), SeparationFactor);
            BeatPosition interval = BeatInterval(Mathf.FloorToInt(factor), SeparationFactor);
            float        end      = metronome.ToBeat(PeekRange.y);

            while (beat < end && labelCount < MaxTickLabels)
            {
                float beatDensity = GetSeparationFactor(beat, SeparationFactor) - factor;
                float labelAlpha  = Mathf.Clamp01(beatDensity - 2.5f) * .5f;

                if (labelAlpha > 0)
                {
                    TimelineTick tick;
                    if (Ticks.Count <= labelCount)
                    {
                        tick = Instantiate(TickSample, TicksHolder);
                        Ticks.Add(tick);
                    }
                    else
                    {
                        tick = Ticks[labelCount];
                        if (!tick.gameObject.activeSelf)
                            tick.gameObject.SetActive(true);
                    }

                    RectTransform rt = (RectTransform)tick.transform;
                    float normX = (metronome.ToSeconds(beat) - PeekRange.x) / (PeekRange.y - PeekRange.x);
                    rt.anchorMin = new(normX, 0f);
                    rt.anchorMax = new(normX, 1f);

                    tick.Image.color = Color.clear;
                    tick.Label.color = labelColor;
                    tick.Label.alpha = labelAlpha;
                    tick.Label.text  = GetBeatString(beat);

                    labelCount++;
                }

                beat += interval;
            }

            for (int i = labelCount; i < Ticks.Count; i++)
                if (Ticks[i].gameObject.activeSelf)
                    Ticks[i].gameObject.SetActive(false);

            // Prune pool back to a reasonable size to avoid accumulation from rapid scroll
            int poolCap = MaxTickLabels + 8;
            while (Ticks.Count > poolCap)
            {
                Destroy(Ticks[^1].gameObject);
                Ticks.RemoveAt(Ticks.Count - 1);
            }
        }

        void HideAllTicks()
        {
            foreach (var t in Ticks)
                if (t.gameObject.activeSelf)
                    t.gameObject.SetActive(false);
        }

        public void DiscardTickTexture()
        {
            if (TicksImage == null) return;
            Destroy(TicksImage.texture);
            TicksImage.texture = null;
            tickLastDensity    = 0;
            tickViewportWidth  = 0;
        }

        // Flat sbyte[] audio cache populated once on clip load.
        // Interleaved: index = sample * channels + channel, range [-127, 127].
        sbyte[] waveCache;
        int     waveCacheChannels;
        int     waveCacheFrequency;

        struct WaveformStats { public float min, max, rmsSqSum; }
        WaveformStats[][] _waveMipChain; // Tiered stats for faster baking

        public void CacheWaveformData()
        {
            AudioClip clip = Chartmaker.main.SongSource.clip;
            if (clip == null) { waveCache = null; _waveMipChain = null; return; }

            int channels  = clip.channels;
            int samples   = clip.samples;
            int totalSamples = samples * channels;
            const int chunkSamples = 44100 * 2; // 1 second of stereo at 44.1kHz

            waveCache          = new sbyte[totalSamples];
            waveCacheChannels  = channels;
            waveCacheFrequency = clip.frequency;

            float[] chunk = new float[chunkSamples];
            int written = 0;
            while (written < samples)
            {
                int count = Mathf.Min(chunkSamples / channels, samples - written);
                clip.GetData(chunk, written);
                int end = written * channels + count * channels;
                for (int i = written * channels; i < end; i++)
                    waveCache[i] = (sbyte)Mathf.RoundToInt(Mathf.Clamp(chunk[i - written * channels], -1f, 1f) * 127f);
                written += count;
            }

            // Generate MipChain lazily in background — only build level 0 upfront,
            // extend to deeper levels on demand when UpdateWaveform requests them.
            Task.Run(() =>
            {
                sbyte[] localCache = waveCache;
                if (localCache == null) return;

                int baseSize = 64;
                int numMips  = 10;
                var mipChain = new WaveformStats[numMips][];

                // Level 0 only — deeper levels built on demand via ExtendMipChain
                int count0 = samples / baseSize;
                mipChain[0] = new WaveformStats[count0 * channels];

                for (int i = 0; i < count0; i++)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        float min = 1f, max = -1f, rmsSqSum = 0f;
                        int start = i * baseSize * channels + ch;
                        for (int s = 0; s < baseSize; s++)
                        {
                            float val = localCache[start + s * channels] / 127f;
                            if (val < min) min = val;
                            if (val > max) max = val;
                            rmsSqSum += val * val;
                        }
                        mipChain[0][i * channels + ch] = new WaveformStats { min = min, max = max, rmsSqSum = rmsSqSum };
                    }
                }

                _waveMipChain = mipChain;
            });

            DiscardWaveform();
            UpdateTimeline(true);
        }

        // Read-ahead waveform buffer: dynamically sized to cover WaveTargetBufferSeconds.
        const int   WaveBufferHalfPadMin     = 4;   // minimum pad at low zoom
        const int   WaveBufferHalfPadMax     = 16;  // maximum pad at high zoom
        const float WaveReconstructThreshold = 0.62f;
        const float WaveTargetBufferSeconds  = 60f; // aim for this many seconds of buffer total

        // Returns buffer half-pad scaled by zoom: wider at high zoom (small step) so
        // rapid pixel-level scrolling triggers fewer recentres per second.
        int ComputeWaveBufferHalfPad(float step)
        {
            // step ~ seconds per pixel; high zoom = small step
            // clamp between 1/freq (max zoom) and ~0.01 (low zoom threshold)
            float t = Mathf.InverseLerp(0.001f, 0.02f, step); // 0 = high zoom, 1 = low zoom
            return Mathf.RoundToInt(Mathf.Lerp(WaveBufferHalfPadMax, WaveBufferHalfPadMin, t));
        }

        int   waveViewportWidth  = 0;
        int   waveViewportHeight = 0;
        float waveTime, waveStep, waveViewStep, waveLastDensity = 0;

        volatile bool _bakeInFlight  = false;
        volatile bool _bakeReady     = false;
        Texture2D     _bakeDstTexture;
        Color[]       _bakeResultBuffer;

        int ComputeWaveTexWidth(int vpWidth, float step)
        {
            int halfPad    = ComputeWaveBufferHalfPad(step);
            int targetCols = Mathf.RoundToInt(WaveTargetBufferSeconds / step);
            int minCols    = vpWidth * (halfPad * 2 + 1);
            int preferred  = Mathf.Max(targetCols, minCols);
            return Mathf.Clamp(preferred, vpWidth, SystemInfo.maxTextureSize);
        }

        #endregion

        #region Waveform

        public void UpdateWaveform()
        {
            if (
                TimelineHeight <= 0
                || Mathf.Approximately(PeekRange.y, PeekRange.x)
                || Options.WaveformMode == 0
                || waveCache == null
            )
            {
                WaveformImage.enabled = false;
                return;
            }

            // Optional: Hide when playing if WaveformIdle is configured that way
            // But ALWAYS show if we are interacting (zooming/scrolling)
            if (!isDragged && lastLimit == PeekRange && Options.WaveformIdle < (Chartmaker.main.SongSource.isPlaying ? 1 : 0))
            {
                 // But keep it visible if we already have a texture and aren't moving
                 if (!WaveformImage.enabled) return;
            }

            if (!WaveformImage.enabled)
                WaveformImage.enabled = true;

            Color color = Themer.main.Keys["TimelineTickMain"];

            RectTransform waveRT = WaveformImage.rectTransform;
            int vpWidth  = Mathf.Max(1, (int)waveRT.rect.width);
            int vpHeight = Mathf.Max(1, (int)waveRT.rect.height);

            if (waveRT.rect.width <= 0 || waveRT.rect.height <= 0) return;

            float step    = (PeekRange.y - PeekRange.x) / vpWidth;
            float density = waveCacheFrequency * step;
            int   texWidth = ComputeWaveTexWidth(vpWidth, step);

            // Shader optimization: Waveform mode needs 1px height per channel
            int texHeight = Options.WaveformMode == 1 ? Mathf.Max(1, waveCacheChannels) : vpHeight;

            Texture2D texture = WaveformImage.texture as Texture2D;

            if (waveViewportWidth != vpWidth || waveViewportHeight != vpHeight)
            {
                texture = null;
                waveViewportWidth  = vpWidth;
                waveViewportHeight = vpHeight;
            }

            // Naive refresh on zoom: invalidate texture if density changes significantly
            if (!(Math.Abs(waveLastDensity / density - 1) < 0.0001f))
                texture = null;

            // Always track current viewport step for LOD — independent of bake cadence
            waveViewStep = step;

            if (!texture || texture.height != texHeight)
            {
                // Bake into a staging texture — keep old texture on WaveformImage until
                // _bakeReady fires, so zoom shows a lo-res stretched hold instead of blank.
                Texture2D stagingTex = new Texture2D(texWidth, texHeight, TextureFormat.RGBAHalf, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode   = TextureWrapMode.Clamp,
                };
                waveLastDensity = density;
                waveStep        = step;
                waveTime        = PeekRange.x - step * vpWidth * ComputeWaveBufferHalfPad(step);
                TriggerWaveBake(stagingTex, texWidth, texHeight, step, color);
            }
            else
            {
                // Simple margin check for re-centering
                int   viewportLeftCol   = Mathf.RoundToInt((PeekRange.x - waveTime) / waveStep);
                float reconstructMargin = ComputeWaveBufferHalfPad(step) * vpWidth * WaveReconstructThreshold;

                if (viewportLeftCol < reconstructMargin || viewportLeftCol + vpWidth > texWidth - reconstructMargin)
                {
                    // Recentre
                    waveTime = PeekRange.x - step * vpWidth * ComputeWaveBufferHalfPad(step);
                    waveStep = step;
                    TriggerWaveBake(texture, texWidth, texHeight, step, color);
                }
            }

            // Duration-based UV mapping: scales perfectly even during rapid zooming
            float texDuration = waveStep * texWidth;
            float uvLeft = (PeekRange.x - waveTime) / texDuration;
            float uvSize = (PeekRange.y - PeekRange.x) / texDuration;
            WaveformImage.uvRect = new Rect(uvLeft, 0f, uvSize, 1f);
            
            // Update waveform properties based on timeline zoom
            if (WaveformImage.material != null)
            {
                WaveformImage.material.SetFloat("_Channels", waveCacheChannels);
                WaveformImage.material.SetFloat("_Thickness", 1f / waveViewportHeight * waveCacheChannels);
                WaveformImage.material.SetFloat("_DarkAlpha", Mathf.Clamp(Mathf.Sqrt(5 / density), 0.5f, 0.8f));
            }
        }
        
        volatile bool _mipExtendInFlight = false;

        void ExtendMipChain(int upToLevel)
        {
            _mipExtendInFlight = true;
            var chain    = _waveMipChain;
            int channels = waveCacheChannels;
            int samples  = waveCache != null ? waveCache.Length / channels : 0;

            Task.Run(() =>
            {
                try
                {
                    if (chain == null) return;
                    int baseSize = 64;

                    // Find the last built level to start from
                    int startFrom = 0;
                    for (int m = 0; m < chain.Length; m++)
                    {
                        if (chain[m] != null) startFrom = m;
                        else break;
                    }

                    for (int m = startFrom + 1; m <= upToLevel && m < chain.Length; m++)
                    {
                        int count = samples / (baseSize << m);
                        var level = new WaveformStats[count * channels];

                        for (int i = 0; i < count; i++)
                        {
                            for (int ch = 0; ch < channels; ch++)
                            {
                                var s1 = chain[m - 1][(i * 2) * channels + ch];
                                var s2 = chain[m - 1][(i * 2 + 1) * channels + ch];
                                level[i * channels + ch] = new WaveformStats
                                {
                                    min      = Math.Min(s1.min, s2.min),
                                    max      = Math.Max(s1.max, s2.max),
                                    rmsSqSum = s1.rmsSqSum + s2.rmsSqSum
                                };
                            }
                        }

                        // Assign atomically — main thread reads chain[m] as null check
                        chain[m] = level;
                    }
                }
                finally
                {
                    _mipExtendInFlight = false;
                }
            });
        }

        Color[] _wavePixelBuffer;

        void TriggerWaveBake(Texture2D texture, int texWidth, int texHeight, float step, Color color)
        {
            if (_bakeInFlight) return;

            int needed = texWidth * texHeight;
            if (_wavePixelBuffer == null || _wavePixelBuffer.Length != needed)
                _wavePixelBuffer = new Color[needed];

            // Capture locals for the background thread
            Color[]           buffer   = _wavePixelBuffer;
            int               mode     = Options.WaveformMode;
            float             viewStep = waveViewStep;
            float             bakeTime = waveTime;

            _bakeInFlight     = true;
            _bakeReady        = false;
            _bakeDstTexture   = texture;
            _bakeResultBuffer = buffer;

            switch (mode)
            {
                case 1: WaveformImage.material = WaveformMaterial; break;
                case 2: WaveformImage.material = null; break;
            }

            Task.Run(() =>
            {
                switch (mode)
                {
                    case 1: waveBakeWaveform(buffer, texWidth, step, viewStep, color, bakeTime); break;
                    case 2: waveBakeSpectrogram(buffer, texWidth, texHeight, step, color, bakeTime); break;
                }
                _bakeReady    = true;
                _bakeInFlight = false;
            });
        }

        WaveformStats[] _waveStatsBuffer;

        void waveBakeWaveform(Color[] pixels, int texWidth, float step, float viewStep, Color color, float bakeTime)
        {
            sbyte[] localWaveCache = waveCache;
            if (localWaveCache == null) return;

            int channels = waveCacheChannels;
            int freq = waveCacheFrequency;

            // LOD selection driven by viewport step (what the user sees), not bake step
            int sampleWindowPerChannel = Mathf.Max(1, Mathf.CeilToInt(freq * viewStep));

            // Bake step used for column time positions
            float density = freq * step;
            int sampleWindow = Mathf.Max(1, Mathf.CeilToInt(density / channels) * channels);

            // Finest mip whose bin size meets or exceeds the visible samples-per-pixel.
            // When the finest mip bin is still coarser than what the viewport demands,
            // fall through to raw samples so zoomed-in waveforms stay crisp.
            int mipIndex = -1;
            if (_waveMipChain != null)
            {
                // Find the needed mip level
                int neededMip = 0;
                for (int m = 0; m < _waveMipChain.Length; m++)
                {
                    neededMip = m;
                    if ((64 << m) >= sampleWindowPerChannel) break;
                }

                // If the needed level isn't built yet, extend the chain on a background task
                if (_waveMipChain[neededMip] == null && !_mipExtendInFlight)
                    ExtendMipChain(neededMip);

                // Walk up to the deepest built level that satisfies the request
                for (int m = 0; m < _waveMipChain.Length; m++)
                {
                    if (_waveMipChain[m] == null) break;
                    mipIndex = m;
                    if ((64 << m) >= sampleWindowPerChannel) break;
                }
                if (mipIndex == 0 && sampleWindowPerChannel < 64)
                    mipIndex = -1;
            }

            // Pass 1: compute raw stats per column per channel into _waveStatsBuffer
            int statsCount = channels * texWidth;
            if (_waveStatsBuffer == null || _waveStatsBuffer.Length != statsCount)
                _waveStatsBuffer = new WaveformStats[statsCount];

            for (int ch = 0; ch < channels; ch++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    float min = 1f, max = -1f, rms = 0f;
                    float secStart = bakeTime + x * step;
                    float secEnd = secStart + step;

                    if (mipIndex >= 0)
                    {
                        int window = 64 << mipIndex;
                        int posStart = Mathf.FloorToInt(secStart * freq / window);
                        int posEnd = Mathf.CeilToInt(secEnd * freq / window);
                        float rmsSqSumAccum = 0f;
                        int actualSamples = 0;

                        for (int p = posStart; p < posEnd; p++)
                        {
                            int idx = p * channels + ch;
                            if (idx >= 0 && idx < _waveMipChain[mipIndex].Length)
                            {
                                var stats = _waveMipChain[mipIndex][idx];
                                if (stats.min < min) min = stats.min;
                                if (stats.max > max) max = stats.max;
                                rmsSqSumAccum += stats.rmsSqSum;
                                actualSamples += window;
                            }
                        }
                        if (actualSamples > 0)
                            rms = Mathf.Sqrt(rmsSqSumAccum / actualSamples);
                    }
                    else
                    {
                        int pos = Mathf.FloorToInt(secStart * freq) * channels + ch;
                        int posEnd = Mathf.Min(pos + sampleWindow, localWaveCache.Length);

                        if (pos >= 0 && pos < localWaveCache.Length)
                        {
                            int samplesRead = 0;
                            for (int i = pos; i < posEnd; i += channels)
                            {
                                float sample = localWaveCache[i] / 127f;
                                if (sample < min) min = sample;
                                if (sample > max) max = sample;
                                rms += sample * sample;
                                samplesRead++;
                            }
                            if (samplesRead > 0)
                                rms = Mathf.Sqrt(rms / samplesRead);
                        }
                    }

                    _waveStatsBuffer[ch * texWidth + x] = new WaveformStats { min = min, max = max, rmsSqSum = rms };
                }
            }

            // Pass 2: write pixels, bridging each column to its neighbours so bars connect.
            // min is pulled down to the previous column's max, max is pulled up to the next
            // column's min — guaranteeing no gaps without blurring the packed values.
            for (int ch = 0; ch < channels; ch++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    var cur  = _waveStatsBuffer[ch * texWidth + x];
                    float min = cur.min;
                    float max = cur.max;

                    if (x > 0)
                    {
                        float prevMax = _waveStatsBuffer[ch * texWidth + x - 1].max;
                        if (prevMax < min) min = prevMax;
                    }
                    if (x < texWidth - 1)
                    {
                        float nextMin = _waveStatsBuffer[ch * texWidth + x + 1].min;
                        if (nextMin > max) max = nextMin;
                    }

                    // Pack into pixel: R=(min+1)/2, G=(max+1)/2, B=RMS
                    // Each channel gets its own row in the texture
                    pixels[ch * texWidth + x] = new Color((min + 1) * 0.5f, (max + 1) * 0.5f, cur.rmsSqSum, 1f);
                }
            }
        }

        void waveBakeSpectrogram(Color[] pixels, int texWidth, int texHeight, float step, Color color, float bakeTime)
        {
            int   channels   = waveCacheChannels;
            int   resolution = 512;
            float denY       = 1f / texHeight * channels;

            float[][] fft = new float[channels][];
            for (int i = 0; i < channels; i++)
                fft[i] = new float[resolution];

            FrequencyScale freqScale  = Chartmaker.Preferences.FrequencyScale;
            float          freqMin   = Chartmaker.Preferences.FrequencyMin;
            float          freqMax   = Chartmaker.Preferences.FrequencyMax;
            FFTWindow      fftWindow = Chartmaker.Preferences.FFTWindow;

            FrequencyScaling.GetScalingFunctions(freqScale, out var scale, out var unscale);
            float minScale = scale(freqMin);
            float maxScale = scale(freqMax);

            for (int x = 0; x < texWidth; x++)
            {
                float sec = bakeTime + x * step;
                int   pos = ((int)(sec * waveCacheFrequency) - resolution / 2) * channels;

                if (pos >= 0 && pos + resolution * channels <= waveCache.Length)
                {
                    for (int y = 0; y < resolution * channels; y++)
                    {
                        int ch = (pos + y) % channels;
                        int p  = y / channels;
                        fft[ch][p] = waveCache[pos + y] / 127f;
                    }
                    foreach (var t in fft)
                        FFT.Transform(t, fftWindow);
                }

                float sPos = 0;
                for (int y = 0; y < texHeight; y++)
                {
                    int   ch    = Mathf.FloorToInt(sPos);
                    float cPos  = Mathf.Clamp(unscale(Mathf.Lerp(minScale, maxScale, sPos % 1)) / waveCacheFrequency * resolution, 0, resolution - 1);
                    float value = Mathf.Sqrt(Mathf.Lerp(fft[ch][Mathf.FloorToInt(cPos)], fft[ch][Mathf.CeilToInt(cPos)], cPos % 1) / resolution * cPos) / 4;
                    pixels[y * texWidth + x] = color * new Color(1, 1, 1, value);
                    sPos += denY;
                }
            }
        }

        public void DiscardWaveform()
        {
            _bakeInFlight       = false;
            _bakeReady          = false;
            _mipExtendInFlight  = false;
            _bakeDstTexture     = null;
            _bakeResultBuffer   = null;
            Destroy(WaveformImage.texture);
            WaveformImage.texture = null;
            waveLastDensity    = 0;
            waveViewportWidth  = 0;
            waveViewportHeight = 0;
        }

        #endregion

        #region Density Graph

        Material densityGraphMat = null;

        bool densityGraphDirty       = false;
        float densityGraphDirtyTimer = 0;

        public void UpdateDensityGraph()
        {
        
            Color color = Themer.main.Keys["TimelineTickMain"];

            // Initialize the density map
            Texture2D texture = null;
            if (DensityGraphImage.texture is Texture2D imageTexture)
                texture = imageTexture;

            RectTransform graphRT = DensityGraphImage.rectTransform;
            if (!texture || texture.width != (int)graphRT.rect.width || texture.height != (int)graphRT.rect.height)
            {
                Destroy(DensityGraphImage.texture);
                DensityGraphImage.texture = texture = new Texture2D((int)graphRT.rect.width, (int)graphRT.rect.height, TextureFormat.ARGB32, false);
            }

            // Calculate the density map
            const float RANGE_PADDING_PX = 20;

            float[] densityMap = new float[texture.width / 3];
            float densityMapPaddingSec = (Chartmaker.main.SongSource.clip.length + 10)
                / texture.width * RANGE_PADDING_PX;
            Vector2 densityMapRange = new (
                -densityMapPaddingSec - 5,
                Chartmaker.main.SongSource.clip.length + 5 + densityMapPaddingSec
            );

            void addAtTime(float weight, float time)
            {

                int pos = Mathf.FloorToInt(
                    Mathf.InverseLerp(densityMapRange.x, densityMapRange.y, time)
                        * texture.width / 3
                );

                UnityEngine.Debug.Log(weight + " " + time + " " + pos);

                if (pos < 0 || pos >= densityMap.Length) return;

                densityMap[pos] += weight;
            }

            if (Chartmaker.main.CurrentChart != null)
            {
                foreach (Lane lane in Chartmaker.main.CurrentChart.Lanes)
                {
                    foreach (HitObject hit in lane.Objects)
                    {
                        float time = Chartmaker.main.CurrentSong.Timing.ToSeconds(hit.Offset);
                        float weight = hit.Type == HitObject.HitType.Normal ? 3 : 1;
                        if (hit.Flickable)
                        {
                            weight++;
                            if (float.IsFinite(hit.FlickDirection)) weight++;
                        }
                        addAtTime(weight, time);

                        for (float t = 0; t < hit.HoldLength; t += 0.5f)
                        {
                            float tickTime = Chartmaker.main.CurrentSong.Timing.ToSeconds(hit.Offset + t);
                            addAtTime(1, tickTime);
                        }
                    } 
                }
            }

            float densityMapMax = Mathf.Max(densityMap) + 1;

            // Draw the density map
            if (!densityGraphMat)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                densityGraphMat = new Material(shader);
                densityGraphMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                densityGraphMat.SetInt("_ZWrite", 0);
                densityGraphMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }

            RenderTexture currentTexture = RenderTexture.active;
            RenderTexture drawingTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = drawingTexture;

            densityGraphMat.SetPass(0);
            GL.Clear(true, true, Color.clear);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);
            GL.Color(color);
            for (int i = 0; i < densityMap.Length; i++)
            {
                float density = densityMap[i];
                float heightY = density / densityMapMax * 0.9f + (density > 0 ? 1 : 0) * 0.05f;
                Vector2[] corners = new Vector2[] {
                    new ((i * 3 + 1f) / texture.width, 0),
                    new ((i * 3 + 3f) / texture.width, 0),
                    new ((i * 3 + 3f) / texture.width, heightY),
                    new ((i * 3 + 1f) / texture.width, heightY),
                };
                GL.Vertex(corners[0]);
                GL.Vertex(corners[1]);
                GL.Vertex(corners[2]);
                GL.Vertex(corners[3]);
            }
            GL.End();
            GL.PopMatrix();

            Graphics.CopyTexture(drawingTexture, texture);
            RenderTexture.active = currentTexture;
            RenderTexture.ReleaseTemporary(drawingTexture);

            densityGraphDirty = false;
            densityGraphDirtyTimer = 0;
        }

        public void SetDensityGraphDirty(float timeout = 1)
        {
            densityGraphDirty = true;
            densityGraphDirtyTimer = timeout;
        }

        #endregion

        #region Beat Lines

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

        #endregion

        #region Scrollbar

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

        #endregion

        #region Beat Utils

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

        #endregion

        #region Timeline Interactivity

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

        public bool IsTimelineDragging()
        {
            return (int)dragMode % 2 == 1;
        }
        
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
            else if (eventData.button == PointerEventData.InputButton.Right) 
            {
                dragMode = TimelineDragMode.SeekBarRightClick;
                // Immediately call OnDrag to set current time
                OnDrag(eventData);
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

            if (IsTimelineDragging())
            {

                localPos(ItemsHolder, out dragEnd);

                // Auto-scroll when pointer overshoots the left or right edge.
                // Speed scales linearly with overshoot distance, capped at 3× viewport width per second.
                float holderWidth  = ItemsHolder.rect.width;
                float overshoot    = dragEnd.x < 0 ? dragEnd.x : dragEnd.x > holderWidth ? dragEnd.x - holderWidth : 0f;
                if (overshoot != 0f)
                {
                    float viewportSec  = PeekRange.y - PeekRange.x;
                    float scrollSpeed  = Mathf.Clamp(overshoot / holderWidth, -3f, 3f) * viewportSec;
                    float scrollDelta  = scrollSpeed * Time.unscaledDeltaTime;
                    PeekRange.x = Mathf.Clamp(PeekRange.x + scrollDelta, limit.x, limit.y - viewportSec);
                    PeekRange.y = PeekRange.x + viewportSec;
                }

                timeEnd = Mathf.Lerp(PeekRange.x, PeekRange.y, dragEnd.x / holderWidth);

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
                if (dragMode == TimelineDragMode.SeekBarRightClick)
                {
                    time = (localMousePos.x / sliderWidth + TimeSliderHolder.pivot.x) * width + limit.x;
                }
                else
                {
                    time = ((localMousePos - dragStart).x / sliderWidth + TimeSliderHolder.pivot.x) * width + limit.x;
                }
            }
            else
                return;
        
            switch (dragMode)
            {
                case TimelineDragMode.SeekBarRightClick:
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
            if (eventData.button == PointerEventData.InputButton.Right && IsTimelineDragging())
            {
                Chartmaker.main.SongSource.time = Mathf.Clamp(metronome.ToSeconds(beatStart), 0, Chartmaker.main.SongSource.clip.length);
            }
            
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

        #endregion

        #region Toolbar Interactivity

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
            float scale = Chartmaker.main.ChartmakerCanvas.scaleFactor;
            ResizeTimeline(Input.mousePosition.y / scale, false);
        }
        public void OnResizerEndDrag()
        {
            float scale = Chartmaker.main.ChartmakerCanvas.scaleFactor;
            ResizeTimeline(Input.mousePosition.y / scale);
        }

        public float SnapTimeline(float height)
        {
            return height < 84 ? 52 : Mathf.Max(Mathf.Round((height - 80) / 24) * 24 + 92, 116);
        }
    
        public void ResizeTimeline(float height, bool snap = true)
        {
            float scale = Chartmaker.main.ChartmakerCanvas.scaleFactor;

            float maxHeight = SnapTimeline(Screen.height / scale * 0.5f);
            height = Mathf.Round(Mathf.Clamp(height, 52, maxHeight));
        
            if (snap)
                height = SnapTimeline(height);
       
            Chartmaker.main.TimelineHolder.anchoredPosition = new(
                Chartmaker.main.TimelineHolder.sizeDelta.x, 
                -Mathf.Pow(Mathf.Max(116 - height, 0) / 64, 2) * 32
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
       
            TimelineHeight = height <= 52 ? 0 : Mathf.Max(Mathf.RoundToInt((height - 92) / 24), 1);
      
            if (snap) 
            {
                if (TimelineHeight > 0) TimelineRestoreHeight = TimelineHeight;
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
            ResizeTimeline(60);
        }
    
        public void Restore()
        {
            if (TimelineHeight <= 0) ResizeTimeline(TimelineRestoreHeight * 24 + 92);
            PlayerView.main.IsMaximised = false;
        }

        #endregion
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

        CurrentTime       = 2,
        PeekRange         = 4,
        PeekStart         = 6,
        PeekEnd           = 8,
        SeekBarRightClick = 10,

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