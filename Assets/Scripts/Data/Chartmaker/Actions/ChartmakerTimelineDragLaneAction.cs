using System.Collections;
using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;

public class ChartmakerTimelineDragLaneAction: IChartmakerAction
{
    public IList Targets = new List<object>();
    public string Keyword;
    public BeatPosition Value;

    public string GetName()
    {
        return "Drag " + Chartmaker.GetItemName(Targets);
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
                foreach (Timestamp ts in step.Storyboard.Timestamps) ts.Offset += value;
            }
            foreach (HitObject hit in lane.Objects) 
            {
                hit.Offset += value;
                foreach (Timestamp ts in hit.Storyboard.Timestamps) ts.Offset += value;
            }
            foreach (Timestamp ts in lane.Storyboard.Timestamps) 
            {
                ts.Offset += value;
            }
        }

    }
}