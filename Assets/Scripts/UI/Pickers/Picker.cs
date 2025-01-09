using System;
using System.Collections;
using UnityEngine;

public abstract class Picker : MonoBehaviour
{

    [HideInInspector]
    public bool isOpen = false;

    public Action OnSet;

    public virtual void Awake()
    {
        gameObject.SetActive(false);
    }

    public virtual void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) 
        {
            if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, Input.mousePosition, null))
            {
                Close();
            }
        }
    }

    public virtual void Open()
    {
        gameObject.SetActive(isOpen = true);

        // Set popup position to mouse pointer position
        RectTransform rt = (RectTransform)transform;
        RectTransform parent = (RectTransform)rt.parent;
        rt.anchoredPosition = (Vector2)Input.mousePosition - parent.rect.size / 2;
        Rect rect = rt.rect;
        rect.position += rt.anchoredPosition;
        if (rect.xMin < parent.rect.width / -2) rt.anchoredPosition += Vector2.right * (-rect.xMin - parent.rect.width / 2);
        if (rect.xMax > parent.rect.width / 2) rt.anchoredPosition += Vector2.left * (rect.xMax - parent.rect.width / 2);
        if (rect.yMin < parent.rect.height / -2) rt.anchoredPosition += Vector2.up * (-rect.yMin - parent.rect.height / 2);
        if (rect.yMax > parent.rect.height / 2) rt.anchoredPosition += Vector2.down * (rect.yMax - parent.rect.height / 2);

        StartCoroutine(Intro());
    }

    public virtual void Close()
    {
        gameObject.SetActive(isOpen = false);
        StopCoroutine(Intro());
        OnSet = null;
    }

    // ----------- Animation

    IEnumerator Intro()
    {
        RectTransform rt = (RectTransform)transform;
        rt.anchoredPosition -= new Vector2(-2, 2);
        yield return new WaitForSecondsRealtime(0.05f);
        rt.anchoredPosition += new Vector2(-2, 2);
    }
}