using UnityEngine;
using System.Collections;
using KappaCam.Menu;
using KappaCam.Menu.Components;

namespace KappaCam.Lights {
    public class LightMenu : MonoBehaviour {

        public LightController lightController;
        private string lightName = "New Light";
        private Vector2 scrollPosition = Vector2.zero;

        private bool isRangeChanged = false;

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
                lightController.CreateLight(lightName, lightController.selectedLightType, lightController.lightColor, lightController.lightIntensity, lightController.lightRange);
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

            GUILayout.BeginHorizontal();
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
            float newRange = AdvancedSlider.Draw(lightController.lightRange, 0.01f, 1000.0f, ref isRangeChanged);

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
