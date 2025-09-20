using System.Collections.Generic;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerMultiEditAction: IChartmakerAction
    {
        public List<ChartmakerMultiEditActionItem> Targets = new List<ChartmakerMultiEditActionItem>();
        public string                              Keyword;

        public string GetName() => 
            "Multi Edit " + Behaviors.Chartmaker.Chartmaker.GetItemName(Targets[0].Target) + " " + Keyword;

        public void Undo() 
        {
            foreach (ChartmakerMultiEditActionItem item in Targets)
                item.Target.GetType().GetField(Keyword).SetValue(item.Target, item.From);
        }
        public void Redo() 
        {
            foreach (ChartmakerMultiEditActionItem item in Targets)
                item.Target.GetType().GetField(Keyword).SetValue(item.Target, item.To);
        }
    }

    public class ChartmakerMultiEditActionItem
    {
        public object Target;
        public object From;
        public object To;
    }
}