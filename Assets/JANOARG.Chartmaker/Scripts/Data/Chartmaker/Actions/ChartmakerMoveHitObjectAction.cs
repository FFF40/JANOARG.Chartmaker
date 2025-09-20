using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMoveHitObjectAction : ChartmakerMoveAction<HitObject>
    {

        public override string GetName() => 
            "Move Hit Object";

        protected override void Do(Vector3 offset) 
        {
            Item.Position += offset.x;
            foreach (Timestamp timestamp in Item.Storyboard.Timestamps)
            {
                if (timestamp.ID != TimestampIDs.Position) 
                    continue;

                timestamp.From += offset.x;
                timestamp.Target += offset.x;
            }
        }
    }
}

