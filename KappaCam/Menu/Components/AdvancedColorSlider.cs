using UnityEngine;

namespace KappaCam.Menu.Components {
    public class AdvancedColorSlider {
        private static string incrementStr = "0.1";
        private static string[] currentValueControlNames = new string[] { "RValueTextField", "GValueTextField", "BValueTextField" };
        private static string incrementValueControlName = "IncrementValueTextField";

        public static Color Draw(Color currentColor, float minValue, float maxValue, ref bool isChanged) {
            GUILayout.BeginHorizontal();
            Color tempColor = currentColor;
            float[] colorValues = new float[] { currentColor.r, currentColor.g, currentColor.b };

            for (int i = 0; i < 3; i++) {
                colorValues[i] = GUILayout.HorizontalSlider(colorValues[i], minValue, maxValue, GUILayout.ExpandWidth(true));
                GUI.SetNextControlName(currentValueControlNames[i]);
                string valueStr = GUILayout.TextField(colorValues[i].ToString("F2"), GUILayout.Width(100));
                if (GUI.GetNameOfFocusedControl() == currentValueControlNames[i]) {
                    float parsedValue;
                    if (float.TryParse(valueStr, out parsedValue) && Mathf.Abs(parsedValue - colorValues[i]) > Mathf.Epsilon) {
                        colorValues[i] = Mathf.Clamp(parsedValue, minValue, maxValue);
                        isChanged = true;
                    }
                }
            }

            tempColor = new Color(colorValues[0], colorValues[1], colorValues[2], currentColor.a);

            GUI.SetNextControlName(incrementValueControlName);
            incrementStr = GUILayout.TextField(incrementStr, GUILayout.Width(50));
            float incrementValue;
            if (!float.TryParse(incrementStr, out incrementValue)) {
                incrementValue = 0.1f;
            }

            if (GUILayout.Button("+", GUILayout.Width(50))) {
                for (int i = 0; i < 3; i++) {
                    colorValues[i] = Mathf.Clamp(colorValues[i] + incrementValue, minValue, maxValue);
                }
                isChanged = true;
            }
            if (GUILayout.Button("-", GUILayout.Width(50))) {
                for (int i = 0; i < 3; i++) {
                    colorValues[i] = Mathf.Clamp(colorValues[i] - incrementValue, minValue, maxValue);
                }
                isChanged = true;
            }

            currentColor = new Color(colorValues[0], colorValues[1], colorValues[2], currentColor.a);
            GUIStyle colorBoxStyle = new GUIStyle(GUI.skin.box);
            colorBoxStyle.normal.background = MakeTex(2, 2, currentColor);
            GUILayout.Box("", colorBoxStyle, GUILayout.Width(25), GUILayout.Height(25));

            GUILayout.EndHorizontal();

            return currentColor;
        }

        private static Texture2D MakeTex(int width, int height, Color col) {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
