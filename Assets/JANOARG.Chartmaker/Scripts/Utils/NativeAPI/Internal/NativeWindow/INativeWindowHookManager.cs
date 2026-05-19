using UnityEngine;

namespace JANOARG.Chartmaker.Utils.NativeAPI.Internal.NativeWindow
{
    internal interface INativeWindowHookManager
    {
        public void HookWindow(nint windowHandle);
        public void UnhookWindow(nint windowHandle);
    }    
}