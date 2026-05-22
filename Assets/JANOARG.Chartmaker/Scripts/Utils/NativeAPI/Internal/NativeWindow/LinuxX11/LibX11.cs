
using System;
using System.Runtime.InteropServices;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.LinuxX11
{
    internal static class LibX11
    {
        [DllImport("libX11.so.6")]
        public static extern nint XOpenDisplay(nint display);

        [DllImport("libX11.so.6")]
        public static extern nint XDefaultRootWindow(nint display);

        [DllImport("libX11.so.6")]
        public static extern int XMapWindow(nint display, nint window);

        [DllImport("libX11.so.6")]
        public static extern int XMoveWindow(nint display, nint window, int x, int y);

        [DllImport("libX11.so.6")]
        public static extern int XResizeWindow(nint display, nint window, uint width, uint height);

        [DllImport("libX11.so.6")]
        public static extern int XFetchName(nint display, nint window, ref nint window_name);

        [DllImport("libX11.so.6")]
        public static extern int XStoreName(nint display, nint window, string window_name);

        [DllImport("libX11.so.6")]
        public static extern int XDestroyWindow(nint display, nint window);

        [DllImport("libX11.so.6")]
        public static extern int XCloseDisplay(nint display);

        [DllImport("libX11.so.6")]
        public static extern nint XAllocSizeHints(nint display);

        [DllImport("libX11.so.6")]
        public static extern int XGetWMSizeHints(nint display, nint window, nint hints_return, ref long supplied_return);

        [DllImport("libX11.so.6")]
        public static extern int XSetWMSizeHints(nint display, nint window, nint hints);
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
}