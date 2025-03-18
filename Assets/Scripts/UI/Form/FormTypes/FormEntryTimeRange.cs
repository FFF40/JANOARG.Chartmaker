using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormEntryTimeRange : FormEntry<Vector2>
{
    public TMP_InputField FieldX;
    public Button ButtonX;
    public TMP_InputField FieldY;
    public Button ButtonY;

    public new void Start() 
    {
        base.Start();
        Reset();
    }

    public void Reset()
    {
        FieldX.SetTextWithoutNotify(CurrentValue.x.ToString());
        FieldY.SetTextWithoutNotify(CurrentValue.y.ToString());
    }
    
    public void SetValue(int index, string value)
    {
        if (float.TryParse(value, out float v)) 
        {
            CurrentValue[index] = v;
            SetValue(CurrentValue);
        }
    }
    public void SetX(string value) => SetValue(0, value);
    public void SetY(string value) => SetValue(1, value);
    
    public void SetToCurrentTime(int index) 
    {
        CurrentValue[index] = Chartmaker.main.SongSource.time;
        SetValue(CurrentValue);
        Reset();
    }
}
