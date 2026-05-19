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
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JANOARG.Chartmaker.Behaviors.Chartmaker.PickHandler;

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
        public Dictionary<string, ChartmakerLaneGroupPlayer> LaneGroupPlayers { get; private set; } = new();
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
            get
            {
                return
                    HierarchyPanel.main.IsCollapsed
                    && InspectorPanel.main.IsCollapsed
                    && TimelinePanel.main.TimelineHeight <= 0
                ;
            }
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
                UpdateObjects();
            lastTargetAspect = targetAspect;
        }

        public void UpdateObjects() => UpdateObjects(InformationBar.main.sec, InformationBar.main.beat);

        public void UpdateObjects(float sec, float beat)
        {
            CurrentTime = sec;

            if (Chartmaker.main.CurrentChart != null)
            {
                if (Chartmaker.main.CurrentChart != Manager?.CurrentChart) 
                    Manager = new ChartManager(
                        Chartmaker.main.CurrentSong,
                        Chartmaker.main.CurrentChart,
                        speed: 121, 
                        time:  sec, 
                        pos:   beat
                    );
                else 
                    Manager!.Update(sec, beat);
            
                MainCamera.transform.position = Manager.Camera.CameraPivot;
                MainCamera.transform.eulerAngles = Manager.Camera.CameraRotation; 
                MainCamera.transform.Translate(Vector3.back * Manager.Camera.PivotDistance);

                RenderSettings.fogColor = MainCamera.backgroundColor = Manager.PalleteManager.CurrentPallete.BackgroundColor;
                BoundingBox.color = NotificationText.color = NotificationBox.color = Manager.PalleteManager.CurrentPallete.InterfaceColor;

                // Pass 1: sync group player dict to Manager.Groups (post-ChartManager.Update,
                // so duplicates in chart.Groups are already collapsed by the Dictionary).
                foreach (var pair in LaneGroupPlayers)
                    pair.Value.CurrentGroup = null;

                foreach (var pair in Manager.Groups)
                {
                    string groupName = pair.Key;
                    if (!LaneGroupPlayers.TryGetValue(groupName, out ChartmakerLaneGroupPlayer groupPlayer))
                    {
                        groupPlayer = Instantiate(LaneGroupPlayerSample, Holder);
                        #if UNITY_EDITOR
                        groupPlayer.gameObject.name = $"Lane Group ({groupName})";
                        #endif
                        LaneGroupPlayers[groupName] = groupPlayer;
                    }
                    groupPlayer.CurrentGroup = pair.Value;
                }

                // Destroy group players no longer in Manager.Groups.
                var toRemove = new System.Collections.Generic.List<string>();
                foreach (var pair in LaneGroupPlayers)
                    if (pair.Value.CurrentGroup == null) { Destroy(pair.Value.gameObject); toRemove.Add(pair.Key); }
                foreach (string key in toRemove) LaneGroupPlayers.Remove(key);

                // Pass 2: resolve GO parent hierarchy BEFORE applying any local transforms.
                foreach (var pair in LaneGroupPlayers)
                {
                    string parentGroupName = pair.Value.CurrentGroup.CurrentGroup.Group;
                    Transform desiredParent = !string.IsNullOrEmpty(parentGroupName) && LaneGroupPlayers.TryGetValue(parentGroupName, out ChartmakerLaneGroupPlayer parentPlayer)
                        ? parentPlayer.transform
                        : Holder;
                    if (pair.Value.transform.parent != desiredParent)
                        pair.Value.transform.SetParent(desiredParent, worldPositionStays: false);
                }

                // Pass 3: apply local transforms — hierarchy is now correct.
                foreach (var pair in LaneGroupPlayers)
                    pair.Value.UpdateObjects(pair.Value.CurrentGroup);

                // Update lane players, parenting each under its group player (or Holder if ungrouped).
                for (int a = 0; a < Manager.Lanes.Count; a++)
                {
                    LaneManager laneManager = Manager.Lanes[a];
                    string laneGroupName = laneManager.Current.Group;

                    Transform desiredParent = !string.IsNullOrEmpty(laneGroupName)
                        ? (LaneGroupPlayers.TryGetValue(laneGroupName, out ChartmakerLaneGroupPlayer laneGroupPlayer) ? laneGroupPlayer.transform : Holder)
                        : Holder;

                    if (LanePlayers.Count <= a)
                    {
                        LanePlayers.Add(Instantiate(LanePlayerSample, desiredParent));
                        #if UNITY_EDITOR
                        string beatRange = $"Lane ({(BeatPosition)laneManager.Steps[0].Offset} > {(BeatPosition)laneManager.Steps[^1].Offset})";
                        LanePlayers[a].gameObject.name = string.IsNullOrEmpty(laneManager.Current.Name) ? beatRange : laneManager.Current.Name;
                        #endif
                    }
                    else if (LanePlayers[a].transform.parent != desiredParent)
                        LanePlayers[a].transform.SetParent(desiredParent, worldPositionStays: false);

                    LanePlayers[a].UpdateObjects(laneManager);
                }

                while (LanePlayers.Count > Manager.Lanes.Count)
                {
                    Destroy(LanePlayers[Manager.Lanes.Count].gameObject);
                    LanePlayers.RemoveAt(Manager.Lanes.Count);
                }
            
                if (Chartmaker.main.SongSource.isPlaying && !TimelinePanel.main.isDragged && PlayOptions.HitsoundsVolume > 0)
                {
                    if (Manager.HitObjectsRemaining[0] < HitObjectsRemaining[0])
                        SoundPlayer.PlayOneShot(Chartmaker.Preferences.PerfectHitsounds ? AltNormalHitSound : NormalHitSound, PlayOptions.HitsoundsVolume);
                
                    if (Manager.HitObjectsRemaining[1] < HitObjectsRemaining[1])
                        SoundPlayer.PlayOneShot(Chartmaker.Preferences.PerfectHitsounds ? AltCatchHitSound : CatchHitSound, PlayOptions.HitsoundsVolume);
               
                    if (Manager.FlicksRemaining < FlicksRemaining && !Chartmaker.Preferences.PerfectHitsounds)
                        SoundPlayer.PlayOneShot(FlickSound, PlayOptions.HitsoundsVolume);
                }
                HitObjectsRemaining = Manager.HitObjectsRemaining;
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
                    bool hasGroup = !string.IsNullOrEmpty(laneManager.Current.Group) 
                                    && Manager.Groups.TryGetValue(laneManager.Current.Group, out laneGroupManager);
                
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

        CursorType CurrentCursor = 0;

        public void UpdateCursor(Vector2 position, Camera eventCamera)
        {
            bool contains(RectTransform rt) => rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, position, eventCamera);

            CursorType Cursor = 0;

            if (CurrentDragMode != HandleDragMode.None) 
            {
                Cursor = CursorType.Grabbing;
            }
            else if (contains((RectTransform)transform)) 
            {
                if (
                    (!contains((RectTransform)CoverToolbar.transform)) &&
                    (contains(StartHandle) || contains(CenterHandle) || contains(EndHandle))
                ) Cursor = CursorType.Grab;
            }

            if (CurrentCursor != Cursor)
            {
                if (CurrentCursor != 0) CursorChanger.PopCursor();
                if (Cursor != 0) CursorChanger.PushCursor(Cursor);
                CurrentCursor = Cursor;
                BorderlessWindow.UpdateCursor();
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