using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;

namespace JANOARG.Chartmaker.Editor
{
    /// <summary>
    /// Pins standalone Linux builds to OpenGL Core. The app can only choose a graphics
    /// backend at launch, so making OpenGL Core the build default means the default
    /// configuration never has to restart itself -- and sidesteps the RADV Vulkan
    /// present crash on affected Linux systems.
    /// </summary>
    public class BuildGraphicsAPISettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneLinux64) return;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.OpenGLCore });
        }
    }
}
