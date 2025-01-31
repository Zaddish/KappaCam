using UnityEngine;
using System.Collections.Generic;

namespace KappaCam.Pathing {
    public class BezierPathGenerator : MonoBehaviour {
        public List<Vector3> keyframedPositions = new List<Vector3>();
        public List<Quaternion> keyframedRotations = new List<Quaternion>();
        public float pathDuration = 5.0f;
        public bool isPathPlaying = false;

        private List<GameObject> debugSpheres = new List<GameObject>();
        private List<LineRenderer> debugLines = new List<LineRenderer>();
        private LineRenderer smoothPathLine;

        public bool showDebugVisuals = false;

        public void AddKeyframe(Vector3 position, Quaternion rotation) {
            keyframedPositions.Add(position);
            keyframedRotations.Add(rotation);
            if (showDebugVisuals) {
                DrawDebugVisuals();
            }
        }

        public void RemoveKeyframe(int index) {
            if (index >= 0 && index < keyframedPositions.Count) {
                keyframedPositions.RemoveAt(index);
                keyframedRotations.RemoveAt(index);
                if (showDebugVisuals) {
                    DrawDebugVisuals();
                }
            }
        }

        public void ToggleDebugVisuals() {
            showDebugVisuals = !showDebugVisuals;
            if (showDebugVisuals) {
                DrawDebugVisuals();
            } else {
                ClearDebugVisuals();
            }
        }

        private void ClearDebugVisuals() {
            foreach (var sphere in debugSpheres) {
                if (sphere != null) Destroy(sphere);
            }
            debugSpheres.Clear();

            foreach (var line in debugLines) {
                if (line != null) Destroy(line.gameObject);
            }
            debugLines.Clear();

            if (smoothPathLine != null) {
                Destroy(smoothPathLine.gameObject);
                smoothPathLine = null;
            }
        }
        private LineRenderer CreateLineRenderer(Color color, float width) {
            GameObject lineObj = new GameObject("DebugLine");
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.useWorldSpace = true;
            return lineRenderer;
        }

        private GameObject CreateNonCollidingSphere(Vector3 position, float size, Color color) {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = position;
            sphere.transform.localScale = new Vector3(size, size, size);
            Destroy(sphere.GetComponent<Collider>()); // remove collider
            Renderer sphereRenderer = sphere.GetComponent<Renderer>();
            sphereRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            sphereRenderer.material.color = color;
            return sphere;
        }
        private void DrawDebugLine(Vector3 start, Vector3 end, Color color, float width = 0.1f) {
            LineRenderer lineRenderer = CreateLineRenderer(color, width);
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            debugLines.Add(lineRenderer);
        }

        private void DrawDebugSphere(Vector3 position, float size, Color color) {
            GameObject sphere = CreateNonCollidingSphere(position, size, color);
            debugSpheres.Add(sphere);
        }

        private void DrawDebugVisuals() {
            ClearDebugVisuals();
            for (int i = 0; i < keyframedPositions.Count; i++) {
                DrawDebugSphere(keyframedPositions[i], 0.2f, Color.red);
                if (i < keyframedPositions.Count - 1) {
                    DrawDebugLine(keyframedPositions[i], keyframedPositions[i + 1], Color.red, 0.01f);
                }
            }

            if (keyframedPositions.Count >= 3) {
                for (int i = 0; i < keyframedPositions.Count - 1; i++) {
                    Vector3 p0 = keyframedPositions[Mathf.Max(i - 1, 0)];
                    Vector3 p1 = keyframedPositions[i];
                    Vector3 p2 = keyframedPositions[i + 1];
                    Vector3 p3 = keyframedPositions[Mathf.Min(i + 2, keyframedPositions.Count - 1)];

                    Vector3 c1 = p1 + (p2 - p0) / 6f;
                    Vector3 c2 = p2 - (p3 - p1) / 6f;

                    DrawDebugSphere(c1, 0.15f, Color.yellow);
                    DrawDebugSphere(c2, 0.15f, Color.yellow);

                    DrawDebugLine(p1, c1, Color.yellow, 0.01f);
                    DrawDebugLine(p2, c2, Color.yellow, 0.01f);
                }
            }

            List<Vector3> pathPoints = CalculateBezierPathPoints();
            if (pathPoints.Count > 1) {
                if (smoothPathLine == null) {
                    smoothPathLine = CreateLineRenderer(Color.green, 0.15f);
                }
                smoothPathLine.positionCount = pathPoints.Count;
                smoothPathLine.SetPositions(pathPoints.ToArray());
            }

            for (int i = 0; i < pathPoints.Count; i += 5) {
                float t = (float)i / (pathPoints.Count - 1);
                Vector3 position = pathPoints[i];
                Quaternion rotation = CalculatePiecewiseBezierQuaternion(t);
                DrawDebugLine(position, position + rotation * Vector3.forward * 0.5f, Color.blue, 0.05f);
            }
        }

        public List<Vector3> CalculateBezierPathPoints() {
            List<Vector3> pathPoints = new List<Vector3>();
            int count = keyframedPositions.Count;

            if (count == 0) return pathPoints;
            if (count == 1) {
                pathPoints.Add(keyframedPositions[0]);
                return pathPoints;
            }

            if (count == 2) {
                Vector3 p0 = keyframedPositions[0];
                Vector3 p1 = keyframedPositions[1];
                int steps = 20; // arbitrary 
                for (int i = 0; i <= steps; i++) {
                    float t = i / (float)steps;
                    pathPoints.Add(Vector3.Lerp(p0, p1, t));
                }
                return pathPoints;
            }

            for (int i = 0; i < count - 1; i++) {
                Vector3 p0 = keyframedPositions[Mathf.Max(i - 1, 0)];
                Vector3 p1 = keyframedPositions[i];
                Vector3 p2 = keyframedPositions[i + 1];
                Vector3 p3 = keyframedPositions[Mathf.Min(i + 2, count - 1)];

                Vector3 controlPoint1 = p1 + (p2 - p0) / 6f;
                Vector3 controlPoint2 = p2 - (p3 - p1) / 6f;

                for (float t = 0; t <= 1.0001f; t += 0.05f) {
                    Vector3 point = CalculateCubicBezierPoint(p1, controlPoint1, controlPoint2, p2, t);
                    pathPoints.Add(point);
                }
            }
            return pathPoints;
        }

        public Vector3 CalculatePiecewiseBezierPath(float t) {
            int n = keyframedPositions.Count;
            if (n == 0) return Vector3.zero;
            if (n == 1) return keyframedPositions[0];
            if (n == 2) return Vector3.Lerp(keyframedPositions[0], keyframedPositions[1], Mathf.Clamp01(t));

            t = Mathf.Clamp01(t);
            int totalSegments = n - 1;
            float preciseIndex = t * totalSegments;
            int segmentIndex = Mathf.FloorToInt(preciseIndex);

            float tSegment = preciseIndex - segmentIndex;
            segmentIndex = Mathf.Clamp(segmentIndex, 0, totalSegments - 1);

            Vector3 p0 = keyframedPositions[Mathf.Max(segmentIndex - 1, 0)];
            Vector3 p1 = keyframedPositions[segmentIndex];
            Vector3 p2 = keyframedPositions[Mathf.Min(segmentIndex + 1, n - 1)];
            Vector3 p3 = keyframedPositions[Mathf.Min(segmentIndex + 2, n - 1)];

            Vector3 c1 = p1 + (p2 - p0) / 6f;
            Vector3 c2 = p2 - (p3 - p1) / 6f;

            return CalculateCubicBezierPoint(p1, c1, c2, p2, tSegment);
        }

        private Vector3 CalculateCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0)
                 + (3f * uu * t * p1)
                 + (3f * u * tt * p2)
                 + (ttt * p3);
        }

        public Quaternion CalculatePiecewiseBezierQuaternion(float t) {
            int count = keyframedRotations.Count;
            if (count == 0) return Quaternion.identity;
            if (count == 1) return keyframedRotations[0];
            if (count == 2) return Quaternion.Slerp(keyframedRotations[0], keyframedRotations[1], Mathf.Clamp01(t));

            t = Mathf.Clamp01(t);
            int totalSegments = count - 1;
            float preciseIndex = t * totalSegments;
            int segmentIndex = Mathf.FloorToInt(preciseIndex);

            float tSegment = preciseIndex - segmentIndex;
            segmentIndex = Mathf.Clamp(segmentIndex, 0, totalSegments - 1);

            Quaternion q0 = keyframedRotations[Mathf.Max(segmentIndex - 1, 0)];
            Quaternion q1 = keyframedRotations[segmentIndex];
            Quaternion q2 = keyframedRotations[Mathf.Min(segmentIndex + 1, count - 1)];
            Quaternion q3 = keyframedRotations[Mathf.Min(segmentIndex + 2, count - 1)];

            Vector4 v0 = ToVector4(q0);
            Vector4 v1 = ToVector4(q1);
            Vector4 v2 = ToVector4(q2);
            Vector4 v3 = ToVector4(q3);

            if (Vector4.Dot(v1, v2) < 0f) v2 = -v2;
            if (Vector4.Dot(v0, v1) < 0f) v0 = -v0;
            if (Vector4.Dot(v2, v3) < 0f) v3 = -v3;

            Vector4 c1 = v1 + (v2 - v0) / 6f;
            if (c1.sqrMagnitude > 1e-8f) c1.Normalize();
            else c1 = v1;

            Vector4 c2 = v2 - (v3 - v1) / 6f;
            if (c2.sqrMagnitude > 1e-8f) c2.Normalize();
            else c2 = v2;

            Vector4 result = CalculateCubicBezierPoint4D(v1, c1, c2, v2, tSegment);
            return NormalizeQuaternion(ToQuaternion(result));
        }

        private Vector4 CalculateCubicBezierPoint4D(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, float t) {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            return (uuu * p0)
                 + (3f * uu * t * p1)
                 + (3f * u * tt * p2)
                 + (ttt * p3);
        }

        private Vector4 ToVector4(Quaternion q) {
            return new Vector4(q.w, q.x, q.y, q.z);
        }
        private Quaternion ToQuaternion(Vector4 v) {
            return new Quaternion(v.y, v.z, v.w, v.x);
        }

        private Quaternion NormalizeQuaternion(Quaternion q) {
            float mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (mag < 1e-8f) return Quaternion.identity;
            return new Quaternion(q.x / mag, q.y / mag, q.z / mag, q.w / mag);
        }
    }
}
