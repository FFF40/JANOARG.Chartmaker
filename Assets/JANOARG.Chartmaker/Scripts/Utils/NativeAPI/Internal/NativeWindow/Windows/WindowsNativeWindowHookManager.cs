
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.Windows
{
    internal class WindowsNativeWindowHookManager : INativeWindowHookManager
    {
        Dictionary<nint, WindowsNativeWindowHookData> hookData = new();



        public bool HookWindow(nint windowHandle)
        {
            if (windowHandle == 0) return false;
            if (hookData.ContainsKey(windowHandle)) return true;

            var targetHookData = new WindowsNativeWindowHookData();
            hookData[windowHandle] = targetHookData;

            var newProgDelegate = new User32.WinProc(WindowProc);
            var newProc = Marshal.GetFunctionPointerForDelegate(newProgDelegate);
            targetHookData.NewProc = newProgDelegate;
            targetHookData.OldProc = User32.SetWindowLong(windowHandle, WinWindowLong.WinProc, newProc);
            targetHookData.IsActive = User32.GetActiveWindow() == windowHandle;

            return true;
        }

        public bool UnhookWindow(nint windowHandle)
        {
            if (!hookData.ContainsKey(windowHandle)) return false;

            var targetHookData = hookData[windowHandle];
            User32.SetWindowLong(windowHandle, WinWindowLong.WinProc, targetHookData.OldProc);
            hookData.Remove(windowHandle);

            return true;
        }

        public WindowsNativeWindowHookData GetHookData(nint windowHandle)
        {
            if (!hookData.ContainsKey(windowHandle)) return null;
            return hookData[windowHandle];
        }



        nint WindowProc(nint hWnd, WinWindowMessage msg, nint wParam, nint lParam)
        {
            if (!hookData.TryGetValue(hWnd, out var targetHookData))
                return User32.DefWindowProc(hWnd, msg, wParam, lParam);

            switch (msg)
            {
                case WinWindowMessage.Size:
                {
                    targetHookData.State = (int)wParam switch
                    {
                        2 => WindowState.Maximized,
                        1 => WindowState.Minimized,
                        _ => WindowState.Floating,
                    };
                    return User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
                }
                case WinWindowMessage.GetMinMaxInfo:
                {
                    var minMaxInfo = Marshal.PtrToStructure<WinMinMaxInfo>(lParam);

                    if (targetHookData.MinSize != Vector2Int.zero)
                    {
                        minMaxInfo.MinTrackSize.x = targetHookData.MinSize.x;
                        minMaxInfo.MinTrackSize.y = targetHookData.MinSize.y;
                    }

                    if (targetHookData.MaxSize != Vector2Int.zero)
                    {
                        minMaxInfo.MaxTrackSize.x = targetHookData.MaxSize.x;
                        minMaxInfo.MaxTrackSize.y = targetHookData.MaxSize.y;
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, false);
                    return User32.DefWindowProc(hWnd, msg, wParam, lParam);
                }
                case WinWindowMessage.NcCalcSize:
                {
                    if (targetHookData.Style == WindowStyle.Native)
                        return User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);

                    bool isMaximized = targetHookData.State == WindowState.Maximized || User32.IsZoomed(hWnd);
                    if (wParam != 0)
                    {
                        var size = Marshal.PtrToStructure<WinNcCalcSizeParams>(lParam);
                        size.rect0.top += isMaximized ? 7 : 0;
                        size.rect0.bottom -= 7;
                        size.rect0.left += 7;
                        size.rect0.right -= 7;
                        Marshal.StructureToPtr(size, lParam, true);
                    }
                    else
                    {
                        var size = Marshal.PtrToStructure<WinRect>(lParam);
                        size.top += isMaximized ? 7 : 0;
                        size.bottom -= 7;
                        size.left += 7;
                        size.right -= 7;
                        Marshal.StructureToPtr(size, lParam, true);
                    }
                    return 0;
                }
                case WinWindowMessage.StyleChanged:
                {
                    if ((int)wParam == (int)WinWindowLong.Style)
                    {
                        var styleStruct = Marshal.PtrToStructure<WinStyleStruct>(lParam);
                        targetHookData.Style = (styleStruct.newStyle & (uint)WinWindowStyle.Caption) != 0
                            ? targetHookData.Style
                            : WindowStyle.Custom;
                    }
                    return User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
                }
                case WinWindowMessage.SetCursor: case WinWindowMessage.MouseMove:
                {
                    var proc = User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
                    if (targetHookData.CurrentCursor == 0) return proc;
                    
                    User32.SetCursor(User32.LoadCursor(0, targetHookData.CurrentCursor));
                    return -1;
                }
                case WinWindowMessage.NcHitTest:
                {
                    var proc = User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
                    if (targetHookData.Style == WindowStyle.Native || targetHookData.HitTestZone <= 1) return proc;
                    return targetHookData.HitTestZone;
                }
                case WinWindowMessage.Activate:
                {
                    targetHookData.IsActive = wParam != 0;
                    return User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
                }

                default:
                    return User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
            }
        }
    }

    internal class WindowsNativeWindowHookData
    {
        public nint OldProc;
        public User32.WinProc NewProc;
        public Vector2Int MinSize;
        public Vector2Int MaxSize;
        public nint CurrentCursor;
        public int HitTestZone = 1;
        public bool IsActive = true;
        public WindowStyle Style = WindowStyle.Native;
        public WindowState State = WindowState.Floating;
    }
}
