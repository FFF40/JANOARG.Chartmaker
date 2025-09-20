using System.Collections;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerDeleteAction : IChartmakerAction 
    {
        public IList  Target;
        public object Item;

        public string GetName()
        {
            return "Delete " + Behaviors.Chartmaker.Chartmaker.GetItemName(Item);
        }

        public void Undo() 
        {
            if (Item is IList item)
                foreach (object i in item)
                    Target.Add(i);
            else 
                Target.Add(Item);
        
            Behaviors.Chartmaker.Chartmaker.SortList(Target);
        }
        public void Redo() 
        {
            if (Item is IList item)
                foreach (object i in item)
                    Target.Remove(i);
            else 
                Target.Remove(Item);
        }
    }
}

