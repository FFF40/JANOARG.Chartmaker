
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.LinuxX11
{
    internal static class LibX11
    {
        const string LIBX11_PATH = "libX11.so.6";

        [DllImport(LIBX11_PATH)]
        public static extern nint XOpenDisplay(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern nint XDefaultRootWindow(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern int XQueryTree(nint display, nint window, out nint root_return, out nint parent_return, out nint children_return, out nint nchildren_return);

        [DllImport(LIBX11_PATH)]
        public static extern int XConfigureWindow(nint display, nint window, nuint value_mask, ref XWindowChanges changes);

        [DllImport(LIBX11_PATH)]
        public static extern int XDefaultScreen(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern int XDisplayWidth(nint display, int screenNumber);

        [DllImport(LIBX11_PATH)]
        public static extern int XDisplayHeight(nint display, int screenNumber);

        [DllImport(LIBX11_PATH)]
        public static extern int XMapWindow(nint display, nint window);

        [DllImport(LIBX11_PATH)]
        public static extern int XUnmapWindow(nint display, nint window);

        [DllImport(LIBX11_PATH)]
        public static extern int XMoveWindow(nint display, nint window, int x, int y);

        [DllImport(LIBX11_PATH)]
        public static extern int XResizeWindow(nint display, nint window, uint width, uint height);

        [DllImport(LIBX11_PATH)]
        public static extern int XFetchName(nint display, nint window, ref nint window_name);

        [DllImport(LIBX11_PATH)]
        public static extern int XStoreName(nint display, nint window, string window_name);

        [DllImport(LIBX11_PATH)]
        public static extern int XIconifyWindow(nint display, nint window, int screenNumber);

        [DllImport(LIBX11_PATH)]
        public static extern int XCloseDisplay(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern nint XAllocSizeHints();

        [DllImport(LIBX11_PATH)]
        public static extern int XGetWMNormalHints(nint display, nint window, nint hints_return, ref long supplied_return);

        [DllImport(LIBX11_PATH)]
        public static extern void XSetWMNormalHints(nint display, nint window, nint hints);

        [DllImport(LIBX11_PATH)]
        public static extern int XDefineCursor(nint display, nint window, nint cursor);

        [DllImport(LIBX11_PATH)]
        public static extern int XUndefineCursor(nint display, nint window);

        [DllImport(LIBX11_PATH)]
        public static extern int XFree(nint data);

        [DllImport(LIBX11_PATH)]
        public static extern int XFlush(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern nint XInternAtom(nint display, string atomName, bool onlyIfExists);

        [DllImport(LIBX11_PATH)]
        public static extern void XChangeProperty(nint display, nint window, nint property, nint type, int format, int mode, ref MotifWmHints data, int nelements);

        [DllImport(LIBX11_PATH)]
        public static extern void XChangeProperty(nint display, nint window, nint property, nint type, int format, int mode, nint[] data, int nelements);

        [DllImport(LIBX11_PATH)]
        public static extern int XDeleteProperty(nint display, nint window, nint property);

        [DllImport(LIBX11_PATH)]
        public static extern int XSendEvent(nint display, nint window, bool propagate, nint eventMask, ref XEvent eventSend);

        [DllImport(LIBX11_PATH)]
        public static extern int XGetWindowAttributes(nint display, nint window, out XWindowAttributes windowAttributes);

        [DllImport(LIBX11_PATH)]
        public static extern int XTranslateCoordinates(nint display, nint srcWindow, nint destWindow, int srcX, int srcY, out int destX, out int destY, out nint child);

        [DllImport(LIBX11_PATH)]
        public static extern int XGetWindowProperty(nint display, nint window, nint property, nint longOffset, nint longLength, bool delete, nint reqType, out nint actualTypeReturn, out int actualFormatReturn, out nint nitemsReturn, out nint bytesAfterReturn, out nint propReturn);

        [DllImport(LIBX11_PATH)]
        public static extern bool XQueryPointer(nint display, nint window, out nint root_return, out nint child_return, out int root_x_return, out int root_y_return, out int win_x_return, out int win_y_return, out nint mask_return);

        [DllImport(LIBX11_PATH)]
        public static extern int XSync(nint display, bool discard);

        [DllImport(LIBX11_PATH)]
        public static extern int XUngrabPointer(nint display, nint time);

        [DllImport(LIBX11_PATH)]
        public static extern int XSelectInput(nint display, nint window, nint eventMask);

        [DllImport(LIBX11_PATH)]
        public static extern int XPending(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern int XNextEvent(nint display, ref XEvent eventReturn);
    }

    internal static class LibXCursor
    {
        const string LIBXCURSOR_PATH = "libXcursor.so.1";

        [DllImport(LIBXCURSOR_PATH)]
        public static extern nint XcursorLibraryLoadCursor(nint display, string name);
    }

    internal enum XSizeHintsFlags : long
    {
        USPosition = 1L << 0,
        USSize = 1L << 1,
        PPosition = 1L << 2,
        PSize = 1L << 3,
        PMinSize = 1L << 4,
        PMaxSize = 1L << 5,
        PResizeInc = 1L << 6,
        PAspect = 1L << 7,
        PBaseSize = 1L << 8,
        PWinGravity = 1L << 9,
    }

    internal struct XSizeHints
    {
        public long flags;
        public int x, y;
        public int width, height;
        public int min_width, min_height;
        public int max_width, max_height;
        public int width_inc, height_inc;
        public int min_aspect_x, min_aspect_y;
        public int max_aspect_x, max_aspect_y;
        public int base_width, base_height;
        public int win_gravity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MotifWmHints
    {
        public nuint flags;
        public nuint functions;
        public nuint decorations;
        public nint inputMode;
        public nuint status;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XWindowAttributes
    {
        public int x, y;
        public int width, height;
        public int border_width;
        public int depth;
        public nint visual;
        public nint root;
        public int c_class;
        public int bit_gravity;
        public int win_gravity;
        public int backing_store;
        public nint backing_planes;
        public nint backing_pixel;
        public bool save_under;
        public nint colormap;
        public bool map_installed;
        public int map_state;
        public nint all_event_masks;
        public nint your_event_mask;
        public nint do_not_propagate_mask;
        public bool override_redirect;
        public nint screen;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XWindowChanges
    {
        public int x, y;
        public int width, height;
        public int border_width;
        public nint sibling;
        public int stack_mode;
    }

    internal static class XConstants
    {
        public const nuint CWX = 1 << 0;
        public const nuint CWY = 1 << 1;
        public const nuint CWWidth = 1 << 2;
        public const nuint CWHeight = 1 << 3;
        public const nuint CWBorderWidth = 1 << 4;
        public const nuint CWOverrideRedirect = 1 << 9;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XClientMessageEvent
    {
        public int type;
        public nuint serial;
        public int send_event;
        public nint display;
        public nint window;
        public nint message_type;
        public int format;
        public nint data0;
        public nint data1;
        public nint data2;
        public nint data3;
        public nint data4;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XConfigureEvent
    {
        public int type;
        public nint serial;
        public bool send_event;
        public nint display;
        public nint eventWindow;
        public nint window;
        public int x;
        public int y;
        public int width;
        public int height;
        public int border_width;
        public nint above;
        public bool override_redirect;
    }

    [StructLayout(LayoutKind.Explicit, Size = 192)]
    internal struct XEvent
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(0)] public XClientMessageEvent clientMessage;
        [FieldOffset(0)] public XConfigureEvent configureEvent;
    }

    internal static class X11Convert
    {
        public static readonly Dictionary<CursorStyle, string[]> CursorNames = new () {
            { CursorStyle.Arrow, new [] { 
                "default", 
                "arrow",
                "top_left_arrow",
                "left_arrow",
                "left_ptr",
                "08ffe1e65f80fcfdf9fff11263e74c48",
            } },
            { CursorStyle.Crosshair, new [] { 
                "crosshair", 
                "tcross",
            } },
            { CursorStyle.Text, new [] { 
                "text", 
                "ibeam",
                "xterm",
            } },
            { CursorStyle.TextVertical, new [] { 
                "vertical-text", 
                "048008013003cff3c00c801001200000",
            } },


            { CursorStyle.Busy, new [] { 
                "wait",
                "watch",
                "clock",
                "0426c94ea35c87780ff01dc239897213",
            } },
            { CursorStyle.BackgroundBusy, new [] { 
                "half-busy",
                "progress",
                "left_ptr_watch",
                "00000000000000020006000e7e9ffc3f",
                "08e8e1c95fe2fc01f976f1e063a24ccd", 
                "3ecb610c1bf2410f44200f48c40d3599",
                "9116a3ea924ed2162ecab71ba103b17f",
            } },
            { CursorStyle.Blocked, new [] { 
                "not-allowed",
                "crossed_circle",
                "forbidden",
                "03b6e0fcb3499374a867c041f52298f0",
            } },


            { CursorStyle.HandPointing, new [] { 
                "pointing_hand",
                "hand",
                "hand1",
                "hand2",
                "pointer",
                "e29285e634086352946a0e7090d73106",
            } },
            { CursorStyle.HandGrabReady, new [] { 
                "openhand",
                "grab",
                "5aca4d189052212118709018842178c0",
                "9d800788f1b08800ae810202380a0822",
            } },
            { CursorStyle.HandGrabbing, new [] { 
                "closedhand",
                "grabbing",
                "move",
                "dnd-none",
                "208530c400c041818281048008011002",
            } },
            { CursorStyle.HandGrabbingBlocked, new [] { 
                "dnd-no-drop",
                "no-drop",
                "03b6e0fcb3499374a867c041f52298f0",
            } },


            { CursorStyle.ResizeTop, new [] {
                "top_side",
                "n-resize",
                "sb_up_arrow",
                "up-arrow",
            } },
            { CursorStyle.ResizeRight, new [] {
                "right_side",
                "e-resize",
                "sb_right_arrow",
                "right-arrow",
            } },
            { CursorStyle.ResizeBottom, new [] {
                "bottom_side",
                "s-resize",
                "sb_down_arrow",
                "down-arrow",
            } },
            { CursorStyle.ResizeLeft, new [] {
                "left_side",
                "w-resize",
                "sb_left_arrow",
                "left-arrow",
            } },
            { CursorStyle.ResizeTopLeft, new [] {
                "top_left_corner",
                "nw-resize",
            } },
            { CursorStyle.ResizeTopRight, new [] {
                "top_right_corner",
                "ne-resize",
            } },
            { CursorStyle.ResizeBottomRight, new [] {
                "bottom_right_corner",
                "se-resize",
            } },
            { CursorStyle.ResizeBottomLeft, new [] {
                "bottom_left_corner",
                "sw-resize",
            } },
            { CursorStyle.ResizeVertical, new [] {
                "sb_v_double_arrow",
                "size_ver",
                "size-ver",
                "v_double_arrow",
                "double_arrow",
                "00008160000006810000408080010102",
                "split_v",
                "based_arrow_up",
                "based_arrow_down",
            } },
            { CursorStyle.ResizeHorizontal, new [] {
                "sb_h_double_arrow",
                "size_hor",
                "size-hor",
                "h_double_arrow",
                "028006030e0e7ebffc7f7070c0600140",
                "split_h",
            } },
            { CursorStyle.ResizeDiagonalTopLeftBottomRight, new [] {
                "fd_double_arrow",
                "size_bdiag",
                "size-bdiag",
                "50585d75b494802d0151028115016902",
                "fcf1c3c7cd4491d801f1e1c78f100000",
            } },
            { CursorStyle.ResizeDiagonalBottomLeftTopRight, new [] {
                "bd_double_arrow",
                "size_fdiag",
                "size-fdiag",
                "38c5dff7c7b8962045400281044508d2",
                "c7088f0f3e6c8088236ef8e1e3e70000",
            } },
        };
    }
}
