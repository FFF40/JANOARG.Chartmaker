
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;

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
        public static extern int XMapWindow(nint display, nint window);

        [DllImport(LIBX11_PATH)]
        public static extern int XMoveWindow(nint display, nint window, int x, int y);

        [DllImport(LIBX11_PATH)]
        public static extern int XResizeWindow(nint display, nint window, uint width, uint height);

        [DllImport(LIBX11_PATH)]
        public static extern int XFetchName(nint display, nint window, ref nint window_name);

        [DllImport(LIBX11_PATH)]
        public static extern int XStoreName(nint display, nint window, string window_name);

        [DllImport(LIBX11_PATH)]
        public static extern int XDestroyWindow(nint display, nint window);

        [DllImport(LIBX11_PATH)]
        public static extern int XCloseDisplay(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern nint XAllocSizeHints(nint display);

        [DllImport(LIBX11_PATH)]
        public static extern int XGetWMSizeHints(nint display, nint window, nint hints_return, ref long supplied_return);

        [DllImport(LIBX11_PATH)]
        public static extern int XSetWMSizeHints(nint display, nint window, nint hints);

        [DllImport(LIBX11_PATH)]
        public static extern int XDefineCursor(nint display, nint window, nint cursor);
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
            { CursorStyle.ResizeDiagonalTopLeft, new [] {
                "fd_double_arrow",
                "size_bdiag",
                "size-bdiag",
                "50585d75b494802d0151028115016902",
                "fcf1c3c7cd4491d801f1e1c78f100000",
            } },
            { CursorStyle.ResizeDiagonalTopRight, new [] {
                "bd_double_arrow",
                "size_fdiag",
                "size-fdiag",
                "38c5dff7c7b8962045400281044508d2",
                "c7088f0f3e6c8088236ef8e1e3e70000",
            } },
        };
    }
}