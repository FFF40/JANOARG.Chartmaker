using System.Collections.Generic;
using System.Linq;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerIndexedDeleteAction<T> : IChartmakerAction 
    {
        public List<T>                  Target;
        public SortedDictionary<int, T> Items = new();

        public string GetName() => 
            "Delete " + Behaviors.Chartmaker.Chartmaker.GetItemName(Items.Values.ToList());

        public void Undo() 
        {
            foreach (var entry in Items)
                Target.Insert(entry.Key, entry.Value);
        }
        public void Redo() 
        {
            foreach (var entry in Items)
                Target.Remove(entry.Value);
        }
    }
}

