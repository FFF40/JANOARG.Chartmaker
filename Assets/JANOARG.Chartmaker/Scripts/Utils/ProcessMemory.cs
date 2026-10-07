using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JANOARG.Chartmaker.Utils
{
    /// <summary>
    /// Reads the whole-process memory footprint, i.e. everything the OS attributes to the
    /// running app - the engine runtime, native plugins, graphics driver and managed heap.
    /// This is the ground truth the Unity Profiler's own counters cannot see.
    /// Note: in the Editor this is the entire Unity Editor process, not a player build.
    /// </summary>
    public static class ProcessMemory
    {
        static Process _process;

        static Process Current
        {
            get
            {
                if (_process == null)
                {
                    try { _process = Process.GetCurrentProcess(); }
                    catch { _process = null; }
                }
                return _process;
            }
        }

        /// <summary>
        /// Returns the process memory footprint in bytes. <paramref name="workingSet"/> is the
        /// resident set (what the OS has physically backed), <paramref name="privateBytes"/> is
        /// the committed private memory. Either may be 0 if the platform does not report it.
        /// </summary>
        public static bool TryGet(out long workingSet, out long privateBytes)
        {
            workingSet = privateBytes = 0;

            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (TryGetWindows(out workingSet, out privateBytes))
                return true;
            #endif

            // System.Diagnostics.Process works under Mono but is unreliable on IL2CPP,
            // so it is only a fallback for the native path above.
            Process process = Current;
            if (process == null) return false;

            try
            {
                process.Refresh();
                workingSet = process.WorkingSet64;
                privateBytes = process.PrivateMemorySize64;
                return workingSet > 0 || privateBytes > 0;
            }
            catch
            {
                return false;
            }
        }

        #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();

        [DllImport("psapi.dll", SetLastError = true)]
        static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, uint size);

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_MEMORY_COUNTERS
        {
            public uint    cb;
            public uint    PageFaultCount;
            public UIntPtr PeakWorkingSetSize;
            public UIntPtr WorkingSetSize;
            public UIntPtr QuotaPeakPagedPoolUsage;
            public UIntPtr QuotaPagedPoolUsage;
            public UIntPtr QuotaPeakNonPagedPoolUsage;
            public UIntPtr QuotaNonPagedPoolUsage;
            public UIntPtr PagefileUsage;
            public UIntPtr PeakPagefileUsage;
        }

        static bool TryGetWindows(out long workingSet, out long privateBytes)
        {
            workingSet = privateBytes = 0;

            try
            {
                PROCESS_MEMORY_COUNTERS counters = new();
                counters.cb = (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS));

                if (!GetProcessMemoryInfo(GetCurrentProcess(), out counters, counters.cb))
                    return false;

                workingSet = (long)counters.WorkingSetSize.ToUInt64();
                privateBytes = (long)counters.PagefileUsage.ToUInt64();
                return workingSet > 0 || privateBytes > 0;
            }
            catch
            {
                return false;
            }
        }
        #endif
    }
}
