using System;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Inspector
{
    public class ChartStatsInspector : MonoBehaviour
    {
        public Chart HightlightedChart;

        [Header("Palette")]
        public TMP_Text LaneStyleCount;
        public TMP_Text HitStyleCount;

        [Header( "Overall Lane Stats" )] 
        public TMP_Text LaneCount;
        public TMP_Text LaneGroupCount;
        public TMP_Text LaneStep;

        [Header( "Hit Objects" )]
        public TMP_Text TotalHitObjects;
        public TMP_Text Taps;
        public TMP_Text Catches;
        public TMP_Text Flickables;
        public TMP_Text Holds;
        public TMP_Text HoldTicks;

        [Header("Score")]
        public TMP_Text EXScore;
        public TMP_Text MaxStreak;

        void Start()
        {
            UpdateStats();
        }

        void UpdateStats()
        {
            if (HightlightedChart == null)
            {
                LaneStyleCount.text = "-";
                HitStyleCount.text = "-";
                LaneCount.text = "-";
                LaneGroupCount.text = "-";
                LaneStep.text = "-";
                TotalHitObjects.text = "-";
                Taps.text = "-";
                Catches.text = "-";
                Flickables.text = "-";
                Holds.text = "-";
                EXScore.text = "-";
                MaxStreak.text = "-";
                return;
            }

            // Palette
            LaneStyleCount.text = HightlightedChart.Palette.LaneStyles.Count.ToString();
            HitStyleCount.text = HightlightedChart.Palette.HitStyles.Count.ToString();

            // Overall Lane Stats
            LaneCount.text = HightlightedChart.Lanes.Count.ToString();
            LaneGroupCount.text = HightlightedChart.Groups.Count.ToString();

            int laneStepCount = 0;
            int totalHitObjects = 0;
            int taps = 0;
            int catches = 0;
            int omniFlickables = 0;
            int directionalFlickables = 0;
            int tapHolds = 0;
            int catchHolds = 0;
            int holdTicks = 0;

            foreach (var lane in HightlightedChart.Lanes)
            {
                laneStepCount += lane.LaneSteps.Count; 
                
                var objects = lane.Objects;
                totalHitObjects += objects.Count;

                // Hit Object Count
                foreach (var obj in objects)
                {
                    if (obj.Type is HitObject.HitType.Normal)
                        taps++;
                    else if (obj.Type is HitObject.HitType.Catch)
                        catches++;
        
                    if (obj.Flickable)
                    {
                        if (float.IsFinite(obj.FlickDirection))
                            directionalFlickables++;
                        else
                            omniFlickables++;
                    }
                    if (obj.HoldLength > 0)
                    {
                        if (obj.Type is HitObject.HitType.Normal)
                            tapHolds++;
                        else if (obj.Type is HitObject.HitType.Catch)
                            catchHolds++;
                        holdTicks += Mathf.CeilToInt(obj.HoldLength / 0.5f);
                    }
                }
            }

            LaneStep.text = laneStepCount.ToString();
            TotalHitObjects.text = totalHitObjects.ToString();
            Taps.text = taps.ToString();
            Catches.text = catches.ToString();
            Flickables.text = $"{omniFlickables}/{directionalFlickables}";
            Holds.text = $"{tapHolds}/{catchHolds}";
            HoldTicks.text = holdTicks.ToString();

            // Score
            // Get Max Streak
            // EX Score 

            EXScore.text = (
                (taps * 3) +
                catches +
                holdTicks +
                omniFlickables +
                (directionalFlickables * 2)
            ).ToString();
            
            MaxStreak.text = (totalHitObjects + holdTicks).ToString();
        }
    }
    
}