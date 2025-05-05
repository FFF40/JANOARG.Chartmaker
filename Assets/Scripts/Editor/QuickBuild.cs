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

        // Build for Windows
        BuildPipeline.BuildPlayer(new BuildPlayerOptions() {
            locationPathName = Path.Combine(path, "Chartmaker-win-x86_64/Chartmaker.exe"),
            target = BuildTarget.StandaloneWindows64
        });

        // Build for Linux
        BuildPipeline.BuildPlayer(new BuildPlayerOptions() {
            locationPathName = Path.Combine(path, "Chartmaker-linux-x86_64/Chartmaker.x86_64"),
            target = BuildTarget.StandaloneLinux64,
        });

        // Create Linux install script
        string linuxInstallScriptPath = Path.Combine(path, "Chartmaker-linux-x86_64/install.sh");
        string installDir = $"/opt/{Application.productName}"; // Installation path
        File.AppendAllLines(linuxInstallScriptPath, new string[]
        {
            "#!/bin/bash",
            "PACKAGE_VERSION=" + Application.version,
            "PACKAGE_RELEASE=1",
            "PACKAGE_NAME=" + Application.productName,
            $"INSTALL_PATH=\"{installDir}\" # Needs to be absolute from root"
        });

        string templatePath = Path.Combine(Application.dataPath, "Scripts/Editor/LinuxInstallScriptGenerator_TEMPLATE.sh");
        string script = File.ReadAllText(templatePath);
        File.AppendAllText(linuxInstallScriptPath, script);
        
        // Zip files
        #if UNITY_EDITOR_LINUX
        
        
            string scriptPath = Path.Combine(path, "zip.sh");
            File.AppendAllLines(scriptPath, new string[] {
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
                    Arguments = $"-c \"{scriptPath.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            Debug.Log(process.StandardOutput.ReadToEnd());
            Debug.Log(process.StandardError.ReadToEnd());
            process.WaitForExit();
            Debug.Log("zip exit code = " + process.ExitCode);
            if (process.ExitCode == 0) File.Delete(scriptPath);
            else Debug.LogWarning("zip.sh execution failed - please run the script file manually");
        #endif

        Application.OpenURL("file://" + path);
    }
}