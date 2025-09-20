using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMoveHitObjectEndAction : ChartmakerMoveAction<HitObject>
    {

        public override string GetName() => 
            "Move Hit Object End";

        protected override void Do(Vector3 offset) 
        {
            Item.Length += offset.x;
            foreach (Timestamp timestamp in Item.Storyboard.Timestamps)
            {
                if (timestamp.ID != TimestampIDs.Length)
                    continue;

                timestamp.From += offset.x;
                timestamp.Target += offset.x;
            }
        }
    }
}

