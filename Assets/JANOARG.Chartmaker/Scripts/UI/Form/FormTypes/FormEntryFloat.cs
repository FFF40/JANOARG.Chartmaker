using TMPro;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryFloat : FormEntry<float>
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
            if (float.TryParse(value, out float v))
                SetValue(v);
        }
    }
}
