using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JANOARG.Chartmaker.Data.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker.Actions;
using JANOARG.Chartmaker.UI.Cursor;
using JANOARG.Chartmaker.UI.NativeUI;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Shared.Utils.Animation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JANOARG.Chartmaker.Behaviors.Chartmaker.PickHandler;
using JANOARG.Chartmaker.Utils.NativeAPI;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class PlayerView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IDragHandler, IEndDragHandler
    {
        public static PlayerView    main;
        public RectTransform playerViewBound;

        public Camera MainCamera;
        public Image  BoundingBox;
        [Space]
        public ChartManager Manager;

        // A lane offset lent to these hit objects for the length of one manager pass. See UpdateObjects.
        readonly List<HitObject> PreviewPositionTargets = new();
        float                    PreviewPositionOffset;
        float                    PreviewWidthOffset;

        // What the last lend borrowed and owes back, recorded rather than recomputed so the return is exact.
        readonly List<HitObject> _LentHits = new();
        readonly List<float>     _LentPositions = new();
        readonly List<float>     _LentWidths = new();
        readonly List<(Timestamp Stamp, float From, float Target)> _LentTimestamps = new();
        [Space]
        [Header("Cover")]
        public CoverViewMode CurrentCoverViewMode;
        public GameObject DarkBackground;
        public Image      CoverBackground;
        public RectMask2D CoverMask;
        public RawImage   CoverLayerSample;
        public List<RawImage> CoverLayers { get; private set; } = new();
        public RectTransform IconRenderCanvas;
        [Space]
        public GameObject CoverToolbar;
        public GameObject MaskButtonHighlight;
        public GameObject PanoramaButtonHighlight;
        public GameObject IconButtonHighlight;
        [Space]
        [Header("World")]
        public Transform Holder;
        public ChartmakerLaneGroupPlayer LaneGroupPlayerSample;
        public ChartmakerLanePlayer LanePlayerSample;
        public Dictionary<ulong, ChartmakerLaneGroupPlayer> LaneGroupPlayers { get; private set; } = new();
        public List<ChartmakerLanePlayer> LanePlayers { get; private set; } = new();
        public ChartmakerHitPlayer HitPlayerSample;
        public MeshRenderer        HoldMeshSample;
        [Space]
        public Mesh FreeFlickIndicator;
        public Mesh ArrowFlickIndicator;
        [Space]
        public PlayOptionsPanel PlayOptions;
        [Space]
        public AudioSource SoundPlayer;
        public AudioClip NormalHitSound;
        public AudioClip CatchHitSound;
        public AudioClip FlickSound;
        public AudioClip AltNormalHitSound;
        public AudioClip AltCatchHitSound;
        [Space]
        public Graphic NotificationText;
        public Graphic NotificationBox;
        [Space]
        public RectTransform CurrentLaneLine;
        public RectTransform SelectedItemLine;
        public RectTransform StartHandle;
        public RectTransform CenterHandle;
        public RectTransform EndHandle;
        [Space]
        public float[] GridSize = {0.5f};

        public float CurrentTime { get; private set; }

        public bool IsMaximised
        {
            get =>
                HierarchyPanel.main.IsCollapsed
                && InspectorPanel.main.IsCollapsed
                && TimelinePanel.main.TimelineHeight <= 0;

            set
            {
                if (value)
                {
                    if (!HierarchyPanel.main.IsCollapsed)
                        HierarchyPanel.main.Collapse();

                    if (!InspectorPanel.main.IsCollapsed)
                        InspectorPanel.main.Collapse();

                    if (TimelinePanel.main.TimelineHeight > 0)
                        TimelinePanel.main.Collapse();
                }
                else
                {
                    if (HierarchyPanel.main.IsCollapsed)
                        HierarchyPanel.main.Restore();

                    if (InspectorPanel.main.IsCollapsed)
                        InspectorPanel.main.Restore();

                    if (TimelinePanel.main.TimelineHeight <= 0)
                        TimelinePanel.main.Restore();
                }
            }
        }
    
        readonly List<ulong> _GroupRemovalScratch = new();

        // Which players exist and what they parent to only changes when the chart is edited,
        // so it is derived on the same signal as the lane windows and the storyboard clones.
        bool _HierarchyDirty = true;

        Transform ResolveLaneParent(LaneManager lane)
        {
            ulong groupUuid = lane.Current.GroupUuid;

            return groupUuid != 0
                   && LaneGroupPlayers.TryGetValue(groupUuid, out ChartmakerLaneGroupPlayer player)
                ? player.transform
                : Holder;
        }

        static readonly ProfilerMarker sr_GroupPlayers = new("PlayerView: Group Players");
        static readonly ProfilerMarker sr_LanePlayers  = new("PlayerView: Lane Players");

        readonly LaneWindowIndex LaneWindows = new();
        bool[] LaneActiveMask = System.Array.Empty<bool>();

        int[] HitObjectsRemaining = new [] { 0, 0 };
        int   FlicksRemaining     = 0;

        public HandleDragMode CurrentDragMode;
        bool                  isDragged;
        bool                  isAnimating;
        float                 lastTargetAspect;
        Vector2               CoverPosition;

        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            InitMeshes();
        }


        public void Update()
        {
            // Camera is being used by the render modal
            if (MainCamera.targetTexture)
            {
                return;
            }

            RectTransform rt = (RectTransform)transform;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            float scale = Chartmaker.main.ChartmakerCanvas.scaleFactor;
        
            Rect bound = new(
                corners[0].x,
                corners[0].y,
                corners[2].x - corners[0].x,
                corners[2].y - corners[0].y
            );

            MainCamera.rect = new(
                bound.x / Screen.width,
                bound.y / Screen.height,
                bound.width / Screen.width,
                bound.height / Screen.height
            );

            // Resize bounds after main camera to account for UI scaling
            bound.position /= scale;    
            bound.size /= scale;

            Rect safeZone = new(
                bound.x + 12,
                bound.y + 12,
                bound.width - 24,
                bound.height - 24
            );

            float targetAspect;
            if (HierarchyPanel.main.CurrentMode == HierarchyMode.PlayableSong)
            {
                safeZone.yMin += 32;

                targetAspect = CurrentCoverViewMode switch
                {
                    CoverViewMode.Panorama => 880 / 200f,
                    CoverViewMode.Icon => 1,
                    _ => 880 / 200f
                };
            }
            else 
                targetAspect = 7 / 4f;

            if (safeZone.width / safeZone.height > targetAspect)
            {
                float width = safeZone.height * targetAspect;
                safeZone.x += (safeZone.width - width) / 2;
                safeZone.width = width;
            }
            else
            {
                float height = safeZone.width / targetAspect;
                safeZone.y += (safeZone.height - height) / 2;
                safeZone.height = height;
            }

            BoundingBox.rectTransform.sizeDelta = safeZone.size;
            float camRatio = safeZone.height / bound.height;
            MainCamera.fieldOfView = Mathf.Atan2(Mathf.Tan(30 * Mathf.Deg2Rad), camRatio) * 2 * Mathf.Rad2Deg;

            if (!Mathf.Approximately(CurrentTime, InformationBar.main.sec) || !Mathf.Approximately(targetAspect, lastTargetAspect))
                UpdateObjectsForFrame();
            lastTargetAspect = targetAspect;
        }

        /// <summary>
        /// The ultimate invalidator. Call this when you mess with the datas and
        /// it will update the chart view.
        /// </summary>
        public void UpdateObjects()
        {
            LaneWindows.Invalidate();
            Manager?.MarkSourcesChanged();
            _HierarchyDirty = true;
            UpdateObjectsForFrame();
        }

        /// <summary>The per-frame path — same work, but the windows are already current.</summary>
        void UpdateObjectsForFrame() => UpdateObjects(InformationBar.main.sec, InformationBar.main.beat);

        /// <summary>
        /// Shows a selection at a lane position it has not been moved to yet. Nothing is rendered here - the drag
        /// that sets this renders through OnHistoryDo on the same pointer move. Pass no targets, or a zero offset,
        /// to clear.
        /// </summary>
        public void SetHitPositionPreview(IList targets, float offset)
        {
            PreviewPositionTargets.Clear();
            
            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (isShift)
            {
                PreviewPositionOffset = 0;
                PreviewWidthOffset = offset;
            }
            else
            {
                PreviewPositionOffset = offset;
                PreviewWidthOffset = 0;
            }

            if (targets == null || Mathf.Approximately(offset, 0))
                return;

            foreach (object item in targets)
                if (item is HitObject hit)
                    PreviewPositionTargets.Add(hit);
        }

        public void ClearHitPositionPreview() => SetHitPositionPreview(null, 0);

        public void UpdateObjects(float sec, float beat)
        {
            CurrentTime = sec;

            if (Chartmaker.main.CurrentChart != null)
            {
                // The previewed offset is lent to the chart for the manager pass below and taken straight
                // back after it. Only that pass reads hit positions out of the chart - everything below it
                // works off the values that pass baked - so the window is one call wide, and no save,
                // inspector read or timeline render can see the borrowed values.
                LendHitPositionPreview();

                if (Chartmaker.main.CurrentChart != Manager?.CurrentChart)
                {
                    Manager = new ChartManager(
                        Chartmaker.main.CurrentSong,
                        Chartmaker.main.CurrentChart,
                        speed: 121,
                        time:  sec,
                        pos:   beat
                    );

                    // Windows and hierarchy belong to the old chart; the first frame rebuilds both.
                    LaneWindows.Invalidate();
                    _HierarchyDirty = true;
                }
                else
                {
                    LaneWindows.GetActive(
                        Chartmaker.main.CurrentChart,
                        Chartmaker.main.CurrentSong,
                        Manager.CurrentSpeed,
                        sec,
                        ref LaneActiveMask
                    );

                    // The selected lane keeps updating even when culled: UpdateHandles reads
                    // its mesh and endpoints directly, and a culled LaneManager has none.
                    if (InspectorPanel.main.CurrentHierarchyObject is Lane selectedLane)
                    {
                        int selectedIndex = Chartmaker.main.CurrentChart.Lanes.IndexOf(selectedLane);

                        if (selectedIndex >= 0 && selectedIndex < LaneActiveMask.Length)
                            LaneActiveMask[selectedIndex] = true;
                    }

                    Manager!.Update(sec, beat, LaneActiveMask);
                }

                ReturnHitPositionPreview();
            
                MainCamera.transform.position = Manager.Camera.CameraPivot;
                MainCamera.transform.eulerAngles = Manager.Camera.CameraRotation; 
                MainCamera.transform.Translate(Vector3.back * Manager.Camera.PivotDistance);

                RenderSettings.fogColor = MainCamera.backgroundColor = Manager.PalleteManager.CurrentPallete.BackgroundColor;
                BoundingBox.color = NotificationText.color = NotificationBox.color = Manager.PalleteManager.CurrentPallete.InterfaceColor;

                sr_GroupPlayers.Begin();

                // Passes 1 and 2 derive structure — which players exist and what they parent to
                // — and structure only changes when the chart is edited. The manager instances
                // they cache persist across frames, so on an unedited frame there is nothing to
                // re-derive and only pass 3 needs to run.
                if (_HierarchyDirty)
                {
                    // Pass 1: sync group player dict to Manager.Groups (post-ChartManager.Update,
                    // so duplicates in chart.Groups are already collapsed by the Dictionary).
                    foreach (var pair in LaneGroupPlayers)
                        pair.Value.CurrentGroup = null;

                    foreach (var pair in Manager.Groups)
                    {
                        ulong groupUuid = pair.Key;
                        if (!LaneGroupPlayers.TryGetValue(groupUuid, out ChartmakerLaneGroupPlayer groupPlayer))
                        {
                            groupPlayer = Instantiate(LaneGroupPlayerSample, Holder);
                            #if UNITY_EDITOR
                            groupPlayer.gameObject.name = $"Lane Group ({pair.Value.CurrentGroup.Name})";
                            #endif
                            LaneGroupPlayers[groupUuid] = groupPlayer;
                        }
                        groupPlayer.CurrentGroup = pair.Value;
                    }

                    // Destroy group players no longer in Manager.Groups.
                    _GroupRemovalScratch.Clear();
                    var toRemove = _GroupRemovalScratch;
                    foreach (var pair in LaneGroupPlayers)
                        if (pair.Value.CurrentGroup == null) { Destroy(pair.Value.gameObject); toRemove.Add(pair.Key); }
                    foreach (ulong key in toRemove) LaneGroupPlayers.Remove(key);

                    // Pass 2: resolve GO parent hierarchy BEFORE applying any local transforms.
                    foreach (var pair in LaneGroupPlayers)
                    {
                        ulong parentGroupUuid = pair.Value.CurrentGroup.CurrentGroup.GroupUuid;
                        Transform desiredParent = parentGroupUuid != 0 && LaneGroupPlayers.TryGetValue(parentGroupUuid, out ChartmakerLaneGroupPlayer parentPlayer)
                            ? parentPlayer.transform
                            : Holder;
                        if (pair.Value.transform.parent != desiredParent)
                            pair.Value.transform.SetParent(desiredParent, worldPositionStays: false);
                    }
                }

                // Pass 3: apply local transforms — hierarchy is now correct.
                foreach (var pair in LaneGroupPlayers)
                    pair.Value.UpdateObjects(pair.Value.CurrentGroup);

                sr_GroupPlayers.End();
                sr_LanePlayers.Begin();

                // Update lane players, parenting each under its group player (or Holder if ungrouped).
                for (int a = 0; a < Manager.Lanes.Count; a++)
                {
                    LaneManager laneManager = Manager.Lanes[a];

                    // Culled: Current/Steps/mesh are absent or stale, so nothing below may
                    // touch them. The slot is kept rather than compacted because LanePlayers
                    // is index-aligned with Manager.Lanes and the handle code resolves lanes
                    // by IndexOf against the chart.
                    if (!laneManager.IsActive)
                    {
                        if (LanePlayers.Count > a && LanePlayers[a].gameObject.activeSelf)
                            LanePlayers[a].gameObject.SetActive(false);

                        continue;
                    }

                    if (LanePlayers.Count <= a)
                    {
                        LanePlayers.Add(Instantiate(LanePlayerSample, ResolveLaneParent(laneManager)));
                        #if UNITY_EDITOR
                        string beatRange = $"Lane ({(BeatPosition)laneManager.Steps[0].Offset} > {(BeatPosition)laneManager.Steps[^1].Offset})";
                        LanePlayers[a].gameObject.name = string.IsNullOrEmpty(laneManager.Current.Name) ? beatRange : laneManager.Current.Name;
                        #endif
                    }

                    // Lane.Group is a plain string, so a lane's parent can only change on an
                    // edit. The reactivation case is here because a lane culled while the
                    // hierarchy changed never reached this loop to be re-parented.
                    else if (_HierarchyDirty || !LanePlayers[a].gameObject.activeSelf)
                    {
                        Transform desiredParent = ResolveLaneParent(laneManager);

                        if (LanePlayers[a].transform.parent != desiredParent)
                            LanePlayers[a].transform.SetParent(desiredParent, worldPositionStays: false);
                    }

                    if (!LanePlayers[a].gameObject.activeSelf)
                        LanePlayers[a].gameObject.SetActive(true);

                    LanePlayers[a].UpdateObjects(laneManager);
                }

                while (LanePlayers.Count > Manager.Lanes.Count)
                {
                    Destroy(LanePlayers[Manager.Lanes.Count].gameObject);
                    LanePlayers.RemoveAt(Manager.Lanes.Count);
                }

                sr_LanePlayers.End();

                // Cleared only after both the group and lane passes have consumed it.
                _HierarchyDirty = false;
            
                if (Chartmaker.main.SongSource.isPlaying && !TimelinePanel.main.isDragged && PlayOptions.HitsoundsVolume > 0)
                {
                    if (Manager.HitObjectsRemaining[0] < HitObjectsRemaining[0])
                        SoundPlayer.PlayOneShot(Chartmaker.Preferences.PerfectHitsounds ? AltNormalHitSound : NormalHitSound, PlayOptions.HitsoundsVolume);
                
                    if (Manager.HitObjectsRemaining[1] < HitObjectsRemaining[1])
                        SoundPlayer.PlayOneShot(Chartmaker.Preferences.PerfectHitsounds ? AltCatchHitSound : CatchHitSound, PlayOptions.HitsoundsVolume);
               
                    if (Manager.FlicksRemaining < FlicksRemaining && !Chartmaker.Preferences.PerfectHitsounds)
                        SoundPlayer.PlayOneShot(FlickSound, PlayOptions.HitsoundsVolume);
                }
                // Copy values, not the reference: ChartManager reuses its array across frames,
                // so aliasing it here would make the comparisons above always read equal and
                // no hitsound would ever fire.
                HitObjectsRemaining[0] = Manager.HitObjectsRemaining[0];
                HitObjectsRemaining[1] = Manager.HitObjectsRemaining[1];
                FlicksRemaining = Manager.FlicksRemaining;
            }

            UpdateHandles();

            if (HierarchyPanel.main.CurrentMode == HierarchyMode.PlayableSong) 
            {
                DarkBackground.SetActive(true);
                CoverToolbar.SetActive(true);

                CoverBackground.rectTransform.sizeDelta = CurrentCoverViewMode switch
                {
                    CoverViewMode.Panorama => new(880, 200),
                    CoverViewMode.Icon => Vector2.one * Chartmaker.main.CurrentSong.Cover.IconSize,
                    _ => CoverBackground.rectTransform.sizeDelta
                };

                // Scale and position must be set before computing parallaxOffset
                // so the IconCenter division uses the current frame's scale.
                CoverBackground.rectTransform.localScale = Vector3.one * (BoundingBox.rectTransform.rect.width / CoverBackground.rectTransform.sizeDelta.x);
                CoverBackground.rectTransform.anchoredPosition = BoundingBox.rectTransform.anchoredPosition;
                CoverBackground.color = Chartmaker.main.CurrentSong.Cover.BackgroundColor;

                float scale = CoverBackground.rectTransform.localScale.x;
                Vector2 parallaxOffset = CoverPosition / scale;

                Vector2 iconOffset = CurrentCoverViewMode == CoverViewMode.Icon
                    ? Chartmaker.main.CurrentSong.Cover.IconCenter
                    : Vector2.zero;

                BoundingBox.color = NotificationText.color = NotificationBox.color = Color.white;
                BoundingBox.rectTransform.anchoredPosition = new Vector2 (0, 16) + CoverPosition;

                int index = 0;
                foreach (CoverLayer layer in Chartmaker.main.CurrentSong.Cover.Layers)
                {
                
                    RawImage image;
                
                    if (CoverLayers.Count <= index)
                    {
                        image = Instantiate(CoverLayerSample, CoverBackground.rectTransform);
                        CoverLayers.Add(image);
                    }
                    else 
                        image = CoverLayers[index];

                    image.texture = layer.Texture;
                
                    if (layer.Tiling)
                    {
                        image.rectTransform.sizeDelta = CoverBackground.rectTransform.sizeDelta;
                        image.rectTransform.anchoredPosition = Vector2.zero;
                    
                        Vector2 imgSize = new Vector2(1, (float)layer.Texture.height / layer.Texture.width) * (880 * layer.Scale);
                    
                        image.uvRect = Rect2UV(
                            new (
                                -CoverBackground.rectTransform.sizeDelta * .5f, 
                                CoverBackground.rectTransform.sizeDelta
                            ), 
                            new (
                                layer.Position - parallaxOffset * layer.ParallaxFactor + iconOffset - imgSize * .5f, 
                                imgSize
                            ));
                    }
                    else 
                    {
                        image.rectTransform.sizeDelta = new Vector2(1, (float)layer.Texture.height / layer.Texture.width) * (layer.Scale * 880);
                        image.rectTransform.anchoredPosition = layer.Position - parallaxOffset * layer.ParallaxFactor + iconOffset;
                        image.uvRect = new (0, 0, 1, 1);
                    }

                    index++;
                }

                while (CoverLayers.Count > Chartmaker.main.CurrentSong.Cover.Layers.Count)
                {
                    Destroy(CoverLayers[^1].gameObject);
                    CoverLayers.RemoveAt(CoverLayers.Count - 1);
                }

                UpdateCoverToolbar();
            }
            else 
            {
                BoundingBox.rectTransform.anchoredPosition = new (0, 0);
                DarkBackground.SetActive(false);
                CoverToolbar.SetActive(false);
            }

            // Offsets the previewed hit objects in the chart, recording what they held. Position timestamps move
            // with them, matching ChartmakerMoveHitObjectAction - a storyboarded position would otherwise
            // overwrite the offset the moment the manager evaluated it, and the note would sit still under the
            // drag.
            //
            // Anything a previous lend failed to give back is returned first, so a throw between the two
            // cannot strand the borrowed values in the chart for longer than a frame.
            void LendHitPositionPreview()
            {
                ReturnHitPositionPreview();

                bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (PreviewPositionTargets.Count <= 0)
                    return;

                foreach (HitObject hit in PreviewPositionTargets)
                {
                    _LentHits.Add(hit);
                    _LentPositions.Add(hit.Position);
                    _LentWidths.Add(hit.Length);

                    hit.Position += PreviewPositionOffset;
                    hit.Length += PreviewWidthOffset;

                    foreach (Timestamp stamp in hit.Storyboard.Timestamps)
                    {
                        if (stamp.ID == TimestampIDs.Position)
                        {
                            _LentTimestamps.Add((stamp, stamp.From, stamp.Target));

                            stamp.From   += PreviewPositionOffset;
                            stamp.Target += PreviewPositionOffset;
                        }
                    }
                }
            }

            // Puts back what the lend borrowed, by assignment rather than by subtracting the offset again - a
            // preview held across many frames would otherwise drift the values it only borrowed. Safe to call
            // when nothing is outstanding.
            void ReturnHitPositionPreview()
            {
                for (int a = 0; a < _LentHits.Count; a++)
                {
                    _LentHits[a].Position = _LentPositions[a];
                    _LentHits[a].Length = _LentWidths[a];
                }

                for (int a = 0; a < _LentTimestamps.Count; a++)
                {
                    (Timestamp stamp, float from, float target) = _LentTimestamps[a];

                    stamp.From   = from;
                    stamp.Target = target;
                }

                _LentHits.Clear();
                _LentPositions.Clear();
                _LentWidths.Clear();
                _LentTimestamps.Clear();
            }
        }

        private void UpdateCoverToolbar()
        {
            MaskButtonHighlight.SetActive(CoverMask.enabled);

            PanoramaButtonHighlight.SetActive(CurrentCoverViewMode == CoverViewMode.Panorama);
            IconButtonHighlight.SetActive(CurrentCoverViewMode == CoverViewMode.Icon);
        }

        public void ToggleCoverMask()
        {
            CoverMask.enabled = !CoverMask.enabled;
            UpdateCoverToolbar();
        }

        public void UpdateHandles() 
        {
            CurrentLaneLine.gameObject.SetActive(false);
            SelectedItemLine.gameObject.SetActive(false);
            StartHandle.gameObject.SetActive(false);
            CenterHandle.gameObject.SetActive(false);
            EndHandle.gameObject.SetActive(false);

            if (Chartmaker.main.SongSource.isPlaying)
                return;
        
            switch (HierarchyPanel.main.CurrentMode)
            {
                case HierarchyMode.PlayableSong:
                    switch (InspectorPanel.main.CurrentObject)
                    {
                        case CoverLayer layer: 
                        {
                            float scale = CoverBackground.rectTransform.localScale.x;
                            Vector2 offset = new Vector2(0, 16) + CoverPosition * (1 - layer.ParallaxFactor);
                       
                            if (CurrentCoverViewMode == CoverViewMode.Icon) 
                                offset -= (1 - layer.ParallaxFactor) / scale * Chartmaker.main.CurrentSong.Cover.IconCenter;
                    
                            Vector2 center = layer.Position * scale + offset;
                            CenterHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Center);
                            CenterHandle.anchoredPosition = center;
                    
                            Vector2 left = Vector2.right * (440 * layer.Scale * scale) + center;
                            StartHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                            StartHandle.anchoredPosition = left;
                    
                            SelectedItemLine.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                            SelectedItemLine.anchoredPosition = (center + left) / 2;
                            SelectedItemLine.sizeDelta = new(440 * layer.Scale * scale, SelectedItemLine.sizeDelta.y);
                            SelectedItemLine.eulerAngles = Vector2.zero;
                        } break;
                    }

                    // TODO: Maybe implement this? What is this for?
#pragma warning disable CS0164 // This label has not been referenced
                    endSel: ;
#pragma warning restore CS0164 // This label has not been referenced
                    break;
                case HierarchyMode.Chart:
                {
                    {
                        if (Chartmaker.main.CurrentChart != null && InspectorPanel.main.CurrentHierarchyObject is Lane currentLane)
                        {
                            int index = Chartmaker.main.CurrentChart.Lanes.IndexOf(currentLane);
                            if (index < 0) 
                                goto endLane;
                    
                            LaneManager laneManager = Manager.Lanes[index];
                            if ((laneManager.CurrentMesh?.vertexCount ?? 0) > 2)
                            {
                                Vector2 start = MainCamera.WorldToScreenPoint(laneManager.StartPos);
                                Vector2 end = MainCamera.WorldToScreenPoint(laneManager.EndPos);
                        
                                CurrentLaneLine.gameObject.SetActive(true);
                                CurrentLaneLine.position = (start + end) / 2;
                                CurrentLaneLine.sizeDelta = new(Vector2.Distance(start, end), CurrentLaneLine.sizeDelta.y);
                                CurrentLaneLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.left, end - start));
                            }
                        }
                    }

                    endLane: 

                    switch (InspectorPanel.main.CurrentObject)
                    {
                        case Lane lane: 
                        {
                            int index = Chartmaker.main.CurrentChart!.Lanes.IndexOf(lane);
                            if (index < 0)
                                goto endSelect;
                    
                            LaneManager laneManager = Manager.Lanes[index];
                    
                            Vector2 center = MainCamera.WorldToScreenPoint(laneManager.FinalPosition);
                            CenterHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Center);
                            CenterHandle.position = center;

                            if ((laneManager.CurrentMesh?.vertexCount ?? 0) > 2)
                            {
                                Vector2 start = MainCamera.WorldToScreenPoint(laneManager.StartPos);
                                Vector2 end = MainCamera.WorldToScreenPoint(laneManager.EndPos);
                        
                                SelectedItemLine.gameObject.SetActive(true);
                                SelectedItemLine.position = (start + end) / 2;
                                SelectedItemLine.sizeDelta = new(Vector2.Distance(start, end), SelectedItemLine.sizeDelta.y);
                                SelectedItemLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector3.left, end - start));
                        
                                if (SelectedItemLine.sizeDelta.x > 20) 
                                {
                                    StartHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                                    StartHandle.position = start;
                            
                                    EndHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.End);
                                    EndHandle.position = end;
                                    EndHandle.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
                                }
                            }
                        } break;
                        case LaneStep step: 
                        {
                            if (InspectorPanel.main.CurrentHierarchyObject is not Lane currentLane) return;

                            int laneIndex = Chartmaker.main.CurrentChart!.Lanes.IndexOf(currentLane);
                            if (laneIndex < 0)
                                goto endSelect;
                    
                            LaneManager laneManager = Manager.Lanes[laneIndex];

                            int index = currentLane.LaneSteps.IndexOf(step);
                            if (index < 0)
                                goto endSelect;
                    
                            LaneStepManager laneStepManager = laneManager.Steps[index];

                            if (laneStepManager.Offset >= Chartmaker.main.SongSource.time)
                            {
                                Vector3 offset = laneManager.FinalRotation * Vector3.forward * (laneStepManager.Distance - laneManager.CurrentDistance) + laneManager.FinalPosition;
                                Vector2 middlePointPosition = (laneStepManager.CurrentStep.StartPointPosition + laneStepManager.CurrentStep.EndPointPosition) / 2;
                        
                                Vector2 start = MainCamera.WorldToScreenPoint(laneManager.FinalRotation * laneStepManager.CurrentStep.StartPointPosition + offset);
                                Vector2 end  = MainCamera.WorldToScreenPoint(laneManager.FinalRotation * laneStepManager.CurrentStep.EndPointPosition + offset);
                                Vector2 center = MainCamera.WorldToScreenPoint(laneManager.FinalRotation * middlePointPosition + offset);
                        
                                SelectedItemLine.gameObject.SetActive(true);
                                SelectedItemLine.position = (start + end) / 2;
                                SelectedItemLine.sizeDelta = new(Vector2.Distance(start, end), SelectedItemLine.sizeDelta.y);
                                SelectedItemLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector3.left, end - start));
                        
                                CenterHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Center);
                                CenterHandle.position = center;
                        
                                if (SelectedItemLine.sizeDelta.x > 20) 
                                {
                                    StartHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                                    StartHandle.position = start;
                           
                                    EndHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.End);
                                    EndHandle.position = end;
                                    EndHandle.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
                            
                                }
                            }
                        } break;
                        case HitObject hit: 
                        {
                            if (InspectorPanel.main.CurrentHierarchyObject is not Lane currentLane) return;

                            int laneIndex = Chartmaker.main.CurrentChart!.Lanes.IndexOf(currentLane);
                            if (laneIndex < 0)
                                goto endSelect;
                    
                            LaneManager laneManager = Manager.Lanes[laneIndex];

                            int index = currentLane.Objects.IndexOf(hit);
                            if (index < 0) 
                                goto endSelect;
                    
                            HitObjectManager hitObjectManager = laneManager.Objects[index];

                            if (hitObjectManager.TimeEnd >= Chartmaker.main.SongSource.time)
                            {
                                Vector2 start = MainCamera.WorldToScreenPoint(laneManager.FinalRotation * (hitObjectManager.StartPos + laneManager.CurrentDistance * Vector3.back) + laneManager.FinalPosition);
                                Vector2 end = MainCamera.WorldToScreenPoint(laneManager.FinalRotation * (hitObjectManager.EndPos + laneManager.CurrentDistance * Vector3.back) + laneManager.FinalPosition);
                                Vector2 center = MainCamera.WorldToScreenPoint(laneManager.FinalRotation * (hitObjectManager.Position + laneManager.CurrentDistance * Vector3.back) + laneManager.FinalPosition);
                        
                                SelectedItemLine.gameObject.SetActive(true);
                                SelectedItemLine.position = (start + end) / 2;
                                SelectedItemLine.sizeDelta = new(Vector2.Distance(start, end), SelectedItemLine.sizeDelta.y);
                                SelectedItemLine.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector3.left, end - start));
                        
                                CenterHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Center);
                                CenterHandle.position = center;
                        
                                if (SelectedItemLine.sizeDelta.x > 20) 
                                {
                                    StartHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.Start);
                                    StartHandle.position = start;
                        
                                    EndHandle.gameObject.SetActive(CurrentDragMode is HandleDragMode.None or HandleDragMode.End);
                                    EndHandle.position = end;
                                    EndHandle.eulerAngles = new(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
                                }
                            }
                        } break;
                    }
            
                    endSelect: ;

                    break;
                }
            }
        }

        public void InitMeshes() 
        {
            if (!FreeFlickIndicator) 
            {
                Mesh mesh = new();
                List<Vector3> verts = new();
                List<int> tris = new();

                verts.AddRange(new Vector3[] { new(-1, 0), new(0, 2), new(0, -.5f), new(1, 0), new(0, -2), new(0, .5f) });
                tris.AddRange(new [] {0, 1, 2, 3, 4, 5});

                mesh.SetVertices(verts);
                mesh.SetUVs(0, verts);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                FreeFlickIndicator = mesh;
            }
            if (!ArrowFlickIndicator) 
            {
                Mesh mesh = new();
                List<Vector3> verts = new();
                List<int> tris = new();

                verts.AddRange(new Vector3[] { new(-1, 0), new(0, 2.2f), new(1, 0), new(.71f, -.71f), new(0, -1), new(-.71f, -.71f) });
                tris.AddRange(new [] {0, 1, 2, 2, 3, 0, 3, 4, 0, 4, 5, 0});

                mesh.SetVertices(verts);
                mesh.SetUVs(0, verts);
                mesh.SetTriangles(tris, 0);
                mesh.RecalculateNormals();
                ArrowFlickIndicator = mesh;
            }
        }
        float holdDurationThreshold = 0.8f;
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (isAnimating) return;

            bool Contains(RectTransform rt) =>
                rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.pressPosition, eventData.pressEventCamera);

            CurrentDragMode = HandleDragMode.None;

            if (Contains((RectTransform)CoverToolbar.transform)) 
                CurrentDragMode = HandleDragMode.None;
            else if (Contains(StartHandle))
                CurrentDragMode = HandleDragMode.Start;
            else if (Contains(CenterHandle)) 
                CurrentDragMode = HandleDragMode.Center;
            else if (Contains(EndHandle))
                CurrentDragMode = HandleDragMode.End;
            else if (HierarchyPanel.main.CurrentMode == HierarchyMode.PlayableSong)
                CurrentDragMode = HandleDragMode.Background;

            if (CurrentDragMode == HandleDragMode.None)
            {
                if (HierarchyPanel.main.CurrentMode == HierarchyMode.Chart)
                {
                    Ray ray = MainCamera.ScreenPointToRay(eventData.position);
                    RaycastHit[] raycastHits = Physics.RaycastAll(ray, 1000, -1, QueryTriggerInteraction.Collide);
                    Array.Sort(raycastHits, (x, y) => x.distance.CompareTo(y.distance));
                    foreach (RaycastHit raycastHit in raycastHits)
                    {
                        PlayerViewPickHandler pickHandler = raycastHit.collider.GetComponent<PlayerViewPickHandler>();
                       
                        if (pickHandler && pickHandler.Pick(eventData)) 
                            break;
                    }
                }

                return;
            }

            if (HierarchyPanel.main.CurrentMode == HierarchyMode.PlayableSong && CurrentDragMode == HandleDragMode.Background)
            {
                OnDragEvent += (ev) =>
                {
                    CoverPosition += ev.delta;
                };
            }
            else switch (InspectorPanel.main.CurrentObject)
            {
            
                case CoverLayer layer:
                {
                    float scale = CoverBackground.rectTransform.localScale.x;
                    Vector2 offset = new (0, 16);

                    OnDragEvent += (ev) => 
                    {
                        ChartmakerHistory history = Chartmaker.main.History;
                        switch (CurrentDragMode)
                        {
                            case HandleDragMode.Center:
                                history.SetItem(layer, "Position", layer.Position + ev.delta / scale); break;
                            case HandleDragMode.Start:
                                history.SetItem(layer, "Scale", layer.Scale + ev.delta.x / 440 / scale); break;
                        }
                        Chartmaker.main.OnHistoryUpdate();
                    };
                }
                    break;

                case Lane lane:
                {
                    int index = Chartmaker.main.CurrentChart.Lanes.IndexOf(lane);
                    if (index < 0) return;
                    LaneManager laneManager = Manager.Lanes[index];
                    LaneGroupManager laneGroupManager = null;
                    bool hasGroup = laneManager.Current.GroupUuid != 0
                                    && Manager.Groups.TryGetValue(laneManager.Current.GroupUuid, out laneGroupManager);
                
                    Vector3 Inv(Vector3 x)      => Quaternion.Inverse(laneManager.FinalRotation) * (x - laneManager.FinalPosition);
                    Vector3 GroupInv(Vector3 x) => hasGroup ? Quaternion.Inverse(laneGroupManager.FinalRotation) * (x - laneGroupManager.FinalPosition) : x;

                    Func<Vector3> get = 
                        CurrentDragMode switch
                        {
                            HandleDragMode.Start => (() => Inv(laneManager.StartPos)),
                            HandleDragMode.Center => (() => GroupInv(laneManager.FinalPosition)),
                            HandleDragMode.End => (() => Inv(laneManager.EndPos)),
                            _ => null
                        };
                    
                    Vector3 gizmoAnchor = get();
                
                    OnDragEvent += (ev) => {
                        Vector3? dragPosNull = CurrentDragMode == HandleDragMode.Center 
                            ? (hasGroup
                                ? RaycastScreenToPlane(ev.position, laneGroupManager!.FinalPosition + laneGroupManager.FinalRotation * Vector3.forward * get().z, laneGroupManager.FinalRotation)
                                : RaycastScreenToPlane(ev.position, Vector3.forward * get().z, Quaternion.identity))
                            : RaycastScreenToPlane(ev.position, laneManager.FinalPosition + laneManager.FinalRotation * Vector3.forward * get().z, laneManager.FinalRotation);
                        Vector3 dragPos;
                        if (dragPosNull != null)
                        {
                            if (CurrentDragMode is HandleDragMode.Center)
                                dragPos = GroupInv((Vector3)dragPosNull);
                            else
                                dragPos = Inv((Vector3)dragPosNull);
                        
                            if (GridSize[0] > 0)
                            {
                                Vector3 des = new();
                            
                                for (int x = 0; x < 3; x++) 
                                    des[x] = Mathf.Round(dragPos[x] / GridSize[0]) * GridSize[0];
                            
                                dragPos = des;
                            }
                        }
                        else
                            dragPos = gizmoAnchor;
                
                        switch (CurrentDragMode)
                        {
                            case HandleDragMode.Start:
                                DoMove<ChartmakerMoveLaneStartAction, Lane>(lane, (Vector3)dragPos - get()); break;
                            case HandleDragMode.Center:
                                DoMove<ChartmakerMoveLaneAction, Lane>(lane, (Vector3)dragPos - get()); break;
                            case HandleDragMode.End:
                                DoMove<ChartmakerMoveLaneEndAction, Lane>(lane, (Vector3)dragPos - get()); break;
                        }
                    };                  
                } 
                    break;
            
                case LaneStep step:
                {
                    if (InspectorPanel.main.CurrentHierarchyObject is not Lane currentLane) 
                        return;

                    int laneIndex = Chartmaker.main.CurrentChart.Lanes.IndexOf(currentLane);
                    if (laneIndex < 0) 
                        return;
                
                    LaneManager laneManager = Manager.Lanes[laneIndex];

                    int index = currentLane.LaneSteps.IndexOf(step);
                    if (index < 0) 
                        return;
                
                    LaneStepManager laneStepManager = laneManager.Steps[index];

                    Vector3 Inv(Vector3 x) => Quaternion.Inverse(laneManager.FinalRotation) * (x - laneManager.FinalPosition);

                    Func<Vector3> get = 
                        CurrentDragMode switch
                        {
                            HandleDragMode.Start => (() => laneStepManager.CurrentStep.StartPointPosition),
                            HandleDragMode.Center => (() => (laneStepManager.CurrentStep.StartPointPosition + laneStepManager.CurrentStep.EndPointPosition) / 2),
                            HandleDragMode.End => (() => laneStepManager.CurrentStep.EndPointPosition),
                            _ => null
                        };
                    
                    Vector3 gizmoAnchor = get!();

                    OnDragEvent += (ev) => {
                        Vector3? dragPos = 
                            RaycastScreenToPlane(ev.position, laneManager.FinalPosition + laneManager.FinalRotation * Vector3.forward * (laneStepManager.Distance - laneManager.CurrentDistance), laneManager.FinalRotation);
                    
                        if (dragPos != null)
                        {
                            dragPos = Inv((Vector3)dragPos);
                        
                            if (GridSize[0] > 0)
                            {
                                Vector3 des = new();
                            
                                for (int x = 0; x < 3; x++)
                                    des[x] = Mathf.Round((dragPos?[x] ?? 0) / GridSize[0]) * GridSize[0];
                            
                                dragPos = des;
                            } 
                        }
                        else
                        {
                            dragPos = gizmoAnchor;
                        }
                
                        switch (CurrentDragMode)
                        {
                            case HandleDragMode.Start:
                                DoMove<ChartmakerMoveLaneStepStartAction, LaneStep>(step, (Vector3)dragPos - get()); break;
                            case HandleDragMode.Center:
                                DoMove<ChartmakerMoveLaneStepAction, LaneStep>(step, (Vector3)dragPos - get()); break;
                            case HandleDragMode.End:
                                DoMove<ChartmakerMoveLaneStepEndAction, LaneStep>(step, (Vector3)dragPos - get()); break;
                        }
                    };
                }
                    break;
            
                case HitObject hit:
                {
                    if (InspectorPanel.main.CurrentHierarchyObject is not Lane lane) 
                        return;

                    int lindex = Chartmaker.main.CurrentChart.Lanes.IndexOf(lane);
                
                    if (lindex < 0)
                        return;
                
                    LaneManager laneManager = Manager.Lanes[lindex];

                    int index = lane.Objects.IndexOf(hit);
                    if (index < 0)
                        return;
                
                    HitObjectManager hitObjectManager = laneManager.Objects[index];
                
                    Vector3 Inv(Vector3 x)
                    {
                        Vector3 point = Quaternion.Inverse(laneManager.FinalRotation) * (x - laneManager.FinalPosition) - Vector3.forward * (hitObjectManager.Position.z - laneManager.CurrentDistance);
                        return Vector3.right * (Quaternion.Euler(0, 0, Vector2.SignedAngle(laneManager.EndPosLocal - laneManager.StartPosLocal, Vector2.right)) * (point - laneManager.StartPosLocal)).x / Vector2.Distance(laneManager.StartPosLocal, laneManager.EndPosLocal);
                    }

                    Func<Vector3> get = CurrentDragMode switch
                    {
                        HandleDragMode.Start => (() => Vector3.right * hitObjectManager.Current.Position),
                        HandleDragMode.Center => (() => Vector3.right * (hitObjectManager.Current.Position + hitObjectManager.Current.Length / 2)),
                        HandleDragMode.End => (() => Vector3.right * (hitObjectManager.Current.Position + hitObjectManager.Current.Length)),
                        _ => null
                    };
                    
                    Vector3 gizmoAnchor = get!();

                    OnDragEvent += (PointerEventData ev) => 
                    {
                        Vector3? dragPos = 
                            RaycastScreenToPlane(
                                ev.position, 
                                laneManager.FinalPosition + laneManager.FinalRotation * Vector3.forward * (hitObjectManager.Position.z - laneManager.CurrentDistance), 
                                laneManager.FinalRotation);
                   
                        if (dragPos != null)
                        {
                            dragPos = Inv((Vector3)dragPos);
                        
                            if (GridSize[0] > 0)
                            {
                                Vector3 des = new();
                                des[0] = Mathf.Round((dragPos?[0] ?? 0) / 0.05f) * 0.05f;
                                dragPos = des;
                            } 
                        }
                        else
                            dragPos = gizmoAnchor;
                
                        switch (CurrentDragMode)
                        {
                            case HandleDragMode.Start:
                                DoMove<ChartmakerMoveHitObjectStartAction, HitObject>(hit, (Vector3)dragPos - get()); break;
                            case HandleDragMode.Center:
                                DoMove<ChartmakerMoveHitObjectAction, HitObject>(hit, (Vector3)dragPos - get()); break;
                            case HandleDragMode.End:
                                DoMove<ChartmakerMoveHitObjectEndAction, HitObject>(hit, (Vector3)dragPos - get()); break;
                        }
                    };
                }
                    break;
            }
        
            UpdateHandles();
            UpdateCursor(eventData.position, eventData.pressEventCamera);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragged)
            {
                OnEndDrag(eventData);
            }
        }

        CursorStyle CurrentCursor = 0;

        public void UpdateCursor(Vector2 position, Camera eventCamera)
        {
            bool contains(RectTransform rt) => rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, position, eventCamera);

            CursorStyle Cursor = CursorStyle.None;

            if (CurrentDragMode != HandleDragMode.None) 
            {
                Cursor = CursorStyle.HandGrabbing;
            }
            else if (contains((RectTransform)transform)) 
            {
                if (
                    (!contains((RectTransform)CoverToolbar.transform)) &&
                    (contains(StartHandle) || contains(CenterHandle) || contains(EndHandle))
                ) Cursor = CursorStyle.HandGrabReady;
            }

            if (CurrentCursor != Cursor)
            {
                if (CurrentCursor != 0) 
                    CursorManager.main.PopCursor();
                if (Cursor != 0) 
                    CursorManager.main.PushCursor(Cursor);
                CurrentCursor = Cursor;
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isDragged)
            {
                UpdateCursor(eventData.position, eventData.pressEventCamera);
            }
        }

        public delegate void PointerEvent(PointerEventData eventData);

        public void OnDrag(PointerEventData eventData) 
        {
            if (CurrentDragMode != HandleDragMode.None)
            {
                isDragged = true;
                OnDragEvent?.Invoke(eventData);
                UpdateObjects();
            }
        }

        public PointerEvent OnDragEvent;

        public void OnEndDrag(PointerEventData eventData)
        {
            if (CurrentDragMode != HandleDragMode.None)
            {
                InspectorPanel.main.UpdateForm();
                TimelinePanel.main.UpdateItems();
            }
            isDragged = false;
            OnDragEvent = null;
            CurrentDragMode = HandleDragMode.None;
            UpdateHandles();
            UpdateCursor(eventData.position, eventData.pressEventCamera);
        }
    
        public Vector3? RaycastScreenToPlane(Vector3 pos, Vector3 center, Quaternion rotation)
        {
            Plane plane = new (rotation * Vector3.back, center);
            Ray ray = MainCamera.ScreenPointToRay(new Vector2(pos.x, pos.y));
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            return null;
        }

        public Rect Rect2UV(Rect parent, Rect child) 
        {
            return new(
                (parent.min - child.min) / child.size,
                parent.size / child.size
            );
        }

        public void DoMove<TAction, TTarget>(TTarget item, Vector3 offset) where TAction : ChartmakerMoveAction<TTarget>, new()
        {
            if (offset == Vector3.zero) return;

            TAction action = null;
            ChartmakerHistory history = Chartmaker.main.History;

            if (history.ActionsBehind.Count > 0 && history.ActionsBehind.Peek() is TAction)
            {
                action = (TAction)history.ActionsBehind.Peek();
                if (!action.Item.Equals(item)) action = null;
            }

            if (action == null)
            {
                action = new()
                {
                    Item = item
                };
                history.ActionsBehind.Push(action);
            }
            history.ActionsAhead.Clear();

            action.Undo();
            action.Offset += offset;
            action.Redo();

            Chartmaker.main.OnHistoryUpdate();
        }

        public void SetCoverViewMode(int mode) 
        {
            SetCoverViewMode((CoverViewMode)mode);
        }

        public void SetCoverViewMode(CoverViewMode mode) 
        {
            CurrentCoverViewMode = mode;
            UpdateObjects();
        }

        public void MoveCoverToCenter()
        {
            if (!isAnimating) StartCoroutine(MoveCoverToCenterAnim());
        }

        IEnumerator MoveCoverToCenterAnim()
        {
            isAnimating = true;

            Vector2 posStart = CoverPosition;

            void Animate1(float t) 
            {
                float ease = Ease.Get(t, EaseFunction.Cubic, EaseMode.Out);

                CoverPosition = posStart * (1 - ease);
                UpdateObjects();
            }
            for (float t = 0; t < 1; t += Time.deltaTime / .2f) { Animate1(t); yield return null; }
            Animate1(1);

            isAnimating = false;
        }

        public void ClearObjects()
        {
            if (Manager == null) return;
            Manager.Dispose();
            Manager = null;
            foreach (ChartmakerLanePlayer lane in LanePlayers) {
                foreach (ChartmakerHitPlayer hit in lane.HitPlayers) Destroy (hit.gameObject);
                Destroy(lane.gameObject);
            }
            LanePlayers.Clear();
            foreach (var pair in LaneGroupPlayers)
                Destroy(pair.Value.gameObject);
            LaneGroupPlayers.Clear();
        }

        public void UpdateIconFile() 
        {
            Vector2Int resolution = new (128, 128);

            Transform originalParent = CoverBackground.rectTransform.parent;
            IconRenderCanvas.gameObject.SetActive(true);
            // Set the canvas size first — it's used in the scale calculation below.
            IconRenderCanvas.sizeDelta = Vector2.one * resolution.x;
            CoverBackground.rectTransform.SetParent(IconRenderCanvas);
            CoverBackground.rectTransform.sizeDelta = Vector2.one * Chartmaker.main.CurrentSong.Cover.IconSize;
            CoverBackground.rectTransform.localScale = Vector2.one * IconRenderCanvas.sizeDelta.x / Chartmaker.main.CurrentSong.Cover.IconSize;
            CoverBackground.rectTransform.anchoredPosition3D = Vector3.zero;
            CoverBackground.rectTransform.localRotation = Quaternion.identity;
            // Disable mask so it doesn't crop layers during the icon render.
            CoverBackground.GetComponent<RectMask2D>().enabled = false;
        
            Vector2 parallaxOffset = Chartmaker.main.CurrentSong.Cover.IconCenter;
        
            int index = 0;
            foreach (CoverLayer layer in Chartmaker.main.CurrentSong.Cover.Layers) {
                RawImage image = CoverLayers[index];

                image.texture = layer.Texture;
                image.rectTransform.localRotation = Quaternion.identity;
            
                if (layer.Tiling)
                {
                    image.rectTransform.sizeDelta = CoverBackground.rectTransform.sizeDelta;
                    image.rectTransform.anchoredPosition3D = Vector2.zero;
                    Vector2 imgSize = new Vector2(1, (float)layer.Texture.height / layer.Texture.width) * (880 * layer.Scale);
                    image.uvRect = Rect2UV(new (
                        -CoverBackground.rectTransform.sizeDelta * .5f,
                        CoverBackground.rectTransform.sizeDelta
                    ), new (
                        layer.Position + parallaxOffset * layer.ParallaxFactor - imgSize * .5f,
                        imgSize
                    ));
                }
                else 
                {
                    image.rectTransform.sizeDelta = new Vector2(1, (float)layer.Texture.height / layer.Texture.width) * (layer.Scale * 880);
                    image.rectTransform.anchoredPosition3D = layer.Position + parallaxOffset * layer.ParallaxFactor;
                    image.uvRect = new (0, 0, 1, 1);
                }

                index++;
            }

            // Force the canvas to rebuild layout before rendering.
            Canvas.ForceUpdateCanvases();

            RenderTexture rtex = new (resolution.x, resolution.y, 24);
            RenderTexture.active = rtex;
            rtex.Create();

            Camera camera = Camera.main;
            camera.targetTexture = rtex;
            camera.rect = new Rect(0, 0, 1, 1); // normalized viewport coords, not pixels
            camera.Render();

            Texture2D tex = new (resolution.x, resolution.y);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(
                Path.Combine(Path.GetDirectoryName(Chartmaker.main.CurrentSongPath), Chartmaker.main.CurrentSong.Cover.IconTarget), 
                tex.EncodeToPNG()
            );
        
        
            RenderTexture.active = camera.targetTexture = null;
            camera.rect = new Rect(0, 0, 1, 1);
            Destroy(tex);
            Destroy(rtex);

            IconRenderCanvas.gameObject.SetActive(false);
            CoverBackground.rectTransform.SetParent(originalParent);
            CoverBackground.rectTransform.localRotation = Quaternion.identity;
            CoverBackground.rectTransform.anchoredPosition3D = Vector3.zero;
            CoverBackground.GetComponent<RectMask2D>().enabled = true;
            UpdateObjects();
        }

            // Time windows telling the view which lanes are worth updating on a given frame.
            // 
            // The Client solves this with a forward-only cursor over lanes sorted by cue time,
            // destroying each lane once passed. The Chartmaker cannot: time scrubs backwards, so
            // a passed lane has to be able to come back. This keeps every lane's window in a
            // sorted list instead and answers "which lanes overlap this instant" by binary
            // search. Windows are in seconds, matching LaneStepManager.Offset and the cue
            // formula below.
        class LaneWindowIndex
        {
            // Ported from PlayerScreen.cs in the Client, which arrived at these by playtesting.
            // The lead-time cap is what makes storyboarded Speed a non-issue: an exact
            // distance/speed lead would be wrong the moment Speed animates, but a capped one
            // only has to be generous.
            const float VisibilityDistance = 200f;
            const float MaxLeadTime        = 5f;
            const float GraceTime          = 5f;

            struct Window
            {
                public float Cue;
                public float End;
                public int   LaneIndex;
            }

            // Sorted by Cue.
            readonly List<Window> _Windows = new();

            // Indices into _Windows, sorted by End.
            readonly List<int> _ByEnd = new();

            // _EnterCursor counts windows whose Cue has passed; _ExitCursor counts windows
            // whose End has passed. A lane is active when it has entered and not exited, so
            // moving time only has to walk the boundaries actually crossed.
            int _EnterCursor;
            int _ExitCursor;

            float _LastTime;
            bool  _Dirty = true;

            // <summary>Marks the index stale; the rebuild happens on the next query.</summary>
            public void Invalidate() => _Dirty = true;

            // Updates <paramref name="mask"/> to one flag per lane, rebuilding first if
            // needed so callers never have to sequence Invalidate against Rebuild.
            // The mask carries across calls — it is stepped, not recomputed.
            public void GetActive(Chart chart, PlayableSong song, float speed, float time, ref bool[] mask)
            {
                int laneCount = chart.Lanes.Count;
                bool resized = mask == null || mask.Length < laneCount;

                if (resized) mask = new bool[laneCount];

                if (_Dirty)
                {
                    Rebuild(chart, song, speed);
                    Reset(time, mask, laneCount);

                    return;
                }

                // A fresh array has none of the previous state to step from.
                if (resized)
                {
                    Reset(time, mask, laneCount);

                    return;
                }

                if (time >= _LastTime) StepForward(time, mask);
                else                   StepBackward(time, mask);

                _LastTime = time;
            }

            // <summary>Rebuilds the mask and both cursors from scratch.</summary>
            void Reset(float time, bool[] mask, int laneCount)
            {
                for (var i = 0; i < laneCount; i++)
                    mask[i] = false;

                _EnterCursor = CountCueAtOrBefore(time);
                _ExitCursor  = CountEndBefore(time);

                for (var i = 0; i < _EnterCursor; i++)
                    if (_Windows[i].End >= time)
                        mask[_Windows[i].LaneIndex] = true;

                _LastTime = time;
            }

            // Enters before exits: a window crossed in both directions this step must end up
            // inactive, and Cue <= End guarantees the exit loop has the last word.
            void StepForward(float time, bool[] mask)
            {
                while (_EnterCursor < _Windows.Count && _Windows[_EnterCursor].Cue <= time)
                {
                    mask[_Windows[_EnterCursor].LaneIndex] = true;
                    _EnterCursor++;
                }

                while (_ExitCursor < _ByEnd.Count && _Windows[_ByEnd[_ExitCursor]].End < time)
                {
                    mask[_Windows[_ByEnd[_ExitCursor]].LaneIndex] = false;
                    _ExitCursor++;
                }
            }

            // Mirror of StepForward: un-exits before un-enters, since a window retreating past
            // its Cue has necessarily retreated past its End too.
            void StepBackward(float time, bool[] mask)
            {
                while (_ExitCursor > 0 && _Windows[_ByEnd[_ExitCursor - 1]].End >= time)
                {
                    _ExitCursor--;
                    mask[_Windows[_ByEnd[_ExitCursor]].LaneIndex] = true;
                }

                while (_EnterCursor > 0 && _Windows[_EnterCursor - 1].Cue > time)
                {
                    _EnterCursor--;
                    mask[_Windows[_EnterCursor].LaneIndex] = false;
                }
            }

            void Rebuild(Chart chart, PlayableSong song, float speed)
            {
                _Windows.Clear();
                _ByEnd.Clear();

                for (var a = 0; a < chart.Lanes.Count; a++)
                {
                    Lane lane = chart.Lanes[a];

                    // No steps means no geometry and no window; it stays culled.
                    if (lane.LaneSteps.Count == 0) continue;

                    // LaneSteps are offset-sorted — ChartManager.FindStepIndex binary-searches
                    // them — so [0] and [^1] are the true extremes without scanning.
                    float start = song.Timing.ToSeconds(lane.LaneSteps[0].Offset);
                    float end   = song.Timing.ToSeconds(lane.LaneSteps[^1].Offset);

                    float laneSpeed = Mathf.Abs(lane.LaneSteps[0].Speed) * speed;

                    float cue = laneSpeed > 0.0001f
                        ? start - Mathf.Min(VisibilityDistance / laneSpeed, MaxLeadTime) - GraceTime
                        : float.NegativeInfinity;

                    // A lane's own storyboard (typically a Group-driven flight) can start well
                    // before its steps do; without this the lane pops in mid-animation.
                    // Storyboard.Timestamps is kept offset-sorted on insert.
                    if (lane.Storyboard.Timestamps.Count > 0)
                        cue = Mathf.Min(cue, song.Timing.ToSeconds(lane.Storyboard.Timestamps[0].Offset) - GraceTime);

                    _Windows.Add(new Window { Cue = cue, End = end + GraceTime, LaneIndex = a });
                }

                _Windows.Sort((x, y) => x.Cue.CompareTo(y.Cue));

                for (var a = 0; a < _Windows.Count; a++)
                    _ByEnd.Add(a);

                _ByEnd.Sort((x, y) => _Windows[x].End.CompareTo(_Windows[y].End));

                _Dirty = false;
            }

            // <summary>How many windows have a Cue at or before <paramref name="time"/>.</summary>
            int CountCueAtOrBefore(float time)
            {
                int lo = 0, hi = _Windows.Count;

                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;

                    if (_Windows[mid].Cue <= time) lo = mid + 1;
                    else hi = mid;
                }

                return lo;
            }

            // <summary>How many windows have an End strictly before <paramref name="time"/>.</summary>
            int CountEndBefore(float time)
            {
                int lo = 0, hi = _ByEnd.Count;

                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;

                    if (_Windows[_ByEnd[mid]].End < time) lo = mid + 1;
                    else hi = mid;
                }

                return lo;
            }
        }
    }

    public enum HandleDragMode
    {
        None,
        Start,
        Center,
        End,
        Background,
    }

    public enum CoverViewMode 
    {
        Panorama = 0,
        Icon     = 1
    }
}
