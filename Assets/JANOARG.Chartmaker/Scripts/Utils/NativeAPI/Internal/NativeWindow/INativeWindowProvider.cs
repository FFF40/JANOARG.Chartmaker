using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow
{
    internal interface INativeWindowProvider : INativeAPIProvider<NativeWindowController>
    {
        /// <summary>
        /// Whether the app can position/size its own top-level window directly.
        /// True on native X11 (and Windows); false under XWayland, where the
        /// compositor owns window placement and client positioning is ignored.
        /// </summary>
        public bool SupportsClientPositioning { get; }

        public nint GetMainWindowHandle();

        public bool HookWindow(nint windowHandle);
        public bool UnhookWindow(nint windowHandle);

        public string GetWindowName(nint windowHandle);
        public bool SetWindowName(nint windowHandle, string name);

        public bool GetWindowActive(nint windowHandle);

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
        public bool SetWindowHitTestZone(nint windowHandle, int zone);
        public bool StartWindowDrag(nint windowHandle, Vector2Int pointerPosition);
        public bool StartWindowResize(nint windowHandle, Vector2Int pointerPosition, WindowResizeEdge edge);
        public Vector2Int GetPointerPosition();
        public int GetPointerButtonMask();
        public bool SetWindowType(nint windowHandle, string typeName);

        public void PumpEvents();
    }
}
