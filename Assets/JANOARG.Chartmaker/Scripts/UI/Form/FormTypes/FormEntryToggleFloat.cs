using TMPro;
using UnityEngine;
using UnityEngine.UI;
using JANOARG.Chartmaker.Utils.Math;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryToggleFloat : FormEntry<float>
    {
        public Toggle         Toggle;
        public TMP_InputField Field;
        public GameObject     NotField;

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void Reset()
        {
            Toggle.SetIsOnWithoutNotify(!float.IsNaN(CurrentValue));
            Field.gameObject.SetActive(Toggle.isOn);
            NotField.SetActive(!Toggle.isOn);
            Field.SetTextWithoutNotify(Toggle.isOn ? CurrentValue.ToString() : "");
        }

        public void SetValue(bool value)
        {
            Field.gameObject.SetActive(value);
            NotField.SetActive(!value);
            if (value) SetValue(Field.text);
            else SetValue(float.NaN);
        }
    
        public void SetValue(string value)
        {
            if (ExpressionUtils.TryEvaluate(value, out float v)) 
                SetValue(v);
        }
    }
}
