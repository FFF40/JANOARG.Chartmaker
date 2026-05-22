using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.LinuxX11
{
    internal class LinuxX11NativeWindowProvider : INativeWindowProvider
    {
        readonly Dictionary<nint, Stack<CursorStyle>> cursorStacks = new();

        private nint display;
        private nint currentWindow;

        public nint GetMainWindowHandle()
        {
            var h = Process.GetCurrentProcess().MainWindowHandle;
            return h;
        }

        void EnsureDisplay()
        {
            if (display == 0)
            {
                display = LibX11.XOpenDisplay(0);
            }
            if (currentWindow == 0) 
            {
                currentWindow = Process.GetCurrentProcess().MainWindowHandle;
            }
        }

        public void HookWindow(nint windowHandle) { EnsureDisplay(); }
        public void UnhookWindow(nint windowHandle) { /* no-op */ }

        public string GetWindowName(nint windowHandle)
        {
            EnsureDisplay();
            nint namePtr = 0;
            LibX11.XFetchName(display, windowHandle, ref namePtr);
            return Marshal.PtrToStringAnsi(namePtr) ?? string.Empty;
        }

        public void SetWindowName(nint windowHandle, string name)
        {
            EnsureDisplay();
            LibX11.XStoreName(display, windowHandle, name);
        }

        public WindowState GetWindowState(nint windowHandle)
        {
            // X11 doesn't expose state easily here; return Floating as default
            return WindowState.Floating;
        }

        public void SetWindowState(nint windowHandle, WindowState state)
        {
            // Best-effort: map minimize/restore via unmap/map
            EnsureDisplay();
            var h = windowHandle;
            switch (state)
            {
                case WindowState.Minimized: LibX11.XDestroyWindow(display, h); break;
                case WindowState.Maximized: /* no-op */ break;
                case WindowState.Floating: /* no-op */ break;
            }
        }

        public WindowStyle GetWindowStyle(nint windowHandle) => WindowStyle.Native;
        public void SetWindowStyle(nint windowHandle, WindowStyle style) { /* no-op */ }

        public RectInt GetWindowRect(nint windowHandle)
        {
            // Not implementing a full query here; return zero rect
            return new(0, 0, 0, 0);
        }

        public void SetWindowRect(nint windowHandle, RectInt rect)
        {
            MoveWindow(windowHandle, new Vector2Int(rect.x, rect.y));
            ResizeWindow(windowHandle, new Vector2Int(rect.width, rect.height));
        }

        public void MoveWindow(nint windowHandle, Vector2Int position)
        {
            EnsureDisplay();
            LibX11.XMoveWindow(display, windowHandle, position.x, position.y);
        }

        public void ResizeWindow(nint windowHandle, Vector2Int size)
        {
            EnsureDisplay();
            LibX11.XResizeWindow(display, windowHandle, (uint)size.x, (uint)size.y);
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
                return st.Pop();
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
        }

        public Vector2Int GetWindowMinSize(nint windowHandle)
        {
            EnsureDisplay();
            var hintsPtr = LibX11.XAllocSizeHints(display);
            long supplied = 0;
            if (LibX11.XGetWMSizeHints(display, windowHandle, hintsPtr, ref supplied) != 0)
            {
                var hints = Marshal.PtrToStructure<XSizeHints>(hintsPtr);
                if ((hints.flags & (long)XSizeHintsFlags.PMinSize) != 0)
                {
                    return new Vector2Int(hints.min_width, hints.min_height);
                }
            }
            return new Vector2Int(0, 0);
        }

        public void SetWindowMinSize(nint windowHandle, Vector2Int minSize)
        {
            EnsureDisplay();
            var hintsPtr = LibX11.XAllocSizeHints(display);
            var hints = new XSizeHints
            {
                flags = (long)XSizeHintsFlags.PMinSize,
                min_width = minSize.x,
                min_height = minSize.y
            };
            Marshal.StructureToPtr(hints, hintsPtr, false);
            LibX11.XSetWMSizeHints(display, windowHandle, hintsPtr);
        }

        public Vector2Int GetWindowMaxSize(nint windowHandle)
        {
            EnsureDisplay();
            var hintsPtr = LibX11.XAllocSizeHints(display);
            long supplied = 0;
            if (LibX11.XGetWMSizeHints(display, windowHandle, hintsPtr, ref supplied) != 0)
            {
                var hints = Marshal.PtrToStructure<XSizeHints>(hintsPtr);
                if ((hints.flags & (long)XSizeHintsFlags.PMaxSize) != 0)
                {
                    return new Vector2Int(hints.width, hints.height);
                }
            }
            return new Vector2Int(0, 0);
        }

        public void SetWindowMaxSize(nint windowHandle, Vector2Int maxSize)
        {
            EnsureDisplay();
            var hintsPtr = LibX11.XAllocSizeHints(display);
            var hints = new XSizeHints
            {
                flags = (long)XSizeHintsFlags.PMaxSize,
                width = maxSize.x,
                height = maxSize.y
            };
            Marshal.StructureToPtr(hints, hintsPtr, false);
            LibX11.XSetWMSizeHints(display, windowHandle, hintsPtr);
        }
    }
}
