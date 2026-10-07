using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JANOARG.Chartmaker.Constants;
using UnityEngine;
using UnityEngine.Rendering;

namespace JANOARG.Chartmaker.Utils
{
    /// <summary>
    /// User-selectable graphics backend. <see cref="Automatic"/> leaves the choice to
    /// Unity, every other value maps to a Unity <c>-force-*</c> command line argument.
    /// </summary>
    public enum GraphicsAPI
    {
        Automatic,
        OpenGLCore,
        Vulkan,
        Direct3D11,
        Metal,
    }

    public static class GraphicsAPIUtils
    {
        // Appended to the relaunch command line so an unavailable/ignored forced API
        // can't put the app into a relaunch loop.
        const string AppliedFlag = "-cm-gfx-api-applied";

        // Any flag that makes Unity pick a graphics backend. Stripped from the relaunch
        // command line so the one we append is the only one in effect.
        static readonly HashSet<string> GraphicsArgs = new()
        {
            "-force-glcore",
            "-force-vulkan",
            "-force-d3d11",
            "-force-d3d12",
            "-force-metal",
            "-force-glcore32", "-force-glcore33",
            "-force-glcore40", "-force-glcore41", "-force-glcore42",
            "-force-glcore43", "-force-glcore44", "-force-glcore45",
        };

        public static string ToCommandLineArgument(GraphicsAPI api) => api switch
        {
            GraphicsAPI.OpenGLCore  => "-force-glcore",
            GraphicsAPI.Vulkan      => "-force-vulkan",
            GraphicsAPI.Direct3D11  => "-force-d3d11",
            GraphicsAPI.Metal       => "-force-metal",
            _                       => null,
        };

        public static GraphicsAPI FromDeviceType(GraphicsDeviceType type) => type switch
        {
            GraphicsDeviceType.OpenGLCore => GraphicsAPI.OpenGLCore,
            GraphicsDeviceType.Vulkan     => GraphicsAPI.Vulkan,
            GraphicsDeviceType.Direct3D11 => GraphicsAPI.Direct3D11,
            GraphicsDeviceType.Metal      => GraphicsAPI.Metal,
            _                             => GraphicsAPI.Automatic,
        };

        /// <summary>
        /// Unity decides the graphics backend before any managed code runs, so a saved
        /// preference can only be honoured by restarting the player with the matching
        /// <c>-force-*</c> argument. Called once, before the splash screen.
        /// </summary>
        public static void ApplyPreferenceOnStartup(ChartmakerPrefs prefs)
        {
            // The editor is started with its own -force-* flag; never fight it.
            if (Application.isEditor || prefs == null) return;

#if UNITY_STANDALONE
            if (prefs.GraphicsAPI == GraphicsAPI.Automatic) return;

            List<string> userArgs = GetUserArguments();
            if (userArgs.Contains(AppliedFlag)) return;

            string arg = ToCommandLineArgument(prefs.GraphicsAPI);
            if (arg == null) return;

            if (FromDeviceType(SystemInfo.graphicsDeviceType) == prefs.GraphicsAPI) return;

            try
            {
                string exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exe)) return;

                var relaunchArgs = userArgs
                    .Where(a => a != AppliedFlag && !GraphicsArgs.Contains(a))
                    .Append(arg)
                    .Append(AppliedFlag);

                UnityEngine.Debug.Log($"Graphics API '{prefs.GraphicsAPI}' requested; restarting with {arg}.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = string.Join(" ", relaunchArgs.Select(Quote)),
                    UseShellExecute = false,
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                });

                // Quit gracefully so the engine tears the window/session down properly
                // (an abrupt Environment.Exit leaves a defunct process behind, which
                // Unity's Build And Run never reaps).
                Application.Quit();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"Could not apply graphics API '{prefs.GraphicsAPI}': {e.Message}");
            }
#endif
        }

        // Unity's argument list may or may not include the executable as [0]; drop it if present.
        static List<string> GetUserArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            int start = args.Length > 0 && LooksLikeExecutablePath(args[0]) ? 1 : 0;
            return args.Skip(start).ToList();
        }

        static bool LooksLikeExecutablePath(string arg)
        {
            if (string.IsNullOrEmpty(arg) || arg.StartsWith("-")) return false;
            return arg.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                || arg.EndsWith(".x86_64", StringComparison.OrdinalIgnoreCase)
                || arg.EndsWith(".app", StringComparison.OrdinalIgnoreCase)
                || arg.IndexOf('/') >= 0
                || arg.IndexOf('\\') >= 0;
        }

        static string Quote(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "\"\"";
            if (arg.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return arg;
            return "\"" + arg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
