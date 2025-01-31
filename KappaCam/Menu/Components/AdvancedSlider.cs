using System.Collections.Generic;
using UnityEngine;
using System;

namespace KappaCam.Menu.Components {
    public enum SliderMode {
        Direct,
    }

    public static class AdvancedSlider {
        private static Dictionary<string, string> s_incrTextDict = new Dictionary<string, string>();
        private static Dictionary<string, float> s_knobValueDict = new Dictionary<string, float>();
        private static Dictionary<string, bool> s_isDragging = new Dictionary<string, bool>();
        private static Dictionary<string, bool> s_isRegistered = new Dictionary<string, bool>();
        private static Dictionary<string, string> s_currentValueTextDict = new Dictionary<string, string>();

        public static void Draw(
            string uniqueId,
            Func<float> getter,
            Action<float> setter,
            float minValue,
            float maxValue,
            ref bool isChanged,
            SliderMode sliderMode
        ) {

            float currentValue = getter();
            float knobValue = (sliderMode == SliderMode.Direct)
                ? currentValue
                : (s_knobValueDict.ContainsKey(uniqueId) ? s_knobValueDict[uniqueId] : 0f);

            if (!s_knobValueDict.ContainsKey(uniqueId))
                s_knobValueDict[uniqueId] = knobValue;
            if (!s_isDragging.ContainsKey(uniqueId))
                s_isDragging[uniqueId] = false;
            if (!s_incrTextDict.ContainsKey(uniqueId))
                s_incrTextDict[uniqueId] = "1.0";
            if (!s_isRegistered.ContainsKey(uniqueId))
                s_isRegistered[uniqueId] = false;

            if (!s_currentValueTextDict.ContainsKey(uniqueId))
                s_currentValueTextDict[uniqueId] = currentValue.ToString("F2");

            GUILayout.BeginHorizontal();

            float sliderMin = (sliderMode == SliderMode.Direct) ? minValue : -1f;
            float sliderMax = (sliderMode == SliderMode.Direct) ? maxValue : 1f;

            float newKnobValue = GUILayout.HorizontalSlider(knobValue, sliderMin, sliderMax, GUILayout.ExpandWidth(true));
            Rect sliderRect = GUILayoutUtility.GetLastRect();
            if (Mathf.Abs(newKnobValue - knobValue) > 0.0001f) {
                knobValue = newKnobValue;
                if (sliderMode == SliderMode.Direct) { setter(knobValue); }
                s_knobValueDict[uniqueId] = knobValue;
                isChanged = true;

                s_currentValueTextDict[uniqueId] = knobValue.ToString("F2");
            }

            string currentValueStr = s_currentValueTextDict[uniqueId];
            string newValueStr = GUILayout.TextField(currentValueStr, GUILayout.Width(60));

            if (newValueStr != currentValueStr) {
                s_currentValueTextDict[uniqueId] = newValueStr;

                if (float.TryParse(newValueStr, out float newValue)) {
                    newValue = Mathf.Clamp(newValue, minValue, maxValue);

                    setter(newValue);
                    isChanged = true;

                    s_knobValueDict[uniqueId] = newValue;
                }
            }

            string oldIncrText = s_incrTextDict[uniqueId];
            string newIncrText = GUILayout.TextField(oldIncrText, GUILayout.Width(40));
            if (newIncrText != oldIncrText)
                s_incrTextDict[uniqueId] = newIncrText;

            float incrementValue = ParseIncrement(uniqueId);

            if (GUILayout.Button("+", GUILayout.Width(30))) {
                float updatedValue = currentValue + incrementValue;
                updatedValue = Mathf.Clamp(updatedValue, minValue, maxValue);
                setter(updatedValue);
                isChanged = true;

                s_currentValueTextDict[uniqueId] = updatedValue.ToString("F2");
                s_knobValueDict[uniqueId] = updatedValue;
            }
            if (GUILayout.Button("-", GUILayout.Width(30))) {
                float updatedValue = currentValue - incrementValue;
                updatedValue = Mathf.Clamp(updatedValue, minValue, maxValue);
                setter(updatedValue);
                isChanged = true;

                s_currentValueTextDict[uniqueId] = updatedValue.ToString("F2");
                s_knobValueDict[uniqueId] = updatedValue;
            }
            GUILayout.EndHorizontal();
        }

        private static float ParseIncrement(string uniqueId) {
            if (s_incrTextDict.TryGetValue(uniqueId, out string t)) {
                if (float.TryParse(t, out float val))
                    return val;
            }
            return 1f;
        }
    }
}
