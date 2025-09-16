using System;
using System.Collections;
using System.Collections.Generic;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FormEntryEasing : FormEntry<IEaseDirective>
{
    public TMP_Text ValueLabel;
    public LineGraph Graph;

    public new void Start() 
    {
        base.Start();
        Reset();
    }

    public void Reset()
    {
        ValueLabel.text = CurrentValue.ToString();
        
        float[] graph = new float[64];
        for (int i = 0; i < graph.Length; i++) graph[i] = CurrentValue.Get(i / (graph.Length - 1f));
        Graph.Values = graph;
    }

    public void OpenPicker()
    {
        EasingPicker.main.CurrentEasing = CurrentValue;
        EasingPicker.main.Open();
        EasingPicker.main.OnSet = () => {
            SetValue(EasingPicker.main.CurrentEasing);
            Reset();
        };
    }
}