using System.Collections;
using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerTimelineDragLaneAction: IChartmakerAction
    {
        public IList        Targets = new List<object>();
        public string       Keyword;
        public BeatPosition Value;

        public string GetName()
        {
            return "Drag " + Behaviors.Chartmaker.Chartmaker.GetItemName(Targets);
        }

        public void Undo() 
        {
            Do(-Value);
        }
        public void Redo() 
        {
            Do(Value);
        }

        void Do(BeatPosition value) 
        {
            foreach (Lane lane in Targets) 
            {
                foreach (LaneStep step in lane.LaneSteps) 
                {
                    step.Offset += value;
                    foreach (Timestamp timestamp in step.Storyboard.Timestamps) 
                        timestamp.Offset += value;
                }
            
                foreach (HitObject hit in lane.Objects) 
                {
                    hit.Offset += value;
                    foreach (Timestamp timestamp in hit.Storyboard.Timestamps) 
                        timestamp.Offset += value;
                }
            
                foreach (Timestamp timestamp in lane.Storyboard.Timestamps) 
                    timestamp.Offset += value;
            }

        }
    }
}