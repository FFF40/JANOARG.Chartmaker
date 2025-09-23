using System.Linq;
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
            // Check if it looks like a math expression (contains an operator)
            if (MathableInputField.OpList.Any(op => value.Contains(op)))
            {
                SetValue(value);

                return;
            }

            // Fallback: raw integer parse
            if (int.TryParse(value, out int intValue))
            {
                SetValue(intValue);
            }
        }

    }
}
