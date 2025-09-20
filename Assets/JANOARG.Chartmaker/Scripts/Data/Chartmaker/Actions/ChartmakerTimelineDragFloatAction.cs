using System.Collections;
using System.Collections.Generic;

namespace JANOARG.Chartmaker.Data.Chartmaker.Actions
{
    public class ChartmakerTimelineDragFloatAction: IChartmakerAction
    {
        public IList  Targets = new List<object>();
        public string Keyword;
        public float  Value;

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

        void Do(float value) 
        {
            System.Reflection.FieldInfo field = Targets[0].GetType().GetField("Offset");
        
            foreach (object item in Targets)
                field.SetValue(item, (float)field.GetValue(item) + value);
        }
    }
}
