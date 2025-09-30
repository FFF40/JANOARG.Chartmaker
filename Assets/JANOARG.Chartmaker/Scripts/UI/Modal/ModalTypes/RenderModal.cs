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
        public RectTransform FormCompoundField;
        [Space]
        public RectTransform VisualizerHolder;
        public RectTransform VisualizerScreenArea;
        public RectTransform VisualizerSafeArea;

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
        Process FFmpegProcess;

        // I'm not gonna make 3 different enums for this
        enum MediaFormat
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
            public MediaFormat Format;
            public string FfmpegArg;
            public string Description;
            public MediaFormat[] Compatibility;
        }
        
        private readonly Dictionary<MediaFormat, string> _formatDisplayNames = new()
        {
            { MediaFormat.mp4,    "MP4 (.mp4)" },
            { MediaFormat.webm,   "WebM (.webm)" },
            { MediaFormat.mkv,    "Matroska (.mkv)" },
            { MediaFormat.mov,    "QuickTime (.mov)" },
            // { MediaFormat.flv,    "Flash (.flv)" },
        };
        private readonly Dictionary<MediaFormat, string> _encodingDisplayNames = new()
        {
            { MediaFormat.h264,   "H.264/AVC" },
            { MediaFormat.h265,   "H.265/HEVC" },
            { MediaFormat.vp8,    "VP8" },
            { MediaFormat.vp9,    "VP9" },
            { MediaFormat.av1,    "AV1" },

            { MediaFormat.aac,    "AAC" },
            { MediaFormat.mp3,    "MP3" },
            { MediaFormat.vorbis, "Vorbis" },
            { MediaFormat.opus,   "Opus" },
            { MediaFormat.alac,   "Apple Lossless" },
            { MediaFormat.pcm,    "PCM" },
        };

        private readonly RenderFormatItem[] _VideoEncoders =
        {
            // H.264
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "libx264",
                Description = "Software",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv }
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_amf",
                Description = "AMD",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv }
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_nvenc",
                Description = "NVIDIA",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv }
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_qsv",
                Description = "Intel QSV",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv }
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_vulkan",
                Description = "Vulkan",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_vaapi",
                Description = "VA-API",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264",
                Description = "Legacy",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv }
            },

            // H.265
            new() {
                Format = MediaFormat.h265,
                FfmpegArg = "libx265",
                Description = "Software",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.h265,
                FfmpegArg = "hevc_amf",
                Description = "AMD",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.h265,
                FfmpegArg = "hevc_nvenc",
                Description = "NVIDIA",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.h265,
                FfmpegArg = "hevc_qsv",
                Description = "Intel QSV",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov }
            },

            // VPX
            new() {
                Format = MediaFormat.vp8,
                FfmpegArg = "libvpx",
                Description = "Software",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.vp8,
                FfmpegArg = "vp8_vaapi",
                Description = "VA-API",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.vp8,
                FfmpegArg = "vp8",
                Description = "Legacy",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },

            new() {
                Format = MediaFormat.vp9,
                FfmpegArg = "libvpx-vp9",
                Description = "Software",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.vp9,
                FfmpegArg = "vp9_vaapi",
                Description = "VA-API",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.vp9,
                FfmpegArg = "vp9_qsv",
                Description = "Intel QSV",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.vp9,
                FfmpegArg = "vp9",
                Description = "Legacy",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },

            // AV1
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "libaom-av1",
                Description = "AOMedia",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "librav1e",
                Description = "rav1e",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "libsvtav1",
                Description = "SVT",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_nvenc",
                Description = "NVIDIA",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_qsv",
                Description = "Intel QSV",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_amf",
                Description = "AMD",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_vaapi",
                Description = "VA-API",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            }
        };

        
        private readonly RenderFormatItem[] _AudioEncoders = 
        {
            // AAC
            new() {
                Format = MediaFormat.aac,
                FfmpegArg = "aac",
                Description = "Software",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mov, MediaFormat.mkv }
            },

            // MPEG
            new() {
                Format = MediaFormat.mp3,
                FfmpegArg = "mp3",
                Description = "Software",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv }
            },
            
            // Opus
            new() {
                Format = MediaFormat.opus,
                FfmpegArg = "libopus",
                Description = "Software",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.opus,
                FfmpegArg = "opus",
                Description = "Legacy",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            
            // Vorbis
            new() {
                Format = MediaFormat.vorbis,
                FfmpegArg = "libvorbis",
                Description = "Software",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.vorbis,
                FfmpegArg = "vorbis",
                Description = "Legacy",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv }
            },
            
            // Apple
            new() {
                Format = MediaFormat.alac,
                FfmpegArg = "alac",
                Description = "Software",
                Compatibility = new[] { MediaFormat.mov, MediaFormat.mp4 }
            },
            
            // PCM variants
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_s8",
                Description = "8-bit Signed",
                Compatibility = new[] { MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_s16le",
                Description = "16-bit Signed Little Endian",
                Compatibility = new[] { MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_s24le",
                Description = "24-bit Signed Little Endian",
                Compatibility = new[] { MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_s32le",
                Description = "32-bit Signed Little Endian",
                Compatibility = new[] { MediaFormat.mkv, MediaFormat.mov }
            },
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_s64le",
                Description = "64-bit Signed Little Endian",
                Compatibility = new[] { MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_vidc",
                Description = "Archimedes VIDC",
                Compatibility = new[] { MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_alaw",
                Description = "A-law",
                Compatibility = new[] { MediaFormat.mkv }
            },
            new() {
                Format = MediaFormat.pcm,
                FfmpegArg = "pcm_mulaw",
                Description = "Mu-law",
                Compatibility = new[] { MediaFormat.mkv }
            }
        };

        private Camera _Camera;


        Vector2 GetCRFRange(MediaFormat format) => format switch
        {
            // x/h.264 typical range
            MediaFormat.h264  => new Vector2(51, 18), 
            MediaFormat.h265  => new Vector2(51, 18), 
            MediaFormat.vp8   => new Vector2(63, 4), 
            MediaFormat.vp9   => new Vector2(63, 4), 
            _ => new Vector2(63, 0), 
        };
        

        public void Awake()
        {
            if (main) Close();
            else main = this;
        }

        public void OnDestroy()
        {
            if (FFmpegProcess != null)
            {
                KillFFmpegProcess();
            }
            if (PrefsDirty)
            {
                Prefs.Save(Behaviors.Chartmaker.Chartmaker.PreferencesStorage);
                Behaviors.Chartmaker.Chartmaker.main.StartSavePrefsRoutine();
            }
        }

        private void KillFFmpegProcess()
        {
            if (!FFmpegProcess.HasExited) FFmpegProcess.Kill();
            FFmpegProcess.StandardInput.BaseStream?.Close();
            FFmpegProcess.StandardOutput.BaseStream?.Close();
            FFmpegProcess.StandardError.BaseStream?.Close();
            FFmpegProcess.Dispose();
            FFmpegProcess = null;
        }

        new void Start()
        {
            _Camera = Camera.main;
            base.Start();
            Prefs.Load(Behaviors.Chartmaker.Chartmaker.PreferencesStorage);

            CustomiseFFmpegDisclaimer();

            TimeRange = new(-5, Behaviors.Chartmaker.Chartmaker.main.CurrentSong.Clip.length + 5);
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

        public void UpdateResolutionVisualizer()
        {
            Vector2 size = VisualizerHolder.rect.size;
            size -= Vector2.one * 20;
            float ratio = size.x / size.y;

            float screenRatio = Prefs.Resolution.x / (float)Prefs.Resolution.y;
            if (ratio > screenRatio) size.x = size.y * screenRatio; // x > y
            else size.y = size.x / screenRatio; // y > x
            VisualizerScreenArea.sizeDelta = size;

            float safeRatio = 7 / 4f;
            ratio = size.x / size.y;
            if (ratio > safeRatio) size.x = size.y * safeRatio; // x > y
            else size.y = size.x / safeRatio; // y > x
            VisualizerSafeArea.sizeDelta = size;
        }

        public void InitForm()
        {
            var ffmpeg = Formmaker.main.Spawn<FormEntryFile, string>(
                FFmpegFieldHolder,
                "FFmpeg Path", () => Prefs.FFmpegPath, x =>
                {
                    Prefs.FFmpegPath = x;
                    PrefsDirty = true;

                    CheckFFmpeg();
                }
            );
            ffmpeg.AcceptedTypes = new List<FileModalFileType> {
                new("FFmpeg executable", "exe"),
                new("All files"),
            };
            SpawnForm<FormEntryString, string>("Output", () => OutputPath, x =>
            {
                OutputPath = x;
            });

            // Pre declaration for allowing dropdown item updates
            FormEntryDropdown videoFormatField = null, videoEncoderField = null; 
            FormEntryDropdown audioFormatField = null, audioEncoderField = null; 

            // Helper method to update encoder options
            void UpdateEncoderOptions(FormEntryDropdown formatField, FormEntryDropdown encoderField, RenderFormatItem[] encoders)
            {
                if (!formatField || !encoderField) return;

                formatField.ValidValues.Clear();
                encoderField.ValidValues.Clear();

                List<RenderFormatItem> validEncoders = new();

                foreach (var encoder in encoders)
                {
                    if (encoder.Compatibility.Contains((MediaFormat)Prefs.OutputType))
                    {
                        validEncoders.Add(encoder);
                        if (!formatField.ValidValues.ContainsKey(encoder.Format))
                        {
                            formatField.ValidValues.Add(encoder.Format, _encodingDisplayNames[encoder.Format]);
                        }
                    }
                }

                if (
                    formatField.CurrentValue == null
                    || !formatField.ValidValues.ContainsKey(formatField.CurrentValue)
                )
                {
                    int validIndex = validEncoders.FindIndex(x => x.FfmpegArg == (string)encoderField.CurrentValue);
                    UnityEngine.Debug.Log(validIndex);
                    if (validIndex >= 0)
                    {
                        formatField.CurrentValue = validEncoders[validIndex].Format;
                    }
                    else
                    {
                        formatField.CurrentValue = validEncoders[0].Format;
                    }
                    UnityEngine.Debug.Log(formatField.CurrentValue);
                }

                foreach (var encoder in validEncoders)
                {
                    if (encoder.Format == (MediaFormat)formatField.CurrentValue)
                    {
                        encoderField.ValidValues.Add(encoder.FfmpegArg, encoder.Description);
                    }
                }

                if (
                    encoderField.CurrentValue == null
                    || !encoderField.ValidValues.ContainsKey(encoderField.CurrentValue)
                ) {
                    encoderField.CurrentValue = Array
                        .Find(encoders, x => x.Format == (MediaFormat)formatField.CurrentValue)
                        .FfmpegArg;
                }

                formatField.Reset();
                encoderField.SetValue(encoderField.CurrentValue);
                encoderField.Reset();
            }

            void MakeCompoundField(FormEntryDropdown formatField, FormEntryDropdown encoderField)
            {
                var holder = Instantiate(FormCompoundField, formatField.DropdownButton.transform.parent);
                holder.gameObject.SetActive(true);
                formatField.DropdownButton.transform.SetParent(holder);
                encoderField.DropdownButton.transform.SetParent(holder);
                encoderField.gameObject.SetActive(false);
                
            }



            SpawnForm<FormEntryHeader>("Time");
            var timeField = SpawnForm<FormEntryTimeRange, Vector2>("Range (sec)", () => TimeRange, x =>
            {
                TimeRange = new(x.x, Mathf.Max(x.x, x.y));
            });

            var timeActions = SpawnForm<FormEntryButton>("Set Full Song");
            timeActions.Button.onClick.AddListener(() =>
            {
                timeField.FieldX.text = (-5).ToString();
                timeField.FieldY.text = (Behaviors.Chartmaker.Chartmaker.main.CurrentSong.Clip.length + 5).ToString();
            });




            SpawnForm<FormEntryHeader>("Format");
            // Create format field
            var formatField = SpawnForm<FormEntryDropdown, object>("File Format", () => Prefs.OutputType, x =>
            {
                Prefs.OutputType = (int)x;
                UpdateEncoderOptions(videoFormatField, videoEncoderField, _VideoEncoders);
                UpdateEncoderOptions(audioFormatField, audioEncoderField, _AudioEncoders);
            }
            );
            // Add format options
            foreach (var (format, displayName) in _formatDisplayNames.Select((kvp, i) => (i, kvp.Value)))
            {
                formatField.ValidValues.Add(format, displayName);
            }
            
            // Create video encoder fields
            videoFormatField = SpawnForm<FormEntryDropdown, object>("Video Encoding", () => videoFormatField.CurrentValue, v => {
                UpdateEncoderOptions(videoFormatField, videoEncoderField, _VideoEncoders);
            });
            videoEncoderField = SpawnForm<FormEntryDropdown, object>("", () => Prefs.VideoEncoder, v => {
                if (Prefs.VideoEncoder == (string)v) return;
                PrefsDirty = true;
                Prefs.VideoEncoder = (string)v;
            });
            videoEncoderField.CurrentValue = Prefs.VideoEncoder; // Initialize valud for encoder update method;
            MakeCompoundField(videoFormatField, videoEncoderField);
            UpdateEncoderOptions(videoFormatField, videoEncoderField, _VideoEncoders);

            // Create audio encoder field
            audioFormatField = SpawnForm<FormEntryDropdown, object>("Audio Encoding", () => audioFormatField.CurrentValue, v => {
                UpdateEncoderOptions(audioFormatField, audioEncoderField, _AudioEncoders);
            });
            audioEncoderField = SpawnForm<FormEntryDropdown, object>("", () => Prefs.AudioEncoder, v => {
                if (Prefs.AudioEncoder == (string)v) return;
                PrefsDirty = true;
                Prefs.AudioEncoder = (string)v;
            });
            audioEncoderField.CurrentValue = Prefs.AudioEncoder; // Initialize valud for encoder update method;
            MakeCompoundField(audioFormatField, audioEncoderField);
            UpdateEncoderOptions(audioFormatField, audioEncoderField, _AudioEncoders);



            SpawnForm<FormEntryHeader>("Quality");
            var resField = SpawnForm<FormEntryVector2, Vector2>("Resolution (px)", () => Prefs.Resolution, x =>
            {
                Prefs.Resolution = new((int)x.x, (int)x.y); PrefsDirty = true;
                UpdateResolutionVisualizer();
            });
            var resActions = SpawnForm<FormEntryButton>("Resolution Presets");
            // --
            resActions.TitleLabel.text = "Asp. Ratio Presets";
            var ratioBtn = Instantiate(resActions.Button, resActions.transform);
            ratioBtn.onClick.AddListener(() =>
            {
                void setRatio(float ratio)
                {
                    resField.FieldX.text = (Prefs.Resolution.y * ratio).ToString("0");
                }

                ContextMenuListAction getItem(string name, float ratio)
                    => new(name + " (" + ratio.ToString("0.####") + ")", () => setRatio(ratio), _checked: Math.Abs(ratio - Prefs.Resolution.x / (float)Prefs.Resolution.y) < 0.001f);

                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    new ContextMenuListAction("Standard", () => { }, _enabled: false),
                    getItem("5:4", 5 / 4f),
                    getItem("4:3", 4 / 3f),

                    new ContextMenuListSeparator(),

                    new ContextMenuListAction("Wide", () => { }, _enabled: false),
                    getItem("16:10", 16 / 10f),
                    getItem("16:9", 16 / 9f),

                    new ContextMenuListSeparator(),

                    new ContextMenuListAction("Ultra-wide", () => { }, _enabled: false),
                    getItem("256:135", 256 / 135f),
                    getItem("21:9", 21 / 9f),
                    getItem("64:27", 64 / 27f),
                    getItem("12:5", 12 / 5f),
                    getItem("32:9", 32 / 9f)
                ), (RectTransform)ratioBtn.transform);
            });
            // --
            resActions.TitleLabel.text = "Resolution Presets";
            resActions.Button.onClick.AddListener(() =>
            {
                void setRes(float res)
                {
                    float ratio = Prefs.Resolution.x / (float)Prefs.Resolution.y;
                    resField.FieldX.text = (res * ratio).ToString("0");
                    resField.FieldY.text = (res).ToString("0");
                }
                ContextMenuListAction getItem(string name, float res)
                    => new(name, () => setRes(res), _checked: Prefs.Resolution.y == res);

                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    getItem("480p (SD)", 480),
                    getItem("720p (HD)", 720),
                    getItem("1080p (FHD)", 1080),
                    getItem("1440p (QHD)", 1440),
                    getItem("2160p (4K UHD)", 2160),
                    getItem("2880p (5K)", 2880),
                    getItem("4320p (8K UHD)", 4320)
                ), (RectTransform)resActions.Button.transform);
            });

            var fpsField = SpawnForm<FormEntryFloat, float>("Frame Rate (fps)", () => Prefs.FrameRate, x =>
            {
                Prefs.FrameRate = x; PrefsDirty = true;
            });
            var fpsPresets = SpawnForm<FormEntryButton>("Frame Rate Presets");
            fpsPresets.Button.onClick.AddListener(() =>
            {
                void setFPS(float fps)
                {
                    fpsField.Field.text = fps.ToString();
                }
                ContextMenuListAction getItem(string name, float fps)
                    => new(name, () => setFPS(fps), _checked: Prefs.FrameRate == fps);

                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    getItem("24fps (Film)", 24),
                    getItem("25fps (PAL)", 25),
                    getItem("29.97fps (NTSC)", 29.97f),
                    getItem("30fps (Standard SD)", 30),
                    getItem("48fps (Film HD)", 48),
                    getItem("50fps (PAL HD)", 50),
                    getItem("59.94fps (NTSC HD)", 59.94f),
                    getItem("60fps (Standard HD)", 60)
                ), (RectTransform)fpsPresets.Button.transform);
            });
            SpawnForm<FormEntrySpace>();

            var antiAliasingField = SpawnForm<FormEntryDropdown, object>("Anti-Aliasing", () => Prefs.AntiAliasing, a =>
            {
                Prefs.AntiAliasing = (int)a;
            });
            antiAliasingField.ValidValues.Add(0, "None");
            antiAliasingField.ValidValues.Add(2, "2x MSAA");
            antiAliasingField.ValidValues.Add(4, "4x MSAA");
            antiAliasingField.ValidValues.Add(8, "8x MSAA");
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

            vqualField = SpawnForm<FormEntryRange, float>("Video Quality", () => Prefs.VideoQuality * 100, x =>
            {
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

            SpawnForm<FormEntryInt, int>("Audio Bitrate (kbps)", () => Prefs.AudioBitRate, x =>
            {
                Prefs.AudioBitRate = x; PrefsDirty = true;
            });

            SpawnForm<FormEntryHeader>("Other");
            SpawnForm<FormEntryBool, bool>("Open File on Complete", () => Prefs.OpenOnComplete, x =>
            {
                Prefs.OpenOnComplete = x; PrefsDirty = true;
            });

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            UpdateResolutionVisualizer();
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
            IsAnimating = true;

            // FFmpeg process setup
            Stream ffmpegInputStream = null;
            Task ffmpegTask = null;

            Texture2D tex = null;
            RenderTexture rtex = null;
            
            var chartmaker = Behaviors.Chartmaker.Chartmaker.main;
            var loaderPanel = chartmaker.LoaderPanel;

            bool cancelFlag = false;


            try
            {
                InitializeETATracking();

                chartmaker.Loader.SetActive(true);
                loaderPanel.ActionLabel.text = "Rendering...";
                loaderPanel.ProgressBar.value = 0;
                loaderPanel.ProgressLabel.text = "Initializing...";
                loaderPanel.SetCancelButton(() => cancelFlag = true);

                await Task.Delay(300);

                // Pre-calculate constants
                var resolution = Prefs.Resolution;
                var frameRate = Prefs.FrameRate;
                var timeRange = TimeRange;

                float delta = 1f / frameRate;
                int totalFrames = Mathf.CeilToInt((timeRange.y - timeRange.x) * frameRate);
                float camHeight = Mathf.Min(1f, 7f / 4f * resolution.x / resolution.y) * 0.9f;
                float fov = Mathf.Atan2(Mathf.Tan(30f * Mathf.Deg2Rad), camHeight) * 2f * Mathf.Rad2Deg;

                Vector2 crfRange = GetCRFRange(Array.Find(_VideoEncoders, x => x.FfmpegArg == Prefs.VideoEncoder).Format);
                int crf = Mathf.RoundToInt(Mathf.LerpUnclamped(crfRange.x, crfRange.y, Prefs.VideoQuality));

                string videoFormatArg = Prefs.VideoEncoder;
                string audioFormatArg = Prefs.AudioEncoder;
                string extensionArg = ((MediaFormat)Prefs.OutputType).ToString();

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
                    throw new Exception("Failed to create render texture: " + e.Message);
                }

                // Use RGB24 format for direct byte access - no alpha channel needed
                tex = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
                Rect rectConfig = new(0, 0, resolution.x, resolution.y);

                // Setup output path
                string folder = Helper.GetRenderFolder();

                Directory.CreateDirectory(folder);

                string outputPath = Path.Combine(
                    folder,
                    (string.IsNullOrWhiteSpace(OutputPath) ? DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() : OutputPath) + "." + extensionArg);

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
                                    $"-y \"{outputPath}\"";

                UnityEngine.Debug.Log("FFmpeg args: " + ffmpegArgs);

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

                FFmpegProcess = new Process { StartInfo = startInfo };
                FFmpegProcess.Start();
                ffmpegInputStream = FFmpegProcess.StandardInput.BaseStream;

                // Start async task to read FFmpeg output (for debugging/logging)
                ffmpegTask = Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while ((line = FFmpegProcess.StandardError.ReadLine()) != null)
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
                int framePipedIndex = 0;
                int frameYieldIndex = 0;

                loaderPanel.ProgressLabel.text = $"Streaming frames... (0/{totalFrames})";

                ConcurrentQueue<byte[]> frameQueue = new();
                bool rendering = true;
                bool brokenPipe = false;
                Exception pipeError = null;
                long framebufferLimit = 2_000_000_000;
                if (SystemInfo.systemMemorySize > 0) framebufferLimit = Math.Min(
                    framebufferLimit,
                    SystemInfo.systemMemorySize * 1_048_576L // 20% of system's memory
                );

                var pipingThread = new Thread(() =>
                {
                    while (rendering || !frameQueue.IsEmpty)
                    {
                        if (frameQueue.TryDequeue(out var frame))
                        {
                            try
                            {
                                ffmpegInputStream.Write(frame, 0, frame.Length);
                                ffmpegInputStream.Flush();
                                framePipedIndex++;
                            }
                            catch (Exception e)
                            {
                                pipeError = e;
                                brokenPipe = true;
                                rendering = false;
                            }

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
                int frameSize = resolution.x * resolution.y * 3; // RGB24 = 3 bytes per pixel
                byte[] frameBuffer = new byte[frameSize];
                int frameBufferSize = frameBuffer.Length;

                // Pre-calculate thresholds
                int maxFrameCount = (int)(framebufferLimit / frameBufferSize);
                int resumeFrameCount = maxFrameCount * 3 / 4;

                // Main rendering loop
                while (time < timeRange.y && frameIndex < totalFrames)
                {
                    if (frameQueue.Count >= maxFrameCount)
                    {
                        while (frameQueue.Count >= resumeFrameCount)
                        {
                            await Task.Yield();

                            UpdateETAProgress(framePipedIndex, totalFrames);

                            loaderPanel.ActionLabel.text = $"Rendering... ({framePipedIndex} / {totalFrames})";
                            loaderPanel.ProgressLabel.text = _EtaString;
                            loaderPanel.ProgressBar.value = (float)framePipedIndex / totalFrames;
                        }
                        continue;
                    }

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

                    if (FFmpegProcess.HasExited)
                    {
                        rendering = false;
                        throw new Exception("FFmpeg process ended prematurely. Your copy of FFmpeg might not support the selected encoders.");
                    }

                    // Queue frame for FFmpeg
                    frameQueue.Enqueue((byte[])frameBuffer.Clone());

                    frameIndex++;
                    frameYieldIndex++;

                    // Update progress less frequently (by average fps) for performance
                    float averageFrameTime = _RecentFrameTimes.Count > 0
                        ? _RecentFrameTimes.Sum() / _RecentFrameTimes.Count : 0.033f; // fallback to ~30fps
                    float averageFPS = averageFrameTime > 0
                        ? 1f / averageFrameTime : 30f;
                    int yieldInterval = Mathf.Clamp(Mathf.RoundToInt(averageFPS) / 5, 10, 120);

                    if (frameYieldIndex > yieldInterval || frameIndex == totalFrames)
                    {
                        frameYieldIndex = 0;

                        UpdateETAProgress(framePipedIndex, totalFrames);

                        loaderPanel.ActionLabel.text = $"Rendering... ({framePipedIndex} / {totalFrames})";
                        loaderPanel.ProgressLabel.text = _EtaString;
                        loaderPanel.ProgressBar.value = (float)framePipedIndex / totalFrames;

                        await Task.Yield();
                    }
                    
                    if (brokenPipe)
                    {
                        Exception e = new TaskCanceledException($"Broken pipe to FFmpeg - it may have crashed: \n{pipeError.Message} \n\nTry using another configuration?");
                        ThrowRenderModal(e, rtex, tex);
                        throw e;
                    }

                    if (cancelFlag)
                    {
                        rendering = false;
                        throw new TaskCanceledException("Cancelled");
                    }
                }

                // Close the input stream to signal end of video data
                rendering = false;
                pipingThread.Join();

                loaderPanel.ProgressLabel.text = "Finalizing video...";

                // Wait for FFmpeg to finish processing
                if (FFmpegProcess != null && !FFmpegProcess.HasExited)
                {
                    // Wait for FFmpeg to complete, but with timeout
                    bool finished = FFmpegProcess.WaitForExit(30000); // 30 second timeout
                    if (!finished)
                    {
                        UnityEngine.Debug.LogWarning("FFmpeg process timed out, forcing termination");
                        KillFFmpegProcess();
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

                Close();
                chartmaker.Notify("Render completed!");

                if (Prefs.OpenOnComplete && !string.IsNullOrEmpty(outputPath))
                {
                    Application.OpenURL("file://" + outputPath);
                }
            }
            catch (TaskCanceledException)
            {
                transform.Translate(2 * Screen.height * Vector2.up);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);

                // Prevent the error modal from interfering with the scene when rendering 
                // is interrupted via exiting play mode on unity editor
#if UNITY_EDITOR
                if (!Application.isPlaying) return;
#endif

                transform.Translate(2 * Screen.height * Vector2.up);
                ThrowRenderModal(e, rtex, tex);
            }
            finally
            {
                KillFFmpegProcess();

                loaderPanel.SetNoCancelButton();
                chartmaker.Loader.SetActive(false);

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

                chartmaker.Loader.SetActive(false);
            }

            IsAnimating = false;
        }

        private void ThrowRenderModal(Exception e, RenderTexture rtex, Texture tex)
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
            process.WaitForExit();

            output.ExitCode = process.ExitCode;

            process.Dispose();
        
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
        private int lastEtaFrame;
        private float lastEtaUpdateTime;
        private const int ETA_SAMPLE_SIZE = 30; // Number of frames to average for ETA calculation

        // Initialize ETA tracking (add this at the start of RenderRoutine)
        private void InitializeETATracking()
        {
            renderStopwatch = System.Diagnostics.Stopwatch.StartNew();
            _RecentFrameTimes = new Queue<float>(ETA_SAMPLE_SIZE);
            lastEtaUpdateTime = 0f;
            lastEtaFrame = 0;
        }
        
        private void UpdateETAProgress(int currentFrame, int totalFrames)
        {
            float currentTime = (float)renderStopwatch.Elapsed.TotalSeconds;

            if (currentFrame == lastEtaFrame)
            {
                return;
            }
            
            // Track frame time for moving average
            if (_RecentFrameTimes.Count > 0)
            {
                float frameTime = currentTime - lastEtaUpdateTime;
                int frameCount = currentFrame - lastEtaFrame;
                float msPerFrame = frameTime / frameCount;
                _RecentFrameTimes.Enqueue(msPerFrame);

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
            lastEtaFrame = currentFrame;
            
            _EtaString = ETAString(currentFrame, totalFrames, currentTime);
        }

        // Format progress text with ETA information
        private string ETAString(int currentFrame, int totalFrames, float elapsedSeconds)
        {
            float progress = (float)currentFrame / totalFrames;
            
            if (currentFrame < 5 || _RecentFrameTimes.Count == 0)
            {
                // Not enough data for accurate ETA, show basic progress
                return $"{currentFrame} / {totalFrames} | --- fps | --- remaining ";
            }
            
            float averageFrameTime = _RecentFrameTimes.Average();
            
            int remainingFrames = totalFrames - currentFrame;
            float estimatedTimeRemaining = remainingFrames * averageFrameTime;
            
            float currentFPS = _RecentFrameTimes.Count > 0 ? 1f / averageFrameTime : 0f;
            
            string etaText = FormatTimeSpanETA(estimatedTimeRemaining);
            
            return $"{currentFPS:F1} fps | About {etaText} remaining";
        }

        // Helper method to format time spans nicely
        private string FormatTimeSpanETA(float seconds)
        {
            if (seconds < 0) return "---";
            if (seconds > long.MaxValue) return "---";
            
            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            
            if (timeSpan.TotalHours >= 1)
            {
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")}";
            }
            else if (timeSpan.TotalSeconds >= 57.5)
            {
                return $"{(int)Math.Max(timeSpan.TotalMinutes, 1)} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")}";
            }
            else if (timeSpan.TotalSeconds >= 2.5)
            {
                return $"{(int)Math.Round(timeSpan.TotalSeconds / 5) * 5} seconds";
            }
            else
            {
                return $"moments";
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
     
        public string VideoEncoder;
        public string AudioEncoder;
        
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
