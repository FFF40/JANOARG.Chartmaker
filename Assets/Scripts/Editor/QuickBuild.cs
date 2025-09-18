using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
public class QuickBuild
{
    [MenuItem("JANOARG/Quick Build", priority = 1000)]
    public static void Build ()
    {
        string path = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds");

        if (Directory.Exists(path)) Directory.Delete(path, true);
        Directory.CreateDirectory(path);

        // Build for Android
        BuildPipeline.BuildPlayer(new BuildPlayerOptions() {
            locationPathName = Path.Combine(path, "Chartmaker-android-arm64.apk"),
            target = BuildTarget.Android
        });

        // Build for Windows
        BuildPipeline.BuildPlayer(new BuildPlayerOptions() {
            locationPathName = Path.Combine(path, "Chartmaker-win-x86_64/Chartmaker.exe"),
            target = BuildTarget.StandaloneWindows64
        });

        // Build for Linux
        BuildPipeline.BuildPlayer(new BuildPlayerOptions() {
            locationPathName = Path.Combine(path, "Chartmaker-linux-x86_64/Chartmaker.x86_64"),
            target = BuildTarget.StandaloneLinux64
        });

        // Zip files
        #if UNITY_EDITOR_LINUX
            string scriptPath = Path.Combine(path, "zip.sh");

            EditorUtility.DisplayProgressBar("Compressing build files", "Creating zip.sh...", 0);
            File.WriteAllLines(scriptPath, new string[] {
                "#!/bin/bash",
                "cd " + path.Replace(" ", "\\ "),
                "tar -czvf Chartmaker-linux-x86_64.tar.gz Chartmaker-linux-x86_64/",
                "zip -r Chartmaker-win-x86_64.zip Chartmaker-win-x86_64/*",
            });

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"chmod 755 {scriptPath.Replace("\"", "\\\"").Replace(" ", "\\ ")}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            EditorUtility.DisplayProgressBar("Compressing build files", "Creating zip.sh...", 0.01f);
            process.Start();
            EditorUtility.DisplayProgressBar("Compressing build files", "Creating zip.sh...", 0.02f);
            process.WaitForExit();
            
            EditorUtility.DisplayProgressBar("Compressing build files", "Starting zip.sh...", 0.1f);

            process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{scriptPath.Replace("\"", "\\\"").Replace(" ", "\\ ")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            string message = "Running zip.sh...";
            process.OutputDataReceived += (obj, args) => {
                message = args.Data;
            };
            process.ErrorDataReceived += (obj, args) => {
                message = args.Data;
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            while (!process.HasExited) {
                EditorUtility.DisplayProgressBar("Compressing build files", message, 0.2f);
            }
            Debug.Log("zip exit code = " + process.ExitCode);
            if (process.ExitCode == 0) File.Delete(scriptPath);
            else Debug.LogWarning("zip.sh execution failed - please run the script file manually");

            EditorUtility.ClearProgressBar();
        #endif

        Application.OpenURL("file://" + path);
    }
}