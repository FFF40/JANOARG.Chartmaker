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
        const int ResizeBorderSize = 8;
        const int ResizeBorderVisualSize = 1;
        static readonly Color ResizeBorderColor = new(1f, 1f, 1f, 0.18f);
        static Texture2D resizeBorderTexture;
        bool resizeCursorActive;
        CursorStyle activeResizeCursor;
        bool isNativeResizing;
        Vector2Int resizeStartPointer;
        RectInt resizeStartRect;
        WindowResizeEdge resizeEdge;

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
            targetWindow.PumpEvents();

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

            UpdateResizeEdges();

            if (isDragging && !framed && NativeWindow.IsApiAvailable)
            {
                Vector2Int currentPointer = targetWindow.GetPointerPosition();
                Vector2Int delta = currentPointer - dragStartPointer;
                Vector2Int newPos = dragStartWindowPos + delta;
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

        void OnGUI()
        {
            if (!NativeWindow.IsApiAvailable || framed || maximized || isFullScreen)
                return;

            resizeBorderTexture ??= Texture2D.whiteTexture;
            Color prevColor = GUI.color;
            GUI.color = ResizeBorderColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, ResizeBorderVisualSize), resizeBorderTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - ResizeBorderVisualSize, Screen.width, ResizeBorderVisualSize), resizeBorderTexture);
            GUI.DrawTexture(new Rect(0, 0, ResizeBorderVisualSize, Screen.height), resizeBorderTexture);
            GUI.DrawTexture(new Rect(Screen.width - ResizeBorderVisualSize, 0, ResizeBorderVisualSize, Screen.height), resizeBorderTexture);
            GUI.color = prevColor;
        }

        void UpdateResizeEdges()
        {
            if (isNativeResizing)
            {
                if ((targetWindow.GetPointerButtonMask() & 0x100) != 0)
                {
                    UpdateManualResize(false);
                    return;
                }
                else
                {
                    UpdateManualResize(true); // Force final sync on release
                    isNativeResizing = false;
                }
            }

            if (!NativeWindow.IsApiAvailable || framed || maximized || isFullScreen || isDragging)
            {
                ClearResizeCursor();
                return;
            }

            Vector2 mouse = Input.mousePosition;
            if (!TryGetResizeEdge(mouse, out WindowResizeEdge edge))
            {
                ClearResizeCursor();
                return;
            }

            CursorStyle resizeCursor = GetResizeCursor(edge);
            SetResizeCursor(resizeCursor);

            if (Input.GetMouseButtonDown(0))
            {
                targetWindow.StartResize(targetWindow.GetPointerPosition(), edge);
                resizeStartPointer = targetWindow.GetPointerPosition();
                resizeStartRect = targetWindow.Rect;
                resizeEdge = edge;
                isNativeResizing = true;
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        float lastResizeTime;
        const float ResizeThrottleInterval = 0.05f; // 50ms

        void UpdateManualResize(bool force)
        {
            if (!force && Time.realtimeSinceStartup - lastResizeTime < ResizeThrottleInterval)
                return;

            Vector2Int delta = targetWindow.GetPointerPosition() - resizeStartPointer;
            RectInt rect = resizeStartRect;
            Vector2Int minSize = targetWindow.MinSize;

            switch (resizeEdge)
            {
                case WindowResizeEdge.TopLeft:
                    rect.xMin += delta.x;
                    rect.yMin += delta.y;
                    break;
                case WindowResizeEdge.Top:
                    rect.yMin += delta.y;
                    break;
                case WindowResizeEdge.TopRight:
                    rect.xMax += delta.x;
                    rect.yMin += delta.y;
                    break;
                case WindowResizeEdge.Right:
                    rect.xMax += delta.x;
                    break;
                case WindowResizeEdge.BottomRight:
                    rect.xMax += delta.x;
                    rect.yMax += delta.y;
                    break;
                case WindowResizeEdge.Bottom:
                    rect.yMax += delta.y;
                    break;
                case WindowResizeEdge.BottomLeft:
                    rect.xMin += delta.x;
                    rect.yMax += delta.y;
                    break;
                case WindowResizeEdge.Left:
                    rect.xMin += delta.x;
                    break;
            }

            rect = ApplyResizeMinSize(rect, minSize, resizeEdge);
            RectInt currentRect = targetWindow.Rect;

            bool sizeChanged = rect.width != Screen.width || rect.height != Screen.height;
            bool posChanged = rect.x != currentRect.x || rect.y != currentRect.y;

            if (sizeChanged || posChanged || force)
            {
                lastResizeTime = Time.realtimeSinceStartup;
                targetWindow.Rect = rect;
                if (force)
                {
                    Screen.SetResolution(rect.width, rect.height, FullScreenMode.Windowed);
                }
            }
        }

        RectInt ApplyResizeMinSize(RectInt rect, Vector2Int minSize, WindowResizeEdge edge)
        {
            minSize.x = Mathf.Max(minSize.x, 1);
            minSize.y = Mathf.Max(minSize.y, 1);

            if (rect.width < minSize.x)
            {
                if (edge == WindowResizeEdge.Left || edge == WindowResizeEdge.TopLeft || edge == WindowResizeEdge.BottomLeft)
                    rect.xMin = rect.xMax - minSize.x;
                else
                    rect.xMax = rect.xMin + minSize.x;
            }

            if (rect.height < minSize.y)
            {
                if (edge == WindowResizeEdge.Top || edge == WindowResizeEdge.TopLeft || edge == WindowResizeEdge.TopRight)
                    rect.yMin = rect.yMax - minSize.y;
                else
                    rect.yMax = rect.yMin + minSize.y;
            }

            return rect;
        }

        bool TryGetResizeEdge(Vector2 mousePosition, out WindowResizeEdge edge)
        {
            edge = WindowResizeEdge.Right;
            bool left = mousePosition.x <= ResizeBorderSize;
            bool right = mousePosition.x >= Screen.width - ResizeBorderSize;
            bool bottom = mousePosition.y <= ResizeBorderSize;
            bool top = mousePosition.y >= Screen.height - ResizeBorderSize;

            if (top && left) edge = WindowResizeEdge.TopLeft;
            else if (top && right) edge = WindowResizeEdge.TopRight;
            else if (bottom && right) edge = WindowResizeEdge.BottomRight;
            else if (bottom && left) edge = WindowResizeEdge.BottomLeft;
            else if (top) edge = WindowResizeEdge.Top;
            else if (right) edge = WindowResizeEdge.Right;
            else if (bottom) edge = WindowResizeEdge.Bottom;
            else if (left) edge = WindowResizeEdge.Left;
            else return false;

            return true;
        }

        CursorStyle GetResizeCursor(WindowResizeEdge edge)
        {
            return edge switch
            {
                WindowResizeEdge.Top => CursorStyle.ResizeVertical,
                WindowResizeEdge.Bottom => CursorStyle.ResizeVertical,
                WindowResizeEdge.Left => CursorStyle.ResizeHorizontal,
                WindowResizeEdge.Right => CursorStyle.ResizeHorizontal,
                //WindowResizeEdge.TopLeft => CursorStyle.ResizeDiagonalTopLeftBottomRight,
                //WindowResizeEdge.TopRight => CursorStyle.ResizeDiagonalBottomLeftTopRight,
                //WindowResizeEdge.BottomLeft => CursorStyle.ResizeDiagonalBottomLeftTopRight,
                //WindowResizeEdge.BottomRight => CursorStyle.ResizeDiagonalTopLeftBottomRight,
                _ => CursorStyle.Arrow,
            };
        }

        void SetResizeCursor(CursorStyle cursor)
        {
            if (resizeCursorActive && activeResizeCursor == cursor) return;
            ClearResizeCursor();
            if (!CursorManager.main) return;
            CursorManager.main.PushCursor(cursor);
            activeResizeCursor = cursor;
            resizeCursorActive = true;
        }

        void ClearResizeCursor()
        {
            if (!resizeCursorActive) return;
            if (CursorManager.main) CursorManager.main.PopCursor();
            resizeCursorActive = false;
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
            if (framed || maximized || isNativeResizing || TryGetResizeEdge(Input.mousePosition, out _) || !NativeWindow.IsApiAvailable) return;

            if (targetWindow.StartDrag(targetWindow.GetPointerPosition()))
                return;

            dragStartPointer = targetWindow.GetPointerPosition();
            dragStartWindowPos = targetWindow.Position;
            isDragging = true;
        }

        public void OnDrag(PointerEventData data) { }

        public void OnEndDrag(PointerEventData data)
        {
            isDragging = false;
            if (!NativeWindow.IsApiAvailable) return;
            FinalizeDrag();
        }
    }
}
