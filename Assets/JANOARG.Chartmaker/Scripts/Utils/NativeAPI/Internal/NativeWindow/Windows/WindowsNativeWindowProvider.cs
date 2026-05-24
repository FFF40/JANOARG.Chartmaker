using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.Windows
{
    internal class WindowsNativeWindowProvider : INativeWindowProvider
    {
        readonly WindowsNativeWindowHookManager hookManager = new();

        public nint GetMainWindowHandle()
        {
            var h = Process.GetCurrentProcess().MainWindowHandle;
            return h;
        }

        public bool HookWindow(nint windowHandle)
        {
            return hookManager.HookWindow(windowHandle);
        }
        public bool UnhookWindow(nint windowHandle) 
        { 
            return hookManager.UnhookWindow(windowHandle);
        }

        public string GetWindowName(nint windowHandle)
        {
            var sb = new StringBuilder(1024);
            User32.GetWindowText(windowHandle, sb, sb.Capacity);
            return sb.ToString();
        }

        public bool SetWindowName(nint windowHandle, string name)
        {
            return User32.SetWindowText(windowHandle, name);
        }

        public WindowState GetWindowState(nint windowHandle)
        {
            if (User32.IsIconic(windowHandle)) return WindowState.Minimized;
            if (User32.IsZoomed(windowHandle)) return WindowState.Maximized;
            return WindowState.Floating;
        }

        public bool SetWindowState(nint windowHandle, WindowState state)
        {
            return User32.ShowWindow(windowHandle, Win32Convert.ToPlatformWindowState(state));
        }

        public WindowStyle GetWindowStyle(nint windowHandle)
        {
            throw new NotImplementedException();
        }

        public bool SetWindowStyle(nint windowHandle, WindowStyle style)
        {
            bool succ = User32.SetWindowLong(windowHandle, WinWindowLong.Style, (nint)(WinWindowStyle.Overlapped | WinWindowStyle.Visible)) != 0;
            if (style == WindowStyle.Custom)
            {
                succ &= DwmApi.DwmExtendFrameIntoClientArea(windowHandle, new WinMargin { top = 0, left = 0, bottom = 0, right = 0 }) != 0;
            }
            return succ;
        }

        public RectInt GetWindowRect(nint windowHandle)
        {
            if (User32.GetWindowRect(windowHandle, out WinRect r))
            {
                return new RectInt(r.left, r.top, r.right - r.left, r.bottom - r.top);
            }
            return new(0, 0, 0, 0);
        }

        public bool SetWindowRect(nint windowHandle, RectInt rect)
        {
            return User32.MoveWindow(windowHandle, rect.x, rect.y, rect.width, rect.height, true);
        }

        public bool MoveWindow(nint windowHandle, Vector2Int position)
        {
            if (User32.GetWindowRect(windowHandle, out WinRect r))
            {
                return User32.MoveWindow(windowHandle, position.x, position.y, r.right - r.left, r.bottom - r.top, true);
            }
            return false;
        }

        public bool ResizeWindow(nint windowHandle, Vector2Int size)
        {
            if (User32.GetWindowRect(windowHandle, out WinRect r))
            {
                return User32.MoveWindow(windowHandle, r.left, r.top, size.x, size.y, true);
            }
            return false;
        }

        public bool SetWindowCursor(nint windowHandle, CursorStyle style, bool bestEffort)
        {
            hookManager.HookWindow(windowHandle);
            var hookData = hookManager.GetHookData(windowHandle);
            nint cursor = bestEffort ? Win32Convert.ToPlatformCursorBestEffort(style) : Win32Convert.ToPlatformCursor(style);
            hookData.CurrentCursor = cursor;
            return cursor != 0 || style == CursorStyle.None;
        }

        public Vector2Int GetWindowMinSize(nint windowHandle)
        {
            var hookData = hookManager.GetHookData(windowHandle);
            if (hookData == null) return Vector2Int.zero;
            return hookData.MinSize;
        }

        public bool SetWindowMinSize(nint windowHandle, Vector2Int size)
        {
            hookManager.HookWindow(windowHandle);
            var hookData = hookManager.GetHookData(windowHandle);
            hookData.MinSize = size;
            return true;
        }

        public Vector2Int GetWindowMaxSize(nint windowHandle)
        {
            var hookData = hookManager.GetHookData(windowHandle);
            if (hookData == null) return Vector2Int.zero;
            return hookData.MaxSize;
        }

        public bool SetWindowMaxSize(nint windowHandle, Vector2Int size)
        {
            hookManager.HookWindow(windowHandle);
            var hookData = hookManager.GetHookData(windowHandle);
            hookData.MaxSize = size;
            return true;
        }
    }
}
