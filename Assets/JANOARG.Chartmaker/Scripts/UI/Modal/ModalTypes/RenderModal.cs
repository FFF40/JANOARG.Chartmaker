using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JANOARG.Chartmaker.Behaviors.Chartmaker;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Form;
using JANOARG.Chartmaker.UI.Form.FormTypes;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace JANOARG.Chartmaker.UI.Modal.ModalTypes
{
    public class RenderModal : Modal
    {
        public static RenderModal main;
        public        RenderPrefs Prefs = new();
        public        bool        PrefsDirty;
        [Space]
        public string OutputPath;
        public Vector2 TimeRange;

        [Space]
        public RectTransform FormHolder;
        public VerticalLayoutGroup FormHolderLayout;

        [Space]
        public RectTransform FFmpegFieldHolder;
        [Space]
        public GameObject FFmpegDisclaimer;
        public TMP_Text FFmpegDisclaimerDownloadText;
    
        [NonSerialized] private string FFmpegDownloadLink;
    
        public GameObject BusyDisclaimer;
        public TMP_Text   BusyLabel;

        [Space]
        public bool IsAnimating;

        string FFmpegVersion;

        // I'm not gonna make 3 different enums for this
        enum MediaFormats
        {
            // File Formats
            mp4,
            webm,
            mkv,
            mov,
            flv,
            
            // Video encodings
            h264,
            h265,
            vp8,
            vp9,
            av1,
            
            // Audio encodings
            aac,
            mp3,
            vorbis,
            opus,
            alac,
            pcm
            
        }

        struct RenderFormatItem
        {
            public MediaFormats Tag;
            public string FfmpegArg;
            public string Description;
            public MediaFormats[] Compatibility;
        }
        
        private readonly Dictionary<string, string> _formatDisplayNames = new Dictionary<string, string>
        {
            { "mp4",  "MP4 Video"             },
            { "webm", "WebM Video"            },
            { "mkv",  "Matroska Video (mkv)"  },
            { "mov",  "QuickTime Movie (mov)" },
            { "flv",  "Flash Video (flv)"     }
        };

        private RenderFormatItem[] _VideoEncoding = 
        {
            // H.264
            new() { 
                Tag = MediaFormats.h264,
                FfmpegArg = "h264",
                Description = "H.264/AVC (Legacy)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov, MediaFormats.flv }
            },
            new() { 
                Tag = MediaFormats.h264,
                FfmpegArg = "libx264",
                Description = "H.264/AVC (Software)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov, MediaFormats.flv }
            },
            new() { 
                Tag = MediaFormats.h264,
                FfmpegArg = "h264_amf",
                Description = "H.264/AVC (AMD)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov, MediaFormats.flv }
            },
            new() { 
                Tag = MediaFormats.h264,
                FfmpegArg = "h264_nvenc",
                Description = "H.264/AVC (NVIDIA)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov, MediaFormats.flv }
            },
            new() { 
                Tag = MediaFormats.h264,
                FfmpegArg = "h264_qsv",
                Description = "H.264/AVC (Intel QSV)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov, MediaFormats.flv }
            },
            new() { 
                Tag = MediaFormats.h264,
                FfmpegArg = "h264_vulkan",
                Description = "H.264/AVC (Vulkan)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov }
            },
            new() { 
                Tag = MediaFormats.h264,
                FfmpegArg = "h264_vaapi",
                Description = "H.264/AVC (VA-API)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov }
            },

            // H.265
            new() { 
                Tag = MediaFormats.h265,
                FfmpegArg = "libx265",
                Description = "H.265/HEVC (Software)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov }
            },
            new() { 
                Tag = MediaFormats.h265,
                FfmpegArg = "hevc_amf",
                Description = "H.265/HEVC (AMD)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov }
            },
            new() { 
                Tag = MediaFormats.h265,
                FfmpegArg = "hevc_nvenc",
                Description = "H.265/HEVC (NVIDIA)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov }
            },
            new() { 
                Tag = MediaFormats.h265,
                FfmpegArg = "hevc_qsv",
                Description = "H.265/HEVC (Intel QSV)",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv, MediaFormats.mov }
            },

            // VPX
            new() { 
                Tag = MediaFormats.vp8,
                FfmpegArg = "vp8",
                Description = "VP8 (Legacy)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.vp8,
                FfmpegArg = "libvpx",
                Description = "VP8 (Software)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.vp8,
                FfmpegArg = "vp8_vaapi",
                Description = "VP8 (VA-API)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },

            new() { 
                Tag = MediaFormats.vp9,
                FfmpegArg = "vp9",
                Description = "VP9 (Legacy)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.vp9,
                FfmpegArg = "libvpx-vp9",
                Description = "VP9 (Software)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.vp9,
                FfmpegArg = "vp9_vaapi",
                Description = "VP9 (VA-API)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.vp9,
                FfmpegArg = "vp9_qsv",
                Description = "VP9 (Intel QSV)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },

            // AV1
            new() { 
                Tag = MediaFormats.av1,
                FfmpegArg = "libaom-av1",
                Description = "AV1 (AOMedia)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.av1,
                FfmpegArg = "librav1e",
                Description = "AV1 (rav1e)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.av1,
                FfmpegArg = "libsvtav1",
                Description = "AV1 (SVT)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.av1,
                FfmpegArg = "av1_nvenc",
                Description = "AV1 (NVIDIA)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.av1,
                FfmpegArg = "av1_qsv",
                Description = "AV1 (Intel QSV)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.av1,
                FfmpegArg = "av1_amf",
                Description = "AV1 (AMD)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() { 
                Tag = MediaFormats.av1,
                FfmpegArg = "av1_vaapi",
                Description = "AV1 (VA-API)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            }
        };

        
        private RenderFormatItem[] _AudioEncoding = 
        {
            new() {
                Tag = MediaFormats.aac,
                FfmpegArg = "aac",
                Description = "AAC",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mov, MediaFormats.mkv }
            },
            new() {
                Tag = MediaFormats.mp3,
                FfmpegArg = "mp3",
                Description = "MP3",
                Compatibility = new[] { MediaFormats.mp4, MediaFormats.mkv }
            },
            
            // Opus
            new() {
                Tag = MediaFormats.opus,
                FfmpegArg = "opus",
                Description = "Opus Audio (Legacy)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() {
                Tag = MediaFormats.opus,
                FfmpegArg = "libopus",
                Description = "Opus Audio",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            
            // Vorbis
            new() {
                Tag = MediaFormats.vorbis,
                FfmpegArg = "vorbis",
                Description = "Vorbis Audio (Legacy)",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            new() {
                Tag = MediaFormats.vorbis,
                FfmpegArg = "libvorbis",
                Description = "Vorbis Audio",
                Compatibility = new[] { MediaFormats.webm, MediaFormats.mkv }
            },
            
            // Apple
            new() {
                Tag = MediaFormats.alac,
                FfmpegArg = "alac",
                Description = "Apple Lossless Audio",
                Compatibility = new[] { MediaFormats.mov, MediaFormats.mp4 }
            },
            
            // PCM variants
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_s16le",
                Description = "PCM 16-bit Signed Little Endian",
                Compatibility = new[] { MediaFormats.mkv, MediaFormats.mov }
            },
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_s24le",
                Description = "PCM 24-bit Signed Little Endian",
                Compatibility = new[] { MediaFormats.mkv, MediaFormats.mov }
            },
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_s32le",
                Description = "PCM 32-bit Signed Little Endian",
                Compatibility = new[] { MediaFormats.mkv, MediaFormats.mov }
            },
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_s64le",
                Description = "PCM 64-bit Signed Little Endian",
                Compatibility = new[] { MediaFormats.mkv }
            },
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_s8",
                Description = "PCM 8-bit Signed",
                Compatibility = new[] { MediaFormats.mkv, MediaFormats.mov }
            },
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_vidc",
                Description = "PCM Archimedes VIDC",
                Compatibility = new[] { MediaFormats.mkv }
            },
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_alaw",
                Description = "PCM A-law",
                Compatibility = new[] { MediaFormats.mkv }
            },
            new() {
                Tag = MediaFormats.pcm,
                FfmpegArg = "pcm_mulaw",
                Description = "PCM Mu-law",
                Compatibility = new[] { MediaFormats.mkv }
            }
        };

        private Camera _Camera;


        Vector2 GetCRFRange(MediaFormats format) => format switch
        {
            // x/h.264 typical range
            MediaFormats.mp4  => new Vector2(51, 18),  // min 18 for high quality, max 51
            MediaFormats.webm => new Vector2(63, 4),   // correct as is
            MediaFormats.mkv  => new Vector2(51, 18),  // typically uses h.264/h.265
            MediaFormats.mov  => new Vector2(51, 18),  // typically uses h.264
            MediaFormats.flv  => new Vector2(51, 18),  // typically uses h.264
            _ => throw new InvalidOperationException()
        };
        

        public void Awake()
        {
            if (main) Close();
            else main = this;
        }

        public void OnDestroy()
        {
            if (PrefsDirty)
            {
                Prefs.Save(Behaviors.Chartmaker.Chartmaker.PreferencesStorage);
                Behaviors.Chartmaker.Chartmaker.main.StartSavePrefsRoutine();
            }
        }

        new void Start()
        {
            _Camera = Camera.main;
            base.Start();
            Prefs.Load(Behaviors.Chartmaker.Chartmaker.PreferencesStorage);

            CustomiseFFmpegDisclaimer();

            TimeRange = new (-5, Behaviors.Chartmaker.Chartmaker.main.CurrentSong.Clip.length + 5);
            if (!String.IsNullOrWhiteSpace(Prefs.FFmpegPath)) 
                CheckFFmpeg();

            InitForm();
        }

        private void CustomiseFFmpegDisclaimer()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    FFmpegDisclaimerDownloadText.text = "Download FFmpeg builds for Windows";
                    FFmpegDownloadLink = "https://www.gyan.dev/ffmpeg/builds/";

                    break;
            
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    FFmpegDisclaimerDownloadText.text = "Download FFmpeg builds for Linux";
                    FFmpegDownloadLink = "https://www.ffmpeg.org/download.html#build-linux";

                    break;
            
                default:
                    FFmpegDisclaimerDownloadText.text = "Get FFmpeg";
                    FFmpegDownloadLink = "https://www.ffmpeg.org/download.html";

                    break;
            }
        }

        public void InitForm()
        {
            var ffmpeg = Formmaker.main.Spawn<FormEntryFile, string>(
                FFmpegFieldHolder,
                "FFmpeg Path", () => Prefs.FFmpegPath, x => {
                    Prefs.FFmpegPath = x;
                    PrefsDirty = true;
                
                    CheckFFmpeg();
                }
            );
            ffmpeg.AcceptedTypes = new List<FileModalFileType> {
                new("FFmpeg executable", "exe"),
                new("All files"),
            };
            SpawnForm<FormEntryString, string>("Output", () => OutputPath, x => {
                OutputPath = x; 
            });
            
            // Pre declaration for allowing dropdown item updates
            FormEntryDropdown videoEncoderField = null; 
            FormEntryDropdown audioEncoderField = null; 

            // Helper method to update encoder options
            void UpdateEncoderOptions(FormEntryDropdown field, RenderFormatItem[] encoders)
            {
                if (field == null) 
                    return;
    
                field.ValidValues.Clear();
                for (int i = 0; i < encoders.Length; i++)
                {
                    var encoder = encoders[i];
                    if (encoder.Compatibility.Contains((MediaFormats)Prefs.OutputType))
                        field.ValidValues.Add(i, encoder.Description);
                }
            }

            // Create format field
            var formatField = SpawnForm<FormEntryDropdown, object>("File Format", () => Prefs.OutputType, x => {
                    
                    Prefs.OutputType = (int)x;
                    
                    UpdateEncoderOptions(videoEncoderField, _VideoEncoding);
                    
                    UpdateEncoderOptions(audioEncoderField, _AudioEncoding);
                    
                }
            );

            // Add format options
            foreach (var (format, displayName) in _formatDisplayNames.Select((kvp, i) => (i, kvp.Value)))
            {
                formatField.ValidValues.Add(format, displayName);
            }

            // Create video encoder field
            videoEncoderField = SpawnForm<FormEntryDropdown, object>("Video Encoding", () => Prefs.VideoEncoder, v => {
                    Prefs.VideoEncoder = (int)v;
                    UpdateEncoderOptions(audioEncoderField, _AudioEncoding);
                }
            );
            UpdateEncoderOptions(videoEncoderField, _VideoEncoding);

            
            // Create audio encoder field
            audioEncoderField = SpawnForm<FormEntryDropdown, object>("Audio Encoding", 
                () => Prefs.AudioEncoder, 
                a => Prefs.AudioEncoder = (int)a
            );
            UpdateEncoderOptions(audioEncoderField, _AudioEncoding);
            

            SpawnForm<FormEntryHeader>("Time");
            var timeField = SpawnForm<FormEntryTimeRange, Vector2>("Range (sec)", () => TimeRange, x => {
                TimeRange = new(x.x, Mathf.Max(x.x, x.y)); 
            });
        
            var timeActions = SpawnForm<FormEntryButton>("Set Full Song");
            timeActions.Button.onClick.AddListener(() => {
                timeField.FieldX.text = (-5).ToString();    
                timeField.FieldY.text = (Behaviors.Chartmaker.Chartmaker.main.CurrentSong.Clip.length + 5).ToString();    
            });

            SpawnForm<FormEntryHeader>("Quality");
            var resField = SpawnForm<FormEntryVector2, Vector2>("Resolution (px)", () => Prefs.Resolution, x => {
                Prefs.Resolution = new((int)x.x, (int)x.y); PrefsDirty = true;
            });
            var resActions = SpawnForm<FormEntryButton>("Resolution Presets");
            // --
            resActions.TitleLabel.text = "Asp. Ratio Presets";
            var ratioBtn = Instantiate(resActions.Button, resActions.transform);
            ratioBtn.onClick.AddListener(() => {
                void setRatio(float ratio) 
                {
                    resField.FieldX.text = (Prefs.Resolution.y * ratio).ToString("0");
                }
            
                ContextMenuListAction getItem(string name, float ratio) 
                    => new (name + " (" + ratio.ToString("0.####") + ")", () => setRatio(ratio), _checked: Math.Abs(ratio - Prefs.Resolution.x / (float)Prefs.Resolution.y) < 0.001f);

                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    new ContextMenuListAction("Standard", () => {}, _enabled: false),
                    getItem(   "5:4",       5 / 4f),
                    getItem(   "4:3",       4 / 3f),
                
                    new ContextMenuListSeparator(),
                
                    new ContextMenuListAction("Wide", () => {}, _enabled: false),
                    getItem(   "16:10",    16 / 10f),
                    getItem(   "16:9",      16 / 9f),
                
                    new ContextMenuListSeparator(),
                
                    new ContextMenuListAction("Ultra-wide", () => {}, _enabled: false),
                    getItem(   "256:135", 256 / 135f),
                    getItem(   "21:9",       21 / 9f),
                    getItem(   "64:27",     64 / 27f),
                    getItem(   "12:5",       12 / 5f),
                    getItem(   "32:9",       32 / 9f)
                ), (RectTransform)ratioBtn.transform); 
            });
            // --
            resActions.TitleLabel.text = "Resolution Presets";
            resActions.Button.onClick.AddListener(() => {
                void setRes(float res) 
                {
                    float ratio = Prefs.Resolution.x / (float)Prefs.Resolution.y;
                    resField.FieldX.text = (res * ratio).ToString("0");
                    resField.FieldY.text = (res).ToString("0");
                }
                ContextMenuListAction getItem(string name, float res) 
                    => new (name, () => setRes(res), _checked: Prefs.Resolution.y == res);

                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    getItem(   "240p",            240),
                    getItem(   "480p (SD)",       480),
                    getItem(   "720p (HD)",       720),
                    getItem(   "1080p (FHD)",    1080),
                    getItem(   "1440p (QHD)",    1440),
                    getItem(   "2160p (4K UHD)", 2160),
                    getItem(   "2880p (5K)",     2880),
                    getItem(   "4320p (8K UHD)", 4320)
                ), (RectTransform)resActions.Button.transform);     
            });

            var fpsField = SpawnForm<FormEntryFloat, float>("Frame Rate (fps)", () => Prefs.FrameRate, x => {
                Prefs.FrameRate = x; PrefsDirty = true;
            });
            var fpsPresets = SpawnForm<FormEntryButton>("Frame Rate Presets");
            fpsPresets.Button.onClick.AddListener(() => {
                void setFPS(float fps) 
                {
                    fpsField.Field.text = fps.ToString();
                }
                ContextMenuListAction getItem(string name, float fps) 
                    => new (name, () => setFPS(fps), _checked: Prefs.FrameRate == fps);

                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    getItem(  "16fps",                     16),
                    getItem(  "20fps",                     20),
                    getItem(  "24fps (Film)",              24),
                    getItem(  "25fps (PAL)",               25),
                    getItem(  "29.97fps (NTSC)",       29.97f),
                    getItem(  "30fps (Standard SD)",       30),
                    getItem(  "48fps (Film HD)",           48),
                    getItem(  "50fps (PAL HD)",            50),
                    getItem(  "59.94fps (NTSC HD)",    59.94f),
                    getItem(  "60fps (Standard HD)",       60),
                    getItem(  "72fps",                     72),
                    getItem(  "100fps",                   100),
                    getItem(  "120fps",                   120),
                    getItem(  "144fps",                   144),
                    getItem(  "240fps",                   240),
                    getItem(  "288fps",                   288),
                    getItem(  "300fps",                   300)
                ), (RectTransform)fpsPresets.Button.transform);
            });
            SpawnForm<FormEntrySpace>();

            var antiAliasingField = SpawnForm<FormEntryDropdown, object>("Anti-Aliasing", () => Prefs.AntiAliasing, a =>
            {
                Prefs.AntiAliasing = (int)a;
            });
            antiAliasingField.ValidValues.Add(0,      "None");
            antiAliasingField.ValidValues.Add(2,   "2x MSAA");
            antiAliasingField.ValidValues.Add(4,   "4x MSAA");
            antiAliasingField.ValidValues.Add(8,   "8x MSAA");
            antiAliasingField.ValidValues.Add(16, "16x MSAA");

            SpawnForm<FormEntrySpace>();

            FormEntryRange vqualField = null;
            FormEntryFloat vbitrateField = null;
            
            var vOptions = SpawnForm<FormEntryBool, bool>("Adaptive Bitrate", () => Prefs.AdaptiveBitrate, o =>
            {
                Prefs.AdaptiveBitrate = o;

                switch (o)
                {
                    case true:
                        vqualField.gameObject.SetActive(true);
                        vbitrateField.gameObject.SetActive(false);
                        break;
                    case false:
                        vqualField.gameObject.SetActive(false);
                        vbitrateField.gameObject.SetActive(true);

                        break;
                }
            });
            
            vqualField = SpawnForm<FormEntryRange, float>("Video Quality", () => Prefs.VideoQuality * 100, x => {
                Prefs.VideoQuality = x / 100; PrefsDirty = true;
            });
            vqualField.Range.maxValue = 100; vqualField.Range.wholeNumbers = true;

            vbitrateField = SpawnForm<FormEntryFloat, float>("Video Bitrate (kbps)", () => Prefs.VideoBitRate, v =>
            {
                Prefs.VideoBitRate = v;
            });

            switch (Prefs.AdaptiveBitrate)
            {
                case true:
                    vqualField.gameObject.SetActive(true);
                    vbitrateField.gameObject.SetActive(false);
                    break;
                case false:
                    vqualField.gameObject.SetActive(false);
                    vbitrateField.gameObject.SetActive(true);
                    break;
            }
            
            SpawnForm<FormEntryInt, int>("Audio Bitrate (kbps)", () => Prefs.AudioBitRate, x => {
                Prefs.AudioBitRate = x; PrefsDirty = true;
            });

            SpawnForm<FormEntryHeader>("Other");
            SpawnForm<FormEntryBool, bool>("Open File on Complete", () => Prefs.OpenOnComplete, x => {
                Prefs.OpenOnComplete = x; PrefsDirty = true;
            });
        
            LayoutRebuilder.ForceRebuildLayoutImmediate(FormHolder);
        }

        public void DownloadFFmpeg() 
        {
            Application.OpenURL(FFmpegDownloadLink);
        }

        public void CheckFFmpeg()
        {
            if (!IsAnimating) 
                StartCoroutine(CheckFFmpegRoutine());
        }

        public IEnumerator CheckFFmpegRoutine()
        {
            IsAnimating = true;
            string output = "";
            FFmpegDisclaimer.SetActive(false);
            BusyDisclaimer.SetActive(true);
            BusyLabel.text = "Checking FFmpeg...";
            Task task = Task.Run(async () => {
                output = (await ffmpeg("-version")).Output;
                UnityEngine.Debug.Log(output);
                Match m = Regex.Match(output, @"^ffmpeg version ([^\s]+)");
                if (!m.Success) throw new Exception("Executable doesn't seem to be FFmpeg");
                FFmpegVersion = m.Groups[1].Value;
            });
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Exception != null) 
            {
                BusyLabel.text = "There was an error checking FFmpeg:\n" + task.Exception.Message;
                yield break;
            }
            BusyDisclaimer.SetActive(false);
            IsAnimating = false;
        }

        public void Render() 
        {
            transform.Translate(2 * Screen.height * Vector2.down);
            
            _ = RenderRoutine();
        }

        private string _EtaString;

        private Queue<float> _RecentFrameTimes;
        public async Task RenderRoutine()
        {
            // FFmpeg process setup
            Process ffmpegProcess = null;
            Stream ffmpegInputStream = null;
            Task ffmpegTask = null;

            Texture2D tex = null;
            RenderTexture rtex = null;
            try
            {


                InitializeETATracking();
            
                IsAnimating = true;
            
            
                var chartmaker = Behaviors.Chartmaker.Chartmaker.main;
                var loaderPanel = chartmaker.LoaderPanel;
            
                chartmaker.Loader.SetActive(true);
                loaderPanel.ActionLabel.text = "Rendering...";
                loaderPanel.ProgressBar.value = 0;
                loaderPanel.ProgressLabel.text = "Initializing...";
            
                await Task.Delay(100);

                // Pre-calculate constants
                var resolution = Prefs.Resolution;
                var frameRate = Prefs.FrameRate;
                var timeRange = TimeRange;
            
                float delta = 1f / frameRate;
                int totalFrames = Mathf.CeilToInt((timeRange.y - timeRange.x) * frameRate);
                float camHeight = Mathf.Min(1f, 7f / 4f * resolution.x / resolution.y) * 0.9f;
                float fov = Mathf.Atan2(Mathf.Tan(30f * Mathf.Deg2Rad), camHeight) * 2f * Mathf.Rad2Deg;
            
                Vector2 crfRange = GetCRFRange((MediaFormats)Prefs.VideoEncoder);
                int crf = Mathf.RoundToInt(Mathf.LerpUnclamped(crfRange.x, crfRange.y, Prefs.VideoQuality));
            
                string videoFormatArg = _VideoEncoding[Prefs.VideoEncoder].FfmpegArg;
                string audioFormatArg = _AudioEncoding[Prefs.AudioEncoder].FfmpegArg;
                string extensionArg = ((MediaFormats)Prefs.OutputType).ToString();

                // Setup camera and render texture
                int originalAntiAliasing;
                try
                {
                    rtex = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
                    originalAntiAliasing = QualitySettings.antiAliasing;
                    QualitySettings.antiAliasing = Prefs.AntiAliasing;
                    _Camera.targetTexture = rtex;
                    _Camera.rect = new Rect(0, 0, resolution.x, resolution.y);
                    _Camera.fieldOfView = fov;
                    rtex.Create();
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to create render texture: " + e.Message);;
                }

                // Use RGB24 format for direct byte access - no alpha channel needed
                tex = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
                Rect rectConfig = new Rect(0, 0, resolution.x, resolution.y);

                // Setup output path
                string folder = Helper.GetRenderFolder();
                Directory.CreateDirectory(folder);
                string outputPath = Path.Combine(folder, 
                    (string.IsNullOrWhiteSpace(OutputPath) ? DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() : OutputPath) 
                    + "." + extensionArg);

                // Cache commonly used objects
                var songSource = chartmaker.SongSource;
                var informationBar = InformationBar.main;
                var playerView = PlayerView.main;
            
                // Setup FFmpeg arguments for streaming input
                string qualityOptions = Prefs.AdaptiveBitrate ? $"-crf {crf}" : $"-b:v {Prefs.VideoBitRate}k";
                string audioPath = Path.Combine(Path.GetDirectoryName(chartmaker.CurrentSongPath), chartmaker.CurrentSong.ClipPath);
            
                string ffmpegArgs = $"-f rawvideo -pix_fmt rgb24 -s {resolution.x}x{resolution.y} -r {frameRate} -i pipe:0 " +
                                    $"-ss {timeRange.x} -t {timeRange.y - timeRange.x} -i \"{audioPath}\" " +
                                    $"-vcodec {videoFormatArg} -acodec {audioFormatArg} " +
                                    $"{qualityOptions} -b:a {Prefs.AudioBitRate}k " +
                                    $"-pix_fmt rgb24 -y \"{outputPath}\"";

                // Start FFmpeg process
                ProcessStartInfo startInfo = new ProcessStartInfo(Prefs.FFmpegPath)
                {
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                ffmpegProcess = new Process { StartInfo = startInfo };
                ffmpegProcess.Start();
                ffmpegInputStream = ffmpegProcess.StandardInput.BaseStream;

                // Start async task to read FFmpeg output (for debugging/logging)
                ffmpegTask = Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while ((line = ffmpegProcess.StandardError.ReadLine()) != null)
                        {
                            UnityEngine.Debug.Log($"FFmpeg: {line}");
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"FFmpeg output reading error: {e.Message}");
                    }
                });

                float time = timeRange.x;
                int frameIndex = 0;

                loaderPanel.ProgressLabel.text = $"Streaming frames... (0/{totalFrames})";

                ConcurrentQueue<byte[]> frameQueue = new();
                bool rendering = true;
                
                // Piping thread
                var pipingThread = new Thread(() => 
                {
                    while (rendering || !frameQueue.IsEmpty) 
                    {
                        if (frameQueue.TryDequeue(out var frame)) 
                        {
                            ffmpegInputStream.Write(frame, 0, frame.Length);
                            ffmpegInputStream.Flush();
                            
                        } 
                        else 
                            Thread.Sleep(1);
                    }
                    if (rendering && frameQueue.IsEmpty) 
                    {
                        UnityEngine.Debug.Log("Waiting for new frame.");
                    }
                    
                    if (!rendering)
                        ffmpegInputStream.Close();
                });
                
                pipingThread.Start();
                
                // Pre-allocate buffer for raw frame data
                int frameSize = (resolution.x * resolution.y) * 3; // RGB24 = 3 bytes per pixel
                byte[] frameBuffer = new byte[frameSize];

                // Main rendering loop
                while (time < timeRange.y && frameIndex < totalFrames)
                {
                    // Update scene
                    songSource.time = Mathf.Clamp(time, 0f, songSource.clip.length);
                    informationBar.Update();
                    playerView.UpdateObjects();
                    time += delta;
                    
                    // Render frame
                    RenderTexture.active = rtex;
                    _Camera.Render();

                    tex.ReadPixels(rectConfig, 0, 0);
                    tex.Apply();

                    // Get raw RGB data directly
                    byte[] rawData = tex.GetRawTextureData();
            
                    // Unity's texture data might need to be flipped vertically for FFmpeg
                    int stride = resolution.x * 3; // 3 bytes per pixel for RGB24
    
                    for (int y = 0; y < resolution.y; y++)
                    {
                        int srcOffset = (resolution.y - 1 - y) * stride;
                        int dstOffset = y * stride;
                        Array.Copy(rawData, srcOffset, frameBuffer, dstOffset, stride);
                    }
                    
                    // Queue frame for FFmpeg
                    frameQueue.Enqueue((byte[])frameBuffer.Clone());
                    
                    frameIndex++;
                    UpdateETAProgress(frameIndex, totalFrames);;

                    // Update progress less frequently (by average fps) for performance
                    float averageFrameTime = _RecentFrameTimes.Count > 0 
                        ? _RecentFrameTimes.Sum() / _RecentFrameTimes.Count : 0.033f; // fallback to ~30fps
                    float averageFPS = averageFrameTime > 0 
                        ? 1f / averageFrameTime : 30f;
                    int yieldInterval = Mathf.Clamp(Mathf.RoundToInt(averageFPS), 10, 120);

                    if (frameIndex % yieldInterval == 0 || frameIndex == totalFrames)
                    {
                        loaderPanel.ProgressLabel.text = $"Streaming frames... ({frameIndex}/{totalFrames}) {_EtaString}";
                        loaderPanel.ProgressBar.value = (float)frameIndex / totalFrames;
                        await Task.Yield();
                    }
                }

                // Close the input stream to signal end of video data
                rendering = false;
                pipingThread.Join();
                
                loaderPanel.ProgressLabel.text = "Finalizing video...";
            
                // Wait for FFmpeg to finish processing
                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    // Wait for FFmpeg to complete, but with timeout
                    bool finished = ffmpegProcess.WaitForExit(30000); // 30 second timeout
                    if (!finished)
                    {
                        UnityEngine.Debug.LogWarning("FFmpeg process timed out, forcing termination");
                        ffmpegProcess.Kill();
                    }
                }

                // Wait for output reading task to complete
                if (ffmpegTask != null)
                {
                    try
                    {
                        ffmpegTask.Wait(5000); // 5 second timeout
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"FFmpeg output task error: {e.Message}");
                    }
                }
                QualitySettings.antiAliasing = originalAntiAliasing;
            
                {
                    // Cleanup
                    try
                    {
                        ffmpegProcess?.Kill();
                        ffmpegProcess?.Dispose();
                    
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"Cleanup error: {e.Message}");
                    }

                    _Camera.targetTexture = null;
                    RenderTexture.active = null;
                
                    if (rtex != null)
                    {
                        rtex.Release();
                        Destroy(rtex);
                    }
                    if (tex != null)
                    {
                        Destroy(tex);
                    }

                    Close();
                    chartmaker.Loader.SetActive(false);
                
                    if (Prefs.OpenOnComplete && !string.IsNullOrEmpty(outputPath)) 
                    {
                        Application.OpenURL("file://" + outputPath);
                    }
                
                    IsAnimating = false;
                    chartmaker.Notify("Render completed!");
                }
            }
            catch (Exception e)
            {
                ThrowRenderModal(e, ffmpegProcess, rtex, tex);
            }
        }

        private void ThrowRenderModal(Exception e, Process ffmpegProcess, RenderTexture rtex, Texture tex)
        {
            DialogModal errorModal = ModalHolder.main.Spawn<DialogModal>();

            errorModal.SetDialog("Error rendering!", e.Message, new[] { "Retry", "OK" }, i =>
            {
                switch (i)
                {
                    case 0:
                        Render();
                        break;
                    case 1:
                        errorModal.BodyLabel.text += "\nCleaning up.";
                        try
                        {
                            if (ffmpegProcess == null)
                            {
                            }
                            else
                            {
                                ffmpegProcess.Kill();
                                ffmpegProcess.Dispose();
                                ffmpegProcess.StandardInput.BaseStream?.Close();
                            }

                        }
                        // ReSharper disable once InconsistentNaming
                        catch (Exception in_e)
                        {
                            UnityEngine.Debug.LogWarning($"Cleanup error: {in_e.Message}");
                        }
                        
                        _Camera.targetTexture = null;
                        RenderTexture.active = null;
                
                        if (rtex != null)
                        {
                            rtex.Release();
                            Destroy(rtex);
                        }
                        if (tex != null)
                        {
                            Destroy(tex);
                        }

                        Close();
                        Behaviors.Chartmaker.Chartmaker.main.Loader.SetActive(false);

                        Destroy(errorModal.gameObject);
                        break;
                }
            });
        }
        
        async Task<ProcessOutput> cmd(string file, string args, Action<string> onLineRead = null) 
        {
            ProcessStartInfo startInfo = new(file)
            {
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new()
            {
                StartInfo = startInfo
            };
            process.Start();

            ProcessOutput output = new();

            await Task.WhenAll(
                Task.Run(() => {
                    string line = "";
                    while ((line = process.StandardOutput.ReadLine()) != null)
                    {
                        onLineRead?.Invoke(line);
                        output.Output += line;
                    }     
                }),
                Task.Run(() => {
                    string line = "";
                    while ((line = process.StandardError.ReadLine()) != null)
                    {
                        onLineRead?.Invoke(line);
                        output.Output += line;
                    }     
                })
            );

            output.ExitCode = process.ExitCode;
        
            return output;
        }

        async Task<ProcessOutput> ffmpeg(string args, Action<string> onLineRead = null) 
        {
            return await cmd(Prefs.FFmpegPath, args, onLineRead);
        }

        T SpawnForm<T>(string title = "") where T : FormEntry
            => Formmaker.main.Spawn<T>(FormHolder, title);

        T SpawnForm<T, U>(string title, Func<U> get, Action<U> set) where T : FormEntry<U>
            => Formmaker.main.Spawn<T, U>(FormHolder, title, get, set);

        
        // ETA Stuff
        private System.Diagnostics.Stopwatch renderStopwatch;
        private float lastEtaUpdateTime;
        private const int ETA_SAMPLE_SIZE = 30; // Number of frames to average for ETA calculation
        private const float ETA_UPDATE_INTERVAL = 1f; // Update ETA every second

        // Initialize ETA tracking (add this at the start of RenderRoutine)
        private void InitializeETATracking()
        {
            renderStopwatch = System.Diagnostics.Stopwatch.StartNew();
            _RecentFrameTimes = new Queue<float>(ETA_SAMPLE_SIZE);
            lastEtaUpdateTime = 0f;
        }
        
        private void UpdateETAProgress(int currentFrame, int totalFrames)
        {
            float currentTime = (float)renderStopwatch.Elapsed.TotalSeconds;
            
            // Track frame time for moving average
            if (_RecentFrameTimes.Count > 0)
            {
                float frameTime = currentTime - lastEtaUpdateTime;
                _RecentFrameTimes.Enqueue(frameTime);
                
                if (_RecentFrameTimes.Count > ETA_SAMPLE_SIZE)
                {
                    _RecentFrameTimes.Dequeue();
                }
            }
            else
            {
                // First frame, add a reasonable initial estimate
                _RecentFrameTimes.Enqueue(0.1f);
            }
            
            lastEtaUpdateTime = currentTime;
            
            // Calculate ETA every second or every 10 frames (whichever comes first)
            if (currentFrame % 10 == 0 || (currentTime - lastEtaUpdateTime) >= ETA_UPDATE_INTERVAL)
            { 
                _EtaString = ETAString(currentFrame, totalFrames, currentTime);
            }
        }

        // Format progress text with ETA information
        private string ETAString(int currentFrame, int totalFrames, float elapsedSeconds)
        {
            float progress = (float)currentFrame / totalFrames;
            
            if (currentFrame < 5 || _RecentFrameTimes.Count == 0)
            {
                // Not enough data for accurate ETA, show basic progress
                return string.Empty;
            }
            
            // Calculate average frame time from recent samples
            float averageFrameTime = _RecentFrameTimes.Sum() / _RecentFrameTimes.Count;
            
            // Calculate ETA based on remaining frames
            int remainingFrames = totalFrames - currentFrame;
            float estimatedTimeRemaining = remainingFrames * averageFrameTime;
            
            // Calculate current fps
            float currentFPS = _RecentFrameTimes.Count > 0 ? 1f / averageFrameTime : 0f;
            
            string etaText = FormatTimeSpan(estimatedTimeRemaining);
            string elapsedText = FormatTimeSpan(elapsedSeconds);
            
            return $"\nETA: {etaText} | Elapsed: {elapsedText} | {currentFPS:F1} fps";
        }

        // Helper method to format time spans nicely
        private string FormatTimeSpan(float seconds)
        {
            if (seconds < 0) return "calculating...";
            
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            }
            else
            {
                return $"{timeSpan.Seconds}s";
            }
        }
        
    }
    

    public class RenderPrefs 
    {
        public string FFmpegPath;
        public int    OutputType;

        public Vector2Int Resolution   = new(1024, 800);
        public float      FrameRate    = 30;
        public float      VideoQuality = 0.6f;
        public int        AudioBitRate = 128;
        public float      VideoBitRate = 3200;
     
        public int VideoEncoder;
        public int AudioEncoder;
        
        public bool OpenOnComplete = true;

        public bool AdaptiveBitrate;
        
        public int AntiAliasing;

        public void Load(Storage storage)
        {
            FFmpegPath = storage.Get("RD:FFmpegPath", FFmpegPath);
            OutputType = storage.Get("RD:OutputType", OutputType);

            Resolution.x = storage.Get("RD:Resolution.X", Resolution.x);
            Resolution.y = storage.Get("RD:Resolution.Y", Resolution.y);
           
            FrameRate    = storage.Get("RD:FrameRate", FrameRate);
            
            VideoQuality = storage.Get("RD:VideoQuality", VideoQuality);
            
            AudioBitRate = storage.Get("RD:AudioBitRate", AudioBitRate);
            VideoBitRate = storage.Get("RD:VideoBitRate", VideoBitRate);
            
            VideoEncoder = storage.Get("RD:VideoEncoder", VideoEncoder);
            AudioEncoder = storage.Get("RD:AudioEncoder", AudioEncoder);
            
            OpenOnComplete  = storage.Get("RD:OpenOnComplete", OpenOnComplete);
         
            AdaptiveBitrate = storage.Get("RD:AdaptiveBitrate", AdaptiveBitrate);
           
            AntiAliasing = storage.Get("RD:AntiAliasing", AntiAliasing);
        }

        public void Save(Storage storage)
        {
            storage.Set("RD:FFmpegPath", FFmpegPath);
            storage.Set("RD:OutputType", OutputType);

            storage.Set("RD:Resolution.X", Resolution.x);
            storage.Set("RD:Resolution.Y", Resolution.y);
         
            storage.Set("RD:FrameRate", FrameRate);
          
            storage.Set("RD:VideoQuality", VideoQuality);
            
            storage.Set("RD:AudioBitRate", AudioBitRate);
            storage.Set("RD:VideoBitRate", VideoBitRate);
          
            storage.Set("RD:VideoEncoder", VideoEncoder);
            storage.Set("RD:AudioEncoder", AudioEncoder);
            
            storage.Set("RD:OpenOnComplete", OpenOnComplete);
         
            storage.Set("RD:AdaptiveBitrate", AdaptiveBitrate);
           
            storage.Set("RD:AntiAliasing", AntiAliasing);
        }
    }

    public class ProcessOutput 
    {
        public string Output = "";
        public int    ExitCode;
    }
}
