using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormEntryColorRange : FormEntry<float>
{
    public Slider Range;
    public TMP_InputField Field;

    //readonly Chartmaker Chartmaker;
    //int ColorValuePrefs = Chartmaker.Preferences.ColorValues;

    public new void Start() 
    {
        base.Start();
        Reset();
    }

    ////Change range for mode
    //public void ChangeColorPrefs()
    //{
    //    // if RGB 0-255
    //    if (ColorValuePrefs == 0)
    //    {
    //        Range.maxValue = 255;
    //        if (Range.value % 1 != 0)       Range.value = Mathf.Round(Range.value * 255);
    //        Range.wholeNumbers = true;
    //    }
    //    else
    //    {
    //        Range.wholeNumbers = false;
    //        if (Range.value > 1)            Range.value = Range.value / 255 ;
    //        Range.maxValue = 1;
    //    }
    //}

    public void RangeSet()
    {
        SetValue(Range.value);
        Field.text = CurrentValue.ToString();
    }

    public void FieldSet()
    {
        if (float.TryParse(Field.text, out float value))
        {
            Range.value = value;
            SetValue(Range.value);
        }
    }

    public void Reset()
    {
        Range.value = CurrentValue;
        Field.text = CurrentValue.ToString();
    }
}
