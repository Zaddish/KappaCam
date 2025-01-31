using UnityEngine;
using System.Collections;
using KappaCam.Pathing;
using KappaCam.Menu;
using KappaCam;
using EFT.Communications;
using System;
using KappaCam.Menu.Components;

namespace KappaCam.Pathing {
    public class PathingMenu : MonoBehaviour {

        private BezierPathGenerator pathGenerator = new BezierPathGenerator();
        private int selectedKeyframeIndex = -1;
        private Coroutine pathPlaybackCoroutine = null;
        private bool loopPlayback = false;
        private bool isPaused = false;
        private Vector2 scrollPosition = Vector2.zero;

        void Update() {
            if (Input.GetKeyDown(Plugin.CreateKeyframe.Value.MainKey)) {
                pathGenerator.AddKeyframe(Camera.main.transform.position, Camera.main.transform.rotation);
            }
        }

        public void Menu() {

            IEnumerator MoveAlongSplinePath(float duration) {
                if (pathGenerator.keyframedPositions.Count == 0 || pathGenerator.keyframedRotations.Count == 0) {
                    NotificationManagerClass.DisplayMessageNotification("No keyframes available for path playback", ENotificationDurationType.Long);
                    yield break;
                }
                float elapsedTime = 0f;
                float t = 0f;
                while (t < 1f) {
                    if (isPaused) {
                        yield return null;
                        continue;
                    }
                    Camera.main.transform.position = pathGenerator.CalculatePiecewiseBezierPath(t);
                    Camera.main.transform.rotation = pathGenerator.CalculatePiecewiseBezierQuaternion(t);
                    
                    elapsedTime += Time.deltaTime;
                    t = elapsedTime / duration;
                    yield return null;
                }
                if (loopPlayback) {
                    pathPlaybackCoroutine = StartCoroutine(MoveAlongSplinePath(pathGenerator.pathDuration));
                } else {
                    Camera.main.transform.position = pathGenerator.keyframedPositions[pathGenerator.keyframedPositions.Count - 1];
                    Camera.main.transform.rotation = pathGenerator.keyframedRotations[pathGenerator.keyframedRotations.Count - 1];
                    pathGenerator.isPathPlaying = false;
                }
                
            }

            void TogglePathPlayback() {
                if (pathGenerator.isPathPlaying) {
                    if (isPaused) {
                        isPaused = false;
                        Debug.Log("Path playback resumed.");
                    } else {
                        isPaused = true;
                        Debug.Log("Path playback paused.");
                    }
                } else {
                    // Stop any existing playback coroutine as a precaution, even though we check isPathPlaying.
                    
                    if (pathPlaybackCoroutine != null) {
                        KappaCamController.CamViewInControl = true;
                        StopCoroutine(pathPlaybackCoroutine);
                    }
                    
                    pathPlaybackCoroutine = StartCoroutine(MoveAlongSplinePath(pathGenerator.pathDuration));
                    
                    pathGenerator.isPathPlaying = true;
                    KappaCamController.CamViewInControl = false;
                    Debug.Log("Path playback started.");
                    
                }
            }

            void StopPathPlayback() {
                if (pathPlaybackCoroutine != null) {
                    StopCoroutine(pathPlaybackCoroutine);
                }
                pathGenerator.isPathPlaying = false;
                isPaused = false;
                loopPlayback = false;
                KappaCamController.CamViewInControl = true;
                Debug.Log("Path playback stopped.");
            }

            void ToggleLoopPlayback() {
                loopPlayback = !loopPlayback;
                Debug.Log($"Path playback looping set to: {loopPlayback}");
            }


            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Keyframe")) {
                pathGenerator.AddKeyframe(Camera.main.transform.position, Camera.main.transform.rotation);
            }

            if (selectedKeyframeIndex >= 0 && selectedKeyframeIndex < pathGenerator.keyframedPositions.Count) {
                if (GUILayout.Button("Remove Keyframe")) {
                    pathGenerator.RemoveKeyframe(selectedKeyframeIndex);
                    selectedKeyframeIndex = -1;
                }
            }

            // This button toggles visibility of the path and control points in the scene.
            if (GUILayout.Button("Toggle Debug Visuals")) {
                pathGenerator.ToggleDebugVisuals();
            }

            GUILayout.EndHorizontal();

            // Display a slider for adjusting path duration. This affects how quickly the camera moves along the path.
            GUILayout.Label($"Path Duration: {pathGenerator.pathDuration} seconds");
            bool isDurationChanged = false;
            AdvancedSlider.Draw(
                uniqueId: "pathDurationSlider",
                getter: () => pathGenerator.pathDuration,
                setter: (val) => pathGenerator.pathDuration = val,
                minValue: 1.0f,
                maxValue: 60.0f * 30.0f,
                isChanged: ref isDurationChanged,
                sliderMode: SliderMode.Direct
                );
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(pathGenerator.isPathPlaying ? (isPaused ? "Resume Path" : "Pause Path") : "Play Path")) {
                TogglePathPlayback();
            }

            if (GUILayout.Button("Stop Path")) {
                StopPathPlayback();
            }

            if (GUILayout.Button(loopPlayback ? "Disable Loop" : "Enable Loop")) {
                ToggleLoopPlayback();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Keyframed Positions and Rotations:");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(1200), GUILayout.Height(1000));
            for (int i = 0; i < pathGenerator.keyframedPositions.Count; i++) {
                GUILayout.BeginHorizontal();

                pathGenerator.keyframedPositions[i] = utils.Vector3Field($"Position {i}", pathGenerator.keyframedPositions[i]);
                pathGenerator.keyframedRotations[i] = utils.QuaternionField($"Rotation {i}", pathGenerator.keyframedRotations[i]);

                if (GUILayout.Button("Select")) {
                    selectedKeyframeIndex = i;
                }

                if (GUILayout.Button("Remove")) {
                    pathGenerator.RemoveKeyframe(i);
                    if (selectedKeyframeIndex == i) {
                        selectedKeyframeIndex = -1;
                    }
                    break;
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (selectedKeyframeIndex >= 0 && selectedKeyframeIndex < pathGenerator.keyframedPositions.Count) {
                GUILayout.Label($"Selected Keyframe {selectedKeyframeIndex}:");

                pathGenerator.keyframedPositions[selectedKeyframeIndex] = utils.Vector3Field("Position", pathGenerator.keyframedPositions[selectedKeyframeIndex]);
                pathGenerator.keyframedRotations[selectedKeyframeIndex] = utils.QuaternionField("Rotation", pathGenerator.keyframedRotations[selectedKeyframeIndex]);
            }
        }
    }
}
