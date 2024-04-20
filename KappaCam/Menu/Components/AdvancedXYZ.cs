using UnityEngine;

namespace KappaCam.Menu.Components {
    public class AdvancedXYZ {
        private static string incrementStr = "1.0";
        private static string xyzValuesControlName = "XYZValuesTextField";
        static float initialSliderValue = 0f;
        static bool isDragging = false;

        public static Vector3 Draw(Vector3 currentValues, float minValue, float maxValue, ref bool isChanged) {
            GUILayout.BeginHorizontal();
            incrementStr = GUILayout.TextField(incrementStr, GUILayout.Width(50));
            float incrementValue = float.TryParse(incrementStr, out float parsedIncrement) ? parsedIncrement : 1.0f;
            string xyzValues = GUILayout.TextField($"{currentValues.x:F2}, {currentValues.y:F2}, {currentValues.z:F2}", GUILayout.Width(200));
            GUI.SetNextControlName(xyzValuesControlName);
            if (GUI.GetNameOfFocusedControl() == xyzValuesControlName) {
                string[] values = xyzValues.Split(' ');
                if (values.Length == 3 &&
                    float.TryParse(values[0], out float x) &&
                    float.TryParse(values[1], out float y) &&
                    float.TryParse(values[2], out float z)) {
                    Vector3 parsedValues = new Vector3(x, y, z);
                    if (parsedValues != currentValues) {
                        currentValues = new Vector3(
                            Mathf.Clamp(parsedValues.x, minValue, maxValue),
                            Mathf.Clamp(parsedValues.y, minValue, maxValue),
                            Mathf.Clamp(parsedValues.z, minValue, maxValue)
                        );
                        isChanged = true;
                    }
                }
            }
            
            currentValues.x = DrawAdjustmentControl(currentValues.x, incrementValue, ref isChanged);
            currentValues.y = DrawAdjustmentControl(currentValues.y, incrementValue, ref isChanged);
            currentValues.z = DrawAdjustmentControl(currentValues.z, incrementValue, ref isChanged);

            GUILayout.EndHorizontal();

            return currentValues;
        }

        private static float DrawAdjustmentControl(float value, float increment, ref bool isChanged) {
            if (GUIUtility.hotControl == 0) {
                isDragging = false;
            } else if (GUIUtility.hotControl == GUIUtility.GetControlID(FocusType.Passive)) {
                initialSliderValue = value;
                isDragging = true;
            }
            float sliderValue = GUILayout.HorizontalSlider(isDragging ? value : 0, -1, 1, GUILayout.Width(50));
            if (isDragging) {
                value = initialSliderValue + (sliderValue - initialSliderValue) / increment;
                value = Mathf.Clamp(value, float.MinValue, float.MaxValue);
                isChanged = true;
            }

            if (GUILayout.Button("-", GUILayout.Width(25))) {
                value = Mathf.Clamp(value - increment, float.MinValue, float.MaxValue);
                isChanged = true;
            }
            if (GUILayout.Button("+", GUILayout.Width(25))) {
                value = Mathf.Clamp(value + increment, float.MinValue, float.MaxValue);
                isChanged = true;
            }

            return value;
        }

    }
}
