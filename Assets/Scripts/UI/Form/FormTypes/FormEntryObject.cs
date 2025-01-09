using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class FormEntryObject : FormEntry<object>
{
    [DoNotSerialize]
    public int CurrentIndex; 
    public Button LinkButton;
    public TMP_Text ValueLabel;
    public Button DropdownButton;
    public Image IconImage;

    public ObjectPickerType Type;

    public new void Start() 
    {
        base.Start();
        Reset();
    }

    public void SetType(ObjectPickerType type) 
    {
        Type = type;
    }

    public void LinkClick() 
    {
        if (CurrentValue != null) InspectorPanel.main.SetObject(CurrentValue);
    }

    public void Reset()
    {
        string name;
        if (Type == ObjectPickerType.LaneStyle)
        {
            IconImage.sprite = ObjectPicker.main.LaneStyleIcon;
            ValueLabel.text = CurrentValue == null ? "<i>None" : 
                string.IsNullOrEmpty(name = ((LaneStyle)CurrentValue)?.Name) ? "Lane Style " + CurrentIndex : name;
        }
        else if (Type == ObjectPickerType.HitStyle)
        {
            IconImage.sprite = ObjectPicker.main.HitStyleIcon;
            ValueLabel.text = CurrentValue == null ? "<i>None" : 
                string.IsNullOrEmpty(name = ((HitStyle)CurrentValue)?.Name) ? "Hit Style " + CurrentIndex : name;
        }
    }

    public void OpenList()
    {
        ObjectPicker.main.CurrentObject = CurrentValue;
        ObjectPicker.main.Type = Type;
        ObjectPicker.main.Open();
        ObjectPicker.main.OnSet = () => {
            if (Type == ObjectPickerType.LaneStyle)
            {
                CurrentIndex = Chartmaker.main.CurrentChart.Pallete.LaneStyles.IndexOf((LaneStyle)ObjectPicker.main.CurrentObject);
            }
            else if (Type == ObjectPickerType.HitStyle)
            {
                CurrentIndex = Chartmaker.main.CurrentChart.Pallete.HitStyles.IndexOf((HitStyle)ObjectPicker.main.CurrentObject);
            }
            SetValue(ObjectPicker.main.CurrentObject);
            Reset();
        };
    }
}