using System.Collections.Generic;
using JANOARG.Chartmaker.UI.Cursor;
using JANOARG.Chartmaker.UI.NativeUI;
using JANOARG.Chartmaker.UI.Tooltip;
using JANOARG.Chartmaker.Utils.NativeAPI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class WindowHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static WindowHandler main;

        [Header("Objects")]
        public GameObject WindowControls;
        public RectTransform NavBar;
        public RectTransform ContentHolder;
        public RectTransform ContextMenuHolder;
        public RectTransform ModalHolder;
        public RectTransform LoaderHolder;
        public GameObject    MenuButton;
        public RectTransform SongDetails;
        [Space]
        public TooltipTarget ResizeTooltip;
        public GameObject    ResizeIconMaximize;
        public GameObject    ResizeIconRestore;
        public RectTransform TopBorder;
        [Space]
        public GameObject InactiveBackground;
        public CanvasGroup LeftGroup;
        public CanvasGroup CenterGroup;
        public CanvasGroup RightGroup;
        [Header("Window")]
        public Vector2Int defaultWindowSize;

        NativeWindow targetWindow;

        public bool maximized { get; private set; }
        public bool active { get; private set; }
        bool isFullScreen;

        bool framed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void InitializeWindow()
        {
            Chartmaker.PreferencesStorage = new("cm_prefs");
            Chartmaker.Preferences.Load(Chartmaker.PreferencesStorage);

            if (!NativeWindow.IsApiAvailable) return;

            NativeWindow window = NativeWindow.MainWindow;
            window.Hook();
            window.Title = "JANOARG Chartmaker";
            window.MinSize = new Vector2Int(974, 607);
            window.Style = Chartmaker.Preferences.UseDefaultWindow ? WindowStyle.Native : WindowStyle.Custom;
        }

        public void Awake()
        {
            main = this;
            targetWindow = NativeWindow.MainWindow;
        }

        public void Quit()
        {
        }

        public void Update()
        {
            if (Screen.fullScreen != isFullScreen)
            {
                isFullScreen = Screen.fullScreen;
                WindowControls.SetActive(!isFullScreen);
                if (!isFullScreen) InitializeWindow();
            }

            bool nativeMaximized = NativeWindow.IsApiAvailable && targetWindow.State == WindowState.Maximized;
            bool nativeActive = NativeWindow.IsApiAvailable ? targetWindow.IsActive : Application.isFocused;
            bool nativeFramed = NativeWindow.IsApiAvailable
                ? targetWindow.Style == WindowStyle.Native
                : Chartmaker.Preferences.UseDefaultWindow;

            if (maximized != nativeMaximized)
                OnSizeChange();

            if (active != nativeActive)
                OnActiveChange();

            if (framed != nativeFramed)
            {
                framed = nativeFramed;
                OnFrameChanged();
            }

            if (isDragging && !framed && NativeWindow.IsApiAvailable)
            {
                Vector2Int currentPointer = targetWindow.GetPointerPosition();
                Vector2Int delta = currentPointer - dragStartPointer;
                Vector2Int newPos = dragStartWindowPos + delta;
                UnityEngine.Debug.Log($"[WindowHandler] Drag: isDragging={isDragging}, cur={currentPointer}, start={dragStartPointer}, winStart={dragStartWindowPos}, newPos={newPos}, curWin={targetWindow.Position}");
                if (newPos != targetWindow.Position)
                    targetWindow.Position = newPos;
            }
        }

        public void OnFrameChanged()
        {
            bool isNavbar = !framed || Chartmaker.Preferences.ForceNavigationBar;
            ContentHolder.sizeDelta = ContextMenuHolder.sizeDelta = ModalHolder.sizeDelta
                = LoaderHolder.sizeDelta = NavBar.anchoredPosition
                    = Vector2.up * (isNavbar ? -28 : 0);
            MenuButton.SetActive(!isNavbar);

            TopBorder.gameObject.SetActive(!framed);
            CenterGroup.gameObject.SetActive(!framed);
            RightGroup.gameObject.SetActive(!framed);

            SongDetails.anchoredPosition = Vector2.right * (isNavbar ? 4 : 32);
        }

        public void ResetWindowSize()
        {
            if (NativeWindow.IsApiAvailable) targetWindow.Size = defaultWindowSize;
        }

        public void CloseWindow()
        {
            EventSystem.current.SetSelectedGameObject(null);
            Application.Quit();
        }

        public void MinimizeWindow()
        {
            EventSystem.current.SetSelectedGameObject(null);
            if (NativeWindow.IsApiAvailable) targetWindow.State = WindowState.Minimized;
        }

        public void ResizeWindow()
        {
            EventSystem.current.SetSelectedGameObject(null);

            maximized = !maximized;
            UnityEngine.Debug.Log($"[WindowHandler] ResizeWindow: maximized={maximized}, IsApiAvailable={NativeWindow.IsApiAvailable}");

            if (NativeWindow.IsApiAvailable) targetWindow.State = maximized ? WindowState.Maximized : WindowState.Floating;

            if (NativeWindow.IsApiAvailable)
            {
                var rect = targetWindow.Rect;
                if (!maximized && rect.yMin < 0) targetWindow.Position += Vector2Int.up * rect.yMin;
            }

            OnSizeChange();
        }

        public void FinalizeDrag() 
        {
            if (!maximized) 
            {
                if (!NativeWindow.IsApiAvailable) return;
                var rect = targetWindow.Rect;
                if (rect.yMin - Input.mousePosition.y + Screen.height < 1 && !maximized) ResizeWindow();
                else if (rect.yMin < 0) targetWindow.Position += Vector2Int.up * rect.yMin;
            }
        }

        public void OnSizeChange() 
        {
            maximized = NativeWindow.IsApiAvailable && targetWindow.State == WindowState.Maximized;
            ResizeTooltip.Text = maximized ? "Restore" : "Maximize";
            ResizeIconMaximize.SetActive(!maximized);
            ResizeIconRestore.SetActive(maximized);
            TopBorder.gameObject.SetActive(!maximized);
        }

        private void OnActiveChange() 
        {
            active = NativeWindow.IsApiAvailable ? targetWindow.IsActive : Application.isFocused;
            InactiveBackground.SetActive(!active);
            LeftGroup.alpha = CenterGroup.alpha = RightGroup.alpha = active ? 1 : 0.5f;
        }

        public void OnPointerEnter(PointerEventData data)
        {
            if (!framed)
            {
                if (NativeWindow.IsApiAvailable) targetWindow.SetHitTestZone((int)WindowZone.TitleBar);
            }
        }

        public void OnPointerExit(PointerEventData data)
        {
            if (NativeWindow.IsApiAvailable) targetWindow.SetHitTestZone((int)WindowZone.Client);
        }

        Vector2Int dragStartWindowPos;
        Vector2Int dragStartPointer;
        bool isDragging;

        public void OnBeginDrag(PointerEventData data)
        {
            UnityEngine.Debug.Log($"[WindowHandler] OnBeginDrag: framed={framed}, IsApiAvailable={NativeWindow.IsApiAvailable}");
            if (framed || !NativeWindow.IsApiAvailable) return;
            dragStartPointer = targetWindow.GetPointerPosition();
            dragStartWindowPos = targetWindow.Position;
            isDragging = true;
            UnityEngine.Debug.Log($"[WindowHandler] OnBeginDrag: start={dragStartPointer}, winStart={dragStartWindowPos}");
        }

        public void OnDrag(PointerEventData data) { }

        public void OnEndDrag(PointerEventData data)
        {
            UnityEngine.Debug.Log($"[WindowHandler] OnEndDrag: isDragging was {isDragging}");
            isDragging = false;
            if (!NativeWindow.IsApiAvailable) return;
            FinalizeDrag();
        }
    }
}
