using UnityEngine;

namespace JANOARG.Chartmaker.Behaviors.Runtime {
    static class RuntimeLogManager
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
        }
    }
}