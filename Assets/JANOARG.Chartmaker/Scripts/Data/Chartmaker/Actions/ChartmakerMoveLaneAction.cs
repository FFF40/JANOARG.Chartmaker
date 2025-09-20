using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMoveLaneAction : ChartmakerMoveAction<Lane>
    {

        public override string GetName() => 
            "Move Lane";

        protected override void Do(Vector3 offset) 
        {
            Item.Position += offset;
            foreach (Timestamp timestamp in Item.Storyboard.Timestamps)
            {
                switch (timestamp.ID)
                {
                    case TimestampIDs.Offset_X:
                        timestamp.From += offset.x;
                        timestamp.Target += offset.x;
                        break;
                
                    case TimestampIDs.Offset_Y:
                        timestamp.From += offset.y;
                        timestamp.Target += offset.y;
                        break;
                
                    case TimestampIDs.Offset_Z:
                        timestamp.From += offset.z;
                        timestamp.Target += offset.z;
                        break;
                }
            }
        }
    }
}

