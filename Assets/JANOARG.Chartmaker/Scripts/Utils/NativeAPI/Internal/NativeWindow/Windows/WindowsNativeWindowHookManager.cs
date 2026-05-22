
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow.Windows
{
    internal class WindowsNativeWindowHookManager : INativeWindowHookManager
    {
        Dictionary<nint, WindowsNativeWindowHookData> hookData = new();



        public void HookWindow(nint windowHandle)
        {
            if (hookData.ContainsKey(windowHandle)) return;

            var targetHookData = new WindowsNativeWindowHookData();
            hookData[windowHandle] = targetHookData;

            var newProgDelegate = new User32.WinProc(WindowProc);
            var newProc = Marshal.GetFunctionPointerForDelegate(newProgDelegate);
            targetHookData.OldProc = User32.SetWindowLong(windowHandle, WinWindowLong.WinProc, newProc);
        }

        public void UnhookWindow(nint windowHandle)
        {
            if (!hookData.ContainsKey(windowHandle)) return;

            var targetHookData = hookData[windowHandle];
            User32.SetWindowLong(windowHandle, WinWindowLong.WinProc, targetHookData.OldProc);
            hookData.Remove(windowHandle);
        }

        public WindowsNativeWindowHookData GetHookData(nint windowHandle)
        {
            if (!hookData.ContainsKey(windowHandle)) return null;
            return hookData[windowHandle];
        }



        nint WindowProc(nint hWnd, WinWindowMessage msg, nint wParam, nint lParam)
        {
            var targetHookData = hookData[hWnd];

            switch (msg)
            {
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
                case WinWindowMessage.SetCursor: case WinWindowMessage.MouseMove:
                {
                    var proc = User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
                    if (targetHookData.CursorStack.Count <= 0) return proc;
                    
                    // TODO implement
                    // UpdateCursor();
                    return -1;
                }

                default:
                    return User32.CallWindowProc(targetHookData.OldProc, hWnd, msg, wParam, lParam);
            }
        }
    }

    internal class WindowsNativeWindowHookData
    {
        public nint OldProc;
        public Vector2Int MinSize;
        public Vector2Int MaxSize;
        public Stack<CursorStyle> CursorStack = new();
    }
}