using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMoveLaneEndAction : ChartmakerMoveAction<Lane>
    {

        public override string GetName() => 
            "Move Lane End";

        protected override void Do(Vector3 offset) 
        {
            foreach (LaneStep step in Item.LaneSteps)
            {
                step.EndPointPosition += (Vector2)offset;
                foreach (Timestamp timestamp in step.Storyboard.Timestamps)
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
}

