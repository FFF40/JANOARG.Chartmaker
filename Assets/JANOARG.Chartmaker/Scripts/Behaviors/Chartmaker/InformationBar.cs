using System;
using System.Collections;
using System.Collections.Generic;
using JANOARG.Chartmaker.Constants;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Modal;
using JANOARG.Chartmaker.UI.Modal.ModalTypes;
using JANOARG.Chartmaker.UI.Themeable;
using JANOARG.Chartmaker.Utils;
using JANOARG.Shared.Data.ChartInfo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class InformationBar : MonoBehaviour
    {
        private static WaitForSeconds _waitForSeconds0 = new WaitForSeconds(0);
        public static  InformationBar main;

        [Header("Objects")]
        public Button SongButton;
        public TMP_Text   SongNameLabel;
        public Button     ChartButton;
        public TMP_Text   ChartNameLabel;
        public GameObject PlayIcon;
        public GameObject PauseIcon;
        public TMP_Text   BeatTimeLabel;
        public TMP_Text   SecondTimeLabel;
        [Space]
        public RectTransform ChartButtonTransform;
        public RectTransform ChartDropdownButton;
        [Space]
        public AudioSource SoundPlayer;
        public AudioClip MetronomeSoundMain;
        public AudioClip MetronomeSoundSub;
        [Space]
        public RectTransform Visualizer;
        public Image          VisualizerBarSample;
        public List<Image>    VisualizerBars;
        public VisualizerMode VisualizerMode;
        [Space]
        public PlayOptionsPanel PlayOptions;

        public float sec { get; private set; }
        public float beat { get; private set; }
        public float barPos { get; private set; }

        public float MetronomeIndex { get; private set; }

        public void Awake()
        {
            main = this;
        }

        public void Start()
        {
            UpdateSongButton();
            UpdateChartButton();
            UpdateButtonActivity();
        }

        public void Update()
        {
            UpdatePlayButton();

            if (Chartmaker.main?.CurrentSong == null)
                return;

            sec = Chartmaker.main.SongSource.timeSamples / (float)Chartmaker.main.SongSource.clip.frequency;
            beat = Chartmaker.main.CurrentSong.Timing.ToBeat(sec);
            barPos = Chartmaker.main.CurrentSong.Timing.ToDividedBeat(sec);


            StartCoroutine(UpdateTimeLabel());

            if (!TimelinePanel.main.isDragged && PlayOptions.MetronomeVolume > 0 && Mathf.Floor(beat) > MetronomeIndex)
                SoundPlayer.PlayOneShot(barPos < 1 ? MetronomeSoundMain : MetronomeSoundSub, PlayOptions.MetronomeVolume);
                
            MetronomeIndex = Mathf.Floor(beat);

            UpdateVisualizer();
        }

        private IEnumerator UpdateTimeLabel()
        {
            SecondTimeLabel.text = Mathf.Floor(sec / 60).ToString("00") + ":" + Mathf.Floor(sec % 60).ToString("00") + "s" + Mathf.Floor(sec * 1000 % 1000).ToString("000");
            BeatTimeLabel.text = beat.ToString("0.000").Replace('.', 'b');

            yield return null;
        }

        public void UpdateButtonActivity() 
        {
            SongButton.interactable = HierarchyPanel.main.CurrentMode != HierarchyMode.PlayableSong;
            ChartButton.interactable = Chartmaker.main?.CurrentChart == null || HierarchyPanel.main.CurrentMode != HierarchyMode.Chart;
        }

        public void UpdateSongButton()
        {
        
            if (Chartmaker.main?.CurrentSong != null)
            {
                SongNameLabel.text = Chartmaker.main.CurrentSong.SongName;
            }
        }

        public void UpdateChartButton()
        {
            Chart chart = Chartmaker.main?.CurrentChart;

            if (chart != null)
            {
                ChartNameLabel.text = chart.DifficultyName + " " + chart.DifficultyLevel;
            
                ChartDropdownButton.sizeDelta = new Vector2(24, ChartDropdownButton.sizeDelta.y);
                ChartDropdownButton.anchoredPosition = new Vector2(333, ChartDropdownButton.anchoredPosition.y);
            
                ChartButtonTransform.gameObject.SetActive(true);
            }
            else 
            {
                ChartNameLabel.text = "Select Chart...";
            
                ChartDropdownButton.sizeDelta = new Vector2(160, ChartDropdownButton.sizeDelta.y);
                ChartDropdownButton.anchoredPosition = new Vector2(174, ChartDropdownButton.anchoredPosition.y);
            
                ChartButtonTransform.gameObject.SetActive(false);
            }
        }

        public void FocusSong()
        {
            TimelinePanel.main.SetTabMode(TimelineMode.Timing);
            HierarchyPanel.main.SetMode(HierarchyMode.PlayableSong);
        }

        public void FocusChart()
        {
            if (Chartmaker.main.CurrentChart == null)
                ShowChartPopup(ChartButtonTransform);
            else
            {
                HierarchyPanel.main.SetMode(HierarchyMode.Chart);
                TimelinePanel.main.SetTabMode(TimelineMode.Lanes);
            }
        }

        public void ShowChartPopup()
        {
            ShowChartPopup(ChartDropdownButton);
        }

        public void ShowChartPopup(RectTransform rt)
        {
            ContextMenuList list = new ContextMenuList();
            foreach(ExternalChartMeta chart in Chartmaker.main?.CurrentSong.Charts!)
            {
                ExternalChartMeta exChart = chart;
                list.Items.Add(new ContextMenuListAction(chart.DifficultyName + " " + chart.DifficultyLevel, () => {
                    StartCoroutine(Chartmaker.main.OpenChartRoutine(exChart));
                }));
            }
            if (list.Items.Count > 0) list.Items.Add(new ContextMenuListSeparator());
            list.Items.Add(new ContextMenuListAction("Create Chart...", () => { ModalHolder.main.Spawn<NewChartModal>(); }));
            ContextMenuHolder.main.OpenRoot(list, rt);
        }
    
        public void UpdatePlayButton()
        {
            if (Chartmaker.main == null)
                return;

            PlayIcon.SetActive(!Chartmaker.main.SongSource.isPlaying);
            PauseIcon.SetActive(Chartmaker.main.SongSource.isPlaying);
        }
    
        public void ToggleSong()
        {
            if (Chartmaker.main.SongSource.isPlaying)
            {
                Chartmaker.main.SongSource.Pause();
            
                if (Chartmaker.Preferences.MaximizeOnPlay)
                {
                    TimelinePanel.main.Restore();
                    HierarchyPanel.main.Restore();
                    InspectorPanel.main.Restore();
                }
            }
            else 
            {
                Chartmaker.main.SongSource.Play();
            
                if (Chartmaker.Preferences.MaximizeOnPlay)
                {
                    TimelinePanel.main.Collapse();
                    HierarchyPanel.main.Collapse();
                    InspectorPanel.main.Collapse();
                }
            }
            UpdatePlayButton();
        }
    

        public void UpdateVisualizer()
        {
            int count = 0;

            Image AddBar()
            {
                Image bar = null;
                if (count < VisualizerBars.Count) 
                    bar = VisualizerBars[count];
                else
                    VisualizerBars.Add(bar = Instantiate(VisualizerBarSample, Visualizer));
            
                count++;
            
                return bar;
            }

            Rect rect = Visualizer.rect;
            Color color = Themer.main.Keys["VisualizerContent"];
            AudioClip clip = Chartmaker.main.SongSource.clip;
            int sampleOffset = (int)(sec * clip.frequency);

            if (VisualizerMode == VisualizerMode.Metronome)
            {
                BPMStop stop = Chartmaker.main.CurrentSong.Timing.GetStop(sec, out _);
                if (stop != null)
                {
                    float sizeX = rect.width / stop.Signature;

                    Image bar = AddBar();         
                    bar.rectTransform.sizeDelta = new Vector2(sizeX, rect.height);
                    bar.rectTransform.anchoredPosition = new Vector2(Mathf.Floor(barPos) * sizeX, 0);
                    bar.color = color * new Color(1, 1, 1, Mathf.Pow(1 - (barPos % 1), 2));

                    Image bar2 = AddBar();
                    bar2.rectTransform.sizeDelta = new Vector2(sizeX, rect.height * bar.color.a);
                    bar2.rectTransform.anchoredPosition = new Vector2(Mathf.Floor(barPos) * sizeX, 0);
                    bar2.color = color * new Color(1, 1, 1, .5f);

                    Image bar3 = AddBar();
                    bar3.rectTransform.sizeDelta = new Vector2(sizeX, 1);
                    bar3.rectTransform.anchoredPosition = new Vector2(Mathf.Floor(barPos) * sizeX, rect.height - 1);
                    bar3.color = color * new Color(1, 1, 1, Mathf.Pow(2 - (barPos % 1) * 2, 2));
                }
            }
            else if (VisualizerMode == VisualizerMode.LoudnessMeter)
            {
                const int fftCount = 2 << 8;
                const float riseTime = .05f, fallTime = .5f;

                float[] dataLeft = new float[fftCount], dataRight = new float[fftCount];
                float[] data = new float[fftCount * clip.channels];
            
                clip.GetData(data, Mathf.Clamp(sampleOffset - fftCount / 2, 0, clip.samples - fftCount));
            
                for (int a = 0; a < data.Length; a++) 
                {
                    switch ((a + sampleOffset) % clip.channels)
                    {
                        case 0:
                            dataLeft[a / clip.channels] = data[a]; break;
                        case 1: 
                            dataRight[a / clip.channels] = data[a]; break;
                    }
                }
            
                KWeight.ApplyKWeightFilter(ref dataLeft, clip.frequency);
            
                // Mono/Stereo audio channel check
                if (clip.channels == 1) 
                    dataRight = dataLeft;
                else 
                    KWeight.ApplyKWeightFilter(ref dataRight, clip.frequency);

                float[] loudness = new float[2];
                
                for (int a = 0; a < fftCount / 2; a++) 
                {
                    loudness[0] += dataLeft[a] * dataLeft[a];
                    loudness[1] += dataRight[a] * dataRight[a];
                }

                int segmentCount = 19;
                float barHeight = (rect.height + 1) / loudness.Length;
                float segmentWidth = (rect.width + 1) / segmentCount;
            
                for (int a = 0; a < loudness.Length; a++) 
                {
                    Image bar = AddBar();
                    float barWidth = bar.rectTransform.sizeDelta.x;
                    bar.rectTransform.sizeDelta = new Vector2(
                        Mathf.Lerp(loudness[a] / fftCount, barWidth, Mathf.Pow(0.01f, Time.deltaTime / 0.3f)), 
                        barHeight - 1
                    );
                    bar.rectTransform.anchoredPosition = new Vector2(0, a * barHeight);
                    bar.color = Color.clear;
                    barWidth = bar.rectTransform.sizeDelta.x;

                    float lufs = (10 * Mathf.Log10(barWidth)) - 0.691f;
                    float width = lufs / segmentCount / 2 + 1;

                    for (int b = 0; b < segmentCount; b++) 
                    {
                        Image segment = AddBar();
                        float barLoudness = (segmentCount - b - 1) * -2;
                        float maxBrightness = barLoudness > -14 ? 1 : .8f;
                    
                        segment.rectTransform.sizeDelta = new Vector2(segmentWidth - 1, barHeight - 1);
                        segment.rectTransform.anchoredPosition = new Vector2(b * segmentWidth, a * barHeight);
                        segment.color = color * new Color(1, 1, 1, Mathf.Clamp((width * segmentCount - b) * 2, 0.1f, 1) * maxBrightness);
                    }
                }
            }
            else if (VisualizerMode == VisualizerMode.FrequencyBars)
            {
                const int barCount = 8;
                const int fftCount = 2 << barCount;
                const float riseTime = .02f, fallTime = .4f;

                float[] fftLeft = new float[fftCount], fftRight = new float[fftCount];
                float[] data = new float[fftCount * clip.channels];
                clip.GetData(data, Mathf.Clamp(sampleOffset - fftCount / 2, 0, clip.samples - fftCount));
                for (int a = 0; a < data.Length; a++) 
                {
                    switch ((a + sampleOffset) % clip.channels)
                    {
                        case 0:
                            fftLeft[a / clip.channels] = data[a]; break;
                        case 1:
                            fftRight[a / clip.channels] = data[a]; break;
                    }
                }
                FFT.Transform(fftLeft, Chartmaker.Preferences.FFTWindow);
            
                if (clip.channels == 1) 
                    fftRight = fftLeft;
                else 
                    FFT.Transform(fftRight, Chartmaker.Preferences.FFTWindow);

                float[] barsLeft = new float[barCount], barsRight = new float[barCount];
            
                for (int a = 0; a < barCount; a++) 
                {
                    for (int i = 1 << a; i < 1 << (a + 1); i++) 
                    {
                        barsLeft[a] += fftLeft[i] * (a + 1);
                        barsRight[a] += fftRight[i] * (a + 1);
                    }
                
                    barsLeft[a] *= Mathf.Pow(1 / (1 - (float)a / barCount), 2) * (a + 1) / fftCount / 25;
                    barsRight[a] *= Mathf.Pow(1 / (1 - (float)a / barCount), 2) * (a + 1) / fftCount / 25;
                }
            
                float sizeX = rect.width / barCount;    
                for (int a = 0; a < barCount; a++) 
                {
                    Image barLeft = AddBar();
                
                    float heightLeft = barLeft.rectTransform.rect.height / rect.height;
                    heightLeft = rect.height * Mathf.Min(Mathf.Clamp(Mathf.Sqrt(barsLeft[a] / (1 << a) / 2), heightLeft - Time.deltaTime / fallTime, heightLeft + Time.deltaTime / riseTime), 1);
                    barLeft.rectTransform.sizeDelta = new Vector2(sizeX, heightLeft);
                    barLeft.rectTransform.anchoredPosition = new Vector2(a * sizeX, 0);
                    barLeft.color = color * new Color(1, 1, 1, .5f);

                    Image barRight = AddBar();
                    float heightRight = barRight.rectTransform.rect.height / rect.height;
                    heightRight = rect.height * Mathf.Min(Mathf.Clamp(Mathf.Sqrt(barsRight[a] / (1 << a) / 2), heightRight - Time.deltaTime / fallTime, heightRight + Time.deltaTime / riseTime), 1);
                    barRight.rectTransform.sizeDelta = new Vector2(sizeX, heightRight);
                    barRight.rectTransform.anchoredPosition = new Vector2(a * sizeX, 0);
                    barRight.color = color * new Color(1, 1, 1, .5f);

                    Image barClip = AddBar();
                    float heightClip = barClip.rectTransform.anchoredPosition.y / rect.height; 
                    float fallClip = barClip.rectTransform.anchoredPosition3D.z;
                    float fallClipTarget = rect.height * (heightClip - Time.deltaTime / fallTime * fallClip);
                    if (heightLeft > fallClipTarget || heightRight > fallClipTarget)
                    {
                        heightClip = Mathf.Min(Mathf.Max(heightLeft, heightRight), rect.height - 1);
                        fallClip = 0;
                    }
                    else
                    {
                        heightClip = Mathf.Max(fallClipTarget, 0);
                        fallClip += Time.deltaTime;
                    }
                    barClip.rectTransform.sizeDelta = new Vector2(sizeX, 1);
                    barClip.rectTransform.anchoredPosition3D = new Vector3(a * sizeX, heightClip, fallClip);
                    barClip.color = color * new Color(1, 1, 1, 1);
                }
            }
            else if (VisualizerMode == VisualizerMode.FrequencyFlame)
            {
                const int fftCount = 1024;
                const float riseTime = .03f, fallTime = .5f;

                float[] fftLeft = new float[fftCount], fftRight = new float[fftCount];
                float[] data = new float[fftCount * clip.channels];
                clip.GetData(data, Mathf.Clamp(sampleOffset - fftCount / 2, 0, clip.samples - fftCount));
                for (int a = 0; a < data.Length; a++) 
                {
                    switch ((a + sampleOffset) % clip.channels)
                    {
                        case 0: 
                            fftLeft[a / clip.channels] = data[a]; break;
                        case 1: 
                            fftRight[a / clip.channels] = data[a]; break;
                    }
                }
                FFT.Transform(fftLeft, Chartmaker.Preferences.FFTWindow);
                if (clip.channels == 1) fftRight = fftLeft;
                else FFT.Transform(fftRight, Chartmaker.Preferences.FFTWindow);


                FrequencyScaling.GetScalingFunctions(Chartmaker.Preferences.FrequencyScale, out var scale, out var unscale);

                float minScale = scale(Chartmaker.Preferences.FrequencyMin);
                float maxScale = scale(Chartmaker.Preferences.FrequencyMax);
             
                for (int a = 0; a < rect.width; a++) 
                {
                
                    float clipPosition = Mathf.Clamp(unscale(Mathf.Lerp(minScale, maxScale, (a + .5f) / rect.width)) / clip.frequency * fftCount, 0, fftCount - 1);
                    int clipPositionFloor = Mathf.FloorToInt(clipPosition);
                    int clipPositionCeil = Mathf.CeilToInt(clipPosition);
                    float factor = (clipPosition + .5f) / fftCount / fftCount * 50;
                
                    Image barL = AddBar();
                    float heightLeft = barL.rectTransform.rect.height / rect.height;
                    float fftLeftVal = Mathf.Sqrt(Mathf.Lerp(fftLeft[clipPositionFloor], fftLeft[clipPositionCeil], clipPosition % 1) * factor);
                    heightLeft = rect.height * Mathf.Min(Mathf.Clamp(fftLeftVal * 0.7f, heightLeft - Time.deltaTime / fallTime, heightLeft + Time.deltaTime / riseTime), 1);
                    barL.rectTransform.sizeDelta = new Vector2(1, heightLeft);
                    barL.rectTransform.anchoredPosition = new Vector2(a, 0);
                    barL.color = color * new Color(1, 1, 1, .5f);

                    Image barRight = AddBar();
                    float heightRight = barRight.rectTransform.rect.height / rect.height;
                    float fftRightVal = Mathf.Sqrt(Mathf.Lerp(fftRight[clipPositionFloor], fftRight[clipPositionCeil], clipPosition % 1) * factor);
                    heightRight = rect.height * Mathf.Min(Mathf.Clamp(fftRightVal * 0.7f, heightRight - Time.deltaTime / fallTime, heightRight + Time.deltaTime / riseTime), 1);
                    barRight.rectTransform.sizeDelta = new Vector2(1, heightRight);
                    barRight.rectTransform.anchoredPosition = new Vector2(a, 0);
                    barRight.color = color * new Color(1, 1, 1, .5f);

                    Image barC = AddBar();
                    float heightC = barC.rectTransform.anchoredPosition.y / rect.height; 
                    float fallC = barC.rectTransform.anchoredPosition3D.z;
                    float fallCTarget = rect.height * (heightC - Time.deltaTime / fallTime * fallC);
                    if (heightLeft > fallCTarget || heightRight > fallCTarget)
                    {
                        heightC = Mathf.Min(Mathf.Max(heightLeft, heightRight), rect.height - 1);
                        fallC = 0;
                    }
                    else
                    {
                        heightC = Mathf.Max(fallCTarget, 0);
                        fallC += Time.deltaTime;
                    }
                    barC.rectTransform.sizeDelta = new Vector2(1, 1);
                    barC.rectTransform.anchoredPosition3D = new Vector3(a, heightC, fallC);
                    barC.color = color * new Color(1, 1, 1, 1);
                }
            }
            else if (VisualizerMode == VisualizerMode.SoundWaves)
            {
                int dataCount = (int)rect.width + 1;

                float[] dataLeft = new float[dataCount], dataRight = new float[dataCount];
                float[] data = new float[dataCount * clip.channels];
            
                clip.GetData(data, Mathf.Clamp(sampleOffset - dataCount / 2, 0, clip.samples - dataCount));
            
                for (int a = 0; a < data.Length; a++) 
                {
                    switch ((a + sampleOffset) % clip.channels)
                    {
                        case 0: 
                            dataLeft[a / clip.channels] = data[a]; break;
                        case 1: 
                            dataRight[a / clip.channels] = data[a]; break;
                    }
                }
                if (clip.channels == 1) dataRight = dataLeft;

                for (int a = 0; a < dataCount - 1; a++) 
                {
                    Image barLeft = AddBar();
                    float minLeft = Mathf.Min(dataLeft[a], dataLeft[a + 1]);
                    float maxLeft = Mathf.Max(dataLeft[a], dataLeft[a + 1]);
                    barLeft.rectTransform.sizeDelta = new Vector2(1, (maxLeft - minLeft) / 2 * (rect.height - 1) + 1);
                    barLeft.rectTransform.anchoredPosition = new Vector2(a, (minLeft / 2 + .5f) * (rect.height - 1));
                    barLeft.color = color * new Color(1, 1, 1, .65f);

                    Image barRight = AddBar();
                    float minRight = Mathf.Min(dataRight[a], dataRight[a + 1]);
                    float maxRight = Mathf.Max(dataRight[a], dataRight[a + 1]);
                    barRight.rectTransform.sizeDelta = new Vector2(1, (maxRight - minRight) / 2 * (rect.height - 1) + 1);
                    barRight.rectTransform.anchoredPosition = new Vector2(a, (minRight / 2 + .5f) * (rect.height - 1));
                    barRight.color = color * new Color(1, 1, 1, .65f);
                }
            }
            else if (VisualizerMode == VisualizerMode.Vectorscope)
            {
                int dataCount = 1024;

                float[] dataLeft = new float[dataCount], dataRight = new float[dataCount];
                float[] data = new float[dataCount * clip.channels];
            
                clip.GetData(data, Mathf.Clamp(sampleOffset - dataCount / 2, 0, clip.samples - dataCount));
            
                for (int a = 0; a < data.Length; a++) 
                {
                    switch ((a + sampleOffset) % clip.channels)
                    {
                        case 0: 
                            dataLeft[a / clip.channels] = data[a]; break;
                        case 1: 
                            dataRight[a / clip.channels] = data[a]; break;
                    }
                }
            
                if (clip.channels == 1) 
                    dataRight = dataLeft;

                float opacity = 1;
                Dictionary<int, Image> images = new();
                Rect testRect = new Rect(0, 0, rect.width - 1, rect.height - 1);

                Image barH = AddBar();
                barH.rectTransform.sizeDelta = new (rect.width, 1);
                barH.rectTransform.anchoredPosition = new (0, rect.height / 2);
                barH.color = color * new Color(1, 1, 1, .25f);

                Image barV = AddBar();
                barV.rectTransform.sizeDelta = new (1, rect.height);
                barV.rectTransform.anchoredPosition = new (rect.width / 2, 0);
                barV.color = color * new Color(1, 1, 1, .25f);

                for (int a = 0; a < dataCount - 1; a++) 
                {
                    Vector2 pos = Quaternion.Euler(0, 0, 45) * new Vector2(dataLeft[a], dataRight[a]);
                    pos = rect.width * pos / 2.82842712f + rect.size / 2;
                    if (!testRect.Contains(pos)) continue;
                    int hash = (int)pos.y * (int)rect.width + (int)pos.x;
                    if (images.TryGetValue(hash, out Image dot)) 
                    {
                        dot.color = color * new Color(1, 1, 1, 1 - (1 - opacity) * (1 - dot.color.a));
                    }
                    else 
                    {
                        dot = AddBar();
                        dot.rectTransform.sizeDelta = new Vector2(1, 1);
                        dot.rectTransform.anchoredPosition = pos;
                        dot.color = color * new Color(1, 1, 1, opacity);
                        images.Add(hash, dot);
                    }
                    opacity *= 0.995f;
                }
            }
            else if (VisualizerMode == VisualizerMode.PitchLines)
            {
                const int fftCount = 1 << 8;

                float[] fftLeft = new float[fftCount], fftRight = new float[fftCount];
                float[] data = new float[fftCount * clip.channels];
            
                clip.GetData(data, Mathf.Clamp(sampleOffset - fftCount / 2, 0, clip.samples - fftCount));
            
                for (int a = 0; a < data.Length; a++) 
                {
                    switch ((a + sampleOffset) % clip.channels)
                    {
                        case 0: 
                            fftLeft[a / clip.channels] = data[a]; break;
                        case 1: 
                            fftRight[a / clip.channels] = data[a]; break;
                    }
                }
            
                FFT.Transform(fftLeft, Chartmaker.Preferences.FFTWindow);
            
                if (clip.channels == 1)
                    fftRight = fftLeft;
                else 
                    FFT.Transform(fftRight, Chartmaker.Preferences.FFTWindow);

                float[] notes = new float[12];
                for (int a = 0; a < fftCount / 2; a++) 
                {
                    float freq = (a + 1f) / fftCount * Chartmaker.main.CurrentSong.Clip.frequency;
                    int note = (Mathf.RoundToInt(Mathf.Log(freq / 440, 2) * 12) % 12 + 12) % 12;
                    float mul = freq / Chartmaker.main.CurrentSong.Clip.frequency * FrequencyScaling.AWeight(freq);
                    notes[note] += Mathf.Max(fftLeft[a] * mul, fftRight[a] * mul) / 2;
                }
            
                float sizeX = rect.width / 12;  
                int target = 0;  
                float[] sems = {1, 3, 6, 8, 10};
            
                for (int a = 0; a < 12; a++) 
                {
                    Image barLeft = AddBar();
                
                    barLeft.rectTransform.sizeDelta = new Vector2(sizeX, rect.height * (Array.IndexOf(sems, a) >= 0 ? .5f : 1));
                    barLeft.rectTransform.anchoredPosition = new Vector2(a * sizeX, 0);
                    barLeft.color = color * new Color(1, 1, 1, .25f) * notes[a];
                
                    if (notes[target] < notes[a]) 
                        target = a;
                }
            
                Image freqBar = AddBar();
            
                freqBar.rectTransform.sizeDelta = new Vector2(sizeX, 2);
                freqBar.rectTransform.anchoredPosition = new Vector2(target * sizeX, 0);
                freqBar.color = color * new Color(1, 1, 1);
            }

            while (VisualizerBars.Count > count)
            {
                Destroy(VisualizerBars[count].gameObject);
                VisualizerBars.RemoveAt(count);
            }
        }

        public void SwitchVisualizer()
        {
            VisualizerMode = (VisualizerMode)(((int)VisualizerMode + 1) % ((int)VisualizerMode.None + 1));

            foreach (var bar in VisualizerBars) Destroy(bar.gameObject);
            VisualizerBars.Clear();
        }

        public void SwitchVisualizer(VisualizerMode mode)
        {
            VisualizerMode = mode;
        }

        public void ShowVisualizerMenu()
        {
            ContextMenuHolder.main.OpenRoot(new ContextMenuList(GetVisualizerMenu()), Visualizer, offset: new (2, -4));
        }

        public ContextMenuListItem[] GetVisualizerMenu() 
        {
            ContextMenuListAction VisItem (string name, VisualizerMode mode) 
                => new (name, () => SwitchVisualizer(mode), _checked: VisualizerMode == mode);

            return new ContextMenuListItem[] {
                VisItem("Metronome", VisualizerMode.Metronome),
                new ContextMenuListSeparator(),
                VisItem("Loudness Meter", VisualizerMode.LoudnessMeter),
                VisItem("Classic Bars", VisualizerMode.FrequencyBars),
                VisItem("Classic Flame", VisualizerMode.FrequencyFlame),
                VisItem("Oscilloscope", VisualizerMode.SoundWaves),
                VisItem("Vectorscope", VisualizerMode.Vectorscope),
                new ContextMenuListSeparator(),
                VisItem("Glowing Piano", VisualizerMode.PitchLines),
                new ContextMenuListSeparator(),
                VisItem("Disabled", VisualizerMode.None)
            };
        }
    }

    public enum VisualizerMode
    {
        Metronome,
        LoudnessMeter,
        FrequencyBars,
        FrequencyFlame,
        SoundWaves,
        Vectorscope,
        PitchLines,
        None,
    }
}