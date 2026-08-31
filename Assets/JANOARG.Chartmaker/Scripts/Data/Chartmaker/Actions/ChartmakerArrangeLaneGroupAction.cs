using System.Collections.Generic;
using System.Linq;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerArrangeLaneGroupAction: IChartmakerAction
    {
        public LaneGroup Target;

        public LaneGroup BeforeAdjacent;
        public ulong     BeforeAdjacentUuid;
        public string    BeforeGroup;
        public ulong     BeforeGroupUuid;
        public LaneGroup AfterAdjacent;
        public ulong     AfterAdjacentUuid;
        public string    AfterGroup;
        public ulong     AfterGroupUuid;

        public string GetName()
        {
            return "Arrange Lane Group";
        }

        public void Do(LaneGroup adjacent, ulong adjacentUuid, string group, ulong groupUuid) 
        {
            List<LaneGroup> list = Behaviors.Chartmaker.Chartmaker.main.CurrentChart.Groups;
      
            Target.Group = group;
            Target.GroupUuid = groupUuid;
       
            list.Remove(Target);

            int index = adjacent != null
                ? list.IndexOf(adjacent)
                : list.FindIndex(g => g.UUID == adjacentUuid);

            list.Insert(index + 1, Target);
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