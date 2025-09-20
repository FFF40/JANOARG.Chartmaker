using JANOARG.Chartmaker.Behaviors.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker;
using TMPro;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryKeybind : FormEntry<Keybind>
    {
        public string   Category;
        public TMP_Text Field;

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void Reset()
            => Field.text = CurrentValue.ToString();
    
        public void StartChange() 
        {
            KeyboardHandler.main.StartKeybindChange(this);
        }
    }
}
