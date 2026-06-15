using JANOARG.Chartmaker.Behaviors.Chartmaker;
using JANOARG.Chartmaker.Data;
using JANOARG.Chartmaker.Utils;
using JANOARG.Chartmaker.Utils.NativeAPI;
using JANOARG.Shared.Data.ChartInfo;
using UnityEngine;
using FFTWindow = JANOARG.Chartmaker.Utils.FFTWindow;

namespace JANOARG.Chartmaker.Constants
{
    public class ChartmakerPrefs 
    {
        public bool MaximizeOnPlay;
        public bool SaveOnQuit;
        public bool SaveOnPlay;
        public bool AutoUpdateCheck = true;
        public bool ShowHiddenFiles;

        public string Theme            = "Prototype";
        public bool   CustomCursors    = true;
        public bool   UseDefaultWindow = false;
        public PreferredCursorMode PreferredCursorMode = PreferredCursorMode.PreferCustom;

        public FileSizeBase   FileSizeBase   = Application.platform == RuntimePlatform.WindowsPlayer ? FileSizeBase.Binary : FileSizeBase.Decimal;
        public FFTWindow      FFTWindow      = FFTWindow.Hann;
        public float          FrequencyMin   = 50;
        public float          FrequencyMax   = 20000;
        public FrequencyScale FrequencyScale = FrequencyScale.Mel;
    
        public bool  PerfectHitsounds;
        public bool  ForceNavigationBar;
        public float InterfaceScaling = 1;

        public void Load(Storage storage)
        {
            MaximizeOnPlay = storage.Get("PL:MaximizeOnPlay", MaximizeOnPlay);
            SaveOnPlay = storage.Get("AS:SaveOnPlay", SaveOnPlay);
            SaveOnQuit = storage.Get("AS:SaveOnQuit", SaveOnQuit);
            AutoUpdateCheck = storage.Get("UP:AutoUpdateCheck", AutoUpdateCheck);
            ShowHiddenFiles = storage.Get("FI:ShowHiddenFiles", ShowHiddenFiles);

            Theme = storage.Get("AP:Theme", Theme);
            CustomCursors = storage.Get("AP:CustomCursors", CustomCursors);
            PreferredCursorMode = storage.Get("AP:PreferredCursorMode", CustomCursors ? PreferredCursorMode.PreferCustom : PreferredCursorMode.PreferNative);
            UseDefaultWindow = storage.Get("LA:UseDefaultWindow", UseDefaultWindow);
            ForceNavigationBar = storage.Get("LA:ForceNavigationBar", true);
            
            InterfaceScaling = storage.Get("LA:UIScalingFactor", InterfaceScaling);
        
            QualitySettings.vSyncCount = storage.Get("GS:VSync", 1);
            QualitySettings.antiAliasing = storage.Get("GS:AntiAliasing", 0);

            FileSizeBase = storage.Get("FM:FileSizeBase", FileSizeBase);
            FFTWindow = storage.Get("AL:FFTWindow", FFTWindow);
            FrequencyMin = storage.Get("AL:FrequencyMin", FrequencyMin);
            FrequencyMax = storage.Get("AL:FrequencyMax", FrequencyMax);
            FrequencyScale = storage.Get("AL:FrequencyScale", FrequencyScale);

            PerfectHitsounds = storage.Get("BO:PerfectHitsounds", PerfectHitsounds);
        }
    }
}
