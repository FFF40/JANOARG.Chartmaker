using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using UnityEditor;
using UnityEngine;

namespace JANOARG.Chartmaker.Editor
{

    public class QuickBuild
    {

#if UNITY_EDITOR_LINUX
        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);
#endif

        [MenuItem("JANOARG/Quick Build", priority = 1000)]
        public static void Build()
        {
            string path = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, "Builds");

            if (Directory.Exists(path)) Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            // Build for Windows
            BuildPipeline.BuildPlayer(new BuildPlayerOptions()
            {
                locationPathName = Path.Combine(path, "Chartmaker-win-x86_64/Chartmaker.exe"),
                target = BuildTarget.StandaloneWindows64
            });

            // Build for Linux
            BuildPipeline.BuildPlayer(new BuildPlayerOptions()
            {
                locationPathName = Path.Combine(path, "Chartmaker-linux-x86_64/Chartmaker.x86_64"),
                target = BuildTarget.StandaloneLinux64
            });

            // Zip files
#if UNITY_EDITOR_LINUX
            string scriptPath = Path.Combine(path, "zip.sh");
            File.AppendAllLines(scriptPath, new string[] {
                "#!/bin/bash",
                "cd " + path.Replace(" ", "\\ "),
                "tar -czvf Chartmaker-linux-x86_64.tar.gz Chartmaker-linux-x86_64/",
                "zip -r Chartmaker-win-x86_64.zip Chartmaker-win-x86_64/*",
            });

            chmod(scriptPath, Convert.ToInt32("755", 8));

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c {scriptPath.Replace(" ", "\\\\\\ ")}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
            UnityEngine.Debug.Log(process.StandardError.ReadToEnd());
            process.WaitForExit();
            UnityEngine.Debug.Log("zip exit code = " + process.ExitCode);
            if (process.ExitCode == 0)
            {
                File.Delete(scriptPath);
            }
            else
            {
                UnityEngine.Debug.LogWarning("zip.sh execution failed - please run the script file manually");
            }
#endif

            Application.OpenURL("file://" + path);
        }
    }
}