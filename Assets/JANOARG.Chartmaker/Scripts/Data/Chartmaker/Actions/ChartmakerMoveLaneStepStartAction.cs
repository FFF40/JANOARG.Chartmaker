using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMoveLaneStepStartAction : ChartmakerMoveAction<LaneStep>
    {

        public override string GetName() => 
            "Move Lane Step Start";

        protected override void Do(Vector3 offset) 
        {
            Item.StartPointPosition += (Vector2)offset;
            foreach (Timestamp timestamp in Item.Storyboard.Timestamps)
            {
                switch (timestamp.ID)
                {
                    case TimestampIDs.StartPos_X:
                        timestamp.From += offset.x;
                        timestamp.Target += offset.x;
                        break;
                
                    case TimestampIDs.StartPos_Y:
                        timestamp.From += offset.y;
                        timestamp.Target += offset.y;
                        break;
                }
            }
        }
    }
}

