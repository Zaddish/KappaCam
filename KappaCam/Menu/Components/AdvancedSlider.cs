using UnityEngine;

namespace KappaCam.Menu.Components {
    public class AdvancedSlider {
        private static string incrementStr = "1.0";
        private static string currentValueControlName = "currentValueTextField";
        private static string incrementValueControlName = "incrementValueTextField";

        public static float Draw(float currentValue, float minValue, float maxValue, ref bool isChanged) {
            GUILayout.BeginHorizontal();
            currentValue = GUILayout.HorizontalSlider(currentValue, minValue, maxValue, GUILayout.ExpandWidth(true));

            GUI.SetNextControlName(currentValueControlName);
            string valueStr = GUILayout.TextField(currentValue.ToString("F2"), GUILayout.Width(100));
            if (GUI.GetNameOfFocusedControl() == currentValueControlName) {
                float parsedValue;
                if (float.TryParse(valueStr, out parsedValue) && parsedValue != currentValue) {
                    currentValue = Mathf.Clamp(parsedValue, minValue, maxValue);
                    isChanged = true;
                }
            }

            GUI.SetNextControlName(incrementValueControlName);
            incrementStr = GUILayout.TextField(incrementStr, GUILayout.Width(50));
            float incrementValue;
            if (float.TryParse(incrementStr, out incrementValue)) {
            } else {
                incrementValue = 1.0f;
            }

            if (GUILayout.Button("+", GUILayout.Width(50))) {
                currentValue = Mathf.Clamp(currentValue + incrementValue, minValue, maxValue);
                isChanged = true;
            }
            if (GUILayout.Button("-", GUILayout.Width(50))) {
                currentValue = Mathf.Clamp(currentValue - incrementValue, minValue, maxValue);
                isChanged = true;
            }
            GUILayout.EndHorizontal();

            return currentValue;
        }
    }
}
