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

    //Adjust from slider
    public void RangeSet()
    {
        SetValue(Range.value);
        Field.text = Mathf.Round(CurrentValue * 255f).ToString();
    }

    //Adjust from Text Box
    public void FieldSet()
    {
        Debug.Log(Field.text);
        if (float.TryParse(Field.text, out float value))
        {
            Range.value = value / 255f;
            
            SetValue(Range.value);
        }
    }

    public void Reset()
    {
        Range.value = CurrentValue;
        Field.text = Mathf.Round(CurrentValue * 255f).ToString();
    }
}
