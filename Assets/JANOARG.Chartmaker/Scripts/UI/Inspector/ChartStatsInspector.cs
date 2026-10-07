using System;
using System.Linq;
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
        public HitObjectCountDisplay HitObjectCountDisplay;

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
                HitObjectCountDisplay.UpdateStatsIndeterminate();
                return;
            }

            // Palette
            LaneStyleCount.text = HightlightedChart.Palette.LaneStyles.Count.ToString();
            HitStyleCount.text = HightlightedChart.Palette.HitStyles.Count.ToString();

            // Overall Lane Stats
            LaneCount.text = HightlightedChart.Lanes.Count.ToString();
            LaneGroupCount.text = HightlightedChart.Groups.Count.ToString();

            int laneStepCount = 0;
            foreach (var lane in HightlightedChart.Lanes)
            {
                laneStepCount += lane.LaneSteps.Count; 
            }
            LaneStep.text = laneStepCount.ToString();

            HitObjectCountDisplay.UpdateStats(HightlightedChart.Lanes.SelectMany(x => x.Objects));
        }
    }
    
}