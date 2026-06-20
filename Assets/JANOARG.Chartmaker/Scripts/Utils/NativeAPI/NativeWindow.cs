using System;
using JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI
{
    /// <summary>
    /// Represents a window to perform native API.
    /// </summary>
    public class NativeWindow
    {
        private static readonly NativeWindowController Controller = new();

        private static NativeWindow CachedMainWindow;

        public nint WindowHandle { get; private set; }

        public static NativeWindow MainWindow
        {
            get
            {
                return CachedMainWindow ??= new NativeWindow(Controller.GetMainWindowHandle());
            }
        }

        public static bool IsApiAvailable
        {
            get
            {
                return Controller.IsAvailable;
            }
        }

        /// <summary>
        /// Whether the app can position/size its own top-level window directly.
        /// True on native X11 (and Windows); false under XWayland, where the
        /// compositor owns window placement. Used to gate client-positioning
        /// fallbacks that are no-ops under XWayland.
        /// </summary>
        public bool SupportsClientPositioning => Controller.SupportsClientPositioning;

        private NativeWindow(nint handle)
        {
            WindowHandle = handle;
        }

        public Vector2Int Position
        {
            get
            {
                return Controller.GetWindowRect(WindowHandle).position;
            }
            set
            {
                Controller.MoveWindow(WindowHandle, value);
            }
        }

        public string Title
        {
            get
            {
                return Controller.GetWindowName(WindowHandle);
            }
            set
            {
                Controller.SetWindowName(WindowHandle, value);
            }
        }

        public bool IsActive
        {
            get
            {
                return Controller.GetWindowActive(WindowHandle);
            }
        }

        public Vector2Int Size
        {
            get
            {
                return Controller.GetWindowRect(WindowHandle).size;
            }
            set
            {
                Controller.ResizeWindow(WindowHandle, value);
            }
        }

        public RectInt Rect
        {
            get
            {
                return Controller.GetWindowRect(WindowHandle);
            }
            set
            {
                Controller.SetWindowRect(WindowHandle, value);
            }
        }

        public Vector2Int MinSize
        {
            get
            {
                return Controller.GetWindowMinSize(WindowHandle);
            }
            set
            {
                Controller.SetWindowMinSize(WindowHandle, value);
            }
        }

        public Vector2Int MaxSize
        {
            get
            {
                return Controller.GetWindowMaxSize(WindowHandle);
            }
            set
            {
                Controller.SetWindowMaxSize(WindowHandle, value);
            }
        }

        public WindowState State
        {
            get
            {
                return Controller.GetWindowState(WindowHandle);
            }
            set
            {
                Controller.SetWindowState(WindowHandle, value);
            }
        }

        public WindowStyle Style
        {
            get
            {
                return Controller.GetWindowStyle(WindowHandle);
            }
            set
            {
                Controller.SetWindowStyle(WindowHandle, value);
            }
        }

        public bool SetCurrentCursor(CursorStyle style, bool bestEffort)
        {
            return Controller.SetWindowCursor(WindowHandle, style, bestEffort);
        }

        public bool SetHitTestZone(int zone)
        {
            return Controller.SetWindowHitTestZone(WindowHandle, zone);
        }

        public bool StartDrag(Vector2Int pointerPosition)
        {
            return Controller.StartWindowDrag(WindowHandle, pointerPosition);
        }

        public bool StartResize(Vector2Int pointerPosition, WindowResizeEdge edge)
        {
            return Controller.StartWindowResize(WindowHandle, pointerPosition, edge);
        }

        public Vector2Int GetPointerPosition()
        {
            return Controller.GetPointerPosition();
        }

        public int GetPointerButtonMask()
        {
            return Controller.GetPointerButtonMask();
        }

        public bool SetType(string typeName)
        {
            return Controller.SetWindowType(WindowHandle, typeName);
        }

        public bool Hook()
        {
            return Controller.HookWindow(WindowHandle);
        }

        public bool Unhook()
        {
            return Controller.UnhookWindow(WindowHandle);
        }

        public void PumpEvents()
        {
            Controller.PumpEvents();
        }
    }

    /// <summary>
    /// The display state of the window.
    /// </summary>
    public enum WindowState
    {
        /// <summary>
        /// The window is hidden.
        /// </summary>
        Minimized,

        /// <summary>
        /// The window is displayed and takes a portion of the screen.
        /// </summary>
        Floating,

        /// <summary>
        /// The window is displayed and takes on the entire screen.
        /// </summary>
        Maximized,
    }

    /// <summary>
    /// The style of the window, how windows features like title bar, borders, and controls are managed.
    /// </summary>
    public enum WindowStyle
    {
        /// <summary>
        /// Window features are managed by the operating system.
        /// </summary>
        Native,

        /// <summary>
        /// Window features are manually managed by the app.
        /// </summary>
        Custom,
    }

    public enum WindowResizeEdge
    {
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
    }
}
