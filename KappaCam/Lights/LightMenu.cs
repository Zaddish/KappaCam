using UnityEngine;
using KappaCam.Menu.Components;
using KappaCam.Menu;

namespace KappaCam.Lights {
    public class LightMenu : MonoBehaviour {
        public LightController lightController;
        private string lightName = "New Light";
        private Vector2 scrollPosition = Vector2.zero;

        private bool isRangeChanged = false;
        private bool isIntensityChanged = false;

        private bool isPositionChangedX = false;
        private bool isPositionChangedY = false;
        private bool isPositionChangedZ = false;

        private bool isRotateChangedX = false;
        private bool isRotateChangedY = false;
        private bool isRotateChangedZ = false;

        private void Awake() {
            lightController = new LightController();
        }

        public void Menu() {
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

            if (GUILayout.Button("Create Light")) {
                lightController.CreateLight(
                    lightName,
                    lightController.selectedLightType,
                    lightController.lightColor,
                    lightController.lightIntensity,
                    lightController.lightRange
                );
            }

            if (lightController.selectedLightGameObject != null) {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Back", GUILayout.Width(100))) {
                    lightController.selectedLightGameObject = null;
                }
                GUILayout.EndHorizontal();

                EditLightUI(lightController.selectedLightGameObject);
            } else {
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

            GUILayout.BeginVertical();
            GUILayout.Label("Position:", GUILayout.Width(100));
            Vector3 newPosition = utils.Vector3Field("", lightController.lightPosition);
            if (newPosition != lightController.lightPosition) {
                lightController.lightPosition = newPosition;
                lightGameObject.transform.position = lightController.lightPosition;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Adjust Position:", GUILayout.Width(250));

            GUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(20));
            AdvancedSlider.Draw("PositionXSlider", () => lightController.lightPosition.x, (v) => lightController.lightPosition.x = v, -1000f, 1000f, ref isPositionChangedX, SliderMode.Direct);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:", GUILayout.Width(20));
            AdvancedSlider.Draw("PositionYSlider", () => lightController.lightPosition.y, (v) => lightController.lightPosition.y = v, -1000f, 1000f, ref isPositionChangedY, SliderMode.Direct);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:", GUILayout.Width(20));
            AdvancedSlider.Draw("PositionZSlider", () => lightController.lightPosition.z, (v) => lightController.lightPosition.z = v, -1000f, 1000f, ref isPositionChangedZ, SliderMode.Direct);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            lightGameObject.transform.position = lightController.lightPosition;

            GUILayout.BeginVertical();
            GUILayout.Label("Rotation:", GUILayout.Width(100));
            Vector3 newRotation = utils.Vector3Field("", lightController.lightRotation);
            if (newRotation != lightController.lightRotation) {
                lightController.lightRotation = newRotation;
                lightGameObject.transform.eulerAngles = lightController.lightRotation;
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Adjust Rotation:", GUILayout.Width(250));

            GUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(20));
            AdvancedSlider.Draw("RotationXSlider", () => lightController.lightRotation.x, (v) => lightController.lightRotation.x = v, 0f, 360f, ref isRotateChangedX, SliderMode.Direct);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y:", GUILayout.Width(20));
            AdvancedSlider.Draw("RotationYSlider", () => lightController.lightRotation.y, (v) => lightController.lightRotation.y = v, 0f, 360f, ref isRotateChangedY, SliderMode.Direct);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Z:", GUILayout.Width(20));
            AdvancedSlider.Draw("RotationZSlider", () => lightController.lightRotation.z, (v) => lightController.lightRotation.z = v, 0f, 360f, ref isRotateChangedZ, SliderMode.Direct);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            lightGameObject.transform.eulerAngles = lightController.lightRotation;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", GUILayout.Width(100));
            int newSelectedLightType = GUILayout.SelectionGrid(
                (int)lightController.selectedLightType,
                new string[] { "Point", "Spot", "Directional" },
                3
            );
            if (newSelectedLightType != (int)lightController.selectedLightType) {
                lightController.selectedLightType = (LightType)newSelectedLightType;
                lightComponent.type = lightController.selectedLightType;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Color:", GUILayout.Width(100));
            Color newColor = utils.RGBColorField(lightController.lightColor);
            if (newColor != lightController.lightColor) {
                lightController.lightColor = newColor;
                lightComponent.color = lightController.lightColor;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Intensity:", GUILayout.Width(100));
            AdvancedSlider.Draw("IntensitySlider", () => lightController.lightIntensity, (v) => lightController.lightIntensity = v, 0f, 10f, ref isIntensityChanged, SliderMode.Direct);
            lightComponent.intensity = lightController.lightIntensity;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Range:", GUILayout.Width(100));
            AdvancedSlider.Draw("RangeSlider", () => lightController.lightRange, (v) => lightController.lightRange = v, 0f, 150f, ref isRangeChanged, SliderMode.Direct);
            lightComponent.range = lightController.lightRange;
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Delete")) {
                lightController.lights.Remove(lightGameObject);
                Destroy(lightGameObject);
                lightController.selectedLightGameObject = null;
            }
        }
    }
}
