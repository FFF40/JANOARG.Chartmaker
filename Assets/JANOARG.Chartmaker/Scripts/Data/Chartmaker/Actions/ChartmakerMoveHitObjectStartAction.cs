using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMoveHitObjectStartAction : ChartmakerMoveAction<HitObject>
    {

        public override string GetName() => 
            "Move Hit Object Start";

        protected override void Do(Vector3 offset) 
        {
            Item.Position += offset.x;
            Item.Length -= offset.x;
            foreach (Timestamp timestamp in Item.Storyboard.Timestamps)
            {
                switch (timestamp.ID)
                {
                    case TimestampIDs.Position:
                        timestamp.From += offset.x;
                        timestamp.Target += offset.x;
                        break;
                
                    case TimestampIDs.Length:
                        timestamp.From -= offset.x;
                        timestamp.Target -= offset.x;
                        break;
                }
            }
        }
    }
}

