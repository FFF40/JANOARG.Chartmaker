using System.Collections.Generic;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    /// <summary>
    /// Wraps multiple actions into a single undo/redo entry.
    /// Redo executes actions in forward order; Undo executes in reverse.
    /// </summary>
    public class ChartmakerCompositeAction : IChartmakerAction
    {
        public List<IChartmakerAction> Actions = new();
        public string Name;

        public string GetName() => Name ?? (Actions.Count > 0 ? Actions[0].GetName() : "Composite Action");

        public void Redo()
        {
            foreach (IChartmakerAction action in Actions)
                action.Redo();
        }

        public void Undo()
        {
            for (int i = Actions.Count - 1; i >= 0; i--)
                Actions[i].Undo();
        }
    }
}
