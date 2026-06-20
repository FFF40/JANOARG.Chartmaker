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
        }

        // Convert Unity's window-local cursor position (bottom-left origin) to the
        // root-relative coordinates EWMH _NET_WM_MOVERESIZE expects (top-left origin).
        Vector2Int GetRootPointer()
        {
            RectInt rect = targetWindow.Rect;
            Vector2 mouse = Input.mousePosition;
            return new Vector2Int(
                rect.x + Mathf.RoundToInt(mouse.x),
                rect.y + Mathf.RoundToInt(Screen.height - mouse.y));
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

            // Nudge the window back on-screen after restoring. No-op (and unnecessary)
            // under XWayland, where the compositor owns placement.
            if (NativeWindow.IsApiAvailable && targetWindow.SupportsClientPositioning)
            {
                var rect = targetWindow.Rect;
                if (!maximized && rect.yMin < 0) targetWindow.Position += Vector2Int.up * rect.yMin;
            }

            OnSizeChange();
        }

        public void FinalizeDrag()
        {
            // The custom "drag to top edge = maximize" gesture and the on-screen nudge
            // both rely on client-controlled positioning, so they only run on native X11.
            // Under XWayland the compositor handles its own drag, top-edge snap, and
            // placement during the compositor-mediated move.
            if (!maximized)
            {
                if (!NativeWindow.IsApiAvailable || !targetWindow.SupportsClientPositioning) return;
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
            if (!NativeWindow.IsApiAvailable || framed || maximized || isFullScreen)
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
                // Delegate the resize to the WM / compositor via EWMH _NET_WM_MOVERESIZE.
                // Works on native X11 and XWayland on all compositors; min-size is enforced
                // by the WM from the window's size hints (set via NativeWindow.MinSize).
                targetWindow.StartResize(GetRootPointer(), edge);
                EventSystem.current.SetSelectedGameObject(null);
            }
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

        public void OnBeginDrag(PointerEventData data)
        {
            if (framed || maximized || TryGetResizeEdge(Input.mousePosition, out _) || !NativeWindow.IsApiAvailable) return;

            // Delegate the move to the WM / compositor via EWMH _NET_WM_MOVERESIZE.
            // Works on native X11 and XWayland on all compositors.
            targetWindow.StartDrag(GetRootPointer());
        }

        public void OnDrag(PointerEventData data) { }

        public void OnEndDrag(PointerEventData data)
        {
            if (!NativeWindow.IsApiAvailable) return;
            FinalizeDrag();
        }
    }
}
