using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using System.Linq;
using UnityEngine.Rendering;
using CamUnsnap.Pathing;
using CamUnsnap.Lights;
using CamUnsnap.Menu;

namespace CamUnsnap.Menu
{

    public class CUSMenu : MonoBehaviour
    {
        public static bool Menu = false;
        private static Rect windowLight = new Rect(50, 50, 600, 600);
        private static Rect windowPathing = new Rect(650, 50, 1200, 1000);

        private readonly string[] attachTypes = new string[] { "lookAt", "orbit", "lock" };
        private int currentSelectionIndex = 0;
        private Vector2 scrollPosition = Vector2.zero;


        // pathing shit
        private BezierPathGenerator pathGenerator = new BezierPathGenerator();
        private int selectedKeyframeIndex = -1;
        private Coroutine pathPlaybackCoroutine = null;


        // Light settings for editing
        public LightController lightController;
        private string lightName = "New Light";

        private void Awake() {
            lightController = new LightController();
        }
        void Update() {
            if (Input.GetKeyDown(Plugin.MenuButton.Value.MainKey)) {
                ToggleMenu();
                Cursor.visible = Menu;
                Cursor.lockState = Menu ? CursorLockMode.Confined : CursorLockMode.Locked;
            }
            if (Input.GetKeyDown(Plugin.CreateKeyframe.Value.MainKey)) {
                pathGenerator.AddKeyframe(Camera.main.transform.position, Camera.main.transform.rotation);
            }
        }
        /// <TODO>
        /// Somehow fix the mouse flickering when menu is open
        /// </TODO>
        void ToggleMenu()
        {
            Menu = !Menu;
            if (Menu)
            {
                Cursor.lockState = CursorLockMode.Confined;
                lightController.selectedLightGameObject = null;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        void OnGUI()
        {
            if (Menu)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
                windowLight = GUI.Window(0, windowLight, CUSLight, "CUS Menu");
                windowPathing = GUI.Window(1, windowPathing, CUSPathing, "Bézier Curve Paths");
            }
        }

        

       

        private void CUSPathing(int windowID)
        {

            IEnumerator MoveAlongSplinePath(float duration) {
                float elapsedTime = 0f;
                float t = 0f;

                while (t < 1f) {
                    Camera.main.transform.position = pathGenerator.CalculatePiecewiseBezierPath(t);
                    Camera.main.transform.rotation = pathGenerator.CalculateBezierQuaternion(t);
                    elapsedTime += Time.deltaTime;
                    t = elapsedTime / duration;
                    yield return null;
                }

                Camera.main.transform.position = pathGenerator.keyframedPositions[pathGenerator.keyframedPositions.Count - 1];
                Camera.main.transform.rotation = pathGenerator.keyframedRotations[pathGenerator.keyframedRotations.Count - 1];
                pathGenerator.isPathPlaying = false;
            }

            void TogglePathPlayback() {
                if (pathGenerator.isPathPlaying) {
                    Debug.Log("Path is already playing.");
                } else {
                    // Stop any existing playback coroutine as a precaution, even though we check isPathPlaying.
                    if (pathPlaybackCoroutine != null) {
                        StopCoroutine(pathPlaybackCoroutine);
                    }
                    pathPlaybackCoroutine = StartCoroutine(MoveAlongSplinePath(pathGenerator.pathDuration));
                }
            }


            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Keyframe"))
            {
                pathGenerator.AddKeyframe(Camera.main.transform.position, Camera.main.transform.rotation);
            }

            if (selectedKeyframeIndex >= 0 && selectedKeyframeIndex < pathGenerator.keyframedPositions.Count)
            {
                if (GUILayout.Button("Remove Keyframe"))
                {
                    pathGenerator.RemoveKeyframe(selectedKeyframeIndex);
                    selectedKeyframeIndex = -1;
                }
            }

            // This button toggles visibility of the path and control points in the scene.
            if (GUILayout.Button("Toggle Debug Visuals"))
            {
                pathGenerator.ToggleDebugVisuals();
            }

            GUILayout.EndHorizontal();

            // Display a slider for adjusting path duration. This affects how quickly the camera moves along the path.
            GUILayout.Label($"Path Duration: {pathGenerator.pathDuration} seconds");
            pathGenerator.pathDuration = GUILayout.HorizontalSlider(pathGenerator.pathDuration, 1.0f, 60.0f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(pathGenerator.isPathPlaying ? "Pause Path" : "Play Path"))
            {
                TogglePathPlayback();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Keyframed Positions and Rotations:");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(1200), GUILayout.Height(1000));
            for (int i = 0; i < pathGenerator.keyframedPositions.Count; i++)
            {
                GUILayout.BeginHorizontal();

                pathGenerator.keyframedPositions[i] = utils.Vector3Field($"Position {i}", pathGenerator.keyframedPositions[i]);
                pathGenerator.keyframedRotations[i] = utils.QuaternionField($"Rotation {i}", pathGenerator.keyframedRotations[i]);

                if (GUILayout.Button("Select"))
                {
                    selectedKeyframeIndex = i;
                }

                if (GUILayout.Button("Remove"))
                {
                    pathGenerator.RemoveKeyframe(i);
                    if (selectedKeyframeIndex == i)
                    {
                        selectedKeyframeIndex = -1;
                    }
                    break;
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (selectedKeyframeIndex >= 0 && selectedKeyframeIndex < pathGenerator.keyframedPositions.Count)
            {
                GUILayout.Label($"Selected Keyframe {selectedKeyframeIndex}:");

                pathGenerator.keyframedPositions[selectedKeyframeIndex] = utils.Vector3Field("Position", pathGenerator.keyframedPositions[selectedKeyframeIndex]);
                pathGenerator.keyframedRotations[selectedKeyframeIndex] = utils.QuaternionField("Rotation", pathGenerator.keyframedRotations[selectedKeyframeIndex]);
            }
        }


        void CUSLight(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.BeginVertical();
            GUILayout.Label("Attach Settings", GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Attach Type:");
            int newSelectionIndex = GUILayout.SelectionGrid(currentSelectionIndex, attachTypes, attachTypes.Length);
            if (newSelectionIndex != currentSelectionIndex)
            {
                currentSelectionIndex = newSelectionIndex;
                CUSController.attachType = attachTypes[currentSelectionIndex];
            }
            GUILayout.EndHorizontal();


            // Light creation UI
            GUILayout.Label("Light Settings", GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();

            GUILayout.Label("Light Name:");
            lightName = GUILayout.TextField(lightName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Light Type:");
            int selectedLightTypeIndex = (int)lightController.selectedLightType;
            selectedLightTypeIndex = GUILayout.SelectionGrid(selectedLightTypeIndex, new string[] { "Point", "Spot", "Directional" }, 3);
            if (selectedLightTypeIndex != (int)lightController.selectedLightType) {
                lightController.selectedLightType = (UnityEngine.LightType)selectedLightTypeIndex;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Intensity:");
            lightController.lightIntensity = GUILayout.HorizontalSlider(lightController.lightIntensity, 0.0001f, 100.0f);

            GUILayout.Label("Range:");
            lightController.lightRange = GUILayout.HorizontalSlider(lightController.lightRange, 0.0f, 1000.0f);

            if (GUILayout.Button("Create Light"))
            {
                lightController.CreateLight(lightName, lightController.selectedLightType, lightController.lightColor, lightController.lightIntensity, lightController.lightRange);
            }

            GUILayout.EndVertical();

            if (lightController.selectedLightGameObject != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Back", GUILayout.Width(100)))
                {
                    lightController.selectedLightGameObject = null;
                }
                GUILayout.EndHorizontal();

                EditLightUI(lightController.selectedLightGameObject);
            }
            else
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(780), GUILayout.Height(550));

               
                foreach (var lightGameObject in lightController.lights) {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(lightGameObject.name, GUILayout.Width(200));
                    if (GUILayout.Button("Edit", GUILayout.Width(50))) {
                        lightController.SelectLight(lightGameObject);
                    }
                    if (GUILayout.Button("Delete", GUILayout.Width(50))) {
                        Destroy(lightGameObject);
                        lightController.lights.Remove(lightGameObject);
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
               
                
                GUILayout.EndScrollView();
            }

            

        }
        private void EditLightUI(GameObject lightGameObject) {
            GUILayout.Label("Editing Light: " + lightGameObject.name);

            Light lightComponent = lightGameObject.GetComponent<Light>();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Position: ");
            Vector3 newPosition = utils.Vector3Field("position", lightController.lightPosition);
            if (newPosition != lightController.lightPosition) {
                lightController.lightPosition = newPosition;
                lightGameObject.transform.position = lightController.lightPosition;
            }
            lightController.lightPosition.x += GUILayout.HorizontalSlider(0, -1.0f, 1.0f);
            lightController.lightPosition.y += GUILayout.HorizontalSlider(0, -1.0f, 1.0f);
            lightController.lightPosition.z += GUILayout.HorizontalSlider(0, -1.0f, 1.0f);
            GUILayout.EndHorizontal();

            lightGameObject.transform.position = lightController.lightPosition;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Rotation: ");
            Vector3 newRotation = utils.Vector3Field("rotation", lightController.lightRotation);
            if (newRotation != lightController.lightRotation) {
                lightController.lightRotation = newRotation;
                lightGameObject.transform.eulerAngles = lightController.lightRotation;
            }
            lightController.lightRotation.x = GUILayout.HorizontalSlider(lightController.lightRotation.x, -360.0f, 360.0f);
            lightController.lightRotation.y = GUILayout.HorizontalSlider(lightController.lightRotation.y, -360.0f, 360.0f);
            lightController.lightRotation.z = GUILayout.HorizontalSlider(lightController.lightRotation.z, -360.0f, 360.0f);
            GUILayout.EndHorizontal();

            lightGameObject.transform.eulerAngles = lightController.lightRotation;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Type: ");
            int newSelectedLightType = GUILayout.SelectionGrid((int)lightController.selectedLightType, new string[] { "Point", "Spot", "Directional" }, 3);
            if (newSelectedLightType != (int)lightController.selectedLightType) {
                lightController.selectedLightType = (LightType)newSelectedLightType;
                lightComponent.type = lightController.selectedLightType;
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Color: ");
            Color newColor = utils.RGBColorField(lightController.lightColor);
            if (newColor != lightController.lightColor) {
                lightController.lightColor = newColor;
                lightComponent.color = lightController.lightColor;
            }

            GUILayout.Label("Intensity: ");
            float newIntensity = GUILayout.HorizontalSlider(lightController.lightIntensity, 0.0f, 100.0f);
            if (newIntensity != lightController.lightIntensity) {
                lightController.lightIntensity = newIntensity;
                lightComponent.intensity = lightController.lightIntensity;
            }

            GUILayout.Label("Range: ");
            float newRange = GUILayout.HorizontalSlider(lightController.lightRange, 0.0f, 1000.0f);
            if (newRange != lightController.lightRange) {
                lightController.lightRange = newRange;
                lightComponent.range = lightController.lightRange;
            }

            if (GUILayout.Button("Delete")) {
                lightController.lights.Remove(lightGameObject);
                Destroy(lightGameObject);
                lightController.selectedLightGameObject = null;
            }
        }


    }
}
