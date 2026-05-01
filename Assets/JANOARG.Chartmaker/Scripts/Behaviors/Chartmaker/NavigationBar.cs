using System.Collections;
using System.Collections.Generic;
using JANOARG.Chartmaker.Data.Chartmaker;
using JANOARG.Chartmaker.UI.ContextMenu;
using JANOARG.Chartmaker.UI.Modal;
using JANOARG.Chartmaker.UI.Modal.ModalTypes;
using JANOARG.Shared.Data.ChartInfo;
using JANOARG.Chartmaker.Utils;
using UnityEngine;

namespace JANOARG.Chartmaker.Behaviors.Chartmaker
{
    public class NavigationBar : MonoBehaviour
    {
        [Header("Objects")]
        public RectTransform FileButton;
        public RectTransform EditButton;
        public RectTransform ViewButton;
        public RectTransform OptionsButton;
        public RectTransform HelpButton;
        public RectTransform MenuButton;

        string KeyOf(string id) => KeyboardHandler.main.Keybindings[id].Keybind.ToString();

        private void OpenMenuHelper(System.Func<ContextMenuList> getMenu, RectTransform button)
        {
            ContextMenuHolder.main.OpenRoot(getMenu(), button);
        }

        public void OpenFileMenu() => OpenMenuHelper(GetFileMenu, FileButton);
        public void OpenEditMenu() => OpenMenuHelper(GetEditMenu, EditButton);
        public void OpenViewMenu() => OpenMenuHelper(GetViewMenu, ViewButton);
        public void OpenOptionsMenu() => OpenMenuHelper(GetOptionsMenu, OptionsButton);
        public void OpenHelpMenu() => OpenMenuHelper(GetHelpMenu, HelpButton);

        public void OpenMenu()
        {
            if (Chartmaker.main.CurrentSong != null)
            {
                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    new ContextMenuListSublist("File", GetFileMenu().Items.ToArray()),
                    new ContextMenuListSublist("Edit", GetEditMenu().Items.ToArray()),
                    new ContextMenuListSublist("View", GetViewMenu().Items.ToArray()),
                    new ContextMenuListSublist("Options", GetOptionsMenu().Items.ToArray()),
                    new ContextMenuListSublist("Help", GetHelpMenu().Items.ToArray())
                ), MenuButton);
            }
            else 
            {
                ContextMenuHolder.main.OpenRoot(new ContextMenuList(
                    new ContextMenuListSublist("File", GetFileMenu().Items.ToArray()),
                    new ContextMenuListSublist("View", GetEditMenu().Items.ToArray()),
                    new ContextMenuListSublist("Options", GetOptionsMenu().Items.ToArray()),
                    new ContextMenuListSublist("Help", GetHelpMenu().Items.ToArray())
                ), MenuButton);
            }
        }

        private ContextMenuList GetFileMenu()
        {
            ContextMenuListItem openChartItem;
            if (Chartmaker.main.CurrentSong == null || Chartmaker.main.CurrentSong.Charts.Count <= 0)
                openChartItem = new ContextMenuListAction("Open Chart", () => {}, _enabled: false);
            else 
            {
                ContextMenuListSublist list = new ("Open Chart");
                openChartItem = list;
                foreach (ExternalChartMeta chart in Chartmaker.main.CurrentSong.Charts)
                {
                    ExternalChartMeta exChart = chart;
                    list.Items.Items.Add(new ContextMenuListAction(chart.DifficultyName + " " + chart.DifficultyLevel, () => {
                        StartCoroutine(Chartmaker.main.OpenChartRoutine(exChart));
                    }));
                }
            }

            return new ContextMenuList(
                new ContextMenuListAction("New Song...", () => ModalHolder.main.Spawn<NewSongModal>(), KeyOf("FL:New")),
                new ContextMenuListAction("Open Song...", Chartmaker.main.OpenSongModal, KeyOf("FL:Open")),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Create Chart...", () => ModalHolder.main.Spawn<NewChartModal>(), _enabled: Chartmaker.main.CurrentSong != null),
                openChartItem,
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Save", Chartmaker.main.StartSaveRoutine, KeyOf("FL:Save"), _enabled: Chartmaker.main.CurrentSong != null),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Render...", () => ModalHolder.main.Spawn<RenderModal>(), _enabled: Chartmaker.main.CurrentChart != null),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Reveal Song Folder", () => Application.OpenURL("file://" + System.IO.Path.GetDirectoryName(Chartmaker.main.CurrentSongPath)), _enabled: Chartmaker.main.CurrentSong != null),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Close Song", Chartmaker.main.TryCloseSong, _enabled: Chartmaker.main.CurrentSong != null),
                new ContextMenuListAction("Exit Chartmaker", Application.Quit)
            );
        }

        private ContextMenuList GetEditMenu()
        {
            bool canUndo = Chartmaker.main.History.ActionsBehind.TryPeek(out IChartmakerAction actionBehind);
            bool canRedo = Chartmaker.main.History.ActionsAhead.TryPeek(out IChartmakerAction actionAhead);

            return new ContextMenuList(
                new ContextMenuListAction("Undo <i>" + (actionBehind?.GetName() ?? ""), () => Chartmaker.main.Undo(), KeyOf("ED:Undo"), icon: "Undo", _enabled: canUndo),
                new ContextMenuListAction("Redo <i>" + (actionAhead?.GetName() ?? ""), () => Chartmaker.main.Redo(), KeyOf("ED:Redo"), icon: "Redo", _enabled: canRedo),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Cut", Chartmaker.main.Cut, KeyOf("ED:Cut"), icon: "Cut", _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListAction("Copy", Chartmaker.main.Copy, KeyOf("ED:Copy"), icon: "Copy", _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListAction("Paste <i>" + (Chartmaker.main.CanPaste() ? Chartmaker.GetItemName(Chartmaker.main.ClipboardItem) : ""), Chartmaker.main.Paste, KeyOf("ED:Paste"), icon: "Paste", _enabled: Chartmaker.main.CanPaste()),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Rename", () => HierarchyPanel.main.RenameCurrent(), KeyOf("ED:Rename"), _enabled: Chartmaker.main.CanRename()),
                new ContextMenuListAction("Delete", () => KeyboardHandler.main.Keybindings["ED:Delete"].Invoke(), KeyOf("ED:Delete"), _enabled: Chartmaker.main.CanCopy()),
                new ContextMenuListSeparator(),
                new ContextMenuListSublist("Timeline", 
                    new ContextMenuListAction("Select All", () => KeyboardHandler.main.Keybindings["ED:SelectAll"].Invoke(), KeyOf("ED:SelectAll")),
                    new ContextMenuListAction("Invert Selection", InvertSelection)
                )
            );
        }

        public ContextMenuList GetViewMenu()
        {
            return new ContextMenuList(
                new ContextMenuListSublist("Show", 
                    new ContextMenuListAction("Maximized Player View", () =>
                    {
                        PlayerView.main.IsMaximised = !PlayerView.main.IsMaximised;
                    }, _checked: PlayerView.main.IsMaximised),
                    new ContextMenuListSeparator(),
                    new ContextMenuListAction("Hierarchy Panel", () =>
                    {
                        if (HierarchyPanel.main.IsCollapsed)
                            HierarchyPanel.main.Restore();
                        else
                            HierarchyPanel.main.Collapse();
                    }, _checked: !HierarchyPanel.main.IsCollapsed),
                    new ContextMenuListAction("Inspector Panel", () => {
                        if (InspectorPanel.main.IsCollapsed) 
                            InspectorPanel.main.Restore();
                        else 
                            InspectorPanel.main.Collapse();
                    }, _checked: !InspectorPanel.main.IsCollapsed),
                    new ContextMenuListAction("Timeline Panel", () => {
                        if (TimelinePanel.main.TimelineHeight > 0) 
                            TimelinePanel.main.Collapse();
                        else 
                            TimelinePanel.main.Restore();
                    }, _checked: TimelinePanel.main.TimelineHeight > 0)
                ),
                new ContextMenuListSeparator(),
                new ContextMenuListSublist("Visualizer", InformationBar.main.GetVisualizerMenu())
            );
        }

        private ContextMenuList GetOptionsMenu()
        {
            return new ContextMenuList(
                new ContextMenuListAction("Preferences...", () => ModalHolder.main.Spawn<PreferencesModal>()),
                new ContextMenuListAction("Show Keybindings...", () => ModalHolder.main.Spawn<PreferencesModal>().SetTab(1))
            );
        }

        private ContextMenuList GetHelpMenu()
        {
            return new ContextMenuList(
                new ContextMenuListAction("Interactive Tutorials...", () => ModalHolder.main.Spawn<TutorialModal>()),
                new ContextMenuListAction("Chartmaker Manual...", () => ModalHolder.main.Spawn<HelpModal>()),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Source Code on GitHub", () => Application.OpenURL("https://github.com/FFF40/JANOARG-Chartmaker"), icon: "Github Icon"),
                new ContextMenuListAction("Report an Issue / Suggestion", () => Application.OpenURL("https://github.com/FFF40/JANOARG-Chartmaker/issues"), icon: "External Link"),
                new ContextMenuListAction("FFF40 Studios Discord Server", () => Application.OpenURL("https://discord.gg/vXJTPFQBHm"), icon: "Discord Icon"),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("Check for Updates", () => VersionCheckerModal.InitFetch()),
                new ContextMenuListAction("Show All Releases", () => Application.OpenURL("https://github.com/FFF40/JANOARG-Chartmaker/releases"), icon: "External Link"),
                new ContextMenuListSeparator(),
                new ContextMenuListAction("About Chartmaker...", () => ModalHolder.main.Spawn<AboutModal>(), icon: "Credits")
            );
        }


        private void InvertSelection()
        {
            IList list = InspectorPanel.main.CurrentObject is IList li ? li : new List<object> { InspectorPanel.main.CurrentObject };

            switch (TimelinePanel.main.CurrentMode)
            {
                case TimelineMode.Storyboard:
                {
                    if (InspectorPanel.main.CurrentObject is Storyboardable) 
                        InspectorPanel.main.SetObject(((Storyboardable)InspectorPanel.main.CurrentObject).Storyboard.Timestamps.FindAll(x => InspectorPanel.main.CurrentTimestamp?.Contains(x) == false));
                    break;
                }
                case TimelineMode.Lanes:
                {
                    if (Chartmaker.main.CurrentChart != null) 
                        InspectorPanel.main.SetObject(Chartmaker.main.CurrentChart.Lanes.FindAll(x => !list.Contains(x)));
                    break;
                }
                case TimelineMode.LaneSteps:
                {
                    if (InspectorPanel.main.CurrentHierarchyObject is Lane lane) 
                        InspectorPanel.main.SetObject(lane.LaneSteps.FindAll(x => !list.Contains(x)));
                    break;
                }
                case TimelineMode.HitObjects:
                {
                    if (InspectorPanel.main.CurrentHierarchyObject is Lane lane) 
                        InspectorPanel.main.SetObject(lane.Objects.FindAll(x => !list.Contains(x)));
                    break;
                }
                case TimelineMode.Timing:
                {
                    if (Chartmaker.main.CurrentSong != null) 
                        InspectorPanel.main.SetObject(Chartmaker.main.CurrentSong.Timing.Stops.FindAll(x => !list.Contains(x)));
                    break;
                }
            }
        }
    }
}
