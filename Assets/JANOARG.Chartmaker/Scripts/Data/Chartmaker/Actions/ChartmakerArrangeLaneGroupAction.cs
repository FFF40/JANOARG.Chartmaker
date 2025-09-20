using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerArrangeLaneGroupAction: IChartmakerAction
    {
        public LaneGroup Target;

        public LaneGroup BeforeAdjacent;
        public string    BeforeGroup;
        public LaneGroup AfterAdjacent;
        public string    AfterGroup;

        public string GetName()
        {
            return "Arrange Lane Group";
        }

        public void Do(LaneGroup adjacent, string group) 
        {
            List<LaneGroup> list = Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Groups;
      
            Target.Group = group;
       
            list.Remove(Target);
            list.Insert(list.IndexOf(adjacent) + 1, Target);
        }

        public void Redo()
        {
            Do(AfterAdjacent, AfterGroup);
        }

        public void Undo()
        {
            Do(BeforeAdjacent, BeforeGroup);
        }
    }
}