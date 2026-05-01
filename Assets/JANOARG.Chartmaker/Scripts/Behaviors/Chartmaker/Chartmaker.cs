using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using JANOARG.Chartmaker.Constants;
using JANOARG.Chartmaker.Data;
using JANOARG.Chartmaker.Data.Chartmaker;
using JANOARG.Chartmaker.Data.Chartmaker.Actions;
using JANOARG.Chartmaker.UI;
using JANOARG.Chartmaker.UI.Modal;
using JANOARG.Chartmaker.UI.Modal.ModalTypes;
using JANOARG.Chartmaker.UI.NativeUI;
using JANOARG.Chartmaker.UI.Themeable;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using JANOARG.Shared.Data.Chartmaker;
using JANOARG.Shared.Data.Files;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class Chartmaker : MonoBehaviour
    {
        public static Chartmaker main;
        public Canvas ChartmakerCanvas;

        public string CurrentSongPath;
        public string CurrentChartPath;
        [Space]
        public AudioSource SongSource;
        [Space]
        public NavigationBar NavBar;
        public RectTransform NavBarItemHolder;
        public RectTransform InfoBarHolder;
        public RectTransform TimelineHolder;
        public RectTransform InspectorHolder;
        public RectTransform PickerHolder;
        public RectTransform HierarchyHolder;
        public RectTransform MainViewHolder;
        public RectTransform PlayerViewHolder;
        public GameObject    HomeBackground;
        [Space]
        public TMP_Text NotificationLabel;
        public CanvasGroup NotificationText;
        public CanvasGroup NotificationBox;
        public float       NotificationTime;
        public float       NotificationFlashTime;
        [Space]
        public GameObject Loader;
        public LoaderPanel LoaderPanel;
        public bool        IsDirty;

        public PlayableSong CurrentSong { get; private set; } = null;
        public Chart CurrentChart { get; private set; } = null;
        public ExternalChartMeta CurrentChartMeta { get; private set; } = null;

        public object            ClipboardItem;
        public ChartmakerHistory History = new();

        public static Storage         PreferencesStorage;
        public        Storage         KeybindingsStorage;
        public        Storage         RecentSongsStorage;
        public static ChartmakerPrefs Preferences = new();
        [Space]
        public Themer Themer;

        private Task ActiveTask;

        bool lastPlayed;

        public void Awake()
        {
            main = this;
            KeybindingsStorage = new("cm_keys");
            RecentSongsStorage = new("cm_recent");
            Preferences.Load(PreferencesStorage);
            Themer.InitTheme();
        }

        public void Start()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            ModalHolder.main.Spawn<HomeModal>();
            Application.wantsToQuit += QuitCheck;
            InformationBar.main.PlayOptions.Init();
            TimelinePanel.main.Options.Init();
            if (Preferences.AutoUpdateCheck) VersionCheckerModal.InitFetch(true);
            SetEditorActive(false);
            
            UnityEngine.Debug.Log($"Checking for scaling mismatch: {ChartmakerCanvas.scaleFactor} == {Preferences.InterfaceScaling}");
            if (!Mathf.Approximately(ChartmakerCanvas.scaleFactor, Preferences.InterfaceScaling))
            {
                ChartmakerCanvas.scaleFactor = Preferences.InterfaceScaling;
            }
            
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // I hate modern android, wtf is this
            void RequestStoragePermission()
            {
                bool HasManageStoragePermission()
                {
                    AndroidJavaClass environmentClass = new AndroidJavaClass("android.os.Environment");
                    return environmentClass.CallStatic<bool>("isExternalStorageManager");
                }
                if (!HasManageStoragePermission())
                {
                    // MANAGE_EXTERNAL_STORAGE can't be requested via Unity's Permission API
                    // It must be requested via Android's Settings intent
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                        "android.settings.MANAGE_APP_ALL_FILES_ACCESS_PERMISSION");

                    AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
                    AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", 
                        "package:" + Application.identifier);
                    intent.Call<AndroidJavaObject>("setData", uri);

                    activity.Call("startActivity", intent);
                }
            }
            
            RequestStoragePermission();
            #endif
        }

        public void Update()
        {
            NotificationText.alpha = NotificationTime;
            NotificationBox.alpha = NotificationFlashTime / .5f;
            NotificationTime -= Time.deltaTime;
            NotificationFlashTime -= Time.deltaTime;

            if (Preferences.SaveOnPlay && SongSource.isPlaying && !lastPlayed && IsDirty && ActiveTask?.IsCompleted != false)
            {
                StartSaveRoutine();
            }
            lastPlayed = SongSource.isPlaying;

            if (HomeBackground.activeSelf)
            {
                float lerp = 1 - Mathf.Pow(0.9f, Time.deltaTime);
                Camera camera = PlayerView.main.MainCamera;
                camera.fieldOfView = Mathf.Lerp(
                    camera.fieldOfView, 
                    60, lerp
                );
                RenderSettings.fogColor = camera.backgroundColor = Color.Lerp(
                    camera.backgroundColor, 
                    new (0, 0.05f, 0.15f), lerp
                );
                camera.transform.SetPositionAndRotation(Vector3.Lerp(
                    camera.transform.position,
                    Vector3.zero, lerp
                ), Quaternion.Lerp(
                    camera.transform.rotation, 
                    Quaternion.identity, lerp
                ));
            }
        }

        public void SetEditorActive(bool value)
        {
            NavBar.EditButton.gameObject.SetActive(value);
            InfoBarHolder.gameObject.SetActive(value);
            TimelineHolder.gameObject.SetActive(value);
            InspectorHolder.gameObject.SetActive(value);
            PickerHolder.gameObject.SetActive(value);
            HierarchyHolder.gameObject.SetActive(value);
            PlayerViewHolder.gameObject.SetActive(value);
            HomeBackground.SetActive(!value);
        }

        public void OpenSongModal() 
        {
            FileModal dialogModal = ModalHolder.main.Spawn<FileModal>();

            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
            {
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            }
            dialogModal.AcceptedTypes = new List<FileModalFileType> {
                new("JANOARG Playable Song file", "japs"),
                new("All files"),
            };
            dialogModal.HeaderLabel.text = "Select a Playable Song...";
            dialogModal.SelectLabel.text = "Open";
            dialogModal.OnSelect.AddListener(() => {
                LoaderPanel.SetNoSong();
                StartCoroutine(OpenSongRoutine(dialogModal.SelectedEntry.Path));
            });
        }

        public void OpenSong(string path)
        {
            CurrentChart = null;
            CurrentChartMeta = null;
            CurrentChartPath = "";
            CurrentSong = JAPSDecoder.Decode(File.ReadAllText(path));
            CurrentSongPath = Path.GetFullPath(path);
        }

        public Task OpenSongAsync(string path)
        {
            return Task.Run(() => OpenSong(path));
        }

        public void RemoveFromRecent(int index) 
        {
            List<RecentSong> list = new(RecentSongsStorage.Get("List", new RecentSong[] {}));
            list.RemoveAt(index);
            RecentSongsStorage.Set("List", list.ToArray());
            RecentSongsStorage.Save();
        }

        public void AddToRecent()
        {
            List<RecentSong> list = new(RecentSongsStorage.Get("List", new RecentSong[] {}));
            int index = list.FindIndex(x => x.Path.ToLowerInvariant() == CurrentSongPath.ToLowerInvariant());
            if (index == 0 && list[0].SongName == CurrentSong.SongName && list[0].SongArtist == CurrentSong.SongArtist) return;
            else if (index >= 0) list.RemoveAt(index);
            list.Insert(0, new RecentSong {
                Path = CurrentSongPath,
                IconPath = Path.Combine(Path.GetDirectoryName(CurrentSongPath)!, CurrentSong.Cover.IconTarget),
                SongName = CurrentSong.SongName,
                SongArtist = CurrentSong.SongArtist,
                BackgroundColor = CurrentSong.BackgroundColor,
                InterfaceColor = CurrentSong.InterfaceColor,
            });
            RecentSongsStorage.Set("List", list.ToArray());
            RecentSongsStorage.Save();
        }

        public Task AddToRecentAsync()
        {
            return Task.Run(AddToRecent);
        }
    
        public IEnumerator OpenSongRoutine(string path) {
            if (ActiveTask?.IsCompleted == false) yield break;

            if (CurrentSong != null) 
            {
                DirtyModal(() => { 
                    CloseSong(); StartCoroutine(OpenSongRoutine(path));
                });
                yield break;
            }

            Loader.SetActive(true);
            LoaderPanel.ActionLabel.text = "Loading Playable Song...";
            LoaderPanel.ProgressBar.value = 0;

            LoaderPanel.ProgressLabel.text = "Initializing...";
            yield return new WaitForSeconds(0.5f);

            LoaderPanel.ProgressLabel.text = "Loading .japs file...";

            ActiveTask = OpenSongAsync(path);
            yield return new WaitUntil(() => ActiveTask.IsCompleted);
            if (!ActiveTask.IsCompletedSuccessfully) 
            {
                Loader.SetActive(false);
                DialogModal dialogModal = ModalHolder.main.Spawn<DialogModal>();
                dialogModal.SetDialog("Parsing Error", ActiveTask.Exception!.Message, new string[] {"Ok"}, _ => {});
                yield break;
            }

            ActiveTask = AddToRecentAsync();
            yield return new WaitUntil(() => ActiveTask.IsCompleted);

            LoaderPanel.SetSong(CurrentSong);
            LoaderPanel.ProgressLabel.text = "Loading audio file...";
            LoaderPanel.ProgressBar.value = .2f;

            UnityWebRequest stream = UnityWebRequestMultimedia.GetAudioClip("file://" + Path.Combine(Path.GetDirectoryName(path)!, CurrentSong!.ClipPath).Replace("+", "%2B"), AudioType.UNKNOWN);
            UnityEngine.Debug.Log(stream.url);
            stream.SendWebRequest();
            while (!stream.isDone) 
            {
                LoaderPanel.ProgressLabel.text = $"Loading audio file... ({stream.downloadProgress:P})";
                LoaderPanel.ProgressBar.value = .2f + stream.downloadProgress * 0.4f;
            }

            if (stream.result != UnityWebRequest.Result.Success)
            {
                Loader.SetActive(false);
                DialogModal dialogModal = ModalHolder.main.Spawn<DialogModal>();
                dialogModal.SetDialog("Fetch Error", "Couldn't fetch the audio file!\n" + stream.error, new string[] {"Ok"}, _ => {});
                yield break;
            }
            else
            {
                try
                {
                    SongSource.clip = CurrentSong.Clip = DownloadHandlerAudioClip.GetContent(stream);
                }
                catch (Exception e)
                {
                    Loader.SetActive(false);
                    DialogModal dialogModal = ModalHolder.main.Spawn<DialogModal>();
                    dialogModal.SetDialog("Fetch Error", "Couldn't fetch the audio file!\n" + e.Message, new string[] {"Ok"}, _ => {});
                    yield break;
                }
            }

            LoaderPanel.ProgressLabel.text = "Loading song cover images...";
        
            for (int i = 0; i < CurrentSong.Cover.Layers.Count; i++)
            {
                CoverLayer layer = CurrentSong.Cover.Layers[i];
                bool isError = false;
                string error = "";

                string coverPath = Path.Combine(Path.GetDirectoryName(path)!, layer.Target);
                if (!File.Exists(coverPath))
                {
                    error = "The target file does not exist in song folder. Are you trying to delete it?";
                    isError = true;
                    goto layerLoadEnd;
                }

                ActiveTask = File.ReadAllBytesAsync(coverPath);
                yield return new WaitUntil(() => ActiveTask.IsCompleted);
                if (!ActiveTask.IsCompletedSuccessfully) 
                {
                    error = ActiveTask.Exception!.Message;
                    isError = true;
                    goto layerLoadEnd;
                }

                Texture2D texture = new (1, 1);
                texture.LoadImage(((Task<byte[]>)ActiveTask).Result);
                layer.Texture = texture;

                // I tried to use a try-catch block but id didn't go well with yield statements
                layerLoadEnd:
                if (isError) 
                {
                    DialogModal dialogModal = ModalHolder.main.Spawn<DialogModal>();
                    IEnumerator ModalSetParent()
                    {
                        yield return null;
                        dialogModal.transform.SetParent(Loader.transform);
                    }
                    StartCoroutine(ModalSetParent());
                    int choice = 0;
                    dialogModal.SetDialog("Error", 
                        "Cover Layer \"" + layer.Target + "\" failed to load:\n" + error + "\n\nWhat would you like to do?", 
                        new[] {"Cancel Loading", "", "Remove Layer", "Try Again"}, 
                        x => choice = x);
                    yield return new WaitUntil(() => !dialogModal);
                    switch (choice)
                    {
                        // Cancel loading
                        case 0:
                            Loader.SetActive(false);
                            yield break;
                        // Remove layer
                        case 2:
                            CurrentSong.Cover.Layers.RemoveAt(i);
                            InspectorPanel.main.IsCoverDirty = false;
                            i--;

                            break;
                        // Try again
                        case 3:
                            i--;

                            break;
                    }
                }
            
                LoaderPanel.ProgressLabel.text = $"Loading song cover images... ({i}/{CurrentSong.Cover.Layers.Count})";
                LoaderPanel.ProgressBar.value = .6f + i * 0.4f / CurrentSong.Cover.Layers.Count;
            }

            Loader.SetActive(false);
        
            if (HomeModal.main) 
                HomeModal.main.Close();

            SongSource.time = 0;
        
            InformationBar.main.UpdateSongButton();
            InformationBar.main.UpdateChartButton();
        
            InspectorPanel.main.UpdateButtons();
            InspectorPanel.main.SetObject(null);
            InspectorPanel.main.CurrentHierarchyObject = null;
        
            HierarchyPanel.main.SetMode(HierarchyMode.PlayableSong);
        
            TimelinePanel.main.UpdatePeekLimit();
            TimelinePanel.main.UpdateItems();
        
            PlayerView.main.UpdateObjects();
        
            History = new();
            OnHistoryUpdate();
        
            ClipboardItem = null;
            OnClipboardUpdate();

            BorderlessWindow.RenameWindow(CurrentSong.SongArtist + " - " + CurrentSong.SongName + " // JANOARG Chartmaker");
        
            SetEditorActive(true);
        }
    

        public void OpenChart(ExternalChartMeta chart)
        {
            string path = Path.Combine(Path.GetDirectoryName(CurrentSongPath)!, chart.Target + ".jac");
            CurrentChart = JACDecoder.Decode(File.ReadAllText(path));
            CurrentChartPath = path;
            CurrentChartMeta = chart;
        }

        public Task OpenChartAsync(ExternalChartMeta chart)
        {
            return Task.Run(() => OpenChart(chart));
        }
    
        public IEnumerator OpenChartRoutine(ExternalChartMeta chart) {
            if (ActiveTask?.IsCompleted == false) yield break;
        
            Loader.SetActive(true);
            LoaderPanel.SetSong(CurrentSong);
            LoaderPanel.ActionLabel.text = "Loading Chart...";
            LoaderPanel.ProgressBar.value = 0;

            LoaderPanel.ProgressLabel.text = "Initializing...";
            yield return new WaitForSeconds(0.5f);

            LoaderPanel.ProgressLabel.text = "Loading .jac file...";

            ActiveTask = OpenChartAsync(chart);
            yield return new WaitUntil(() => ActiveTask.IsCompleted);
            if (!ActiveTask.IsCompletedSuccessfully) 
            {
                Loader.SetActive(false);
                DialogModal dialogModal = ModalHolder.main.Spawn<DialogModal>();
                dialogModal.SetDialog("Parsing Error", ActiveTask.Exception!.Message, new string[] {"Ok"}, _ => {});
                yield break;
            }

            Loader.SetActive(false);
            InformationBar.main.UpdateChartButton();
            InspectorPanel.main.UpdateButtons();
            InspectorPanel.main.UpdateForm();
            InspectorPanel.main.CurrentHierarchyObject = null;
            HierarchyPanel.main.SetMode(HierarchyMode.Chart);
            TimelinePanel.main.SetTabMode(TimelineMode.Lanes);
            TimelinePanel.main.UpdateItems();
            PlayerView.main.UpdateObjects();
            History = new();
            OnHistoryUpdate();
            ClipboardItem = null;
            OnClipboardUpdate();
        }

        public void Save()
        {
            if (CurrentSong != null) File.WriteAllText(CurrentSongPath, JAPSEncoder.Encode(CurrentSong, CurrentSong.ClipPath));
            if (CurrentChart != null) File.WriteAllText(CurrentChartPath, JACEncoder.Encode(CurrentChart));
            IsDirty = false;
        }

        public Task SaveAsync()
        {
            return Task.Run(Save);
        }
    
        public IEnumerator SaveRoutine() {
            if (ActiveTask?.IsCompleted == false) yield break;
            if (CurrentSong == null) yield break;
        
            NotifyPending("Saving song data...");

            ActiveTask = SaveAsync();
            yield return new WaitUntil(() => ActiveTask.IsCompleted);
            if (!ActiveTask.IsCompletedSuccessfully) 
            {
                Loader.SetActive(false);
                DialogModal dialogModal = ModalHolder.main.Spawn<DialogModal>();
                dialogModal.SetDialog("Error", ActiveTask.Exception!.Message, new string[] {"Ok"}, _ => {});
                UnityEngine.Debug.LogException(ActiveTask.Exception);
                yield break;
            }

            if (InspectorPanel.main.IsCoverDirty) 
            {
                PlayerView.main.UpdateIconFile();
                InspectorPanel.main.IsCoverDirty = false;
            }

            Notify("Song data saved!");
        }
    
        public IEnumerator SaveThenQuit() {
            if (ActiveTask?.IsCompleted == false) yield break;
            if (CurrentSong == null) yield break;

            Loader.SetActive(true);
            LoaderPanel.ActionLabel.text = "Cleaning up...";
            LoaderPanel.ProgressLabel.text = "Saving song before quitting...";
            LoaderPanel.ProgressBar.value = 0;

            yield return SaveRoutine();

            if (!IsDirty) Application.Quit();
        }
    
        public void StartSaveRoutine() {
            StartCoroutine(SaveRoutine());
        }
    
        public IEnumerator SavePrefsRoutine() {
            if (ActiveTask?.IsCompleted == false) yield break;
        
            NotifyPending("Saving preferences...");

            ActiveTask = Task.Run(() => {
                PreferencesStorage.Save();
                KeybindingsStorage.Save();
            });
            yield return new WaitUntil(() => ActiveTask.IsCompleted);
            if (!ActiveTask.IsCompletedSuccessfully) 
            {
                Loader.SetActive(false);
                DialogModal dialogModal = ModalHolder.main.Spawn<DialogModal>();
                dialogModal.SetDialog("Error", ActiveTask.Exception!.Message, new string[] {"Ok"}, _ => {});
                UnityEngine.Debug.LogException(ActiveTask.Exception);
                yield break;
            }

            Notify("Preferences saved!");
        }
    
        public void StartSavePrefsRoutine() {
            StartCoroutine(SavePrefsRoutine());
        }
    
    
        public void CloseSong() {
            CurrentSongPath = CurrentChartPath = "";
            CurrentSong = null;
            CurrentChart = null;

            if (!HomeModal.main) ModalHolder.main.Spawn<HomeModal>();

            SongSource.time = 0;
            SongSource.Stop();
            SetEditorActive(false);
            PlayerView.main.MainCamera.rect = new (0, 0, 1, 1);
            Resources.UnloadUnusedAssets();

            PlayerView.main.ClearObjects();
        
            BorderlessWindow.RenameWindow("JANOARG Chartmaker");

            IsDirty = false;
        }
    
        public void TryCloseSong() {
            DirtyModal(CloseSong);
        }
    
        public bool QuitCheck() 
        {
            if (!IsDirty) WindowHandler.main.Quit();
            else 
            {
                if (Preferences.SaveOnQuit) StartCoroutine(SaveThenQuit());
                else DirtyModal(() => {
                    IsDirty = false;
                    Application.Quit();
                });
            }
            return !IsDirty;
        }
    
        DialogModal dirtyDialog;
        public void DirtyModal(Action action) 
        {
            if (dirtyDialog)
            {
                return;
            }
            else if (IsDirty)
            {
                dirtyDialog = ModalHolder.main.Spawn<DialogModal>();
                dirtyDialog.SetDialog("Close Song", "Would you like to save changes made to " + CurrentSong.SongName + "?", new [] {"Cancel", "", "Don't Save", "Save"}, a => {
                    if (a == 3) { Save(); action(); }
                    else if (a == 2) { action(); }
                });
            }
            else
            {
                action();
            }
        }
    

        public static string GetItemName(object item) => item switch
        {
            IList list =>       list.Count > 0 
                ? list.Count > 1 
                    ? list.Count + " " + GetItemName(list[0]) + "s" : GetItemName(list[0]) 
                : "Empty List",
            Chart      =>       "Chart",
            BPMStop    =>       "BPM Stop",
            HitStyle   =>       "Hit Style",
            LaneStyle  =>       "Lane Style",
            LaneGroup  =>       "Lane Group",
            Lane       =>       "Lane",
            LaneStep   =>       "Lane Step",
            HitObject  =>       "Hit Object",
            _          =>       item?.ToString().Split(".")[^1] ?? "Null"
        };

        public void OnHistoryDo()
        {
            InspectorPanel.main.UpdateForm();
            TimelinePanel.main.UpdateItems();
            PlayerView.main.UpdateObjects();
            HierarchyPanel.main.UpdateHierarchy();
            IsDirty = true;
        }

        bool _RecursionBuster;

        public void OnHistoryUpdate()
        {
            TimelinePanel timeline = TimelinePanel.main;

            timeline.UndoButton.interactable = History.ActionsBehind.Count > 0;
            timeline.UndoButtonGroup.alpha   = timeline.UndoButton.interactable
                ? 1 : .5f;

            timeline.RedoButton.interactable = History.ActionsAhead.Count > 0;
            timeline.RedoButtonGroup.alpha   = timeline.RedoButton.interactable 
                ? 1 : .5f;

            timeline.ActionsBehindCounter.text = History.ActionsBehind.Count > 999 
                ? "999+" : History.ActionsBehind.Count.ToString();;
        
            timeline.ActionsAheadCounter.text  = History.ActionsAhead.Count > 999 
                ? "999+" : History.ActionsAhead.Count.ToString();;
        }

        public void DoAction(IChartmakerAction action)
        {
            action.Redo();
            History.AddAction(action);
            OnHistoryDo();
            OnHistoryUpdate();
        }

        public void SetItem(object target, string field, object value)
        {
            if (_RecursionBuster)
                return;
        
            History.SetItem(target, field, value);

            if (field == "Offset")
                SortList(GetListTarget(target));
            if (target is Timestamp)
                ((Storyboardable)InspectorPanel.main.CurrentObject).Storyboard.InvalidateCache();
        
            TimelinePanel.main.UpdateItems();
            PlayerView.main.UpdateObjects();
        
            IsDirty = true;
        
            OnHistoryUpdate();
        }

        public IList GetListTarget(object obj) => obj switch {
            IList list  => list.Count > 0 ? GetListTarget(list[0]) : throw new ArgumentException("Can't determine list target of an empty list"),
            Timestamp   => (IList)((Storyboardable)InspectorPanel.main.CurrentObject).Storyboard,
            BPMStop     => CurrentSong.Timing.Stops,
            LaneStyle   => CurrentChart.Palette.LaneStyles,
            HitStyle    => CurrentChart.Palette.HitStyles,
            LaneGroup   => CurrentChart.Groups,
            Lane        => CurrentChart.Lanes,
            LaneStep    => InspectorPanel.main.CurrentHierarchyObject is Lane lane 
                ? lane.LaneSteps : new(),
            HitObject   => InspectorPanel.main.CurrentHierarchyObject is Lane lane 
                ? lane.Objects : new(),
            null        => throw new ArgumentException("Object can't be null"),
            _           => throw new ArgumentException("No list target found for " + obj.GetType()),
        };

        public static void SortList(IList list)
        {
            switch (list)
            {
                case List<BPMStop> bpmStopList:
                    bpmStopList.Sort((x, y) => x.Offset.CompareTo(y.Offset));
                    break;
            
                case List<Timestamp> timeStampList:
                    timeStampList.Sort((x, y) => x.Offset.CompareTo(y.Offset));
                    break;
            
                case List<Lane> laneList:
                    laneList.Sort((x, y) => x.LaneSteps[0].Offset.CompareTo(y.LaneSteps[0].Offset));
                    break;
            
                case List<LaneStep> laneStepList:
                    laneStepList.Sort((x, y) => x.Offset.CompareTo(y.Offset));
                    break;
                case List<HitObject> hitObjectList:
                    hitObjectList.Sort((x, y) => x.Offset.CompareTo(y.Offset));
                    break;
            }
        }

        public void DeleteItem(object obj, bool setNull = true)
        {
            IList list = obj is IList listObject
                ? listObject : new [] { obj };
        
            bool ListType<T>() => list[0] is T;
        
            IChartmakerAction action;
        
            if (obj is Lane || ListType<Lane>()) 
            {
                SortedDictionary<int, Lane> items = new ();
            
                foreach (object item in list) 
                    items.Add(CurrentChart.Lanes.IndexOf((Lane)item), (Lane)item);
            
                action = new ChartmakerIndexedDeleteAction<Lane> 
                {
                    Target = CurrentChart.Lanes,
                    Items = items,
                };
            } else {
                action = new ChartmakerDeleteAction {
                    Target = GetListTarget(obj),
                    Item = obj,
                };
            }
            DoAction(action)
                ;
            if (setNull)
                InspectorPanel.main.UnsetObject();
        }

        public void AddItem(object obj)
        {
            ChartmakerAddAction action = new ChartmakerAddAction 
            {
                Target = GetListTarget(obj),
                Item = obj,
            };
        
            DoAction(action);
            InspectorPanel.main.SetObject(obj, false);
        }
    
        public void AddItem(object obj, float startingOffset)
        {
            IList list = obj is IList listObject 
                ? listObject : new [] { obj };
        
            if (list[0] is BPMStop firstBpmStop)
            {
                float minOffset = float.PositiveInfinity;

                foreach (object item in list)
                {
                    BPMStop stop = (BPMStop)item;
                    minOffset = Mathf.Min(minOffset, stop.Offset);
                }

                float offset = startingOffset - minOffset;
                foreach (object item in list)
                {
                    BPMStop stop = (BPMStop)item;
                    stop.Offset += offset;
                }
            }
            AddItem(list);
        }
        public void AddItem(object obj, BeatPosition startingOffset)
        {
            IList list = obj is IList listObject 
                ? listObject : new [] { obj };

            FieldInfo field = list[0].GetType().GetField("Offset");
        
            if (list[0] is Lane firstLane)
            {
                BeatPosition minOffset = new (int.MaxValue, int.MaxValue - 1, int.MaxValue);

                foreach (object item in list)
                {
                    Lane lane = (Lane)item;
                    minOffset = BeatPosition.Min(minOffset, lane.LaneSteps[0].Offset);
                }

                BeatPosition offset = startingOffset - minOffset;
                foreach (object item in list)
                {
                    Lane lane = (Lane)item;
                    foreach (LaneStep step in lane.LaneSteps) 
                    {
                        step.Offset += offset;
                        foreach (Timestamp ts in step.Storyboard.Timestamps) ts.Offset += offset;
                    }
                    foreach (HitObject hit in lane.Objects)
                    {
                        hit.Offset += offset;
                        foreach (Timestamp ts in hit.Storyboard.Timestamps) ts.Offset += offset;
                    }
                }
            }
            else if (field != null)
            {
                BeatPosition minOffset = new (int.MaxValue, int.MaxValue - 1, int.MaxValue);
                foreach (object item in list)
                {
                    minOffset = BeatPosition.Min(minOffset, (BeatPosition)field.GetValue(item));
                }

                BeatPosition offset = startingOffset - minOffset;

                UnityEngine.Debug.Log(startingOffset + " " + minOffset + " " + offset);

                foreach (object item in list)
                {
                    field.SetValue(item, (BeatPosition)field.GetValue(item) + offset);
                    if (item is Storyboardable isb)
                    {
                        foreach (Timestamp ts in isb.Storyboard.Timestamps) ts.Offset += offset;
                    }
                }
            }
            AddItem(list);
        }
        public IList ListClone(IList obj)
        {
            List<object> newList = new (); 
            foreach (object item in obj) newList.Add(SmartClone(item)); 
            return newList;
        }

        public T DeepClone<T>(T obj) where T : IDeepClonable<T>
        {
            return obj.DeepClone();
        }


        public object SmartClone(object obj) => obj switch {
            IList list           => ListClone(list),
            Timestamp timestamp  => timestamp.DeepClone(),
            BPMStop bpmStop      => bpmStop.DeepClone(),
            LaneStyle laneStyle  => laneStyle.DeepClone(),
            HitStyle hitStyle    => hitStyle.DeepClone(),
            LaneGroup laneGroup  => laneGroup.DeepClone(),
            Lane lane            => lane.DeepClone(),
            LaneStep laneStep    => laneStep.DeepClone(),
            HitObject hitObject  => hitObject.DeepClone(),
            null                 => throw new ArgumentException("Object can't be null"),
            _                    => throw new AssertionException("Unknown object type " + obj.GetType(), "Unknown object type " + obj.GetType()),
        };

        public void Undo(int times = 1)
        {
            _RecursionBuster = true;
            History.Undo(times);
            OnHistoryDo();
            OnHistoryUpdate();
            _RecursionBuster = false;
        }

        public void Redo(int times = 1)
        {
            _RecursionBuster = true;
            History.Redo(times);
            OnHistoryDo();
            OnHistoryUpdate();
            _RecursionBuster = false;
        }

        public bool CanCopy()
        {
            if (InspectorPanel.main.CurrentTimestamp?.Count > 0)
                return true;
        
            object currentItem = InspectorPanel.main.CurrentObject;
        
            return currentItem is not (null or PlayableSong or Cover or CoverLayer or Chart or Palette or CameraController) 
                   && currentItem != CurrentChart?.Groups;
        }

        public bool CanPaste()
        {
            return ClipboardItem != null;
        }

        public bool CanRename()
        {
            if (InspectorPanel.main.CurrentTimestamp?.Count > 0) 
                return false;
        
            object currentItem = InspectorPanel.main.CurrentObject;
            return currentItem is (LaneGroup or Lane or HitStyle or LaneStyle);
        }


        public void OnClipboardUpdate()
        {
            TimelinePanel timeline = TimelinePanel.main;
            object currentItem = InspectorPanel.main.CurrentObject;
        
            timeline.CutButton.interactable = timeline.CopyButton.interactable = CanCopy();
            timeline.CutButtonGroup.alpha   = timeline.CopyButtonGroup.alpha   = timeline.CutButton.interactable 
                ? 1 : .5f;

            timeline.PasteButton.interactable = CanPaste();
            timeline.PasteButtonGroup.alpha   = timeline.PasteButton.interactable ? 1 : .5f;
        }
    
        public void Cut()
        {
            if (!CanCopy())
                return;
        
            if ((InspectorPanel.main.CurrentTimestamp?.Count ?? 0) > 0) 
                ClipboardItem = InspectorPanel.main.CurrentTimestamp;
            else 
                ClipboardItem = InspectorPanel.main.CurrentObject;
        
            DeleteItem(ClipboardItem);
            InspectorPanel.main.UnsetObject();
        }

        public void Copy()
        {
            if (!CanCopy()) 
                return;
        
            if ((InspectorPanel.main.CurrentTimestamp?.Count ?? 0) > 0)
                ClipboardItem = InspectorPanel.main.CurrentTimestamp;
            else
                ClipboardItem = InspectorPanel.main.CurrentObject;
        
            OnClipboardUpdate();
        }

        public void Paste()
        {
            if (!CanPaste()) 
                return;
        
            object obj = SmartClone(ClipboardItem);
        
            if (obj is BPMStop or List<BPMStop> || obj is IList list && list[0] is BPMStop)
                AddItem(obj, SongSource.time);
            else 
                AddItem(obj, TimelinePanel.main.ToRoundedBeat(CurrentSong.Timing.ToBeat(SongSource.time)));
        
            InspectorPanel.main.SetObject(obj, false);
        }

        public void NotifyPending(string text, float time = float.PositiveInfinity)
        {
            NotificationLabel.text = text;
            NotificationTime = time;
        }

        public void Notify(string text, float time = 3, float flashTime = 0.5f)
        {
            NotificationLabel.text = text;
            NotificationTime = time;
            NotificationFlashTime = flashTime;
        }
    }
}