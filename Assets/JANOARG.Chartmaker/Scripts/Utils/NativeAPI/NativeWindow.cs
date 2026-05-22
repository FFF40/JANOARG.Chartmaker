using System;
using JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow;
using UnityEditor.PackageManager.UI;
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
                return CachedMainWindow ??= new NativeWindow(Controller.Provider.GetMainWindowHandle());
            }
        }

        public static bool IsApiAvailable 
        {
            get 
            {
                return Controller.IsAvailable;
            }
        }

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
                return Controller.GetWindowMinSIze(WindowHandle);
            }
            set
            {
                Controller.ResizeWindow(WindowHandle, value);
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

        public CursorStyle CurrentCursor
        {
            get
            {
                return Controller.PeekWindowCursor(WindowHandle);
            }
        }

        public void PushWindowCursor(CursorStyle style)
        {
            Controller.PushWindowCursor(WindowHandle, style);
        }

        public CursorStyle PopWindowCursor()
        {
            return Controller.PopWindowCursor(WindowHandle);
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

    public enum CursorStyle
    {
        None = -1,

        Arrow,
        Crosshair,
        Text,
        TextVertical,

        Busy,
        BackgroundBusy,
        Blocked,

        HandPointing,
        HandGrabReady,
        HandGrabbing,

        ResizeLeft,
        ResizeRight,
        ResizeTop,
        ResizeBottom,
        ResizeTopLeft,
        ResizeTopRight,
        ResizeBottomLeft,
        ResizeBottomRight,
        ResizeVertical,
        ResizeHorizontal,
        ResizeDiagonalTopLeft,
        ResizeDiagonalTopRight,
    }

    /// <summary>
    /// The preferred cursor display style.
    /// </summary>
    public enum PreferredCursorMode
    {
        /// <summary>
        /// Prioritizes app-defined cursors, falls back to OS-defined cursors when no best fit is available.
        /// </summary>
        PreferCustom,

        /// <summary>
        /// Prioritizes OS-defined cursors, falls back to app-defined cursors when no best fit is available.
        /// </summary>
        PreferNative,

        /// <summary>
        /// Prioritizes OS-defined cursors, falls back to other OS-defined cursors when no best fit is available.
        /// </summary>
        PreferNativeBestEffort,
    }
}