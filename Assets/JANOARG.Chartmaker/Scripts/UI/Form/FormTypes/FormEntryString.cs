using TMPro;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryString : FormEntry<string>
    {
        public TMP_InputField Field;

        public new void Start() 
        {
            base.Start();
            Reset();
        }
        public void Reset()
            => Field.SetTextWithoutNotify(CurrentValue);
    }
}
