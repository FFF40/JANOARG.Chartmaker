using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ObjectPickerItemHolder : MonoBehaviour
{
    public Button Button;
    public Image Icon;
    public GameObject Checkmark;
    public TMP_Text Text;

    public object Target;

    public void Select()
    {
        ObjectPicker.main.Select(Target);
    }

    public void SetItem(object item, string name)
    {
        Target = item;
        gameObject.name = Text.text = name;
    }
}
