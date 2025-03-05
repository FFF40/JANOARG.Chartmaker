using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using System;

public class EaseCopyToBottomItem : MonoBehaviour
{
    public Button CopyXButton;
    public Button CopyYButton;
    public Button CopyAllButton;

    public void SetFormItems(FormEntryEasing startX, FormEntryEasing startY, FormEntryEasing endX, FormEntryEasing endY)
    {
        CopyXButton.onClick.AddListener(() => {
            endX.SetValue(startX.CurrentValue);
            endX.Reset();
        });
        CopyYButton.onClick.AddListener(() => {
            endY.SetValue(startY.CurrentValue);
            endY.Reset();
        });
        CopyAllButton.onClick.AddListener(() => {
            CopyXButton.onClick.Invoke();
            CopyYButton.onClick.Invoke();
        });
    }
}
