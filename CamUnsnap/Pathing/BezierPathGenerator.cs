using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.Collections;
using System.Linq;
using UnityEngine.Rendering;

namespace CamUnsnap.Pathing {
    public class BezierPathGenerator {
        public List<Vector3> keyframedPositions = new List<Vector3>();
        public List<Quaternion> keyframedRotations = new List<Quaternion>();
        public float pathDuration = 5.0f;
        public bool isPathPlaying = false;

        private List<GameObject> debugSpheres = new List<GameObject>();
        private List<LineRenderer> debugLines = new List<LineRenderer>();
        private LineRenderer smoothPathLine;

        private bool showDebugVisuals = false;

        public void AddKeyframe(Vector3 position, Quaternion rotation) {
            keyframedPositions.Add(position);
            keyframedRotations.Add(rotation);
            if (showDebugVisuals) {
                DrawDebugVisuals();
            }
        }

        private void ClearDebugVisuals() {
            foreach (var sphere in debugSpheres) {
                if (sphere != null) Object.Destroy(sphere);
            }
            debugSpheres.Clear();

            foreach (var line in debugLines) {
                if (line != null) Object.Destroy(line.gameObject);
            }
            debugLines.Clear();

            if (smoothPathLine != null) {
                Object.Destroy(smoothPathLine.gameObject);
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
            Object.Destroy(sphere.GetComponent<Collider>());
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

        private void DrawDebugVisuals() {
            ClearDebugVisuals();
            for (int i = 0; i < keyframedPositions.Count; i++) {
                DrawDebugSphere(keyframedPositions[i], 0.2f, Color.red);
                if (i < keyframedPositions.Count - 1) {
                    DrawDebugLine(keyframedPositions[i], keyframedPositions[i + 1], Color.red, 0.01f);
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

            // where the cam is pointing
            for (int i = 0; i < pathPoints.Count; i += 5) {
                Vector3 position = pathPoints[i];
                Quaternion rotation = CalculateBezierQuaternion((float)i / (pathPoints.Count - 1));
                DrawDebugLine(position, position + rotation * Vector3.forward * 0.5f, Color.blue, 0.05f);
            }
        }

        public List<Vector3> CalculateBezierPathPoints() {
            List<Vector3> pathPoints = new List<Vector3>();
            for (int i = 0; i < keyframedPositions.Count - 1; i++) {
                Vector3 p0 = keyframedPositions[Mathf.Max(i - 1, 0)];
                Vector3 p1 = keyframedPositions[i];
                Vector3 p2 = keyframedPositions[i + 1];
                Vector3 p3 = keyframedPositions[Mathf.Min(i + 2, keyframedPositions.Count - 1)];

                Vector3 controlPoint1 = p1 + (p2 - p0) / 6f;
                Vector3 controlPoint2 = p2 - (p3 - p1) / 6f;

                for (float t = 0; t <= 1; t += 0.05f) {
                    Vector3 point = CalculateCubicBezierPoint(p1, controlPoint1, controlPoint2, p2, t);
                    pathPoints.Add(point);
                }
            }
            return pathPoints;
        }

        public Vector3 CalculatePiecewiseBezierPath(float t) {
            if (keyframedPositions.Count < 2) {
                // Not enough points to form a path
                return Vector3.zero;
            }

            // Ensure t is within bounds
            t = Mathf.Clamp01(t);

            // calculate the total number of segments and the specific segment t falls into
            int totalSegments = keyframedPositions.Count - 1;
            float preciseSegmentIndex = t * totalSegments;
            int segmentIndex = Mathf.FloorToInt(preciseSegmentIndex);

            // calculate t for the specific segment
            float tSegment = (preciseSegmentIndex - segmentIndex);

            // this stops an overflow
            segmentIndex = Mathf.Clamp(segmentIndex, 0, totalSegments - 1);

            // get control points for the current segment
            Vector3 p0 = keyframedPositions[Mathf.Max(segmentIndex - 1, 0)];
            Vector3 p1 = keyframedPositions[segmentIndex];
            Vector3 p2 = keyframedPositions[Mathf.Min(segmentIndex + 1, keyframedPositions.Count - 1)];
            Vector3 p3 = keyframedPositions[Mathf.Min(segmentIndex + 2, keyframedPositions.Count - 1)];

            // calc control points for the segment
            Vector3 controlPoint1 = p1 + (p2 - p0) / 6f;
            Vector3 controlPoint2 = p2 - (p3 - p1) / 6f;

            return CalculateCubicBezierPoint(p1, controlPoint1, controlPoint2, p2, tSegment);
        }
        Vector3 CalculateCubicBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        }
        public Quaternion CalculateBezierQuaternion(float t) {
            // Early out if there's not enough rotations
            if (keyframedRotations.Count < 2) {
                return Quaternion.identity;
            }

            int count = keyframedRotations.Count;
            List<Quaternion> tempRotations = new List<Quaternion>(keyframedRotations);

            // De Casteljaus algorithm
            // PLEASE FOR THE LOVE OF GOD HELP ME
            for (int k = 1; k < count; ++k) {
                for (int i = 0; i < count - k; ++i) {
                    tempRotations[i] = Quaternion.Slerp(tempRotations[i], tempRotations[i + 1], t);
                }
            }

            return tempRotations[0];
        }
        public Quaternion CalculateBezierRotation(float t) {
            if (keyframedRotations.Count < 2) {
                return Quaternion.identity;
            }

            // nnormalize t to the number of segments
            float totalSegments = keyframedPositions.Count - 1;
            float segmentIndexFloat = t * totalSegments;
            int segmentIndex = Mathf.FloorToInt(segmentIndexFloat);

            // clamp to ensure we don't exceed bounds
            segmentIndex = Mathf.Clamp(segmentIndex, 0, keyframedRotations.Count - 2);
            float segmentT = segmentIndexFloat - segmentIndex;

            // determine the rotations for the current segment
            Quaternion startRot = keyframedRotations[segmentIndex];
            Quaternion endRot = keyframedRotations[segmentIndex + 1];

            // spherical interpolation for smooth rotation cause normal slerp is shit
            Quaternion interpolatedRotation = Quaternion.Slerp(startRot, endRot, segmentT);

            return interpolatedRotation;
        }
    }
}
