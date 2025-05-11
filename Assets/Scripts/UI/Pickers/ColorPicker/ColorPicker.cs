using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ColorPicker : Picker
{
    public static ColorPicker main;

    public Color CurrentColor;
    public Vector3 CurrentHSV;

    public Image OldColor;
    public Image NewColor;
    
    public FormEntryColorRange[] Values;
    public SliderGradient[] Gradients;
    public RectTransform HuePointer;
    public RectTransform RectPointer;
    public ColorRect ColorRect;
    public FormEntryString HexField;

    public bool recursionBuster;

    readonly Chartmaker Chartmaker;
    int ColorValuePrefs;

    // TODO: Make the FormEntryRange text displays 0 - 255  
    // ^ just to a diff FormEntry ig 

    // TODO: Change every Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 1000) / 1000;
    // to a condition/function

    // TODO: Make Color Rect work
    // Slider Position, Field Set not working

    public override void Awake()
    {
        main = this;
        for (int a = 0; a < 4; a++)
        {
            int A = a;

            Values[a].OnGet = () => ((byte)CurrentColor[A] /255f); 
            Values[a].OnSet = (x) => {
                if (recursionBuster) return;
                Values[A].CurrentValue = Mathf.Round(x * 255);
                Debug.Log(Mathf.Round(x * 255));
                if (!Values[A].Field.isFocused) Values[A].Reset();
                CurrentColor[A] = x;
                UpdateHSV();
                UpdateHex();
                UpdateUI();
            };
        }
        HexField.OnGet = () => ColorToHex(CurrentColor);
        HexField.OnSet = (x) => {
            if (recursionBuster) return;
            Color? color = HexToColor(x);
            if (color == null)
            {
                HexField.CurrentValue = ColorToHex(CurrentColor);
            }
            else
            {
                CurrentColor = (Color)color;
                UpdateHSV();
                recursionBuster = true;
                for (int a = 0; a < 4; a++)
                {
                    //Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 1000) / 1000;
                    Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 255);
                    Values[a].Reset();
                }
                recursionBuster = false;
                UpdateUI();
            }
        };


        #region for RGBA 0-1
        //for (int a = 0; a < 4; a++)
        //{
        //    int A = a;
        //    Values[a].OnGet = () => CurrentColor[A];
        //    Values[a].OnSet = (x) => {
        //        if (recursionBuster) return;
        //        x = Mathf.Round(x * 1000) / 1000;
        //        Values[A].CurrentValue = x;
        //        if (!Values[A].Field.isFocused) Values[A].Reset();
        //        CurrentColor[A] = x;
        //        UpdateHSV();
        //        UpdateHex();
        //        UpdateUI();
        //    };
        //}
        //HexField.OnGet = () => ColorToHex(CurrentColor);
        //HexField.OnSet = (x) => {
        //    if (recursionBuster) return;
        //    Color? color = HexToColor(x);
        //    if (color == null)
        //    {
        //        HexField.CurrentValue = ColorToHex(CurrentColor);
        //    }
        //    else
        //    {
        //        CurrentColor = (Color)color;
        //        UpdateHSV();
        //        recursionBuster = true;
        //        for (int a = 0; a < 4; a++)
        //        {
        //            Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 1000) / 1000;
        //            Values[a].Reset();
        //        }
        //        recursionBuster = false;
        //        UpdateUI();
        //    }
        //};
        #endregion
        base.Awake();
    }

    public override void Open()
    {
        base.Open();

        UpdateUI();
        UpdateHSV();
        UpdateHex();
        recursionBuster = true;
        for (int a = 0; a < 4; a++)
        {
            //Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 1000) / 1000;
            Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 255);
            Values[a].Reset();
        }
        recursionBuster = false;
        OldColor.color = CurrentColor;
    }

    public void UpdateHSV()
    {
        Color.RGBToHSV(CurrentColor, out CurrentHSV.x, out CurrentHSV.y, out CurrentHSV.z);
    }

    public void UpdateRGB()
    {
        CurrentColor = Color.HSVToRGB(CurrentHSV.x, CurrentHSV.y, CurrentHSV.z) * new Color(1, 1, 1, CurrentColor.a);
        recursionBuster = true;
        for (int a = 0; a < 4; a++)
        {
            //Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 1000) / 1000;
            Values[a].CurrentValue = Mathf.Round(CurrentColor[a] * 255);
            Values[a].Reset();
        }
        recursionBuster = false;
    }

    public void UpdateHex()
    {
        recursionBuster = true;
        HexField.CurrentValue = ColorToHex(CurrentColor);
        HexField.Reset();
        recursionBuster = false;
    }

    public void UpdateUI()
    {
        NewColor.color = CurrentColor;
        float angle = CurrentHSV.x * Mathf.PI * 2;
        HuePointer.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 100;
        RectPointer.anchorMax = RectPointer.anchorMin = new Vector2(CurrentHSV.y, CurrentHSV.z);
        ColorRect.color = Color.HSVToRGB(CurrentHSV.x, 1, 1);


        for (int a = 0; a < 4; a++)
        {
            Color color = CurrentColor + Color.black;
            color[a] = 0;
            Gradients[a].color = color;
            color[a] = 1;
            Gradients[a].color2 = color;
        }

        OnSet?.Invoke();
    }

    IEnumerator Intro()
    {
        RectTransform rt = (RectTransform)transform;
        rt.anchoredPosition -= new Vector2(-2, 2);
        yield return new WaitForSecondsRealtime(0.05f);
        rt.anchoredPosition += new Vector2(-2, 2);
    }

    // TODO: Fix
    public string ColorToHex(Color color)
    {
        Color32 color32 = color;
        Debug.Log(color32);
        Debug.Log(color);
        return (color32.r * 255).ToString("X2") + color32.g.ToString("X2") + color32.b.ToString("X2") + 
            (color.a == 1 ? "" : color32.a.ToString("X2"));
    }
    public Color? HexToColor(string text)
    {
        Color32 color32 = new Color32(0, 0, 0, 255);
        if (text.Length == 3 || text.Length == 4)
        {
            if (!byte.TryParse("".PadLeft(2, text[0]), NumberStyles.HexNumber, null, out color32.r)) return null;
            if (!byte.TryParse("".PadLeft(2, text[1]), NumberStyles.HexNumber, null, out color32.g)) return null;
            if (!byte.TryParse("".PadLeft(2, text[2]), NumberStyles.HexNumber, null, out color32.b)) return null;
            if (text.Length == 4 && !byte.TryParse("".PadLeft(2, text[3]), NumberStyles.HexNumber, null, out color32.a)) return null;
            return color32;
        }
        if (text.Length == 6 || text.Length == 8)
        {
            if (!byte.TryParse(text[0..2], NumberStyles.HexNumber, null, out color32.r)) return null;
            if (!byte.TryParse(text[2..4], NumberStyles.HexNumber, null, out color32.g)) return null;
            if (!byte.TryParse(text[4..6], NumberStyles.HexNumber, null, out color32.b)) return null;
            if (text.Length == 8 && !byte.TryParse(text[6..8], NumberStyles.HexNumber, null, out color32.a)) return null;
            return color32;
        }
        return null;
    }
}
