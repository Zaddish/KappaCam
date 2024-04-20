using KappaCam.Lights;
using KappaCam.Pathing;
using KappaCam.Menu;
using UnityEngine;
using MonoMod.RuntimeDetour;
using EFT.UI;
using EFT;
using Comfort.Common;

namespace KappaCam.Menu {
    public class Window {
        public delegate void MenuFunction();

        public Rect rect;
        public bool isOpen;
        public string title;
        private bool isResizing;
        private Vector2 resizeStart;

        private MenuFunction renderFunction;


        public Window(Rect initialRect, string windowTitle, MenuFunction menuFunc) {
            rect = initialRect;
            title = windowTitle;
            renderFunction = menuFunc;
            isOpen = false;
            isResizing = false;
        }

        public void Toggle() {
            isOpen = !isOpen;
        }

        public void Render(int id) {
            if (isOpen) {
                rect = GUI.Window(id, rect, InternalRender, title);
                rect = ResizeWindow(rect, ref isResizing, ref resizeStart);
            }
        }

        private void InternalRender(int windowID) {
            if (GUI.Button(new Rect(rect.width - 25, 5, 20, 20), "X")) {
                Toggle();
            }
            GUILayout.BeginArea(new Rect(10, 30, rect.width - 20, rect.height - 40));
            renderFunction();
            GUILayout.EndArea();
            GUI.DragWindow(new Rect(0, 0, rect.width, 20));
        }

        private Rect ResizeWindow(Rect windowRect, ref bool isResizing, ref Vector2 resizeStart) {
            Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
            Rect resizeHandle = new Rect(windowRect.x + windowRect.width - 20, windowRect.y + windowRect.height - 20, 20, 20);

            if (Event.current.type == EventType.MouseDown && resizeHandle.Contains(mouse)) {
                isResizing = true;
                resizeStart = mouse;
            } else if (Event.current.type == EventType.MouseUp && isResizing) {
                isResizing = false;
            } else if (Event.current.type == EventType.MouseDrag && isResizing) {
                Vector2 delta = mouse - resizeStart;
                windowRect.width = Mathf.Max(50, windowRect.width + delta.x);
                windowRect.height = Mathf.Max(50, windowRect.height + delta.y);
                resizeStart = mouse;
                Event.current.Use();
            }

            GUI.Button(resizeHandle, "|||");
            return windowRect;
        }

    }
    public class KappaCamMenu : MonoBehaviour {
        private Window lightMenuWindow;
        private Window pathMenuWindow;
        private bool windowOpen;
        private static GameObject input;
        private bool cursorSet = false;

        void Start() {
            LightMenu lightMenu = new GameObject("LightMenu").AddComponent<LightMenu>();
            PathingMenu pathMenu = new GameObject("PathingMenu").AddComponent<PathingMenu>();

            lightMenuWindow = new Window(new Rect(50, 50, 600, 600), "Light Menu", lightMenu.Menu);
            pathMenuWindow = new Window(new Rect(700, 50, 1200, 1000), "Pathing Menu", pathMenu.Menu);

            DontDestroyOnLoad(lightMenu);
            DontDestroyOnLoad(pathMenu);
        }

        void Update() {
            if (Input.GetKeyDown(Plugin.MenuButton.Value.MainKey)) {
                windowOpen = !windowOpen;
                cursorSet = false;
                if (input == null) {
                    input = GameObject.Find("___Input");
                }
            }
        }

        void OnGUI() {
            if (windowOpen) {
                Cursor.visible = true;
                CursorSettings.SetCursor(ECursorType.Idle);
                Cursor.lockState = CursorLockMode.None;

                GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, 30));
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Light Menu", GUILayout.MaxWidth(100))) {
                    lightMenuWindow.Toggle();
                }

                if (GUILayout.Button("Bézier Curve Paths", GUILayout.MaxWidth(140))) {
                    pathMenuWindow.Toggle();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();

                lightMenuWindow.Render(1);
                pathMenuWindow.Render(2);
                cursorSet = false;
            } else {
                if (!cursorSet) {
                    CursorSettings.SetCursor(ECursorType.Invisible);
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    cursorSet = true;
                }
            }
        }
    }


}
