
using JANOARG.Chartmaker.Utils.Math;
using TMPro;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryInt : FormEntry<int>
    {
        public TMP_InputField Field;

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void Reset() 
            => Field.SetTextWithoutNotify(CurrentValue.ToString());

        public void SetValue(string value)
        {
            if (ExpressionUtils.TryEvaluate(value, out int result))
            {
                SetValue(result);
            }
        }
    }
}
