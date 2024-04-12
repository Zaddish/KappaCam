﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CamUnsnap.Menu {
    internal class utils {
        public static Vector3 Vector3Field(string label, Vector3 value) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            value.x = float.Parse(GUILayout.TextField(value.x.ToString(), GUILayout.Width(100)));
            value.y = float.Parse(GUILayout.TextField(value.y.ToString(), GUILayout.Width(100)));
            value.z = float.Parse(GUILayout.TextField(value.z.ToString(), GUILayout.Width(100)));
            GUILayout.EndHorizontal();
            return value;
        }

        public static Quaternion QuaternionField(string label, Quaternion value) {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            value.x = float.Parse(GUILayout.TextField(value.x.ToString(), GUILayout.Width(100)));
            value.y = float.Parse(GUILayout.TextField(value.y.ToString(), GUILayout.Width(100)));
            value.z = float.Parse(GUILayout.TextField(value.z.ToString(), GUILayout.Width(100)));
            value.w = float.Parse(GUILayout.TextField(value.w.ToString(), GUILayout.Width(100)));
            GUILayout.EndHorizontal();
            return value;
        }

        public Vector3 Vector3Field(Vector3 value) {
            string xStr = GUILayout.TextField(value.x.ToString(), GUILayout.Width(50));
            string yStr = GUILayout.TextField(value.y.ToString(), GUILayout.Width(50));
            string zStr = GUILayout.TextField(value.z.ToString(), GUILayout.Width(50));
            float x = ParseFloat(xStr);
            float y = ParseFloat(yStr);
            float z = ParseFloat(zStr);
            return new Vector3(x, y, z);
        }

        public static Color RGBColorField(Color color) {
            float r = GUILayout.HorizontalSlider(color.r, 0.0f, 1.0f);
            float g = GUILayout.HorizontalSlider(color.g, 0.0f, 1.0f);
            float b = GUILayout.HorizontalSlider(color.b, 0.0f, 1.0f);
            return new Color(r, g, b);
        }

        private float ParseFloat(string value) {
            if (float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result)) {
                return result;
            }
            return 0f;
        }

    }
}