using System;
using JANOARG.Chartmaker.Utils.Math;
using JetBrains.Annotations;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.UI.Form.FormTypes
{
    public class FormEntryRange : FormEntry<float>
    {
        public Slider         Range;
        public TMP_InputField Field;

        public UnityAction EndDragTrigger;
        
        public void InvokeEndDragTrigger() => EndDragTrigger?.Invoke();

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
