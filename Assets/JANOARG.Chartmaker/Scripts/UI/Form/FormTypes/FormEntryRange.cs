using JANOARG.Chartmaker.Utils.Math;
using TMPro;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryRange : FormEntry<float>
    {
        public Slider         Range;
        public TMP_InputField Field;

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void RangeSet()
        {
            SetValue(Range.value);
            Field.SetTextWithoutNotify(CurrentValue.ToString());
        }

        public void FieldSet()
        {
            if (!ExpressionUtils.TryEvaluate(Field.text, out float value)) 
                return;

            Range.SetValueWithoutNotify(value);
            SetValue(Range.value);
        }

        public void Reset()
        {
            Range.SetValueWithoutNotify(CurrentValue);
            Field.SetTextWithoutNotify(CurrentValue.ToString());
        }
    }
}
