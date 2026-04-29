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

        [Header("Score")]
        public TMP_Text EXScore;
        public TMP_Text MaxStreak;

        
        private void Update()
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


            LaneStyleCount.text = HightlightedChart.Palette.LaneStyles.Count.ToString();
            HitStyleCount.text = HightlightedChart.Palette.HitStyles.Count.ToString();

            LaneCount.text = HightlightedChart.Lanes.Count.ToString();
            LaneGroupCount.text = HightlightedChart.Groups.Count.ToString();

            // Get Max Streak
            // Get all notes 
                // Normal
                // Hold Ticks
                // Catch
                // Omnidirectional Flicks
                // Directional Flicks
            // EX Score 
            // Get all lane count
            // Get all lane group count

        }
    }
    
}