using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.Windows
{
    internal static class User32
    {
        const string USER32_PATH = "user32.dll";

        [DllImport(USER32_PATH)]
        public static extern bool EnumThreadWindows(uint dwThreadId, EnumWinProc lpEnumFunc, nint lParam);
        public delegate bool EnumWinProc(nint hWnd, nint lParam);
        [DllImport(USER32_PATH, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(nint hWnd, StringBuilder lpString, int nMaxCount);
    
        [DllImport(USER32_PATH)]
        public static extern nint GetActiveWindow();
        [DllImport(USER32_PATH)]
        public static extern nint FindWindowA(string lpClassName, string lpWindowName);

        [DllImport(USER32_PATH, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport(USER32_PATH, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowText(nint hWnd, string lpString);
        
        [DllImport(USER32_PATH, EntryPoint = "SetWindowLong", CharSet = CharSet.Auto)]
        public static extern nint SetWindowLong32(nint hWnd, int nIndex, nint dwNewLong);
        [DllImport(USER32_PATH, EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        public static extern nint SetWindowLong64(nint hWnd, int nIndex, nint dwNewLong);
        [DllImport(USER32_PATH, EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        public static extern nint GetWindowLong32(nint hWnd, int nIndex);
        [DllImport(USER32_PATH, EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
        public static extern nint GetWindowLong64(nint hWnd, int nIndex);

        public static nint SetWindowLong(nint hWnd, WinWindowLong nIndex, nint dwNewLong)
        {
            if (IntPtr.Size == 8) return SetWindowLong64(hWnd, (int)nIndex, dwNewLong);
            else return SetWindowLong32(hWnd, (int)nIndex, dwNewLong);
        }

        public static nint GetWindowLong(nint hWnd, WinWindowLong nIndex)
        {
            if (IntPtr.Size == 8) return GetWindowLong64(hWnd, (int)nIndex);
            else return GetWindowLong32(hWnd, (int)nIndex);
        }

        [DllImport(USER32_PATH)]
        public static extern bool GetWindowRect(nint hWnd, out WinRect lpRect);
        [DllImport(USER32_PATH)]
        public static extern bool MoveWindow(nint hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport(USER32_PATH)]
        public static extern bool ShowWindow(nint hWnd, WinWindowState nCmdShow);
        [DllImport(USER32_PATH)]
        public static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, WinSetWindowPosFlags flags);

        [DllImport(USER32_PATH)]
        public static extern bool IsIconic(nint hWnd);
        [DllImport(USER32_PATH)]
        public static extern bool IsZoomed(nint hWnd);

        [DllImport(USER32_PATH)]   
        public static extern nint SetCursor(nint hCursor);
        [DllImport(USER32_PATH)]
        public static extern nint LoadCursor(nint hInstance, nint lpCursorName);

        public delegate nint WinProc(nint hWnd, WinWindowMessage msg, nint wParam, nint lParam);
        [DllImport(USER32_PATH)]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, WinWindowMessage msg, IntPtr wParam, IntPtr lParam);
        [DllImport(USER32_PATH)]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, WinWindowMessage wMsg, IntPtr wParam, IntPtr lParam);
    }

    internal static class DwmApi
    {
        const string DWMAPI_PATH = "dwmapi.dll";

        [DllImport(DWMAPI_PATH)]
        public static extern int DwmExtendFrameIntoClientArea(nint hwnd, WinMargin margin);
    }

    internal static class Kernel32
    {
        const string KERNEL32_PATH = "kernel32.dll";

        [DllImport(KERNEL32_PATH)]
        public static extern uint GetCurrentThreadId();
    }



    internal static class Win32Convert
    {
        public static CursorStyle FromPlatformCursor(nint style)
        {
            return style switch
            {
                WinCursorStyle.None => CursorStyle.None,

                WinCursorStyle.Arrow => CursorStyle.Arrow,

                WinCursorStyle.Text => CursorStyle.Text,
                WinCursorStyle.Crosshair => CursorStyle.Crosshair,

                WinCursorStyle.Busy => CursorStyle.Busy,
                WinCursorStyle.BackgroundBusy => CursorStyle.BackgroundBusy,

                WinCursorStyle.HandPointing => CursorStyle.HandPointing,

                WinCursorStyle.ResizeHorizontal => CursorStyle.ResizeHorizontal,
                WinCursorStyle.ResizeVertical => CursorStyle.ResizeVertical,
                WinCursorStyle.ResizeDiagonalTopLeft => CursorStyle.ResizeDiagonalTopLeftBottomRight,
                WinCursorStyle.ResizeDiagonalTopRight => CursorStyle.ResizeDiagonalBottomLeftTopRight,

                _ => CursorStyle.Arrow,
            };
        }

        public static nint ToPlatformCursor(CursorStyle style)
        {
            return style switch
            {
                CursorStyle.None => WinCursorStyle.None,

                CursorStyle.Arrow => WinCursorStyle.Arrow,

                CursorStyle.Text => WinCursorStyle.Text,
                CursorStyle.Crosshair => WinCursorStyle.Crosshair,
                CursorStyle.Blocked => WinCursorStyle.Blocked,

                CursorStyle.Busy => WinCursorStyle.Busy,
                CursorStyle.BackgroundBusy => WinCursorStyle.BackgroundBusy,

                CursorStyle.HandPointing => WinCursorStyle.HandPointing,

                CursorStyle.ResizeHorizontal => WinCursorStyle.ResizeHorizontal,
                CursorStyle.ResizeVertical => WinCursorStyle.ResizeVertical,
                CursorStyle.ResizeDiagonalTopLeftBottomRight => WinCursorStyle.ResizeDiagonalTopLeft,
                CursorStyle.ResizeDiagonalBottomLeftTopRight => WinCursorStyle.ResizeDiagonalTopRight,

                _ => WinCursorStyle.Unknown,
            };
        }

        public static nint ToPlatformCursorBestEffort(CursorStyle style)
        {
            return style switch
            {
                CursorStyle.TextVertical => WinCursorStyle.Text,
                CursorStyle.HandGrabReady => WinCursorStyle.Arrow,
                CursorStyle.HandGrabbing => WinCursorStyle.Arrow,
                CursorStyle.HandGrabbingBlocked => WinCursorStyle.Blocked,

                CursorStyle.ResizeLeft => WinCursorStyle.ResizeHorizontal,
                CursorStyle.ResizeRight => WinCursorStyle.ResizeHorizontal,
                CursorStyle.ResizeTop => WinCursorStyle.ResizeVertical,
                CursorStyle.ResizeBottom => WinCursorStyle.ResizeVertical,
                CursorStyle.ResizeTopLeft => WinCursorStyle.ResizeDiagonalTopLeft,
                CursorStyle.ResizeBottomRight => WinCursorStyle.ResizeDiagonalTopLeft,
                CursorStyle.ResizeTopRight => WinCursorStyle.ResizeDiagonalTopRight,
                CursorStyle.ResizeBottomLeft => WinCursorStyle.ResizeDiagonalTopRight,

                _ => ToPlatformCursor(style),
            };
        }

        public static WindowState FromPlatformWindowState(WinWindowState state)
        {
            return state switch
            {
                WinWindowState.Minimized => WindowState.Minimized,
                WinWindowState.Maximized => WindowState.Maximized,
                WinWindowState.Floating => WindowState.Floating,

                _ => WindowState.Floating,
            };
        }

        public static WinWindowState ToPlatformWindowState(WindowState state)
        {
            return state switch
            {
                WindowState.Minimized => WinWindowState.Minimized,
                WindowState.Maximized => WinWindowState.Maximized,
                WindowState.Floating => WinWindowState.Floating,

                _ => WinWindowState.Floating,
            };
        }
    }



    internal struct WinRect   { public int left, top,   right, bottom; }
    internal struct WinMargin { public int left, right, top,   bottom; }
    internal struct WinPoint  { public int x,    y; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinMinMaxInfo
    {
        public WinPoint Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize;
    }



    internal enum WinWindowState : int
    {
        Maximized = 3,
        Minimized = 6,
        Floating = 9,
    }

    internal enum WinWindowLong : int
    { 
        WinProc = -4,
        Style = -16,
        ExStyle = -20,
    }

    [Flags]
    internal enum WinWindowStyle : uint
    { 
        Overlapped = 0x00000000,
        Popup = 0x80000000,
        Child = 0x40000000,
        Minimize = 0x20000000,
        Visible = 0x10000000,
        Disabled = 0x08000000,
        ClipSiblings = 0x04000000,
        ClipChildren = 0x02000000,
        Maximize = 0x01000000,
        Caption = 0x00C00000,
        Border = 0x00800000,
        DlgFrame = 0x00400000,
        SysMenu = 0x00080000,
        ThickFrame = 0x00040000,
        MinimizeBox = 0x00020000,
        MaximizeBox = 0x00010000,
    }

    internal enum WinWindowMessage : int
    {
        Sizing = 0x0214,
        Size = 0x0005,
        Moving = 0x0216,
        Move = 0x0003,
        GetMinMaxInfo = 0x0024,
        NcCalcSize = 0x0083,
        StyleChanged = 0x007D,
        SetCursor = 0x0020,
        MouseMove = 0x0200,
        NcHitTest = 0x0084,
        Activate = 0x0006,
    }

    [Flags]
    internal enum WinSetWindowPosFlags : uint
    {
        NoSize = 0x0001,
        NoMove = 0x0002,
        NoZOrder = 0x0004,
        FrameChanged = 0x0020,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinWindowPos
    {
        public nint hWnd, hWndInsertAfter;
        public int x, y, cx, cy, flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinNcCalcSizeParams
    {
        public WinRect rect0, rect1, rect2;
        public WinWindowPos pos;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinStyleStruct
    {
        public uint oldStyle, newStyle;
    }

    internal static class WinCursorStyle
    {
        public const nint Unknown = -1;
        public const nint None = 0;

        public const nint Arrow = 32512; 

        public const nint Text = 32513;

        public const nint Busy = 32514;
        public const nint BackgroundBusy = 32650;
        public const nint Blocked = 32648;

        public const nint Crosshair = 32515;

        public const nint HandPointing = 32649;

        public const nint ResizeHorizontal = 32644;
        public const nint ResizeVertical = 32645;
        public const nint ResizeDiagonalTopLeft = 32642;
        public const nint ResizeDiagonalTopRight = 32643;
    }
}
