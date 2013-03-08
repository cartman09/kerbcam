using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP.IO;


namespace KerbCam {

    // Class purely for the purpose for injecting the plugin.
    // Plugin startup taken from:
    // http://forum.kerbalspaceprogram.com/showthread.php/43027
    public class Bootstrap : KSP.Testing.UnitTest {
        public Bootstrap() {
            var gameObject = new GameObject("KerbCam", typeof(KerbCam));
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }
    }

    // Plugin behaviour class.
    public class KerbCam : MonoBehaviour {
        private bool isEnabled = false;
        private MainWindow mainWindow;

        // TODO: Custom and additional keybindings.
        private Event KEY_PATH_TOGGLE_RUNNING = Event.KeyboardEvent(KeyCode.Insert.ToString());
        private Event KEY_PATH_TOGGLE_PAUSE = Event.KeyboardEvent(KeyCode.Home.ToString());
        private Event KEY_PATH_TOGGLE_WINDOW = Event.KeyboardEvent(KeyCode.F8.ToString());
        private Event KEY_DEBUG = Event.KeyboardEvent(KeyCode.F7.ToString());

        public void OnLevelWasLoaded() {
            isEnabled = (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                && HighLogic.LoadedScene == GameScenes.FLIGHT;

            if (!isEnabled) {
                if (State.instance != null) {
                    State.instance.Stop();
                }
                if (mainWindow != null) {
                    mainWindow.HideWindow();
                }
            } else {
                if (State.instance == null) {
                    State.instance = new State();
                }
                if (mainWindow == null) {
                    mainWindow = new MainWindow();
                }
            }
        }

        public void Update() {
            if (!isEnabled)
                return;

            try {
                if (State.instance.SelectedPath != null) {
                    State.instance.SelectedPath.Runner.Update();
                }
            } catch (Exception e) {
                Debug.LogError(e.ToString() + "\n" + e.StackTrace);
            }
        }

        public void OnGUI() {
            if (!isEnabled)
                return;

            try {
                var ev = Event.current;

                if (State.instance.SelectedPath != null) {
                    // Events that require an active path.
                    if (ev.Equals(KEY_PATH_TOGGLE_RUNNING)) {
                        State.instance.SelectedPath.Runner.ToggleRunning();
                    } else if (ev.Equals(KEY_PATH_TOGGLE_PAUSE)) {
                        State.instance.SelectedPath.Runner.TogglePause();
                    }
                }

                if (State.instance.developerMode) {
                    if (ev.Equals(KEY_DEBUG)) {
                        // Random bits of logging used by the developer to
                        // work out whatever the heck he's doing.

                        DebugUtil.LogCameras();
                        
                        DebugUtil.LogVessel(FlightGlobals.ActiveVessel);
                        DebugUtil.LogCamera(Camera.main);
                    }
                }

                if (ev.Equals(KEY_PATH_TOGGLE_WINDOW)) {
                    mainWindow.ToggleWindow();
                }
            } catch (Exception e) {
                Debug.LogError(e.ToString() + "\n" +  e.StackTrace);
            }
        }
    }

    /// <summary>
    /// Central stored state of KerbCam.
    /// </summary>
    class State {
        public static State instance;

        private SimpleCamPath selectedPath;
        public List<SimpleCamPath> paths = new List<SimpleCamPath>();
        private int numCreatedPaths = 0;
        public bool developerMode = false;
        public CameraController camControl = new CameraController();

        public void RemovePathAt(int index) {
            var path = paths[index];
            if (path == selectedPath) {
                SelectedPath = null;
            }
            paths.RemoveAt(index);
        }

        public SimpleCamPath NewPath() {
            numCreatedPaths++;
            var newPath = new SimpleCamPath(
                "Path #" + numCreatedPaths,
                FlightCamera.fetch.camera);
            paths.Add(newPath);
            return newPath;
        }

        public SimpleCamPath SelectedPath {
            get { return selectedPath; }
            set {
                if (selectedPath != null) {
                    selectedPath.Runner.StopRunning();
                    selectedPath.StopDrawing();
                }
                selectedPath = value;
            }
        }

        public void Stop() {
            SelectedPath = null;
        }
    }

    abstract class BaseWindow {
        private bool isWindowOpen = false;
        protected int windowId;

        public BaseWindow() {
            windowId = this.GetHashCode();
        }

        public void ToggleWindow() {
            if (isWindowOpen) {
                HideWindow();
            } else {
                ShowWindow();
            }
        }

        public virtual void ShowWindow() {
            isWindowOpen = true;
            RenderingManager.AddToPostDrawQueue(3, new Callback(DrawGUI));
            GUI.FocusWindow(windowId);
        }

        public virtual void HideWindow() {
            isWindowOpen = false;
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(DrawGUI));
        }

        protected abstract void DrawGUI();
    }

    class MainWindow : BaseWindow {
        private Assembly assembly;

        private SimpleCamPathEditor pathEditor = null;
        private Vector2 pathListScroll = new Vector2();
        private WindowResizer resizer;
        private HelpWindow helpWindow;

        public MainWindow() {
            assembly = Assembly.GetCallingAssembly();
            resizer = new WindowResizer(
                new Rect(10, 100, 200, 200),
                new Vector2(200, 150));
            this.helpWindow = new HelpWindow(assembly);
        }

        public override void HideWindow() {
            base.HideWindow();
            helpWindow.HideWindow();
        }

        protected override void DrawGUI() {
            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                windowId, resizer.Position, DoGUI,
                string.Format(
                    "KerbCam [v{0}]",
                    assembly.GetName().Version.ToString(2)),
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            try {
                C.InitGUIConstants();

                if (State.instance.SelectedPath != null) {
                    // A path is selected.
                    if (pathEditor == null || !pathEditor.IsForPath(State.instance.SelectedPath)) {
                        // Selected path has changed.
                        pathEditor = State.instance.SelectedPath.MakeEditor();
                        resizer.MinWidth = 400;
                        resizer.MinHeight = 250;
                    }
                } else {
                    // No path is selected.
                    if (pathEditor != null) {
                        pathEditor = null;
                        resizer.MinWidth = 200;
                        resizer.MinHeight = 150;
                    }
                }

                GUILayout.BeginVertical(); // BEGIN outer container

                GUILayout.BeginHorizontal(); // BEGIN left/right panes

                GUILayout.BeginVertical(); // BEGIN main controls

                if (GUILayout.Button("New simple path")) {
                    State.instance.SelectedPath = State.instance.NewPath();
                }

                DoPathList();

                GUILayout.EndVertical(); // END main controls

                // Path editor lives in right-hand-frame.
                if (pathEditor != null) {
                    pathEditor.DoGUI();
                }

                GUILayout.EndHorizontal(); // END left/right panes

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                State.instance.developerMode = GUILayout.Toggle(
                    State.instance.developerMode, "");
                GUILayout.Label("Dev mode");
                if (GUILayout.Button("?")) {
                    helpWindow.ToggleWindow();
                }
                resizer.HandleResize();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 25));
            } catch (Exception e) {
                Debug.LogError(e.ToString() + "\n" + e.StackTrace);
            }
        }

        private void DoPathList() {
            // Scroll list allowing selection of an existing path.
            pathListScroll = GUILayout.BeginScrollView(pathListScroll, false, true);
            for (int i = 0; i < State.instance.paths.Count; i++) {
                GUILayout.BeginHorizontal(); // BEGIN path widgets
                if (GUILayout.Button("X", C.DeleteButtonStyle)) {
                    State.instance.RemovePathAt(i);
                    if (i >= State.instance.paths.Count) {
                        break;
                    }
                }

                {
                    var path = State.instance.paths[i];
                    bool isSelected = path == State.instance.SelectedPath;
                    bool doSelect = GUILayout.Toggle(path == State.instance.SelectedPath, "");
                    if (isSelected != doSelect) {
                        if (doSelect) {
                            State.instance.SelectedPath = path;
                        } else {
                            State.instance.SelectedPath = null;
                        }
                    }
                    GUILayout.Label(path.Name);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal(); // END path widgets
            }
            GUILayout.EndScrollView();
        }
    }

    class HelpWindow : BaseWindow {
        private Assembly assembly;
        private WindowResizer resizer;
        private Vector2 helpScroll = new Vector2();
        private string helpText = string.Join("",
            "KerbCam is a basic utility to automatically move the flight",
            " camera along a given path.\n",
            "\n",
            "NOTE: at its current stage of development, it is very rough,",
            " buggy, and feature incomplete. Use at your own risk. It is not",
            " inconceivable that this can crash your spacecraft or do other",
            " nasty things.\n",
            "\n",
            "Note that paths are not saved, and will be lost when KSP",
            " is restarted.",
            "\n",
            "Keys:\n",
            "* [Insert] Toggle playback of the currently selected path.\n",
            "* [Home] Toggle pause of playback.\n",
            "* [F8] Toggle the KerbCam window.\n",
            "\n",
            "Create a new path, then add keys to it by positioning your view",
            " and add the key with the \"New key\" button. Existing points",
            " can be viewed with the \"View\" button or moved to the current",
            " view position with the \"Set\" button.\n",
            "\n",
            "Source is hosted at https://github.com/huin/kerbcam under the",
            " BSD license."
        );

        public HelpWindow(Assembly assembly) {
            this.assembly = assembly;
            resizer = new WindowResizer(
                new Rect(10, 300, 300, 200),
                new Vector2(300, 150));
        }

        protected override void DrawGUI() {
            GUI.skin = HighLogic.Skin;
            resizer.Position = GUILayout.Window(
                windowId, resizer.Position, DoGUI,
                "KerbCam Help",
                resizer.LayoutMinWidth(),
                resizer.LayoutMinHeight());
        }

        private void DoGUI(int windowID) {
            try {
                GUILayout.BeginVertical(); // BEGIN outer container

                GUILayout.Label(string.Format(
                    "KerbCam [v{0}]", assembly.GetName().Version.ToString()));

                // BEGIN text scroller.
                helpScroll = GUILayout.BeginScrollView(helpScroll);
                GUILayout.TextArea(helpText);
                GUILayout.EndScrollView(); // END text scroller.

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Report issue")) {
                    Application.OpenURL("https://github.com/huin/kerbcam/issues");
                }
                if (GUILayout.Button("Close")) {
                    HideWindow();
                }
                resizer.HandleResize();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical(); // END outer container

                GUI.DragWindow(new Rect(0, 0, 10000, 25));
            } catch (Exception e) {
                Debug.LogError(e.ToString() + "\n" + e.StackTrace);
            }
        }
    }
}
