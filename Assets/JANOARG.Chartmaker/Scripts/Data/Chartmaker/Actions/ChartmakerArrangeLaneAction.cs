using System.Collections.Generic;
using System.Linq;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerArrangeLaneAction: IChartmakerAction
    {
        public Lane Target;

        public Lane   BeforeAdjacent;
        public ulong  BeforeAdjacentUuid;
        public string BeforeGroup;
        public ulong  BeforeGroupUuid;
        public Lane   AfterAdjacent;
        public ulong  AfterAdjacentUuid;
        public string AfterGroup;
        public ulong  AfterGroupUuid;

        public string GetName()
        {
            return "Arrange Lane";
        }

        public void Do(Lane adjacent, ulong adjacentUuid, string group, ulong groupUuid) 
        {
            List<Lane> list = Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Lanes;
      
            Target.Group = group;
            Target.GroupUuid = groupUuid;
      
            list.Remove(Target);

            int index = adjacent != null
                ? list.IndexOf(adjacent)
                : list.FindIndex(l => l.UUID == adjacentUuid);

            list.Insert(index + 1, Target);
            list.Sort((x, y) => x.LaneSteps[0].Offset.CompareTo(y.LaneSteps[0].Offset));
        }

        public void Redo()
        {
            Do(AfterAdjacent, AfterAdjacentUuid, AfterGroup, AfterGroupUuid);
        }

        public void Undo()
        {
            Do(BeforeAdjacent, BeforeAdjacentUuid, BeforeGroup, BeforeGroupUuid);
        }
    }
}