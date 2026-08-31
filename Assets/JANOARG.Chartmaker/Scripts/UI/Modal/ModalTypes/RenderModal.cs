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
using JANOARG.Chartmaker.Data;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Form;
using JANOARG.Chartmaker.UI.Form.FormTypes;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using UnityEditor;

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

        // Encoders expose wildly different preset scales (named strings, p1-p7,
        // numeric 0-13, ...) under different flag names (-preset, -cpu-used, -quality,
        // -speed). This collapses all of that down to one fixed scale for the UI;
        // each RenderFormatItem maps these 5 steps onto whatever its own encoder
        // actually accepts.
        public enum EncoderSpeed
        {
            Fastest,
            Fast,
            Balanced,
            Slow,
            Slowest
        }

        struct RenderFormatItem
        {
            public MediaFormat Format;
            public string FfmpegArg;
            public string Description;
            public MediaFormat[] Compatibility;
            public string PresetArg;                            // null => encoder has no speed control
            public Dictionary<EncoderSpeed, string> Presets;
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

        // Shared per-scale preset tables, reused across encoders that share the
        // same underlying -preset/-cpu-used/etc. naming scheme.
        private static readonly Dictionary<EncoderSpeed, string> _x26xPresets = new()
        {
            { EncoderSpeed.Fastest,  "ultrafast" },
            { EncoderSpeed.Fast,     "faster" },
            { EncoderSpeed.Balanced, "medium" },
            { EncoderSpeed.Slow,     "slow" },
            { EncoderSpeed.Slowest,  "veryslow" },
        };
        private static readonly Dictionary<EncoderSpeed, string> _nvencPresets = new()
        {
            { EncoderSpeed.Fastest,  "p1" },
            { EncoderSpeed.Fast,     "p2" },
            { EncoderSpeed.Balanced, "p4" },
            { EncoderSpeed.Slow,     "p6" },
            { EncoderSpeed.Slowest,  "p7" },
        };
        private static readonly Dictionary<EncoderSpeed, string> _qsvPresets = new()
        {
            { EncoderSpeed.Fastest,  "veryfast" },
            { EncoderSpeed.Fast,     "fast" },
            { EncoderSpeed.Balanced, "medium" },
            { EncoderSpeed.Slow,     "slow" },
            { EncoderSpeed.Slowest,  "veryslow" },
        };
        private static readonly Dictionary<EncoderSpeed, string> _amfPresets = new()
        {
            { EncoderSpeed.Fastest,  "speed" },
            { EncoderSpeed.Fast,     "speed" },
            { EncoderSpeed.Balanced, "balanced" },
            { EncoderSpeed.Slow,     "quality" },
            { EncoderSpeed.Slowest,  "quality" },
        };
        private static readonly Dictionary<EncoderSpeed, string> _vpxPresets = new()
        {
            { EncoderSpeed.Fastest,  "5" },
            { EncoderSpeed.Fast,     "4" },
            { EncoderSpeed.Balanced, "2" },
            { EncoderSpeed.Slow,     "1" },
            { EncoderSpeed.Slowest,  "0" },
        };
        private static readonly Dictionary<EncoderSpeed, string> _aomCpuUsedPresets = new()
        {
            { EncoderSpeed.Fastest,  "8" },
            { EncoderSpeed.Fast,     "6" },
            { EncoderSpeed.Balanced, "4" },
            { EncoderSpeed.Slow,     "2" },
            { EncoderSpeed.Slowest,  "0" },
        };
        private static readonly Dictionary<EncoderSpeed, string> _rav1ePresets = new()
        {
            { EncoderSpeed.Fastest,  "10" },
            { EncoderSpeed.Fast,     "8" },
            { EncoderSpeed.Balanced, "5" },
            { EncoderSpeed.Slow,     "2" },
            { EncoderSpeed.Slowest,  "0" },
        };
        private static readonly Dictionary<EncoderSpeed, string> _svtav1Presets = new()
        {
            { EncoderSpeed.Fastest,  "12" },
            { EncoderSpeed.Fast,     "10" },
            { EncoderSpeed.Balanced, "6" },
            { EncoderSpeed.Slow,     "3" },
            { EncoderSpeed.Slowest,  "0" },
        };

        private readonly RenderFormatItem[] _VideoEncoders =
        {
            // H.264
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "libx264",
                Description = "Software",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv },
                PresetArg = "-preset", Presets = _x26xPresets
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_amf",
                Description = "AMD",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv },
                PresetArg = "-quality", Presets = _amfPresets
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_nvenc",
                Description = "NVIDIA",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv },
                PresetArg = "-preset", Presets = _nvencPresets
            },
            new() {
                Format = MediaFormat.h264,
                FfmpegArg = "h264_qsv",
                Description = "Intel QSV",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov, MediaFormat.flv },
                PresetArg = "-preset", Presets = _qsvPresets
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
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov },
                PresetArg = "-preset", Presets = _x26xPresets
            },
            new() {
                Format = MediaFormat.h265,
                FfmpegArg = "hevc_amf",
                Description = "AMD",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov },
                PresetArg = "-quality", Presets = _amfPresets
            },
            new() {
                Format = MediaFormat.h265,
                FfmpegArg = "hevc_nvenc",
                Description = "NVIDIA",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov },
                PresetArg = "-preset", Presets = _nvencPresets
            },
            new() {
                Format = MediaFormat.h265,
                FfmpegArg = "hevc_qsv",
                Description = "Intel QSV",
                Compatibility = new[] { MediaFormat.mp4, MediaFormat.mkv, MediaFormat.mov },
                PresetArg = "-preset", Presets = _qsvPresets
            },

            // VPX
            new() {
                Format = MediaFormat.vp8,
                FfmpegArg = "libvpx",
                Description = "Software",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv },
                PresetArg = "-cpu-used", Presets = _vpxPresets
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
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv },
                PresetArg = "-cpu-used", Presets = _aomCpuUsedPresets
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
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv },
                PresetArg = "-preset", Presets = _qsvPresets
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
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv, MediaFormat.mp4 },
                PresetArg = "-cpu-used", Presets = _aomCpuUsedPresets
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "librav1e",
                Description = "rav1e",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv, MediaFormat.mp4 },
                PresetArg = "-speed", Presets = _rav1ePresets
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "libsvtav1",
                Description = "SVT",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv , MediaFormat.mp4},
                PresetArg = "-preset", Presets = _svtav1Presets
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_nvenc",
                Description = "NVIDIA",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv , MediaFormat.mp4},
                PresetArg = "-preset", Presets = _nvencPresets
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_qsv",
                Description = "Intel QSV",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv , MediaFormat.mp4},
                PresetArg = "-preset", Presets = _qsvPresets
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_amf",
                Description = "AMD",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv , MediaFormat.mp4},
                PresetArg = "-quality", Presets = _amfPresets
            },
            new() {
                Format = MediaFormat.av1,
                FfmpegArg = "av1_vaapi",
                Description = "VA-API",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv , MediaFormat.mp4}
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
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv, MediaFormat.mp4 }
            },
            new() {
                Format = MediaFormat.opus,
                FfmpegArg = "opus",
                Description = "Legacy",
                Compatibility = new[] { MediaFormat.webm, MediaFormat.mkv, MediaFormat.mp4 }
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
            MediaFormat.av1   => new Vector2(63, 0),
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
            FormEntryDropdown speedField = null;

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

            FormEntryRange vqualField = null;
            FormEntryFloat vbitrateField = null;
            FormEntryRange crfField = null;

            FormEntryInt abitrateField = null;

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

                RenderFormatItem encoder = Array.Find(_VideoEncoders, x => x.FfmpegArg == Prefs.VideoEncoder);

                Vector2 encoderCrfRange = GetCRFRange(encoder.Format);
                crfField!.Range.minValue = Mathf.Min(encoderCrfRange.x, encoderCrfRange.y);
                crfField!.Range.maxValue = Mathf.Max(encoderCrfRange.x, encoderCrfRange.y);
                crfField!.SetValue(Mathf.Clamp(crfField.CurrentValue, crfField.Range.minValue, crfField.Range.maxValue));

                speedField!.gameObject.SetActive(encoder.PresetArg != null);
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
                    getItem("3:2", 3 / 2f),

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


            speedField = SpawnForm<FormEntryDropdown, object>("Quality Preset", () => (EncoderSpeed)Prefs.EncoderSpeed, v =>
            {
                Prefs.EncoderSpeed = (int)(EncoderSpeed)v; PrefsDirty = true;
            });
            speedField.TargetEnum(typeof(EncoderSpeed));
            speedField.gameObject.SetActive(Array.Find(_VideoEncoders, x => x.FfmpegArg == Prefs.VideoEncoder).PresetArg != null);


            Vector2 crfRange;

            var videoOptions = SpawnForm<FormEntryDropdown, object>("Video Quality", () => Prefs.VideoQualityMode, o =>
            {
                Prefs.VideoQualityMode = (VideoQualityMode)o;

                crfRange = GetCRFRange(Array.Find(_VideoEncoders, x => x.FfmpegArg == Prefs.VideoEncoder).Format);
                if (Prefs.VideoQualityMode == VideoQualityMode.AdaptiveBitrateCRF)
                {
                    crfField!.SetValue(Mathf.RoundToInt(Mathf.Round(Mathf.LerpUnclamped(crfRange.x, crfRange.y, Prefs.VideoQuality))));
                    crfField.Reset();
                }
                else
                {
                    vqualField!.SetValue(Mathf.RoundToInt(Mathf.InverseLerp(crfRange.x, crfRange.y, Prefs.VideoCrf) * 100));
                    vqualField.Reset();
                }

                f_updateVideoQualityMode();
            });
            void f_updateVideoQualityMode()
            {
                crfField!.gameObject.SetActive(Prefs.VideoQualityMode == VideoQualityMode.AdaptiveBitrateCRF);
                vqualField!.gameObject.SetActive(Prefs.VideoQualityMode == VideoQualityMode.AdaptiveBitrate);
                vbitrateField!.gameObject.SetActive(Prefs.VideoQualityMode == VideoQualityMode.FixedBitrate);
            }

            videoOptions.ValidValues.Add(VideoQualityMode.Auto,               "Automatic");
            videoOptions.ValidValues.Add(VideoQualityMode.FixedBitrate,       "Fixed Bitrate");
            videoOptions.ValidValues.Add(VideoQualityMode.AdaptiveBitrate,    "Adaptive Bitrate");
            videoOptions.ValidValues.Add(VideoQualityMode.AdaptiveBitrateCRF, "Adaptive Bitrate (CRF)");

            crfField = SpawnForm<FormEntryRange, float>("", () => Prefs.VideoCrf, v =>
            {
                Prefs.VideoCrf = Mathf.RoundToInt(v); PrefsDirty = true;
            });
            Vector2 initialCrfRange = GetCRFRange(Array.Find(_VideoEncoders, x => x.FfmpegArg == Prefs.VideoEncoder).Format);
            crfField.Range.minValue = Mathf.Min(initialCrfRange.x, initialCrfRange.y);
            crfField.Range.maxValue = Mathf.Max(initialCrfRange.x, initialCrfRange.y);
            crfField.Range.wholeNumbers = true;

            vqualField = SpawnForm<FormEntryRange, float>("", () => Prefs.VideoQuality * 100, x =>
            {
                Prefs.VideoQuality = x / 100; PrefsDirty = true;
            });
            vqualField.Range.maxValue = 100; vqualField.Range.wholeNumbers = true;

            vbitrateField = SpawnForm<FormEntryFloat, float>("", () => Prefs.VideoBitRate, v =>
            {
                Prefs.VideoBitRate = v;
            });
            f_updateVideoQualityMode();
            
            var audioOptions = SpawnForm<FormEntryDropdown, object>("Audio Quality", () => Prefs.AudioQualityMode, o =>
            {
                Prefs.AudioQualityMode = (AudioQualityMode)o;

                f_updateAudioQualityMode();
            });
            audioOptions.ValidValues.Add(AudioQualityMode.Auto,               "Automatic");
            audioOptions.ValidValues.Add(AudioQualityMode.FixedBitrate,       "Fixed Bitrate");
            void f_updateAudioQualityMode()
            {
                abitrateField!.gameObject.SetActive(Prefs.AudioQualityMode == AudioQualityMode.FixedBitrate);
            }
            abitrateField = SpawnForm<FormEntryInt, int>("", () => Prefs.AudioBitRate, x =>
            {
                Prefs.AudioBitRate = x; PrefsDirty = true;
            });
            f_updateAudioQualityMode();
            


            SpawnForm<FormEntryHeader>("Audio");


            SpawnForm<FormEntryBool, bool>("Include Hit SFX", () => Prefs.AddHitSfx, x =>
            {
                Prefs.AddHitSfx = x; PrefsDirty = true;
            });
            var hitSfxVolField = SpawnForm<FormEntryRange, float>("Hit SFX Volume", () => Prefs.HitSfxVolume, x =>
            {
                Prefs.HitSfxVolume = x; PrefsDirty = true;
            });
            hitSfxVolField.Range.maxValue = 200; hitSfxVolField.Range.wholeNumbers = true;


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

        // Hit SFX overlay
        //
        // The hit sound clips (Normal/Catch/Flick) live under a Resources folder, so in
        // a player build they are packed into the engine's asset archive and no longer
        // exist as files on disk. Loading them as AudioClips and pulling the samples out
        // resolves them the same way chart playback does, in-editor and in a build alike.
        // Must be called from the main thread -- Resources.Load is not thread-safe.
        private static PcmAudio LoadHitSfx(string clipName)
        {
            var clip = Resources.Load<AudioClip>("Sounds/" + clipName);
            if (clip == null) throw new Exception($"Hit SFX clip not found in Resources: Sounds/{clipName}");

            // Clips imported without Preload Audio Data start unloaded, and GetData would
            // hand back silence for them. These assets have Load In Background off, so the
            // load is synchronous and the data is ready when LoadAudioData returns.
            if (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData())
                throw new Exception($"Failed to load audio data for hit SFX clip: Sounds/{clipName}");

            float[] floatSamples = new float[clip.samples * clip.channels];
            if (!clip.GetData(floatSamples, 0))
                throw new Exception($"Failed to read samples from hit SFX clip: Sounds/{clipName}");

            short[] samples = new short[floatSamples.Length];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = (short)Math.Clamp((int)Math.Round(floatSamples[i] * short.MaxValue), short.MinValue, short.MaxValue);

            return new PcmAudio { Samples = samples, Channels = clip.channels, SampleRate = clip.frequency };
        }

        private struct HitSfxEvent
        {
            public float Time; // seconds, relative to the render's output timeline (timeRange.x)
            public HitObject.HitType Type;
            public bool Flickable;
        }

        // Minimal 16-bit PCM WAV reader/writer for the hit-sfx mixing pass below.
        // Both files involved are ffmpeg's own -c:a pcm_s16le output, so a full RIFF
        // parser isn't needed -- just enough chunk-walking to find "fmt " and "data".
        private struct PcmAudio
        {
            public short[] Samples; // interleaved
            public int Channels;
            public int SampleRate;
        }

        private static PcmAudio ReadWavPcm16(string path)
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            reader.ReadBytes(4); // "RIFF"
            reader.ReadInt32();  // file size
            reader.ReadBytes(4); // "WAVE"

            int channels = 0, sampleRate = 0, bitsPerSample = 0;
            short[] samples = null;

            while (stream.Position + 8 <= stream.Length)
            {
                string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                int chunkSize = reader.ReadInt32();
                long chunkEnd = stream.Position + chunkSize;

                if (chunkId == "fmt ")
                {
                    reader.ReadInt16(); // audio format
                    channels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    reader.ReadInt32(); // byte rate
                    reader.ReadInt16(); // block align
                    bitsPerSample = reader.ReadInt16();
                }
                else if (chunkId == "data")
                {
                    if (bitsPerSample != 16) throw new Exception($"Expected 16-bit PCM WAV, got {bitsPerSample}-bit: {path}");
                    samples = new short[chunkSize / 2];
                    for (int i = 0; i < samples.Length; i++) samples[i] = reader.ReadInt16();
                }

                stream.Position = chunkEnd + (chunkEnd % 2); // chunks are word-aligned
            }

            if (samples == null) throw new Exception("WAV file has no data chunk: " + path);
            return new PcmAudio { Samples = samples, Channels = channels, SampleRate = sampleRate };
        }

        private static void WriteWavPcm16(string path, PcmAudio audio)
        {
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);

            int byteRate = audio.SampleRate * audio.Channels * 2;
            int blockAlign = audio.Channels * 2;
            int dataSize = audio.Samples.Length * 2;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)audio.Channels);
            writer.Write(audio.SampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            foreach (short s in audio.Samples) writer.Write(s);
        }

        // Linear-interpolation resample, used only for sfx clips whose native sample
        // rate doesn't match the mix's target rate (Flick.wav is 48kHz, the others are
        // 44.1kHz). Cheap, and only ever run once per distinct clip -- not per hit.
        private static PcmAudio ResampleIfNeeded(PcmAudio audio, int targetRate)
        {
            if (audio.SampleRate == targetRate) return audio;

            int channels = audio.Channels;
            int srcFrames = audio.Samples.Length / channels;
            int dstFrames = (int)Math.Round(srcFrames * (double)targetRate / audio.SampleRate);
            short[] outSamples = new short[dstFrames * channels];

            for (int i = 0; i < dstFrames; i++)
            {
                double srcPos = i * (double)audio.SampleRate / targetRate;
                int i0 = Math.Min((int)srcPos, srcFrames - 1);
                int i1 = Math.Min(i0 + 1, srcFrames - 1);
                double frac = srcPos - i0;
                for (int c = 0; c < channels; c++)
                {
                    short s0 = audio.Samples[i0 * channels + c];
                    short s1 = audio.Samples[i1 * channels + c];
                    outSamples[i * channels + c] = (short)(s0 + (s1 - s0) * frac);
                }
            }

            return new PcmAudio { Samples = outSamples, Channels = channels, SampleRate = targetRate };
        }

        // Additively mixes sfxAudio into destination at timeSeconds, scaled by volume,
        // clamping to avoid int16 overflow. destination must already match sfxAudio's
        // sample rate/channel count (resample beforehand via ResampleIfNeeded).
        private static void MixInPlace(PcmAudio destination, PcmAudio sfxAudio, float timeSeconds, float volume)
        {
            int channels = destination.Channels;
            int destStartFrame = (int)Math.Round(timeSeconds * destination.SampleRate);
            int sfxFrames = sfxAudio.Samples.Length / channels;
            int destFrames = destination.Samples.Length / channels;

            for (int frame = 0; frame < sfxFrames; frame++)
            {
                int destFrame = destStartFrame + frame;
                if (destFrame < 0) continue;
                if (destFrame >= destFrames) break;

                for (int c = 0; c < channels; c++)
                {
                    int destIdx = destFrame * channels + c;
                    int mixed = destination.Samples[destIdx] + (int)Math.Round(sfxAudio.Samples[frame * channels + c] * volume);
                    destination.Samples[destIdx] = (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
                }
            }
        }

        private string _EtaString;

        private Queue<float> _RecentFrameTimes;
        public async Task RenderRoutine()
        {
            IsAnimating = true;

            // FFmpeg process setup
            Stream ffmpegInputStream = null;
            Task ffmpegTask = null;
            string songPcmPath = null;
            string mixedAudioPath = null;

            Texture2D tex = null;
            RenderTexture rtex = null;
            
            var chartmaker = Behaviors.Chartmaker.Chartmaker.main;
            var loaderPanel = chartmaker.LoaderPanel;

            bool cancelFlag = false;

            // Every progress update in the capture loop costs a full Unity frame, since
            // an awaited continuation resumes on the next player loop pass -- and with
            // vsync that frame blocks on vblank for ~16.7ms of idle time. The loader
            // covers the editor for the duration, so there is nothing on screen worth
            // tearing protection; dropping vsync makes those yields cheap enough to
            // afford. Captured on the way in rather than read from prefs so whatever the
            // user actually had set is what gets restored.
            int previousVSyncCount = QualitySettings.vSyncCount;
            QualitySettings.vSyncCount = 0;

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

                // Most video encoders (including libx264) require even width/height
                // since yuv420p subsamples chroma by half in each dimension -- an odd
                // value makes libx264 fail to open the encoder at all, which otherwise
                // just surfaces as the generic "process ended prematurely" error.
                if (resolution.x % 2 != 0 || resolution.y % 2 != 0)
                {
                    throw new Exception($"Resolution {resolution.x}x{resolution.y} has an odd width or height. Most video encoders require both to be even numbers -- adjust the Resolution field and try again.");
                }

                float delta = 1f / frameRate;
                int totalFrames = Mathf.CeilToInt((timeRange.y - timeRange.x) * frameRate);
                float camHeight = Mathf.Min(1f, 7f / 4f * resolution.x / resolution.y) * 0.9f;
                float fov = Mathf.Atan2(Mathf.Tan(30f * Mathf.Deg2Rad), camHeight) * 2f * Mathf.Rad2Deg;

                RenderFormatItem currentEncoder = Array.Find(_VideoEncoders, x => x.FfmpegArg == Prefs.VideoEncoder);

                Vector2 crfRange = GetCRFRange(currentEncoder.Format);
                string videoQualityOptions = Prefs.VideoQualityMode switch {
                    VideoQualityMode.FixedBitrate => $"-b:v {Prefs.VideoBitRate}k",
                    VideoQualityMode.AdaptiveBitrate => $"-crf {Mathf.RoundToInt(Mathf.LerpUnclamped(crfRange.x, crfRange.y, Prefs.VideoQuality))}",
                    VideoQualityMode.AdaptiveBitrateCRF => $"-crf {Prefs.VideoCrf}",
                    _ => "",
                };
                string audioQualityOptions = Prefs.AudioQualityMode switch
                {
                    AudioQualityMode.FixedBitrate => $"-b:a {Prefs.AudioBitRate}k",
                    _ => "",
                };

                string presetOption = currentEncoder.PresetArg != null
                    ? $"{currentEncoder.PresetArg} {currentEncoder.Presets[(EncoderSpeed)Prefs.EncoderSpeed]} "
                    : "";

                string videoFormatArg = Prefs.VideoEncoder;
                string audioFormatArg = Prefs.AudioEncoder;
                string extensionArg = ((MediaFormat)Prefs.OutputType).ToString();

                // Setup camera and render texture
                int originalAntiAliasing;
                try
                {
                    rtex = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
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
                string audioPath = Path.Combine(Path.GetDirectoryName(chartmaker.CurrentSongPath)!, chartmaker.CurrentSong.ClipPath);

                float leadIn = timeRange.x < 0 ? -timeRange.x : 0f;
                float audioStart = leadIn > 0 ? 0f : timeRange.x;
                float renderDuration = timeRange.y - timeRange.x;
                var invariant = System.Globalization.CultureInfo.InvariantCulture;

                // If hit SFX are enabled, pre-mix the song + hit sounds into a single
                // plain WAV *before* video capture starts, so the video-encode pass
                // below ends up with one already-correct audio file and no per-hit
                // work of its own. Mixing the hit overlay inside the same ffmpeg
                // process that's encoding the live video pipe both desyncs the sfx
                // timing and competes with Unity's render/readback loop for CPU on
                // dense charts, stuttering the captured video.
                //
                // The hits themselves are summed straight into the decoded PCM here in
                // C# rather than via ffmpeg's asplit/adelay/amix filters: that filter
                // graph needs one node per hit occurrence and its cost scales with the
                // chart's hit count, whereas direct summing only costs
                // O(hits * clip length) regardless of how long the render is.
                if (Prefs.AddHitSfx && Prefs.HitSfxVolume > 0 && chartmaker.CurrentChart != null)
                {
                    var timing = chartmaker.CurrentSong.Timing;
                    List<HitSfxEvent> hitSfxEvents = new();
                    foreach (var lane in chartmaker.CurrentChart.Lanes)
                    {
                        foreach (var hit in lane.Objects)
                        {
                            float outputTime = timing.ToSeconds(hit.Offset) - timeRange.x;
                            if (outputTime < 0 || outputTime > renderDuration) continue;
                            hitSfxEvents.Add(new HitSfxEvent { Time = outputTime, Type = hit.Type, Flickable = hit.Flickable });
                        }
                    }

                    if (hitSfxEvents.Count > 0)
                    {
                        const int mixSampleRate = 44100;
                        const int mixChannels = 2;

                        loaderPanel.ProgressLabel.text = "Decoding song audio...";

                        int leadInMs = (int)Math.Round(leadIn * 1000);
                        string leadInFilter = leadIn > 0 ? $"-af \"adelay={leadInMs}|{leadInMs}\" " : "";
                        songPcmPath = Path.Combine(folder, "songpcm_" + Guid.NewGuid().ToString("N") + ".wav");
                        string decodeArgs = $"-ss {audioStart.ToString(invariant)} -t {(timeRange.y - audioStart).ToString(invariant)} -i \"{audioPath}\" " +
                                            leadInFilter +
                                            $"-ar {mixSampleRate} -ac {mixChannels} -c:a pcm_s16le -t {renderDuration.ToString(invariant)} " +
                                            $"-y \"{songPcmPath}\"";

                        // Track progress by parsing ffmpeg's own "time=HH:MM:SS.ss"
                        // progress lines. The callback fires on ffmpeg's stderr-reading
                        // background thread so it only ever writes a plain float; the
                        // polling loop below runs on the main thread, like the rest of
                        // this routine, and is what's allowed to touch loaderPanel.
                        float decodeElapsed = 0f;
                        var timeRegex = new Regex(@"time=(\d+):(\d+):(\d+(?:\.\d+)?)");
                        Task<ProcessOutput> decodeTask = ffmpeg(decodeArgs, line =>
                        {
                            Match m = timeRegex.Match(line);
                            if (m.Success)
                            {
                                decodeElapsed = float.Parse(m.Groups[1].Value, invariant) * 3600
                                              + float.Parse(m.Groups[2].Value, invariant) * 60
                                              + float.Parse(m.Groups[3].Value, invariant);
                            }
                        });

                        while (!decodeTask.IsCompleted)
                        {
                            loaderPanel.ProgressBar.value = renderDuration > 0 ? Mathf.Clamp01(decodeElapsed / renderDuration) : 0f;
                            loaderPanel.ProgressLabel.text = $"Decoding song audio... ({decodeElapsed:0.0}s / {renderDuration:0.0}s)";
                            await Task.Yield();
                        }
                        var decodeResult = await decodeTask;

                        if (decodeResult.ExitCode != 0 || !File.Exists(songPcmPath))
                            throw new Exception("Failed to decode song audio for hit SFX mixing:\n" + decodeResult.Output);

                        loaderPanel.ProgressBar.value = 0;
                        loaderPanel.ProgressLabel.text = $"Applying hit SFX... ({hitSfxEvents.Count} hits)";
                        await Task.Yield();

                        PcmAudio mixBuffer = ReadWavPcm16(songPcmPath);
                        if (mixBuffer.Channels != mixChannels)
                            throw new Exception($"Expected {mixChannels}-channel decoded song audio, got {mixBuffer.Channels}");

                        var sfxCache = new Dictionary<string, PcmAudio>();
                        PcmAudio GetSfx(string clipName)
                        {
                            if (!sfxCache.TryGetValue(clipName, out var audio))
                            {
                                audio = ResampleIfNeeded(LoadHitSfx(clipName), mixSampleRate);
                                // MixInPlace walks the sfx with the destination's channel
                                // count, so a layout mismatch would read past the clip.
                                if (audio.Channels != mixChannels)
                                    throw new Exception($"Expected a {mixChannels}-channel hit SFX clip, got {audio.Channels}: Sounds/{clipName}");
                                sfxCache[clipName] = audio;
                            }
                            return audio;
                        }

                        float hitVolume = Math.Max(0f, Prefs.HitSfxVolume) / 100f;

                        int i = 0;
                        foreach (var hit in hitSfxEvents)
                        {
                            MixInPlace(mixBuffer, GetSfx(hit.Type == HitObject.HitType.Catch ? "Catch Hit" : "Normal Hit"), hit.Time, hitVolume);
                            // Flickable is a modifier on top of the hit type, not a
                            // replacement, so its sfx layers over the base hit sound.
                            if (hit.Flickable) MixInPlace(mixBuffer, GetSfx("Flick"), hit.Time, hitVolume);
                            i++;

                            // TODO this should show the progress to the user
                            // if (i % 10 == 0)
                            // {
                            //     loaderPanel.ProgressBar.value = i / (float)hitSfxEvents.Count;
                            //     loaderPanel.ProgressLabel.text = $"Applying hit SFX... ({i}/{hitSfxEvents.Count})";
                            // }
                        }

                        mixedAudioPath = Path.Combine(folder, "hitsfxmix_" + Guid.NewGuid().ToString("N") + ".wav");
                        WriteWavPcm16(mixedAudioPath, mixBuffer);

                        File.Delete(songPcmPath);
                        songPcmPath = null;

                        loaderPanel.ProgressLabel.text = "Initializing...";
                    }
                }

                // The pre-mixed file is already trimmed and lead-in padded, so it needs
                // no -ss/-t/-itsoffset of its own -- just map it straight in.
                string audioInputArgs = mixedAudioPath != null
                    ? $"-i \"{mixedAudioPath}\" "
                    : (leadIn > 0 ? $"-itsoffset {leadIn.ToString(invariant)} " : "") +
                      $"-ss {audioStart.ToString(invariant)} -t {renderDuration.ToString(invariant)} -i \"{audioPath}\" ";

                string ffmpegArgs = $"-f rawvideo -pix_fmt rgb24 -s {resolution.x}x{resolution.y} -r {frameRate} -i pipe:0 " +
                                    audioInputArgs +
                                    $"-map 0:v -map 1:a " +
                                    $"-vcodec {videoFormatArg} -vf format=rgb24 -pix_fmt yuv420p -acodec {audioFormatArg} " +
                                    presetOption +
                                    $"{videoQualityOptions} {audioQualityOptions} " +
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

                float frameOrigin = timeRange.x;
                int frameIndex = 0;
                int framePipedIndex = 0;
                int frameYieldIndex = 0;

                // Yield pacing. Handing the main thread back to Unity costs a whole
                // player loop, so progress updates are bought with render time. Rather
                // than guess that price it gets measured: the cost of one yield and the
                // cost of one captured frame, both smoothed, feed YieldInterval() below.
                // This is the only knob -- the share of render time spent on yields.
                const double yieldOverheadBudget = 0.05;
                var captureTimer = System.Diagnostics.Stopwatch.StartNew();
                var yieldTimer = new System.Diagnostics.Stopwatch();
                var encoderWaitTimer = new System.Diagnostics.Stopwatch();
                double avgCaptureMs = 0;
                double avgYieldMs = 0;
                double avgWaitMs = 0;
                double lastPacingLogSec = 0;

                loaderPanel.ProgressLabel.text = $"Streaming frames... (0/{totalFrames})";

                ConcurrentQueue<byte[]> frameQueue = new();
                bool rendering = true;
                bool brokenPipe = false;
                Exception pipeError = null;
                // Frame buffers are recycled through a fixed pool rather than allocated
                // per frame. A buffer belongs to exactly one stage at a time: the free
                // list, the main thread filling it, the queue, or the piping thread
                // writing it out. The piping thread only hands a buffer back once its
                // bytes have reached the pipe, so the pool size doubles as the queue
                // depth -- the producer stalls on an empty free list, and a buffer can
                // never be refilled while a queued frame still points at it.
                int frameSize = resolution.x * resolution.y * 3; // RGB24 = 3 bytes per pixel

                // Enough slack to ride out encoder hiccups without pinning gigabytes:
                // 16 buffers is about 100 MB at 1080p. Larger resolutions shrink the
                // pool to stay inside the memory budget, down to a floor of two so the
                // readback and the pipe can still overlap.
                long framebufferLimit = 2_000_000_000;
                if (SystemInfo.systemMemorySize > 0) framebufferLimit = Math.Min(
                    framebufferLimit,
                    SystemInfo.systemMemorySize * 1_048_576L / 5 // 20% of system's memory
                );
                int poolSize = (int)Math.Clamp(framebufferLimit / frameSize, 2, 16);

                ConcurrentQueue<byte[]> freeBuffers = new();
                for (int i = 0; i < poolSize; i++) freeBuffers.Enqueue(new byte[frameSize]);

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
                                // Only safe to recycle once the bytes are in the pipe.
                                freeBuffers.Enqueue(frame);
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


                // Async GPU readback hides PCI transfer latency by overlapping
                // readback of frame N with scene update + render of frame N+1.
                // Falls back to synchronous ReadPixels on APIs that don't support it
                // (OpenGL ES — Android builds).
                bool useAsyncReadback = SystemInfo.supportsAsyncGPUReadback;

                // Shared helpers used by both paths.
                void UpdateProgress()
                {
                    UpdateETAProgress(framePipedIndex, totalFrames);
                    loaderPanel.ActionLabel.text = $"Rendering... ({framePipedIndex} / {totalFrames})";
                    loaderPanel.ProgressLabel.text = _EtaString;
                    loaderPanel.ProgressBar.value = (float)framePipedIndex / totalFrames;
                }

                static double Smooth(double average, double sample)
                    => average <= 0 ? sample : average + 0.2 * (sample - average);

                // Captured frames to run between progress updates, from the measured
                // cost of a yield (C) and of a captured frame (W). Holding yields to a
                // share p of total time means p = C / (N*W + C), so N = (C/W)(1-p)/p.
                //
                // Substituting back, the interval between updates comes out at C/p with
                // W cancelling: how responsive the UI can be depends on what a player
                // loop costs, not on how fast the render is. Tightening N alone only
                // trades throughput away at an exchange rate C sets.
                //
                // The fallback applies until both costs have a sample.
                int YieldInterval()
                {
                    if (avgCaptureMs <= 0 || avgYieldMs <= 0) return 10;
                    return (int)Math.Clamp(Math.Round(
                        avgYieldMs / avgCaptureMs * (1 - yieldOverheadBudget) / yieldOverheadBudget), 1, 240);
                }

                // Reported every few seconds rather than only at the end, so the pacing
                // can be read off a render that gets cancelled part way through.
                void LogPacing()
                {
                    UnityEngine.Debug.Log(
                        $"Render yield pacing: player loop {avgYieldMs:F1}ms, capture {avgCaptureMs:F1}ms, " +
                        $"encoder wait {avgWaitMs:F1}ms, interval {YieldInterval()} frames, " +
                        $"progress every {(avgYieldMs / yieldOverheadBudget):F0}ms");
                }

                // Folds the work done since the last yield into the frame-cost estimate,
                // gives Unity its player loop, and times what that loop cost.
                async Task YieldToPlayerLoop()
                {
                    if (frameYieldIndex > 0)
                    {
                        avgCaptureMs = Smooth(avgCaptureMs, captureTimer.Elapsed.TotalMilliseconds / frameYieldIndex);
                        avgWaitMs = Smooth(avgWaitMs, encoderWaitTimer.Elapsed.TotalMilliseconds / frameYieldIndex);
                    }
                    encoderWaitTimer.Reset();
                    frameYieldIndex = 0;

                    UpdateProgress();

                    yieldTimer.Restart();
                    await Task.Yield();
                    avgYieldMs = Smooth(avgYieldMs, yieldTimer.Elapsed.TotalMilliseconds);

                    if (renderStopwatch.Elapsed.TotalSeconds - lastPacingLogSec >= 5)
                    {
                        lastPacingLogSec = renderStopwatch.Elapsed.TotalSeconds;
                        LogPacing();
                    }

                    captureTimer.Restart();
                }

                void UpdateScene(int idx)
                {
                    float time = (float)(frameOrigin + idx / (double)frameRate);
                    float audioTime = Mathf.Clamp(time, 0f, songSource.clip.length);
                    songSource.time = audioTime;
                    float sec  = time >= 0f ? audioTime : time;
                    float beat = chartmaker.CurrentSong.Timing.ToBeat(sec);
                    playerView.UpdateObjects(sec, beat);
                }

                void CheckErrors()
                {
                    if (FFmpegProcess.HasExited)
                    {
                        rendering = false;
                        throw new Exception("FFmpeg process ended prematurely. Your copy of FFmpeg might not support the selected encoders.");
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

                // Backpressure: the pool is the only source of frame buffers, so waiting
                // for one to come free is what keeps the producer in step with ffmpeg.
                // Exactly one buffer is rented per loop iteration, and the main thread is
                // the only renter, so a free buffer observed here is still free below.
                async Task WaitForQueueAsync()
                {
                    if (!freeBuffers.IsEmpty) return;

                    // Blocking on the encoder is not capture work. Counting it as such
                    // overstates the cost of a frame, which drags the derived yield
                    // interval down, and hides whether the render is limited by the GPU
                    // or by ffmpeg.
                    captureTimer.Stop();
                    encoderWaitTimer.Start();

                    while (freeBuffers.IsEmpty)
                    {
                        await Task.Yield();
                        UpdateProgress();
                        // A stalled encoder returns no buffers, so this is where a
                        // cancel or an ffmpeg crash has to be noticed -- otherwise the
                        // wait never ends.
                        CheckErrors();
                    }

                    encoderWaitTimer.Stop();
                    captureTimer.Start();
                }

                byte[] RentBuffer()
                {
                    if (!freeBuffers.TryDequeue(out var buffer))
                        throw new Exception("Frame buffer pool exhausted — WaitForQueueAsync should have blocked before this point.");
                    return buffer;
                }

                int stride = resolution.x * 3;

                // Staging buffer: NativeArray data is copied out here on the main thread
                // (NativeArray becomes invalid after the next Request call), then the
                // vertical flip runs on a worker via Task.Run.
                byte[] staging = new byte[resolution.x * resolution.y * 3];
                Task pendingFlipTask = null;

                void ScheduleFlip(NativeArray<byte> data)
                {
                    byte[] frameBuffer = RentBuffer();
                    data.CopyTo(staging);
                    pendingFlipTask = Task.Run(() =>
                    {
                        int h = resolution.y;
                        for (int y = 0; y < h; y++)
                            Buffer.BlockCopy(staging, (h - 1 - y) * stride,
                                             frameBuffer, y * stride, stride);
                        frameQueue.Enqueue(frameBuffer);
                    });
                }


                void FlipAndEnqueueManaged(byte[] src)
                {
                    byte[] frameBuffer = RentBuffer();
                    for (int y = 0; y < resolution.y; y++)
                        Buffer.BlockCopy(src, (resolution.y - 1 - y) * stride,
                                         frameBuffer, y * stride, stride);
                    frameQueue.Enqueue(frameBuffer);
                }

                if (useAsyncReadback)
                {
                    // Pipelined async path:
                    // Collect frame N's readback → render frame N+1 → issue readback → repeat.
                    // Collect comes before render so the RT is not overwritten before
                    // the previous readback is drained.
                    AsyncGPUReadbackRequest pendingRequest = default;
                    bool hasPending = false;

                    while (frameIndex < totalFrames || hasPending)
                    {
                        await WaitForQueueAsync();

                        // Collect previous frame's readback BEFORE rendering the next
                        // frame — rendering overwrites the RT, so we must drain the
                        // pending readback first to avoid capturing the wrong content.
                        if (hasPending)
                        {
                            AsyncGPUReadback.WaitAllRequests();
                            // Wait for previous flip task before reusing staging buffer.
                            pendingFlipTask?.Wait();

                            if (pendingRequest.hasError)
                            {
                                // Fall back to sync for this frame.
                                // ReadPixels reads the active target; without this it hits the
                                // backbuffer, which is only legal inside the drawing phase.
                                RenderTexture.active = rtex;
                                tex.ReadPixels(rectConfig, 0, 0);
                                FlipAndEnqueueManaged(tex.GetRawTextureData());
                            }
                            else
                            {
                                // Copy NativeArray → staging on main thread, schedule flip on worker.
                                ScheduleFlip(pendingRequest.GetData<byte>());
                            }
                            hasPending = false;
                        }

                        if (frameIndex < totalFrames)
                        {
                            // Render next frame into the RT now that the previous
                            // readback has been collected.
                            UpdateScene(frameIndex);
                            RenderTexture.active = rtex;
                            _Camera.Render();

                            // Issue readback for the frame we just rendered.
                            pendingRequest = AsyncGPUReadback.Request(rtex, 0, GraphicsFormat.R8G8B8_UNorm);
                            hasPending = true;
                            frameIndex++;
                            frameYieldIndex++;
                        }

                        CheckErrors();

                        if (frameYieldIndex >= YieldInterval() || frameIndex == totalFrames)
                        {
                            await YieldToPlayerLoop();
                        }
                    }
                }
                else
                {
                    // Synchronous fallback path (OpenGL ES / unsupported APIs).
                    while (frameIndex < totalFrames)
                    {
                        await WaitForQueueAsync();

                        UpdateScene(frameIndex);
                        RenderTexture.active = rtex;
                        _Camera.Render();

                        tex.ReadPixels(rectConfig, 0, 0);
                        FlipAndEnqueueManaged(tex.GetRawTextureData());

                        CheckErrors();

                        frameIndex++;
                        frameYieldIndex++;

                        if (frameYieldIndex >= YieldInterval() || frameIndex == totalFrames)
                        {
                            await YieldToPlayerLoop();
                        }
                    }
                }

                // Ensure the last flip task has enqueued its frame before signalling done.
                pendingFlipTask?.Wait();

                // Close the input stream to signal end of video data
                rendering = false;
                pipingThread.Join();

                loaderPanel.ProgressLabel.text = "Finalizing video...";

                LogPacing();

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
                QualitySettings.vSyncCount = previousVSyncCount;

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

                if (songPcmPath != null && File.Exists(songPcmPath))
                {
                    File.Delete(songPcmPath);
                }
                if (mixedAudioPath != null && File.Exists(mixedAudioPath))
                {
                    File.Delete(mixedAudioPath);
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
    
    public enum VideoQualityMode
    {
        Auto,
        FixedBitrate,
        AdaptiveBitrate,
        AdaptiveBitrateCRF,
    }
    
    public enum AudioQualityMode
    {
        Auto,
        FixedBitrate,
    }

    public class RenderPrefs 
    {

        public string FFmpegPath;
        public int    OutputType;

        public Vector2Int Resolution   = new(1024, 800);
        public float      FrameRate    = 30;

        public int              EncoderSpeed = (int)RenderModal.EncoderSpeed.Balanced;
        public VideoQualityMode VideoQualityMode;
        public float            VideoBitRate = 3200;
        public float            VideoQuality = 0.6f;
        public int              VideoCrf;
        public AudioQualityMode AudioQualityMode;
        public int              AudioBitRate = 128;
     
        public string VideoEncoder;
        public string AudioEncoder;
        
        public bool OpenOnComplete = true;
        
        public int AntiAliasing;



        public bool  AddHitSfx    = true;
        public float HitSfxVolume = 60;

        public void Load(Storage storage)
        {
            FFmpegPath = storage.Get("RD:FFmpegPath", FFmpegPath);
            OutputType = storage.Get("RD:OutputType", OutputType);

            Resolution.x = storage.Get("RD:Resolution.X", Resolution.x);
            Resolution.y = storage.Get("RD:Resolution.Y", Resolution.y);
           
            FrameRate    = storage.Get("RD:FrameRate", FrameRate);
            
            EncoderSpeed      = storage.Get("RD:EncoderSpeed", EncoderSpeed);
            VideoQualityMode  = storage.Get("RD:VideoQualityMode", VideoQualityMode);
            VideoQuality      = storage.Get("RD:VideoQuality", VideoQuality);
            VideoBitRate      = storage.Get("RD:VideoBitRate", VideoBitRate);
            VideoCrf          = storage.Get("RD:CrfVal", VideoCrf);
            AudioQualityMode  = storage.Get("RD:AudioQualityMode", AudioQualityMode);
            AudioBitRate = storage.Get("RD:AudioBitRate", AudioBitRate);
            
            VideoEncoder = storage.Get("RD:VideoEncoder", VideoEncoder);
            AudioEncoder = storage.Get("RD:AudioEncoder", AudioEncoder);
            
            OpenOnComplete  = storage.Get("RD:OpenOnComplete", OpenOnComplete);

            AntiAliasing = storage.Get("RD:AntiAliasing", AntiAliasing);

            AddHitSfx    = storage.Get("RD:AddHitSfx", AddHitSfx);
            HitSfxVolume = storage.Get("RD:HitSfxVolume", HitSfxVolume);
        }

        public void Save(Storage storage)
        {
            storage.Set("RD:FFmpegPath", FFmpegPath);
            storage.Set("RD:OutputType", OutputType);

            storage.Set("RD:Resolution.X", Resolution.x);
            storage.Set("RD:Resolution.Y", Resolution.y);
         
            storage.Set("RD:FrameRate", FrameRate);
          
            storage.Set("RD:EncoderSpeed", EncoderSpeed);
            storage.Set("RD:VideoQualityMode", VideoQualityMode);
            storage.Set("RD:VideoQuality", VideoQuality);
            storage.Set("RD:VideoBitRate", VideoBitRate);
            storage.Set("RD:CrfVal", VideoCrf);
            storage.Set("RD:AudioQualityMode", AudioQualityMode);
            storage.Set("RD:AudioBitRate", AudioBitRate);
          
            storage.Set("RD:VideoEncoder", VideoEncoder);
            storage.Set("RD:AudioEncoder", AudioEncoder);
            
            storage.Set("RD:OpenOnComplete", OpenOnComplete);

            storage.Set("RD:AntiAliasing", AntiAliasing);

            storage.Set("RD:AddHitSfx", AddHitSfx);
            storage.Set("RD:HitSfxVolume", HitSfxVolume);
        }
    }

    public class ProcessOutput 
    {
        public string Output = "";
        public int    ExitCode;
    }
}
