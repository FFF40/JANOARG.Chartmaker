using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow
{
    internal interface INativeWindowProvider : INativeAPIProvider<NativeWindowController>
    {
        public nint GetMainWindowHandle();

        public bool HookWindow(nint windowHandle);
        public bool UnhookWindow(nint windowHandle);

        public string GetWindowName(nint windowHandle);
        public bool SetWindowName(nint windowHandle, string name);

        public WindowState GetWindowState(nint windowHandle);
        public bool SetWindowState(nint windowHandle, WindowState state);

        public WindowStyle GetWindowStyle(nint windowHandle);
        public bool SetWindowStyle(nint windowHandle, WindowStyle style);

        public RectInt GetWindowRect(nint windowHandle);
        public bool SetWindowRect(nint windowHandle, RectInt rect);
        public bool MoveWindow(nint windowHandle, Vector2Int position);
        public bool ResizeWindow(nint windowHandle, Vector2Int size);

        public Vector2Int GetWindowMinSize(nint windowHandle);
        public bool SetWindowMinSize(nint windowHandle, Vector2Int rect);
        public Vector2Int GetWindowMaxSize(nint windowHandle);
        public bool SetWindowMaxSize(nint windowHandle, Vector2Int rect);

        public bool SetWindowCursor(nint windowHandle, CursorStyle cursor, bool bestEffort);
    }    
}