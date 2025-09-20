using System.Collections;
using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerTimelineDragBeatPositionAction: IChartmakerAction
    {
        public IList        Targets = new List<object>();
        public string       Keyword;
        public BeatPosition Value;

        public string GetName() => 
            "Drag " + Behaviors.Chartmaker.Chartmaker.GetItemName(Targets);

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
            System.Reflection.FieldInfo field = Targets[0].GetType().GetField("Offset");
            foreach (object item in Targets)
            {
                field.SetValue(item, (BeatPosition)field.GetValue(item) + value);
           
                if (item is Storyboardable sb) foreach (Timestamp ts in sb.Storyboard.Timestamps) 
                    ts.Offset += value;
            }
        }
    }
}