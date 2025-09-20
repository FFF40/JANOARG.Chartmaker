using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMoveLaneStepEndAction : ChartmakerMoveAction<LaneStep>
    {

        public override string GetName() => 
            "Move Lane Step End";

        protected override void Do(Vector3 offset) 
        {
            Item.EndPointPosition += (Vector2)offset;
            foreach (Timestamp timestamp in Item.Storyboard.Timestamps)
            {
                switch (timestamp.ID)
                {
                    case TimestampIDs.EndPos_X:
                        timestamp.From += offset.x;
                        timestamp.Target += offset.x;
                        break;
                
                    case TimestampIDs.EndPos_Y:
                        timestamp.From += offset.y;
                        timestamp.Target += offset.y;
                        break;
                }
            }
        }
    }
}

