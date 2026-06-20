using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow
{
    internal class NativeWindowController : NativeAPIController<NativeWindowController>
    {
        public INativeWindowProvider Provider;

        public override bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return Provider != null;
            }
        }

        protected override bool Initialize()
        {
            if (Provider != null) return true;

            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                Provider = new Windows.WindowsNativeWindowProvider();
            #elif UNITY_STANDALONE_LINUX && !UNITY_EDITOR
                Provider = new LinuxX11.LinuxX11NativeWindowProvider();
            #endif

            return Provider != null;
        }

        public bool SupportsClientPositioning
        {
            get
            {
                EnsureInitialized();
                return Provider?.SupportsClientPositioning ?? true;
            }
        }

        public nint GetMainWindowHandle()
        {
            if (IsAvailable) return Provider.GetMainWindowHandle();
            UnityEngine.Debug.LogWarning("\"GetMainWindowHandle\" is not supported on this platform.");
            return 0;
        }

        public string GetWindowName(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowName(windowHandle);
            UnityEngine.Debug.LogWarning("\"GetWindowName\" is not supported on this platform.");
            return "";
        }

        public bool GetWindowActive(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowActive(windowHandle);
            return Application.isFocused;
        }

        public bool SetWindowName(nint windowHandle, string name)
        {
            if (IsAvailable) return Provider.SetWindowName(windowHandle, name);
            UnityEngine.Debug.LogWarning("\"SetWindowName\" is not supported on this platform.");
            return false;
        }

        public RectInt GetWindowRect(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowRect(windowHandle);
            UnityEngine.Debug.LogWarning("\"GetWindowRect\" is not supported on this platform.");
            return new RectInt(0, 0, 0, 0);
        }

        public WindowState GetWindowState(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowState(windowHandle);
            UnityEngine.Debug.LogWarning("\"GetWindowState\" is not supported on this platform.");
            return WindowState.Floating;
        }

        public WindowStyle GetWindowStyle(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowStyle(windowHandle);
            UnityEngine.Debug.LogWarning("\"GetWindowStyle\" is not supported on this platform.");
            return WindowStyle.Native;
        }

        public bool SetWindowStyle(nint windowHandle, WindowStyle style)
        {
            if (IsAvailable) return Provider.SetWindowStyle(windowHandle, style);
            UnityEngine.Debug.LogWarning("\"SetWindowStyle\" is not supported on this platform.");
            return false;
        }

        public bool HookWindow(nint windowHandle)
        {
            if (IsAvailable)
            {
                return Provider.HookWindow(windowHandle);
            }
            UnityEngine.Debug.LogWarning("\"HookWindow\" is not supported on this platform.");
            return false;
        }

        public bool MoveWindow(nint windowHandle, Vector2Int position)
        {
            if (IsAvailable) return Provider.MoveWindow(windowHandle, position);
            UnityEngine.Debug.LogWarning("\"MoveWindow\" is not supported on this platform.");
            return false;
        }

        public bool SetWindowCursor(nint windowHandle, CursorStyle cursor, bool bestEffort)
        {
            if (IsAvailable) return Provider.SetWindowCursor(windowHandle, cursor, bestEffort);
            UnityEngine.Debug.LogWarning("\"SetWindowCursor\" is not supported on this platform.");
            return false;
        }

        public bool SetWindowHitTestZone(nint windowHandle, int zone)
        {
            if (IsAvailable) return Provider.SetWindowHitTestZone(windowHandle, zone);
            return false;
        }

        public bool StartWindowDrag(nint windowHandle, Vector2Int pointerPosition)
        {
            if (IsAvailable) return Provider.StartWindowDrag(windowHandle, pointerPosition);
            return false;
        }

        public bool StartWindowResize(nint windowHandle, Vector2Int pointerPosition, WindowResizeEdge edge)
        {
            if (IsAvailable) return Provider.StartWindowResize(windowHandle, pointerPosition, edge);
            return false;
        }

        public Vector2Int GetPointerPosition()
        {
            if (IsAvailable) return Provider.GetPointerPosition();
            return new Vector2Int(0, 0);
        }

        public int GetPointerButtonMask()
        {
            if (IsAvailable) return Provider.GetPointerButtonMask();
            return 0;
        }

        public bool SetWindowType(nint windowHandle, string typeName)
        {
            if (IsAvailable) return Provider.SetWindowType(windowHandle, typeName);
            return false;
        }

        public bool ResizeWindow(nint windowHandle, Vector2Int size)
        {
            if (IsAvailable) return Provider.ResizeWindow(windowHandle, size);
            UnityEngine.Debug.LogWarning("\"ResizeWindow\" is not supported on this platform.");
            return false;
        }

        public bool SetWindowRect(nint windowHandle, RectInt rect)
        {
            if (IsAvailable) return Provider.SetWindowRect(windowHandle, rect);
            UnityEngine.Debug.LogWarning("\"SetWindowRect\" is not supported on this platform.");
            return false;
        }

        public bool SetWindowState(nint windowHandle, WindowState state)
        {
            if (IsAvailable) return Provider.SetWindowState(windowHandle, state);
            UnityEngine.Debug.LogWarning("\"SetWindowState\" is not supported on this platform.");
            return false;
        }

        public bool UnhookWindow(nint windowHandle)
        {
            if (IsAvailable) return Provider.UnhookWindow(windowHandle);
            UnityEngine.Debug.LogWarning("\"UnhookWindow\" is not supported on this platform.");
            return false;
        }
        
        public Vector2Int GetWindowMinSize(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowMinSize(windowHandle);
            UnityEngine.Debug.LogWarning("\"GetWindowMinSize\" is not supported on this platform.");
            return Vector2Int.zero;
        }
        
        public bool SetWindowMinSize(nint windowHandle, Vector2Int rect)
        {
            if (IsAvailable) return Provider.SetWindowMinSize(windowHandle, rect);
            UnityEngine.Debug.LogWarning("\"SetWindowMinSize\" is not supported on this platform.");
            return false;
        }

        public Vector2Int GetWindowMaxSize(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowMaxSize(windowHandle);
            UnityEngine.Debug.LogWarning("\"GetWindowMaxSize\" is not supported on this platform.");
            return Vector2Int.zero;
        }

        public bool SetWindowMaxSize(nint windowHandle, Vector2Int rect)
        {
            if (IsAvailable) return Provider.SetWindowMaxSize(windowHandle, rect);
            UnityEngine.Debug.LogWarning("\"SetWindowMaxSize\" is not supported on this platform.");
            return false;
        }

        public void PumpEvents()
        {
            if (IsAvailable) Provider.PumpEvents();
        }
    }
}
