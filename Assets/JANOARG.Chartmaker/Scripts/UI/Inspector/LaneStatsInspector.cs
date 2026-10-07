using System;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Inspector
{
    public class LaneStatsInspector : MonoBehaviour
    {
        public Lane HightlightedLane;
        [Header( "Lane Step" )]
        public TMP_Text LaneStep;
        [Header( "Hit Objects" )]
        public HitObjectCountDisplay HitObjectCountDisplay;

        void Start()
        {
            UpdateStats();
        }

        void UpdateStats()
        {
            if (HightlightedLane == null)
            {
                LaneStep.text = "-";
                HitObjectCountDisplay.UpdateStatsIndeterminate();
                return;
            }

            LaneStep.text = HightlightedLane.LaneSteps.Count.ToString();

            HitObjectCountDisplay.UpdateStats(HightlightedLane.Objects);
        }
    }
}