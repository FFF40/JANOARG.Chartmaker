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
        readonly Dictionary<nint, Stack<CursorStyle>> cursorStacks = new();

        public nint GetMainWindowHandle()
        {
            var h = Process.GetCurrentProcess().MainWindowHandle;
            return h;
        }

        public void HookWindow(nint windowHandle)
        {
            // TODO implement
        }
        public void UnhookWindow(nint windowHandle) 
        { 
            // TODO implement
        }

        public string GetWindowName(nint windowHandle)
        {
            var sb = new StringBuilder(1024);
            User32.GetWindowText(windowHandle, sb, sb.Capacity);
            return sb.ToString();
        }

        public void SetWindowName(nint windowHandle, string name)
        {
            User32.SetWindowText(windowHandle, name);
        }

        public WindowState GetWindowState(nint windowHandle)
        {
            if (User32.IsIconic(windowHandle)) return WindowState.Minimized;
            if (User32.IsZoomed(windowHandle)) return WindowState.Maximized;
            return WindowState.Floating;
        }

        public void SetWindowState(nint windowHandle, WindowState state)
        {
            User32.ShowWindow(windowHandle, Win32Convert.ToPlatformWindowState(state));
        }

        public WindowStyle GetWindowStyle(nint windowHandle)
        {
            throw new NotImplementedException();
        }

        public void SetWindowStyle(nint windowHandle, WindowStyle style)
        {
            User32.SetWindowLong(windowHandle, WinWindowLong.Style, (nint)(WinWindowStyle.Overlapped | WinWindowStyle.Visible));
            if (style == WindowStyle.Custom)
            {
                User32.DwmExtendFrameIntoClientArea(windowHandle, new WinMargin { top = 0, left = 0, bottom = 0, right = 0 });
            }
        }

        public RectInt GetWindowRect(nint windowHandle)
        {
            if (User32.GetWindowRect(windowHandle, out WinRect r))
            {
                return new RectInt(r.left, r.top, r.right - r.left, r.bottom - r.top);
            }
            return new(0, 0, 0, 0);
        }

        public void SetWindowRect(nint windowHandle, RectInt rect)
        {
            User32.MoveWindow(windowHandle, rect.x, rect.y, rect.width, rect.height, true);
        }

        public void MoveWindow(nint windowHandle, Vector2Int position)
        {
            if (User32.GetWindowRect(windowHandle, out WinRect r))
            {
                User32.MoveWindow(windowHandle, position.x, position.y, r.right - r.left, r.bottom - r.top, true);
            }
        }

        public void ResizeWindow(nint windowHandle, Vector2Int size)
        {
            if (User32.GetWindowRect(windowHandle, out WinRect r))
            {
                User32.MoveWindow(windowHandle, r.left, r.top, size.x, size.y, true);
            }
        }

        private IntPtr MapCursor(CursorStyle c)
        {
            return c switch
            {
                CursorStyle.None => IntPtr.Zero,
                CursorStyle.Arrow => User32.LoadCursor(IntPtr.Zero, WinCursorStyle.Arrow),
                CursorStyle.Text => User32.LoadCursor(IntPtr.Zero, WinCursorStyle.Text),
                CursorStyle.Crosshair => User32.LoadCursor(IntPtr.Zero, WinCursorStyle.Crosshair),
                CursorStyle.HandPointing => User32.LoadCursor(IntPtr.Zero, WinCursorStyle.HandPointing),
                _ => User32.LoadCursor(IntPtr.Zero, WinCursorStyle.Arrow),
            };
        }

        public CursorStyle PeekWindowCursor(nint windowHandle)
        {
            if (cursorStacks.TryGetValue(windowHandle, out var st) && st.Count > 0) return st.Peek();
            return CursorStyle.None;
        }

        public CursorStyle PopWindowCursor(nint windowHandle)
        {
            if (cursorStacks.TryGetValue(windowHandle, out var st) && st.Count > 0)
            {
                var popped = st.Pop();
                var top = st.Count > 0 ? st.Peek() : CursorStyle.None;
                User32.SetCursor(MapCursor(top));
                return popped;
            }
            return CursorStyle.None;
        }

        public void PushWindowCursor(nint windowHandle, CursorStyle cursor)
        {
            if (!cursorStacks.TryGetValue(windowHandle, out var st))
            {
                st = new Stack<CursorStyle>();
                cursorStacks[windowHandle] = st;
            }
            st.Push(cursor);
            User32.SetCursor(MapCursor(cursor));
        }

        public Vector2Int GetWindowMinSize(nint windowHandle)
        {
            throw new NotImplementedException();
        }

        public void SetWindowMinSize(nint windowHandle, Vector2Int rect)
        {
            throw new NotImplementedException();
        }

        public Vector2Int GetWindowMaxSize(nint windowHandle)
        {
            throw new NotImplementedException();
        }

        public void SetWindowMaxSize(nint windowHandle, Vector2Int rect)
        {
            throw new NotImplementedException();
        }
    }
}
