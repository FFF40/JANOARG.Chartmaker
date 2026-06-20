using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.LinuxX11
{
    internal class LinuxX11NativeWindowProvider : INativeWindowProvider
    {
        private nint display;
        private nint currentWindow;
        private Dictionary<CursorStyle, nint> cursorCache = new ();
        private Dictionary<nint, WindowStyle> windowStyles = new();
        private Dictionary<nint, bool> maximizedFlags = new();
        private Dictionary<nint, XSizeHints> savedSizeHints = new();
        private bool eventMaskSubscribed;

        private bool? isXWayland;

        /// <summary>
        /// True when running under XWayland (a Wayland compositor), where the
        /// compositor owns window placement and client positioning is ignored.
        /// </summary>
        private bool IsXWayland
        {
            get
            {
                isXWayland ??=
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))
                    || string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"),
                        "wayland", StringComparison.OrdinalIgnoreCase);
                return isXWayland.Value;
            }
        }

        public bool SupportsClientPositioning => !IsXWayland;

        public nint GetMainWindowHandle()
        {
            if (display == 0)
            {
                display = LibX11.XOpenDisplay(0);
            }
            if (display == 0) return 0;

            int myPid = Process.GetCurrentProcess().Id;
            nint root = LibX11.XDefaultRootWindow(display);

            // Try _NET_WM_PID first
            nint pidAtom = LibX11.XInternAtom(display, "_NET_WM_PID", true);
            if (pidAtom != 0)
            {
                nint handle = FindWindowByPid(root, pidAtom, myPid);
                if (handle != 0) return handle;
            }

            // Fallback: search by window name
            return FindWindowByName(root, "JANOARG Chartmaker");
        }

        nint FindWindowByPid(nint window, nint pidAtom, int targetPid)
        {
            int result = LibX11.XGetWindowProperty(display, window, pidAtom, 0, 1, false,
                (nint)6 /* XA_CARDINAL */, out _, out int format, out nint itemCount, out _, out nint prop);
            if (result == 0 && prop != 0 && format == 32 && itemCount == 1)
            {
                int pid = Marshal.ReadInt32(prop);
                LibX11.XFree(prop);
                if (pid == targetPid) return window;
            }
            else if (prop != 0) LibX11.XFree(prop);

            if (LibX11.XQueryTree(display, window, out _, out _, out nint children, out nint count) != 0 && children != 0)
            {
                try
                {
                    for (int i = 0; i < (int)count; i++)
                    {
                        nint child = Marshal.ReadIntPtr(children, i * IntPtr.Size);
                        nint found = FindWindowByPid(child, pidAtom, targetPid);
                        if (found != 0) return found;
                    }
                }
                finally { LibX11.XFree(children); }
            }
            return 0;
        }

        nint FindWindowByName(nint window, string targetName)
        {
            nint namePtr = 0;
            if (LibX11.XFetchName(display, window, ref namePtr) != 0 && namePtr != 0)
            {
                string name = Marshal.PtrToStringAnsi(namePtr) ?? "";
                LibX11.XFree(namePtr);
                if (name == targetName) return window;
            }

            if (LibX11.XQueryTree(display, window, out _, out _, out nint children, out nint count) != 0 && children != 0)
            {
                try
                {
                    for (int i = 0; i < (int)count; i++)
                    {
                        nint child = Marshal.ReadIntPtr(children, i * IntPtr.Size);
                        nint found = FindWindowByName(child, targetName);
                        if (found != 0) return found;
                    }
                }
                finally { LibX11.XFree(children); }
            }
            return 0;
        }

        bool EnsureDisplay()
        {
            if (display == 0)
            {
                display = LibX11.XOpenDisplay(0);
            }
            if (currentWindow == 0)
            {
                currentWindow = GetMainWindowHandle();
            }
            if (display != 0 && currentWindow != 0 && !eventMaskSubscribed)
            {
                LibX11.XSelectInput(display, currentWindow, (nint)0x20000);
                eventMaskSubscribed = true;

                nint allowedActionsAtom = LibX11.XInternAtom(display, "_NET_WM_ALLOWED_ACTIONS", false);
                nint atomType = LibX11.XInternAtom(display, "ATOM", false);
                if (allowedActionsAtom != 0 && atomType != 0)
                {
                    nint[] actions = {
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_MOVE", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_RESIZE", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_MINIMIZE", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_MAXIMIZE", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_FULLSCREEN", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_CHANGE_DESKTOP", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_CLOSE", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_ABOVE", false),
                        LibX11.XInternAtom(display, "_NET_WM_ACTION_BELOW", false),
                    };
                    LibX11.XChangeProperty(display, currentWindow, allowedActionsAtom, atomType, 32, 0, actions, actions.Length);
                    LibX11.XFlush(display);
                }

                if (TryGetSizeHints(currentWindow, out var sizeHints))
                {
                    if ((sizeHints.flags & (long)XSizeHintsFlags.PMaxSize) == 0 || sizeHints.max_width == 0 || sizeHints.max_height == 0)
                    {
                        sizeHints.flags |= (long)XSizeHintsFlags.PMaxSize;
                        sizeHints.max_width = 65535;
                        sizeHints.max_height = 65535;
                        SetSizeHints(currentWindow, sizeHints);
                    }
                }
            }
            return display != 0 && currentWindow != 0;
        }

        bool CanUseWindow(nint windowHandle)
        {
            return EnsureDisplay()
                && windowHandle != 0
                && LibX11.XGetWindowAttributes(display, windowHandle, out _) != 0;
        }

        nint Atom(string name, bool onlyIfExists = false)
        {
            if (!EnsureDisplay()) return 0;
            return LibX11.XInternAtom(display, name, onlyIfExists);
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
            if (!CanUseWindow(windowHandle)) return string.Empty;
            nint namePtr = 0;
            if (LibX11.XFetchName(display, windowHandle, ref namePtr) == 0 || namePtr == 0) return string.Empty;
            string name = Marshal.PtrToStringAnsi(namePtr) ?? string.Empty;
            LibX11.XFree(namePtr);
            return name;
        }

        public bool SetWindowName(nint windowHandle, string name)
        {
            if (!CanUseWindow(windowHandle)) return false;
            LibX11.XStoreName(display, windowHandle, name);
            LibX11.XFlush(display);
            return true;
        }

        public bool SetWindowType(nint windowHandle, string typeName)
        {
            if (!CanUseWindow(windowHandle)) return false;
            nint typeAtom = Atom("_NET_WM_WINDOW_TYPE", false);
            nint valueAtom = Atom(typeName, false);
            if (typeAtom == 0 || valueAtom == 0) return false;
            nint[] values = { valueAtom };
            LibX11.XChangeProperty(display, windowHandle, typeAtom, (nint)4 /* XA_ATOM */, 32, 0, values, 1);
            LibX11.XFlush(display);
            return true;
        }

        public bool GetWindowActive(nint windowHandle)
        {
            return Application.isFocused;
        }

        public WindowState GetWindowState(nint windowHandle)
        {
            if (HasWindowState(windowHandle, "_NET_WM_STATE_MAXIMIZED_VERT")
                && HasWindowState(windowHandle, "_NET_WM_STATE_MAXIMIZED_HORZ"))
            {
                maximizedFlags[windowHandle] = true;
                return WindowState.Maximized;
            }
            if (maximizedFlags.TryGetValue(windowHandle, out var max) && max && IsRectMaximized(windowHandle))
                return WindowState.Maximized;
            maximizedFlags[windowHandle] = false;
            return WindowState.Floating;
        }

        private Dictionary<nint, RectInt> preMaximizeRects = new();

        public bool SetWindowState(nint windowHandle, WindowState state)
        {
            if (!CanUseWindow(windowHandle)) return false;
            switch (state)
            {
                case WindowState.Minimized:
                    return LibX11.XIconifyWindow(display, windowHandle, LibX11.XDefaultScreen(display)) != 0;
                case WindowState.Maximized:
                    if (!preMaximizeRects.ContainsKey(windowHandle))
                        preMaximizeRects[windowHandle] = GetWindowRect(windowHandle);

                    // Save current size hints and widen them so PMinSize/PMaxSize can't
                    // block the resize.
                    if (!savedSizeHints.ContainsKey(windowHandle) && TryGetSizeHints(windowHandle, out var preMaximizeHints))
                        savedSizeHints[windowHandle] = preMaximizeHints;

                    var waForHints = GetWorkareaFor(windowHandle);
                    SetWindowMinSize(windowHandle, new Vector2Int(0, 0));
                    SetWindowMaxSize(windowHandle, new Vector2Int(waForHints.width, waForHints.height));

                    SendNetWmState(windowHandle, 1, "_NET_WM_STATE_MAXIMIZED_VERT", "_NET_WM_STATE_MAXIMIZED_HORZ");

                    // Manual resize as a belt-and-braces fallback in case Mutter
                    // doesn't do it on its own before our poll expires. Skipped under
                    // XWayland, where client positioning/sizing is ignored and would
                    // only fight the compositor.
                    if (SupportsClientPositioning)
                    {
                        if (waForHints.width > 0 && waForHints.height > 0)
                            SetWindowRect(windowHandle, waForHints);

                        var preSize = new Vector2Int(
                            preMaximizeRects[windowHandle].width,
                            preMaximizeRects[windowHandle].height);
                        for (int i = 0; i < 25; i++) // up to ~500ms
                        {
                            var r = GetWindowRect(windowHandle);
                            if (r.width != preSize.x || r.height != preSize.y)
                                break;
                            System.Threading.Thread.Sleep(20);
                        }
                    }

                    maximizedFlags[windowHandle] = true;
                    return true;
                case WindowState.Floating:
                    SendNetWmState(windowHandle, 0, "_NET_WM_STATE_MAXIMIZED_VERT", "_NET_WM_STATE_MAXIMIZED_HORZ");

                    if (preMaximizeRects.TryGetValue(windowHandle, out var prev))
                    {
                        // Restoring an explicit rect only works where the client may
                        // position itself; under XWayland the compositor restores the
                        // pre-maximize geometry on its own.
                        if (SupportsClientPositioning)
                            SetWindowRect(windowHandle, prev);
                        preMaximizeRects.Remove(windowHandle);
                    }

                    if (savedSizeHints.TryGetValue(windowHandle, out var restoredHints))
                    {
                        SetSizeHints(windowHandle, restoredHints);
                        savedSizeHints.Remove(windowHandle);
                    }

                    maximizedFlags[windowHandle] = false;
                    return true;
                default:
                    return false;
            }
        }

        RectInt GetWorkareaFor(nint windowHandle)
        {
            int screen = LibX11.XDefaultScreen(display);
            int fullW = LibX11.XDisplayWidth(display, screen);
            int fullH = LibX11.XDisplayHeight(display, screen);
            RectInt fallback = new RectInt(0, 0, fullW, fullH);

            nint root = LibX11.XDefaultRootWindow(display);
            nint workareaAtom = Atom("_NET_WORKAREA", true);
            if (workareaAtom == 0) return fallback;

            int result = LibX11.XGetWindowProperty(display, root, workareaAtom, 0, 64, false,
                (nint)6 /* XA_CARDINAL */, out _, out int format, out nint itemCount, out _, out nint prop);
            if (result != 0 || prop == 0 || format != 32 || itemCount < 4)
            {
                if (prop != 0) LibX11.XFree(prop);
                return fallback;
            }

            try
            {
                int workareaCount = (int)itemCount / 4;
                RectInt first = new RectInt(
                    ReadXPropertyInt32(prop, 0),
                    ReadXPropertyInt32(prop, 1),
                    ReadXPropertyInt32(prop, 2),
                    ReadXPropertyInt32(prop, 3));

                if (workareaCount == 1) return first;

                RectInt current = GetWindowRect(windowHandle);
                int centerX = current.x + current.width / 2;
                int centerY = current.y + current.height / 2;

                for (int i = 0; i < workareaCount; i++)
                {
                    int offset = i * 4;
                    RectInt wa = new RectInt(
                        ReadXPropertyInt32(prop, offset),
                        ReadXPropertyInt32(prop, offset + 1),
                        ReadXPropertyInt32(prop, offset + 2),
                        ReadXPropertyInt32(prop, offset + 3));
                    if (centerX >= wa.x && centerX < wa.x + wa.width &&
                        centerY >= wa.y && centerY < wa.y + wa.height)
                        return wa;
                }

                return first;
            }
            finally
            {
                LibX11.XFree(prop);
            }
        }

        public WindowStyle GetWindowStyle(nint windowHandle)
        {
            if (windowStyles.TryGetValue(windowHandle, out WindowStyle cached))
                return cached;

            if (!CanUseWindow(windowHandle)) return WindowStyle.Native;

            nint motifHints = Atom("_MOTIF_WM_HINTS", true);
            if (motifHints != 0)
            {
                int result = LibX11.XGetWindowProperty(display, windowHandle, motifHints, 0, 5, false,
                    motifHints, out _, out int format, out nint itemCount, out _, out nint prop);
                if (result == 0 && prop != 0 && format == 32 && itemCount >= 3)
                {
                    nuint decorations = (nuint)(long)Marshal.ReadIntPtr(prop, IntPtr.Size * 2);
                    LibX11.XFree(prop);
                    windowStyles[windowHandle] = decorations != 0 ? WindowStyle.Native : WindowStyle.Custom;
                    return windowStyles[windowHandle];
                }
                if (prop != 0) LibX11.XFree(prop);
            }

            return WindowStyle.Native;
        }
        
        public bool SetWindowStyle(nint windowHandle, WindowStyle style)
        {
            if (!CanUseWindow(windowHandle)) return false;

            // Capture the content (client) rect before toggling decorations so the
            // content size/position can be kept constant across the change. Skipped when
            // the style isn't actually changing or while maximized (that state owns the
            // geometry).
            WindowStyle previousStyle = GetWindowStyle(windowHandle);
            bool preserveGeometry = previousStyle != style
                && GetWindowState(windowHandle) != WindowState.Maximized;
            RectInt contentRect = default;
            if (preserveGeometry)
            {
                contentRect = InsetByExtents(GetWindowRect(windowHandle), GetFrameExtents(windowHandle));
                if (contentRect.width < 1 || contentRect.height < 1) preserveGeometry = false;
            }

            // Update cache first so GetWindowStyle returns the right value immediately
            windowStyles[windowHandle] = style;

            // Set window type to NORMAL so WMs (especially Mutter) process _NET_WM_STATE correctly
            SetWindowType(windowHandle, "_NET_WM_WINDOW_TYPE_NORMAL");

            nint motifHints = Atom("_MOTIF_WM_HINTS");
            if (motifHints == 0) return false;

            var hints = new MotifWmHints
            {
                flags = 2, // MWM_HINTS_DECORATIONS
                decorations = style == WindowStyle.Native ? 1u : 0u,
            };
            LibX11.XChangeProperty(display, windowHandle, motifHints, motifHints, 32, 0, ref hints, 5);
            LibX11.XFlush(display);

            // Unmap/remap to force the WM to re-read the Motif hints. Done only on
            // native X11: under XWayland (notably wlroots: Sway/Hyprland) a remap can
            // cause focus loss, flashes, or be treated as a brand-new window (re-tiling).
            // Mutter/KWin pick up the change from the property write above instead.
            if (SupportsClientPositioning)
            {
                // XSync between unmap and remap is required so the WM processes the unmap first.
                LibX11.XUnmapWindow(display, windowHandle);
                LibX11.XSync(display, false);
                LibX11.XMapWindow(display, windowHandle);
                LibX11.XFlush(display);
            }

            // Toggling decorations makes the WM re-place/re-size the window (Mutter
            // reverts it to a default geometry). Re-assert the captured content rect so
            // size stays put — and, where the client may position itself (native X11),
            // position too. Under XWayland the compositor honors the resize and ignores
            // the move, which is exactly the achievable split.
            if (preserveGeometry)
                RestoreContentRect(windowHandle, contentRect, style == WindowStyle.Native);

            return true;
        }

        // Shrinks an outer (frame) rect by the window manager's decoration extents to
        // get the content/client rect.
        RectInt InsetByExtents(RectInt outer, (int left, int right, int top, int bottom) e)
        {
            return new RectInt(
                outer.x + e.left,
                outer.y + e.top,
                System.Math.Max(outer.width - e.left - e.right, 1),
                System.Math.Max(outer.height - e.top - e.bottom, 1));
        }

        // Re-applies a target content rect after a decoration change, expanding it to the
        // outer frame via the new decoration extents. If keeping the content fixed would
        // push the decorated frame outside the work area (title bar above the top panel
        // being the common case), falls back to keeping the frame inside the old content
        // box and letting the content shrink instead.
        void RestoreContentRect(nint windowHandle, RectInt content, bool expectDecorations)
        {
            var e = WaitForFrameExtents(windowHandle, expectDecorations);

            RectInt outer = new RectInt(
                content.x - e.left,
                content.y - e.top,
                content.width + e.left + e.right,
                content.height + e.top + e.bottom);

            RectInt wa = GetWorkareaFor(windowHandle);
            bool outOfBounds =
                outer.x < wa.x ||
                outer.y < wa.y ||
                outer.x + outer.width > wa.x + wa.width ||
                outer.y + outer.height > wa.y + wa.height;
            if (outOfBounds)
                outer = content; // outer-window preservation

            // SetWindowRect configures the toplevel frame; under XWayland the compositor
            // honors the size and ignores the position (the desired split).
            SetWindowRect(windowHandle, outer);
        }

        // Reads _NET_FRAME_EXTENTS (left, right, top, bottom). Returns zeros when the WM
        // publishes no extents (undecorated window, or compositors like wlroots that
        // don't draw server-side decorations for X11 windows).
        (int left, int right, int top, int bottom) GetFrameExtents(nint windowHandle)
        {
            nint atom = Atom("_NET_FRAME_EXTENTS", true);
            if (atom == 0) return (0, 0, 0, 0);

            int result = LibX11.XGetWindowProperty(display, windowHandle, atom, 0, 4, false,
                (nint)6 /* XA_CARDINAL */, out _, out int format, out nint itemCount, out _, out nint prop);
            if (result != 0 || prop == 0 || format != 32 || (int)itemCount < 4)
            {
                if (prop != 0) LibX11.XFree(prop);
                return (0, 0, 0, 0);
            }
            try
            {
                return (
                    ReadXPropertyInt32(prop, 0),
                    ReadXPropertyInt32(prop, 1),
                    ReadXPropertyInt32(prop, 2),
                    ReadXPropertyInt32(prop, 3));
            }
            finally
            {
                LibX11.XFree(prop);
            }
        }

        // Polls until the frame extents match the expected decoration state (or a
        // timeout), since the WM applies decorations asynchronously after the hint write.
        (int left, int right, int top, int bottom) WaitForFrameExtents(nint windowHandle, bool expectDecorations)
        {
            var e = GetFrameExtents(windowHandle);
            for (int i = 0; i < 20; i++) // up to ~400ms
            {
                bool hasDecorations = e.left != 0 || e.right != 0 || e.top != 0 || e.bottom != 0;
                if (hasDecorations == expectDecorations) break;
                System.Threading.Thread.Sleep(20);
                e = GetFrameExtents(windowHandle);
            }
            return e;
        }

        private nint GetToplevelParent(nint window)
        {
            if (window == 0) return 0;
            nint current = window;
            nint root = LibX11.XDefaultRootWindow(display);
            while (current != 0 && current != root)
            {
                nint parent;
                if (LibX11.XQueryTree(display, current, out nint rootReturn, out parent, out _, out _) == 0)
                    break;
                if (parent == 0 || parent == rootReturn)
                    return current;
                current = parent;
            }
            return window;
        }

        public RectInt GetWindowRect(nint windowHandle)
        {
            if (!CanUseWindow(windowHandle)) return new(0, 0, 0, 0);

            nint toplevel = GetToplevelParent(windowHandle);
            if (LibX11.XGetWindowAttributes(display, toplevel, out var attrs) == 0)
                return new(0, 0, 0, 0);

            nint child;
            int rootX = attrs.x;
            int rootY = attrs.y;
            nint root = attrs.root != 0 ? attrs.root : LibX11.XDefaultRootWindow(display);
            LibX11.XTranslateCoordinates(display, toplevel, root, 0, 0, out rootX, out rootY, out child);
            return new RectInt(rootX, rootY, attrs.width, attrs.height);
        }

        public bool SetWindowRect(nint windowHandle, RectInt rect)
        {
            if (!CanUseWindow(windowHandle)) return false;
            nint toplevel = GetToplevelParent(windowHandle);

            var changes = new XWindowChanges
            {
                x = rect.x,
                y = rect.y,
                width = System.Math.Max(rect.width, 1),
                height = System.Math.Max(rect.height, 1)
            };
            // CWX(1) | CWY(2) | CWWidth(4) | CWHeight(8) = 15
            LibX11.XConfigureWindow(display, toplevel, 15, ref changes);
            LibX11.XFlush(display);
            return true;
        }

        public bool MoveWindow(nint windowHandle, Vector2Int position)
        {
            if (!CanUseWindow(windowHandle)) return false;
            nint toplevel = GetToplevelParent(windowHandle);
            int result = LibX11.XMoveWindow(display, toplevel, position.x, position.y);
            LibX11.XFlush(display);
            return result == 0; // Xlib returns 0 on success
        }

        public bool ResizeWindow(nint windowHandle, Vector2Int size)
        {
            if (!CanUseWindow(windowHandle)) return false;
            nint toplevel = GetToplevelParent(windowHandle);
            
            uint w = (uint)System.Math.Max(size.x, 1);
            uint h = (uint)System.Math.Max(size.y, 1);
            
            LibX11.XResizeWindow(display, windowHandle, w, h);
            if (toplevel != windowHandle)
            {
                LibX11.XResizeWindow(display, toplevel, w, h);
            }
            
            LibX11.XFlush(display);
            return true;
        }

        public Vector2Int GetWindowMinSize(nint windowHandle)
        {
            if (!CanUseWindow(windowHandle)) return new Vector2Int(0, 0);
            if (TryGetSizeHints(windowHandle, out var hints))
            {
                if ((hints.flags & (long)XSizeHintsFlags.PMinSize) != 0)
                    return new Vector2Int(hints.min_width, hints.min_height);
            }
            return new Vector2Int(0, 0);
        }

        public bool SetWindowMinSize(nint windowHandle, Vector2Int minSize)
        {
            if (!CanUseWindow(windowHandle)) return false;
            TryGetSizeHints(windowHandle, out var hints);
            hints.flags |= (long)XSizeHintsFlags.PMinSize;
            hints.min_width = minSize.x;
            hints.min_height = minSize.y;
            return SetSizeHints(windowHandle, hints);
        }

        public Vector2Int GetWindowMaxSize(nint windowHandle)
        {
            if (!CanUseWindow(windowHandle)) return new Vector2Int(0, 0);
            if (TryGetSizeHints(windowHandle, out var hints))
            {
                if ((hints.flags & (long)XSizeHintsFlags.PMaxSize) != 0)
                    return new Vector2Int(hints.max_width, hints.max_height);
            }
            return new Vector2Int(0, 0);
        }

        public bool SetWindowMaxSize(nint windowHandle, Vector2Int maxSize)
        {
            if (!CanUseWindow(windowHandle)) return false;
            TryGetSizeHints(windowHandle, out var hints);
            hints.flags |= (long)XSizeHintsFlags.PMaxSize;
            hints.max_width = maxSize.x;
            hints.max_height = maxSize.y;
            return SetSizeHints(windowHandle, hints);
        }

        public bool SetWindowCursor(nint windowHandle, CursorStyle cursor, bool bestEffort)
        {
            if (!CanUseWindow(windowHandle)) return false;
            if (cursor == CursorStyle.None)
            {
                LibX11.XUndefineCursor(display, windowHandle);
                LibX11.XFlush(display);
                return true;
            }

            if (cursorCache.ContainsKey(cursor))
            {
                if (LibX11.XDefineCursor(display, windowHandle, cursorCache[cursor]) != 0)
                {
                    LibX11.XFlush(display);
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
                    bool result = LibX11.XDefineCursor(display, windowHandle, cursorCache[cursor]) == 0;
                    LibX11.XFlush(display);
                    return result;
                }
            }
            UnityEngine.Debug.Log($"Can not find a cursor for the CursorStyle {cursor}");

            return false;
        }

        public bool SetWindowHitTestZone(nint windowHandle, int zone)
        {
            return false;
        }

        public Vector2Int GetPointerPosition()
        {
            if (display == 0) display = LibX11.XOpenDisplay(0);
            if (display == 0) return new Vector2Int(0, 0);

            nint root = LibX11.XDefaultRootWindow(display);
            if (LibX11.XQueryPointer(display, root, out _, out _, out int rootX, out int rootY, out _, out _, out _))
                return new Vector2Int(rootX, rootY);
            return new Vector2Int(0, 0);
        }

        public int GetPointerButtonMask()
        {
            if (display == 0) display = LibX11.XOpenDisplay(0);
            if (display == 0) return 0;

            nint root = LibX11.XDefaultRootWindow(display);
            LibX11.XQueryPointer(display, root, out _, out _, out _, out _, out _, out _, out nint mask);
            return (int)mask;
        }

        public bool StartWindowDrag(nint windowHandle, Vector2Int pointerPosition)
        {
            return SendMoveResize(windowHandle, pointerPosition, 8);
        }

        public bool StartWindowResize(nint windowHandle, Vector2Int pointerPosition, WindowResizeEdge edge)
        {
            // EWMH _NET_WM_MOVERESIZE direction constants. Delegates the resize to the
            // window manager / compositor, which works on native X11 and XWayland alike.
            int direction = edge switch
            {
                WindowResizeEdge.TopLeft => 0,
                WindowResizeEdge.Top => 1,
                WindowResizeEdge.TopRight => 2,
                WindowResizeEdge.Right => 3,
                WindowResizeEdge.BottomRight => 4,
                WindowResizeEdge.Bottom => 5,
                WindowResizeEdge.BottomLeft => 6,
                WindowResizeEdge.Left => 7,
                _ => -1,
            };
            if (direction < 0) return false;
            return SendMoveResize(windowHandle, pointerPosition, direction);
        }

        bool SendMoveResize(nint windowHandle, Vector2Int pointerPosition, int direction)
        {
            if (!CanUseWindow(windowHandle)) return false;

            nint moveResize = Atom("_NET_WM_MOVERESIZE");
            if (moveResize == 0) return false;

            nint toplevel = GetToplevelParent(windowHandle);

            var ev = new XEvent
            {
                type = 33,
                clientMessage = new XClientMessageEvent
                {
                    type = 33,
                    send_event = 1,
                    display = display,
                    window = toplevel,
                    message_type = moveResize,
                    format = 32,
                    data0 = pointerPosition.x,
                    data1 = pointerPosition.y,
                    data2 = direction,
                    data3 = 1, // button 1 (left mouse)
                    data4 = 1, // source = normal application
                }
            };

            LibX11.XUngrabPointer(display, 0);
            int result = LibX11.XSendEvent(display, LibX11.XDefaultRootWindow(display), false,
                (nint)(0x00080000 | 0x00100000), ref ev);
            LibX11.XFlush(display);
            LibX11.XSync(display, false);

            return result != 0;
        }

        bool SendNetWmState(nint windowHandle, nint action, string firstState, string secondState)
        {
            nint netWmState = Atom("_NET_WM_STATE");
            nint state1 = Atom(firstState);
            nint state2 = Atom(secondState);
            if (netWmState == 0 || state1 == 0) return false;

            nint atomType = Atom("ATOM", true);
            if (atomType != 0)
            {
                if (action == 0) // REMOVE
                {
                    // Read current _NET_WM_STATE, filter out the target atoms, write back
                    // (instead of XDeleteProperty, which would also drop FOCUSED / others).
                    int currResult = LibX11.XGetWindowProperty(display, windowHandle, netWmState, 0, 1024, false,
                        atomType, out _, out int currFormat, out nint currCount, out _, out nint currProp);
                    if (currResult == 0 && currProp != 0 && currFormat == 32 && currCount > 0)
                    {
                        try
                        {
                            var remaining = new List<nint>();
                            for (int i = 0; i < (int)currCount; i++)
                            {
                                nint atom = Marshal.ReadIntPtr(currProp, i * IntPtr.Size);
                                if (atom != state1 && atom != state2 && atom != 0)
                                    remaining.Add(atom);
                            }
                            if (remaining.Count > 0)
                            {
                                nint[] arr = remaining.ToArray();
                                LibX11.XChangeProperty(display, windowHandle, netWmState, atomType, 32, 1, arr, remaining.Count);
                            }
                            else
                            {
                                LibX11.XDeleteProperty(display, windowHandle, netWmState);
                            }
                        }
                        finally
                        {
                            LibX11.XFree(currProp);
                        }
                    }
                    LibX11.XFlush(display);
                }
                else if (action == 1) // ADD
                {
                    // No property write for ADD. Let the WM be the sole writer of
                    // _NET_WM_STATE after it processes the ClientMessage. Avoids races
                    // and malformed values like [FULLSCREEN, None] or property doubling.
                }
            }

            var ev = new XEvent
            {
                type = 33,
                clientMessage = new XClientMessageEvent
                {
                    type = 33,
                    send_event = 1,
                    display = display,
                    window = windowHandle,
                    message_type = netWmState,
                    format = 32,
                    data0 = action,
                    data1 = state1,
                    data2 = state2,
                    data3 = 1,
                }
            };

            // Send to root window with SubstructureNotify | SubstructureRedirect
            int result = LibX11.XSendEvent(display, LibX11.XDefaultRootWindow(display), false,
                (nint)(0x00080000 | 0x00100000), ref ev);
            LibX11.XFlush(display);
            LibX11.XSync(display, false);
            return result != 0;
        }

        bool HasWindowState(nint windowHandle, string stateName)
        {
            if (!CanUseWindow(windowHandle)) return false;
            nint netWmState = Atom("_NET_WM_STATE", true);
            nint targetState = Atom(stateName, true);
            nint atomType = Atom("ATOM", true);
            if (netWmState == 0 || targetState == 0) return false;

            int result = LibX11.XGetWindowProperty(display, windowHandle, netWmState, 0, 1024, false, atomType, out _, out int format, out nint itemCount, out _, out nint prop);
            if (result != 0 || prop == 0) return false;

            try
            {
                if (format != 32) return false;
                int count = (int)itemCount;
                for (int i = 0; i < count; i++)
                {
                    nint atom = Marshal.ReadIntPtr(prop, i * IntPtr.Size);
                    if (atom == targetState) return true;
                }
            }
            finally
            {
                LibX11.XFree(prop);
            }
            return false;
        }

        bool TryGetSizeHints(nint windowHandle, out XSizeHints hints)
        {
            hints = default;
            var hintsPtr = LibX11.XAllocSizeHints();
            if (hintsPtr == 0) return false;

            try
            {
                long supplied = 0;
                if (LibX11.XGetWMNormalHints(display, windowHandle, hintsPtr, ref supplied) == 0)
                    return false;
                hints = Marshal.PtrToStructure<XSizeHints>(hintsPtr);
                hints.flags = supplied;
                return true;
            }
            finally
            {
                LibX11.XFree(hintsPtr);
            }
        }

        bool SetSizeHints(nint windowHandle, XSizeHints hints)
        {
            var hintsPtr = LibX11.XAllocSizeHints();
            if (hintsPtr == 0) return false;

            try
            {
                Marshal.StructureToPtr(hints, hintsPtr, false);
                LibX11.XSetWMNormalHints(display, windowHandle, hintsPtr);
                LibX11.XFlush(display);
                return true;
            }
            finally
            {
                LibX11.XFree(hintsPtr);
            }
        }

        bool IsRectMaximized(nint windowHandle)
        {
            RectInt rect = GetWindowRect(windowHandle);
            RectInt workarea = GetWorkareaFor(windowHandle);
            return System.Math.Abs(rect.x - workarea.x) <= 1
                && System.Math.Abs(rect.y - workarea.y) <= 1
                && System.Math.Abs(rect.width - workarea.width) <= 2
                && System.Math.Abs(rect.height - workarea.height) <= 2;
        }

        static int ReadXPropertyInt32(nint prop, int index)
        {
            int byteOffset = index * IntPtr.Size;
            return IntPtr.Size == 8
                ? (int)Marshal.ReadInt64(prop, byteOffset)
                : Marshal.ReadInt32(prop, byteOffset);
        }

        public void PumpEvents()
        {
            if (!EnsureDisplay()) return;

            while (LibX11.XPending(display) > 0)
            {
                var ev = new XEvent();
                LibX11.XNextEvent(display, ref ev);
            }
        }
    }
}
