using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow
{
    internal interface INativeWindowProvider : INativeAPIProvider<NativeWindowController>
    {
        public nint GetMainWindowHandle();

        public void HookWindow(nint windowHandle);
        public void UnhookWindow(nint windowHandle);

        public string GetWindowName(nint windowHandle);
        public void SetWindowName(nint windowHandle, string name);

        public WindowState GetWindowState(nint windowHandle);
        public void SetWindowState(nint windowHandle, WindowState state);

        public WindowStyle GetWindowStyle(nint windowHandle);
        public void SetWindowStyle(nint windowHandle, WindowStyle style);

        public RectInt GetWindowRect(nint windowHandle);
        public void SetWindowRect(nint windowHandle, RectInt rect);
        public void MoveWindow(nint windowHandle, Vector2Int position);
        public void ResizeWindow(nint windowHandle, Vector2Int size);

        public Vector2Int GetWindowMinSize(nint windowHandle);
        public void SetWindowMinSize(nint windowHandle, Vector2Int rect);
        public Vector2Int GetWindowMaxSize(nint windowHandle);
        public void SetWindowMaxSize(nint windowHandle, Vector2Int rect);

        public CursorStyle PeekWindowCursor(nint windowHandle);
        public CursorStyle PopWindowCursor(nint windowHandle);
        public void PushWindowCursor(nint windowHandle, CursorStyle cursor);
    }    
}