using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;

public class ChartmakerMoveHitObjectStartAction : ChartmakerMoveAction<HitObject>
{

    public override string GetName()
    {
        return "Move Hit Object Start";
    }

    public override void Do(Vector3 offset) 
    {
        Item.Position += offset.x;
        Item.Length -= offset.x;
        foreach (Timestamp ts in Item.Storyboard.Timestamps)
        {
            if (ts.ID == TimestampIDs.Position)
            {
                ts.From += offset.x;
                ts.Target += offset.x;
            }
            else if (ts.ID == TimestampIDs.Length)
            {
                ts.From -= offset.x;
                ts.Target -= offset.x;
            }
        }
    }
}

