using System;
using System.Collections;
using System.Collections.Generic;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Cursor;
using JANOARG.Chartmaker.UI.NativeUI;
using JANOARG.Chartmaker.UI.Themeable;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Shared.Utils.Animation;
using JANOARG.Chartmaker.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JANOARG.Chartmaker.Utils.Math;
using JANOARG.Chartmaker.Utils.NativeAPI;

namespace JANOARG.Chartmaker.UI.Pickers.EasingPicker
{
    public class EasingPicker : Picker, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IEndDragHandler
    {
        public static EasingPicker main;

        public IEaseDirective CurrentEasing;

        public Graphic             GraphImage;
        public Material            GraphMaterial;
        public RectTransform       GraphStart;
        public RectTransform       GraphEnd;
        public RectTransform       GraphPointer;
        public List<RectTransform> GraphBezierHandles;
        public List<RectTransform> GraphBezierHandleLines;

        [Space]
        public RectTransform BallHolder;
        public Graphic BallSample;
        List<Graphic>  Balls = new();

        [Space]
        public Button BasicEasingTab;
        public GameObject      BasicEasingFields;
        public ContextMenuItem ItemSample;
        public RectTransform   EaseFunctionsHolder;
        List<ContextMenuItem>  EaseFunctions = new();
        public RectTransform   EaseModesHolder;
        List<ContextMenuItem>  EaseModes = new();
        public CanvasGroup     EaseModesCanvasGroup;

        [Space]
        public Button BezierEasingTab;
        public GameObject     BezierEasingFields;
        public TMP_InputField P1XField;
        public TMP_InputField P1YField;
        public TMP_InputField P2XField;
        public TMP_InputField P2YField;

        float loopTimer;
        bool  isLooping;

        bool                 isDragged;
        EasingPickerDragMode CurrentDragMode;
        CursorStyle          CurrentCursor;

        BasicEaseDirective       cachedBasicEase  = new(EaseFunction.Linear, EaseMode.In);
        CubicBezierEaseDirective cachedBezierEase = new (.25f, .1f, .25f, 1);

        float[] values = new float[64];

        public override void Awake()
        {
            main = this;

            foreach (var item in Enum.GetValues(typeof(EaseFunction)))
            {
                var Item = item;
                string name = Enum.GetName(typeof(EaseFunction), item);
                ContextMenuItem holder = Instantiate(ItemSample, EaseFunctionsHolder);
            
                holder.ContentLabel.text = name;
                holder.Button.onClick.AddListener(() => {
                    SetEaseFunction((EaseFunction)Item);
                });
           
                holder.ShortcutLabel.text = "";
                holder.SubmenuIndicator.SetActive(false);
            
                EaseFunctions.Add(holder);
            }
            foreach (var item in Enum.GetValues(typeof(EaseMode)))
            {
                var Item = item;
                string name = Enum.GetName(typeof(EaseMode), item);
                ContextMenuItem holder = Instantiate(ItemSample, EaseModesHolder);
            
                holder.ContentLabel.text = name;
                holder.ShortcutLabel.text = "";
           
                holder.SubmenuIndicator.SetActive(false);
                holder.Button.onClick.AddListener(() => {
                    SetEaseMode((EaseMode)Item);
                });
            
                EaseModes.Add(holder);
            }

            for (int a = 0; a < 64; a++) 
            {
                Graphic ball = Instantiate(BallSample, BallHolder);
                ball.gameObject.SetActive(true);
            
                Balls.Add(ball);
            }

            base.Awake();
        }

        public override void Update()
        {
            base.Update();

            if (isLooping && !isDragged)
            {
                loopTimer += Time.deltaTime / 1.5f;
                loopTimer %= 1;
            
                UpdateBalls();
            }
        }

        public override void Open()
        {
            base.Open();
        
            UpdateUI();
        
            if (CurrentEasing is CubicBezierEaseDirective cbed) 
            {
                ResetBezierFields();
            }
        }

        public void CacheEase()
        {
            switch (CurrentEasing)
            {
                case BasicEaseDirective basicEase:
                    cachedBasicEase = basicEase;

                    break;
            
                case CubicBezierEaseDirective cubicBezierEase:
                    cachedBezierEase = cubicBezierEase;

                    break;
            }
        }

        public void UpdateUI()
        {
            values = new float[values.Length];
       
            float step = 1.0f / (values.Length - 1);
      
            for (int i = 0; i < values.Length; i++) 
            {
                float value = CurrentEasing.Get(i * step);
                values[i] = value / 1.5f + 0.166667f;
            }
      
            GraphMaterial.SetFloatArray("_Values", values);
            GraphMaterial.SetVector("_Resolution", new(GraphImage.rectTransform.rect.width, GraphImage.rectTransform.rect.height));
        
            float endSlope = (values[^2] - values[^1]) / step * 1.5f;
       
            GraphEnd.localEulerAngles = Vector3.back * (Mathf.Atan(endSlope) * Mathf.Rad2Deg + 90);

            GraphImage.material = GraphMaterial;
            GraphImage.SetMaterialDirty();

            if (CurrentEasing is BasicEaseDirective basicEase)
            {
                BasicEasingFields.SetActive(true);
                BasicEasingTab.interactable = false;
            
                bool isLinear = basicEase.Function == EaseFunction.Linear;
            
                EaseModesCanvasGroup.interactable = !isLinear;
                EaseModesCanvasGroup.alpha = isLinear ? 0.5f : 1;

                string name = Enum.GetName(typeof(EaseFunction), basicEase.Function);
          
                foreach (var item in EaseFunctions)
                    item.CheckedIndicator.SetActive(item.ContentLabel.text == name);

                name = Enum.GetName(typeof(EaseMode), basicEase.Mode);
           
                foreach (var item in EaseModes)
                    item.CheckedIndicator.SetActive(!isLinear && item.ContentLabel.text == name);
            }
            else 
            {
                BasicEasingFields.SetActive(false);
                BasicEasingTab.interactable = true;
            }
        
            if (CurrentEasing is CubicBezierEaseDirective cbed) 
            {
                BezierEasingFields.SetActive(true);
                BezierEasingTab.interactable = false;

                // TODO Add handles
            }
            else 
            {
                BezierEasingFields.SetActive(false);
                BezierEasingTab.interactable = true;
            }

            UpdateBalls();
            UpdateHandles();
        }

        public void UpdateHandles() 
        {
            bool isActive = (CurrentEasing is CubicBezierEaseDirective) 
                            && (!isLooping || CurrentDragMode != EasingPickerDragMode.None);

            foreach (var item in GraphBezierHandles) 
                item.gameObject.SetActive(isActive);
        
            foreach (var item in GraphBezierHandleLines) 
                item.gameObject.SetActive(isActive);
        
            if (!isActive) 
                return;

            Rect rect = ((RectTransform)GraphBezierHandles[0].parent).rect;
            var cubicBezierEase = (CubicBezierEaseDirective)CurrentEasing;
            
            switch (CurrentDragMode)
            {
                case EasingPickerDragMode.P1Handle:
                    GraphBezierHandles[1].gameObject.SetActive(false);
                    GraphBezierHandleLines[1].gameObject.SetActive(false);
                    break;
            
                case EasingPickerDragMode.P2Handle:
                    GraphBezierHandles[0].gameObject.SetActive(false);
                    GraphBezierHandleLines[0].gameObject.SetActive(false);
                    break;
            }

            Vector2 p0 = rect.size * new Vector2(-0.5f, -0.5f);
            Vector2 p1 = (cubicBezierEase.Point1 - new Vector2(0.5f, 0.5f)) * rect.size;
        
            GraphBezierHandles[0].anchoredPosition = p1;
        
            GraphBezierHandleLines[0].anchoredPosition = (p0 + p1) / 2;
            GraphBezierHandleLines[0].sizeDelta = new (Vector2.Distance(p0, p1), 1.25f);
            GraphBezierHandleLines[0].localEulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.right, p1 - p0);
       
        
            Vector2 p3 = rect.size * new Vector2(0.5f, 0.5f);
            Vector2 p2 = (cubicBezierEase.Point2 - new Vector2(0.5f, 0.5f)) * rect.size;
       
            GraphBezierHandles[1].anchoredPosition = p2;
       
            GraphBezierHandleLines[1].anchoredPosition = (p2 + p3) / 2;
            GraphBezierHandleLines[1].sizeDelta = new (Vector2.Distance(p2, p3), 1.25f);
            GraphBezierHandleLines[1].localEulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.right, p2 - p3);
        
        }

        public void UpdateBalls()
        {
            Color col = Themer.main.Keys["Content0"];

            bool isLoopActive = isLooping && !isDragged;

            GraphStart.gameObject.SetActive(!isLoopActive);
            GraphEnd.gameObject.SetActive(!isLoopActive);
            GraphPointer.gameObject.SetActive(isLoopActive);

            if (isLoopActive)
            {
                Rect rect = GraphImage.rectTransform.rect;
                float easeValue = CurrentEasing.Get(loopTimer);
            
                Balls[2].color = col;
                Balls[2].rectTransform.anchoredPosition = (easeValue - 0.5f) * rect.height / 1.5f * Vector2.up;

                GraphPointer.anchoredPosition = new ((loopTimer - .5f) * rect.width, Balls[2].rectTransform.anchoredPosition.y);

                Balls[1].color = col * new Color(1, 1, 1, easeValue / 2);
                Balls[1].rectTransform.anchoredPosition = rect.height * Vector2.down / 3;
                Balls[1].rectTransform.sizeDelta = loopTimer * Balls[2].rectTransform.sizeDelta;

                Balls[0].color = col * new Color(1, 1, 1, (1 - easeValue) / 2);
                Balls[0].rectTransform.anchoredPosition = rect.height * Vector2.up / 3;
                Balls[0].rectTransform.sizeDelta = (1 - loopTimer) * Balls[2].rectTransform.sizeDelta;

                for (int a = 3; a < Balls.Count; a++) 
                    Balls[a].color = Color.clear;
            }
            else
            {
                float height = GraphImage.rectTransform.rect.height;
           
                for (int i = 0; i < values.Length; i++) 
                {
                    Balls[i].color = col * new Color(1, 1, 1, 0.05f);
                    Balls[i].rectTransform.anchoredPosition = (values[i] - .5f) * height * Vector2.up;
                }
            }
        }

        public void StartLoop() 
        {
            isLooping = true;
            loopTimer = 0;

            UpdateHandles();
            UpdateBalls();
        }

        public void StopLoop() 
        {
            isLooping = false;

            Balls[0].rectTransform.sizeDelta = Balls[1].rectTransform.sizeDelta = Balls[2].rectTransform.sizeDelta;

            UpdateHandles();
            UpdateBalls();
        }
    
        // ----------- Basic easing

        public void SwitchToBasicEasing() 
        {
            CacheEase();
      
            CurrentEasing = cachedBasicEase;
       
            OnSet(); 
            UpdateUI();
        }

        public void SetEaseFunction(EaseFunction function) 
        {
            CurrentEasing = new BasicEaseDirective(function, ((BasicEaseDirective)CurrentEasing).Mode);
        
            OnSet(); 
            UpdateUI();
        }
        public void SetEaseMode(EaseMode mode)
        {
            CurrentEasing = new BasicEaseDirective(((BasicEaseDirective)CurrentEasing).Function, mode);
       
            OnSet(); 
            UpdateUI();
        }

        // ----------- Cublic Bezier easing

        public void SwitchToBezierEasing() 
        {
            CacheEase();
      
            CurrentEasing = cachedBezierEase;
     
            OnSet(); 
            UpdateUI();
            ResetBezierFields();
        }

        public void ResetBezierFields() 
        {
            CubicBezierEaseDirective cubicBezierEase = (CubicBezierEaseDirective)CurrentEasing;
       
            P1XField.SetTextWithoutNotify(cubicBezierEase.Point1.x.ToString());
            P1YField.SetTextWithoutNotify(cubicBezierEase.Point1.y.ToString());
            P2XField.SetTextWithoutNotify(cubicBezierEase.Point2.x.ToString());
            P2YField.SetTextWithoutNotify(cubicBezierEase.Point2.y.ToString());
        }

        public void OnBezierFieldChange()
        {
            CubicBezierEaseDirective cubicBezierEase = (CubicBezierEaseDirective)CurrentEasing;
            Vector2 point1 = cubicBezierEase.Point1, point2 = cubicBezierEase.Point2;
       
            if (ExpressionUtils.TryEvaluate(P1XField.text, out float point1X) && ExpressionUtils.TryEvaluate(P1YField.text, out float point1Y)) 
                point1.Set(Mathf.Clamp01(point1X), point1Y);
      
            if (ExpressionUtils.TryEvaluate(P2XField.text, out float point2X) && ExpressionUtils.TryEvaluate(P2YField.text, out float point2Y)) 
                point2.Set(Mathf.Clamp01(point2X), point2Y);
       
            CurrentEasing = new CubicBezierEaseDirective(point1, point2);
       
            OnSet(); 
            UpdateUI();
        }

        // ----------- Animation

        IEnumerator Intro()
        {
            RectTransform rt = (RectTransform)transform;
            rt.anchoredPosition -= new Vector2(-2, 2);
      
            yield return new WaitForSecondsRealtime(0.05f);
      
            rt.anchoredPosition += new Vector2(-2, 2);
        }

        // ----------- Event handling

        public void OnPointerDown(PointerEventData eventData)
        {
            bool contains(RectTransform rt) => rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.pressPosition, eventData.pressEventCamera);

            CurrentDragMode = EasingPickerDragMode.None;

            if (contains(GraphBezierHandles[0]))
                CurrentDragMode = EasingPickerDragMode.P1Handle;
            else if (contains(GraphBezierHandles[1]))
                CurrentDragMode = EasingPickerDragMode.P2Handle;

            switch (CurrentDragMode)
            {
                case EasingPickerDragMode.None:
                    return;
            
                case EasingPickerDragMode.P1Handle or EasingPickerDragMode.P2Handle:
                {
                    Func<Vector2> get; Action<Vector2> set;
                    CubicBezierEaseDirective cubicBezierEase = (CubicBezierEaseDirective)CurrentEasing;
            
                    if (CurrentDragMode == EasingPickerDragMode.P1Handle)
                    {
                        get = () => cubicBezierEase.Point1;
                        set = (x) => CurrentEasing = new CubicBezierEaseDirective(x, cubicBezierEase.Point2);
                    }
                    else
                    {
                        get = () => cubicBezierEase.Point2;
                        set = (x) => CurrentEasing = new CubicBezierEaseDirective(cubicBezierEase.Point1, x);
                    }

                    Vector2 startValue = get();
                    Vector2 startPos = eventData.position;
                    Vector2 size = GraphImage.rectTransform.rect.size / new Vector2(1, 1.5f);

                    const float precision = 100;

                    OnDragEvent = (ev) => {
                        Vector2 value = startValue + (ev.position - startPos) / size;
                   
                        value.x = Mathf.Clamp01(Mathf.Round(value.x * precision) / precision);
                        value.y = Mathf.Round(value.y * precision) / precision;
                    
                        set(value);
                        OnSet(); 
                        UpdateUI();
                        ResetBezierFields();
                    };

                    break;
                }
            }

            UpdateHandles();
            UpdateCursor(eventData.position, eventData.pressEventCamera);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragged)
                OnEndDrag(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            if (!isDragged)
                UpdateCursor(eventData.position, eventData.pressEventCamera);
        }

        public void UpdateCursor(Vector2 position, Camera eventCamera)
        {
            bool contains(RectTransform rt) => rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, position, eventCamera);

            CursorStyle Cursor = CursorStyle.None;

            if (CurrentDragMode != EasingPickerDragMode.None) 
                Cursor = CursorStyle.HandGrabbing;
            else if (contains((RectTransform)transform)) 
            {
                if (contains(GraphBezierHandles[0]) || contains(GraphBezierHandles[1])) 
                    Cursor = CursorStyle.HandGrabReady;
            }

            if (CurrentCursor != Cursor)
            {
                if (CurrentCursor != 0)
                    CursorManager.main.PopCursor();
                if (Cursor != 0) 
                    CursorManager.main.PushCursor(Cursor);
            
                CurrentCursor = Cursor;
            }
        }

        public delegate void PointerEvent(PointerEventData eventData);

        public void OnDrag(PointerEventData eventData) 
        {
            if (CurrentDragMode != EasingPickerDragMode.None)
            {
                isDragged = true;
           
                OnDragEvent?.Invoke(eventData);
            }
        }

        public PointerEvent OnDragEvent;

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragged = false;
            OnDragEvent = null;
     
            CurrentDragMode = EasingPickerDragMode.None;
      
            UpdateHandles();
            UpdateCursor(eventData.position, eventData.pressEventCamera);
        }
    }

    public enum EasingPickerDragMode
    {   
        None,
        P1Handle,
        P2Handle,
    }
}
