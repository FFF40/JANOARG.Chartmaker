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
}