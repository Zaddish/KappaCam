using EFT.UI;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KappaCam.Menu {
    internal static class utils {
        public static Vector3 Vector3Field(string label, Vector3 value) {
            GUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(label)) GUILayout.Label(label, GUILayout.Width(100));

            string xStr = GUILayout.TextField(value.x.ToString("F2"), GUILayout.Width(60));
            string yStr = GUILayout.TextField(value.y.ToString("F2"), GUILayout.Width(60));
            string zStr = GUILayout.TextField(value.z.ToString("F2"), GUILayout.Width(60));

            float.TryParse(xStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float newX);
            float.TryParse(yStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float newY);
            float.TryParse(zStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float newZ);

            GUILayout.EndHorizontal();
            return new Vector3(newX, newY, newZ);
        }

        public static Color RGBColorField(Color color) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("R", GUILayout.Width(20));
            float r = GUILayout.HorizontalSlider(color.r, 0f, 1f, GUILayout.Width(100));
            r = Mathf.Clamp(r, 0f, 1f);

            GUILayout.Label("G", GUILayout.Width(20));
            float g = GUILayout.HorizontalSlider(color.g, 0f, 1f, GUILayout.Width(100));
            g = Mathf.Clamp(g, 0f, 1f);

            GUILayout.Label("B", GUILayout.Width(20));
            float b = GUILayout.HorizontalSlider(color.b, 0f, 1f, GUILayout.Width(100));
            b = Mathf.Clamp(b, 0f, 1f);
            GUILayout.EndHorizontal();

            return new Color(r, g, b, 1f);
        }

        public static Quaternion QuaternionField(string label, Quaternion value) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));

            string xStr = GUILayout.TextField(value.x.ToString("F2"), GUILayout.Width(60));
            string yStr = GUILayout.TextField(value.y.ToString("F2"), GUILayout.Width(60));
            string zStr = GUILayout.TextField(value.z.ToString("F2"), GUILayout.Width(60));
            string wStr = GUILayout.TextField(value.w.ToString("F2"), GUILayout.Width(60));

            float.TryParse(xStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
            float.TryParse(yStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
            float.TryParse(zStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
            float.TryParse(wStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float w);

            GUILayout.EndHorizontal();
            return new Quaternion(x, y, z, w);
        }
    }
}
