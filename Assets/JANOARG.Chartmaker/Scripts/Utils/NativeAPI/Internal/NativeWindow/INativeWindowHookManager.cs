using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow
{
    internal interface INativeWindowHookManager
    {
        public bool HookWindow(nint windowHandle);
        public bool UnhookWindow(nint windowHandle);
    }    
}