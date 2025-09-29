using TMPro;
using UnityEngine;
using JANOARG.Chartmaker.Utils.Math;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryVector3 : FormEntry<Vector3>
    {
        public TMP_InputField FieldX;
        public TMP_InputField FieldY;
        public TMP_InputField FieldZ;

        public new void Start() 
        {
            base.Start();
            Reset();
        }

        public void Reset()
        {
            FieldX.SetTextWithoutNotify(CurrentValue.x.ToString());
            FieldY.SetTextWithoutNotify(CurrentValue.y.ToString());
            FieldZ.SetTextWithoutNotify(CurrentValue.z.ToString());
        }
    
        public void SetValue(int index, string value)
        {
            if (!ExpressionUtils.TryEvaluate(value, out float result))
                return;

            CurrentValue[index] = result;
            SetValue(CurrentValue);
        }
        public void SetX(string value) => SetValue(0, value);
        public void SetY(string value) => SetValue(1, value);
        public void SetZ(string value) => SetValue(2, value);
    }
}
