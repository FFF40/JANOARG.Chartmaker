using JANOARG.Chartmaker.UI.Pickers.ColorPicker;
using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryColor : FormEntry<Color>
    {
        public Image  CurrentColor;
        public Slider AlphaSlider;

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void Reset()
        {
            CurrentColor.color = CurrentValue + new Color(0, 0, 0, 1);
            AlphaSlider.value = CurrentValue.a;
        }

        public void OpenPicker()
        {
            ColorPicker.main.CurrentColor = CurrentValue;
            ColorPicker.main.Open();
            ColorPicker.main.OnSet = () => 
            {
                SetValue(ColorPicker.main.CurrentColor);
                Reset();
            };
        }
    }
}
