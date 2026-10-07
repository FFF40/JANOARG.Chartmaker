
using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;

namespace JANOARG.Chartmaker.UI.Inspector.Utils
{
    public static class HitObjectCounter
    {
        public static HitObjectCountResult Count(IEnumerable<HitObject> objects)
        {
            HitObjectCountResult result = new();
            foreach (var obj in objects)
            {
                if (obj.IsFake)
                {
                    result.FakeCount++;
                    continue;
                }

                result.TotalCount++;

                if (obj.Type is HitObject.HitType.Normal)
                    result.TotalTapCount++;
                else if (obj.Type is HitObject.HitType.Catch)
                    result.TotalCatchCount++;
    
                if (obj.Flickable)
                {
                    result.FlickCount++;

                    if (float.IsFinite(obj.FlickDirection))
                        result.FlickDirectionalCount++;
                    else
                        result.FlickOmnidirectionalCount++;

                    if (obj.Type is HitObject.HitType.Normal)
                        result.FlickTapCount++;
                    else if (obj.Type is HitObject.HitType.Catch)
                        result.FlickCatchCount++;
                }
                if (obj.HoldLength > 0)
                {
                    result.HoldCount++;

                    if (obj.Type is HitObject.HitType.Normal)
                        result.HoldTapCount++;
                    else if (obj.Type is HitObject.HitType.Catch)
                        result.HoldCatchCount++;

                    result.HoldTickCount += Mathf.CeilToInt(obj.HoldLength / 0.5f);
                }
            }

            result.ExScore = 
                (result.TotalTapCount * 3) +
                result.TotalCatchCount +
                result.HoldTickCount +
                result.FlickOmnidirectionalCount +
                (result.FlickDirectionalCount * 2)
            ;
            result.Streak = result.TotalCount + result.HoldTickCount;

            return result;
        }
    }

    public struct HitObjectCountResult
    {
        public int TotalCount;
        public int TotalTapCount;
        public int TotalCatchCount;
        public int HoldCount;
        public int HoldTapCount;
        public int HoldCatchCount;
        public int HoldTickCount;
        public int FlickCount;
        public int FlickTapCount;
        public int FlickCatchCount;
        public int FlickOmnidirectionalCount;
        public int FlickDirectionalCount;
        public int FakeCount;
        public int FakeTapCount;
        public int FakeCatchCount;

        public int ExScore;
        public int Streak;
    }
}