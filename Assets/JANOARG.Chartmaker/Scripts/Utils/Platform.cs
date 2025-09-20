using UnityEngine;

namespace JANOARG.Chartmaker.Utils
{
    public static class Platform
    {
        public static bool IsWin32APIApplicable() => 
            Application.platform == RuntimePlatform.WindowsPlayer;
    }
}