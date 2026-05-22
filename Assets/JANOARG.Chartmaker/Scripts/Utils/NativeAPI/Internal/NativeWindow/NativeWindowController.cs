
using Unity.VisualScripting;
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

        public nint GetMainWindowHandle()
        {
            if (IsAvailable) return Provider.GetMainWindowHandle();
            else throw new System.Exception("API is not supported on this platform.");
        }

        public string GetWindowName(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowName(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void SetWindowName(nint windowHandle, string name)
        {
            if (IsAvailable) Provider.SetWindowName(windowHandle, name);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public RectInt GetWindowRect(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowRect(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public WindowState GetWindowState(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowState(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public WindowStyle GetWindowStyle(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowStyle(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void SetWindowStyle(nint windowHandle, WindowStyle style)
        {
            if (IsAvailable) Provider.SetWindowStyle(windowHandle, style);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void HookWindow(nint windowHandle)
        {
            if (IsAvailable) Provider.HookWindow(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void MoveWindow(nint windowHandle, Vector2Int position)
        {
            if (IsAvailable) Provider.MoveWindow(windowHandle, position);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public CursorStyle PeekWindowCursor(nint windowHandle)
        {
            if (IsAvailable) return Provider.PeekWindowCursor(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public CursorStyle PopWindowCursor(nint windowHandle)
        {
            if (IsAvailable) return Provider.PopWindowCursor(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void PushWindowCursor(nint windowHandle, CursorStyle cursor)
        {
            if (IsAvailable) Provider.PushWindowCursor(windowHandle, cursor);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void ResizeWindow(nint windowHandle, Vector2Int size)
        {
            if (IsAvailable) Provider.ResizeWindow(windowHandle, size);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void SetWindowRect(nint windowHandle, RectInt rect)
        {
            if (IsAvailable) Provider.SetWindowRect(windowHandle, rect);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void SetWindowState(nint windowHandle, WindowState state)
        {
            if (IsAvailable) Provider.SetWindowState(windowHandle, state);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void UnhookWindow(nint windowHandle)
        {
            if (IsAvailable) Provider.UnhookWindow(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }
        
        public Vector2Int GetWindowMinSize(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowMinSize(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }
        
        public void SetWindowMinSize(nint windowHandle, RectInt rect)
        {
            if (IsAvailable) Provider.SetWindowMinSize(windowHandle, rect);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public Vector2Int GetWindowMaxSize(nint windowHandle)
        {
            if (IsAvailable) return Provider.GetWindowMaxSize(windowHandle);
            else throw new System.Exception("API is not supported on this platform.");
        }

        public void SetWindowMaxSize(nint windowHandle, RectInt rect)
        {
            if (IsAvailable) Provider.SetWindowMaxSize(windowHandle, rect);
            else throw new System.Exception("API is not supported on this platform.");
        }
    }
}