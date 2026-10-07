
using System.Collections.Generic;
using JANOARG.Chartmaker.UI.Inspector.Utils;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Inspector
{
    public class HitObjectCountDisplay : MonoBehaviour
    {
        public TMP_Text HitObjectCount;
        public TMP_Text HitObjectTapCatchCount;
        public TMP_Text FlickCount;
        public TMP_Text FlickTapCatchCount;
        public TMP_Text FlickDirOmniCount;
        public TMP_Text HoldCount;
        public TMP_Text HoldTapCatchCount;
        public TMP_Text HoldTickCount;
        public TMP_Text FakeCount;
        public TMP_Text FakeTapCatchCount;
        [Space]
        public TMP_Text EXScore;
        public TMP_Text MaxStreak;

        public void UpdateStatsIndeterminate()
        {
            HitObjectCount.text = "-";
            HitObjectTapCatchCount.text = "-";
            FlickCount.text = "-";
            FlickTapCatchCount.text = "-";
            FlickDirOmniCount.text = "-";
            HoldCount.text = "-";
            HoldTapCatchCount.text = "-";
            HoldTickCount.text = "-";
            FakeCount.text = "-";
            FakeTapCatchCount.text = "-";
            EXScore.text = "-";
            MaxStreak.text = "-";
            return;
        }

        public void UpdateStats(IEnumerable<HitObject> objects)
        {
            var countResult = HitObjectCounter.Count(objects);

            HitObjectCount.text = $"{countResult.TotalCount}";
            HitObjectTapCatchCount.text =  $"({countResult.TotalTapCount}+{countResult.TotalCatchCount})";
            FlickCount.text = $"{countResult.FlickCount}";
            FlickTapCatchCount.text =  $"({countResult.FlickTapCount}+{countResult.FlickCatchCount})";
            FlickDirOmniCount.text =  $"({countResult.FlickDirectionalCount}+{countResult.FlickOmnidirectionalCount})";
            HoldCount.text = $"{countResult.HoldCount}";
            HoldTapCatchCount.text = $"({countResult.HoldTapCount}+{countResult.HoldCatchCount})";
            HoldTickCount.text = $"{countResult.HoldTickCount}";
            FakeCount.text = $"(+{countResult.FakeCount})";
            FakeTapCatchCount.text = $"({countResult.FakeTapCount}+{countResult.FakeCatchCount})";

            // Score
            // Get Max Streak
            // EX Score 

            EXScore.text = countResult.ExScore.ToString();
            MaxStreak.text = countResult.Streak.ToString();
        }
    }
    
}