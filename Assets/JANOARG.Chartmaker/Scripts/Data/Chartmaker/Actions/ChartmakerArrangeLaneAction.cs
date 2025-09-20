using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerArrangeLaneAction: IChartmakerAction
    {
        public Lane Target;

        public Lane   BeforeAdjacent;
        public string BeforeGroup;
        public Lane   AfterAdjacent;
        public string AfterGroup;

        public string GetName()
        {
            return "Arrange Lane";
        }

        public void Do(Lane adjacent, string group) 
        {
            List<Lane> list = Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Lanes;
      
            Target.Group = group;
      
            list.Remove(Target);
            list.Insert(list.IndexOf(adjacent) + 1, Target);
            list.Sort((x, y) => x.LaneSteps[0].Offset.CompareTo(y.LaneSteps[0].Offset));
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