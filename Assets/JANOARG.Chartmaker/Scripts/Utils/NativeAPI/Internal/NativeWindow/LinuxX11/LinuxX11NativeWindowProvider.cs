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
        private nint display;
        private nint currentWindow;
        private Dictionary<CursorStyle, nint> cursorCache = new ();

        public nint GetMainWindowHandle()
        {
            var h = Process.GetCurrentProcess().MainWindowHandle;
            return h;
        }

        bool EnsureDisplay()
        {
            if (display == 0)
            {
                display = LibX11.XOpenDisplay(0);
            }
            if (currentWindow == 0) 
            {
                currentWindow = Process.GetCurrentProcess().MainWindowHandle;
            }
            return display != 0;
        }

        public bool HookWindow(nint windowHandle) 
        { 
            return EnsureDisplay(); 
        }

        public bool UnhookWindow(nint windowHandle) 
        { 
            return true; 
        }

        public string GetWindowName(nint windowHandle)
        {
            if (!EnsureDisplay()) return string.Empty;
            nint namePtr = 0;
            LibX11.XFetchName(display, windowHandle, ref namePtr);
            return Marshal.PtrToStringAnsi(namePtr) ?? string.Empty;
        }

        public bool SetWindowName(nint windowHandle, string name)
        {
            if (!EnsureDisplay()) return false;
            LibX11.XStoreName(display, windowHandle, name);
            return true;
        }

        public WindowState GetWindowState(nint windowHandle)
        {
            return WindowState.Floating;
        }

        public bool SetWindowState(nint windowHandle, WindowState state)
        {
            if (!EnsureDisplay()) return false;
            var h = windowHandle;
            switch (state)
            {
                case WindowState.Minimized: 
                    LibX11.XDestroyWindow(display, h); 
                    return true;
                case WindowState.Maximized: 
                    return true;
                case WindowState.Floating: 
                    return true;
                default:
                    return false;
            }
        }

        public WindowStyle GetWindowStyle(nint windowHandle)
        {
            return WindowStyle.Native;
        }
        
        public bool SetWindowStyle(nint windowHandle, WindowStyle style) 
        { 
            return false; 
        }

        public RectInt GetWindowRect(nint windowHandle)
        {
            return new(0, 0, 0, 0);
        }

        public bool SetWindowRect(nint windowHandle, RectInt rect)
        {
            bool moved = MoveWindow(windowHandle, new Vector2Int(rect.x, rect.y));
            bool resized = ResizeWindow(windowHandle, new Vector2Int(rect.width, rect.height));
            return moved && resized;
        }

        public bool MoveWindow(nint windowHandle, Vector2Int position)
        {
            if (!EnsureDisplay()) return false;
            return LibX11.XMoveWindow(display, windowHandle, position.x, position.y) != 0;
        }

        public bool ResizeWindow(nint windowHandle, Vector2Int size)
        {
            if (!EnsureDisplay()) return false;
            return LibX11.XResizeWindow(display, windowHandle, (uint)size.x, (uint)size.y) != 0;
        }

        public Vector2Int GetWindowMinSize(nint windowHandle)
        {
            if (!EnsureDisplay()) return new Vector2Int(0, 0);
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

        public bool SetWindowMinSize(nint windowHandle, Vector2Int minSize)
        {
            if (!EnsureDisplay()) return false;
            var hintsPtr = LibX11.XAllocSizeHints(display);
            if (hintsPtr == 0) return false;
            
            var hints = new XSizeHints
            {
                flags = (long)XSizeHintsFlags.PMinSize,
                min_width = minSize.x,
                min_height = minSize.y
            };
            Marshal.StructureToPtr(hints, hintsPtr, false);
            LibX11.XSetWMSizeHints(display, windowHandle, hintsPtr);
            return true;
        }

        public Vector2Int GetWindowMaxSize(nint windowHandle)
        {
            if (!EnsureDisplay()) return new Vector2Int(0, 0);
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

        public bool SetWindowMaxSize(nint windowHandle, Vector2Int maxSize)
        {
            if (!EnsureDisplay()) return false;
            var hintsPtr = LibX11.XAllocSizeHints(display);
            if (hintsPtr == 0) return false;

            var hints = new XSizeHints
            {
                flags = (long)XSizeHintsFlags.PMaxSize,
                width = maxSize.x,
                height = maxSize.y
            };
            Marshal.StructureToPtr(hints, hintsPtr, false);
            LibX11.XSetWMSizeHints(display, windowHandle, hintsPtr);
            return true;
        }

        public bool SetWindowCursor(nint windowHandle, CursorStyle cursor, bool bestEffort)
        {
            if (!EnsureDisplay()) return false;

            if (cursorCache.ContainsKey(cursor))
            {
                if (LibX11.XDefineCursor(display, windowHandle, cursorCache[cursor]) != 0)
                {
                    return true;
                }
                else
                {
                    cursorCache.Remove(cursor);
                }
            }

            string[] cursorNames = X11Convert.CursorNames.GetValueOrDefault(cursor, new string[] {});
            foreach (string name in cursorNames)
            {
                nint cursorPtr = LibXCursor.XcursorLibraryLoadCursor(display, name);
                if (cursorPtr != 0)
                {
                    cursorCache[cursor] = cursorPtr;
                    UnityEngine.Debug.Log($"Using cursur {name} (pointer {cursorPtr}) for CursorStyle {cursor}");
                    return ProcesssX11Error(LibX11.XDefineCursor(display, windowHandle, cursorCache[cursor])) != 0;
                }
            }
            UnityEngine.Debug.Log($"Can not find a cursor for the CursorStyle {cursor}");

            return false;
        }

        public static int ProcesssX11Error(int returnValue)
        {
            if (returnValue == 0)
            {
                UnityEngine.Debug.LogWarning($"X11 API returned error {returnValue}");
            }
            return returnValue;
        }
    }
}