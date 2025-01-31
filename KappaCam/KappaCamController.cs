using EFT;
using EFT.UI;
using System;
using System.Linq;
using UnityEngine;
using Comfort.Common;
using EFT.Communications;
using MonoMod.RuntimeDetour;
using System.Threading.Tasks;
using System.Collections.Generic;
using Koenigz.PerfectCulling;
using UnityEngine.SceneManagement;

namespace KappaCam {
    /// <summary>
    /// Represents a stream of positions and angles captured from a parent GameObject
    /// </summary>
    public struct TransformRecording {
        /// <summary>
        /// Creates a new transform recording stream
        /// </summary>
        /// <param name="Target">The GameObject to capture transform data from</param>
        public TransformRecording(GameObject Target) {
            this.Target = Target;
            Positions = new List<Vector3>();
            Angles = new List<Vector3>();
        }

        /// <summary>
        /// Captures and saves the position and angles of the target GameObject on the current frame
        /// </summary>
        public void Capture() {
            Positions.Add(Target.transform.position);
            Angles.Add(Target.transform.localEulerAngles);
        }

        /// <summary>
        /// Clears all recorded position and angle streams
        /// </summary>
        public void Clear() {
            Positions = new List<Vector3>();
            Angles = new List<Vector3>();
        }

        /// <summary>
        /// Checks if any values are present in the current streams
        /// </summary>
        /// <returns>true if there are any non-null values, false otherwise</returns>
        public bool Any() => Positions.Any();

        public Vector3[] this[int index] {
            get => new Vector3[] { Positions[index], Angles[index] };
        }

        public int Length { get => Positions.Count - 1; }

        public List<Vector3> Positions { get; private set; }

        public List<Vector3> Angles { get; private set; }

        
        public readonly GameObject Target;
    }


    public class KappaCamController : MonoBehaviour {
        public static bool mCamUnsnapped = false;
        bool Recording = false;
        bool playingPath = false;
        int currentRecordingIndex = 0;
        GameObject gameCamera;
        
        private Vector3? MemoryPos = null;
        Vector3 currentVelocity = Vector3.zero;
        Vector2 smoothedMouseDelta;
        Vector2 currentMouseDelta;
        private float totalRotationX = 0f;
        private float totalRotationY = 0f;

        List<Detour> Detours = new List<Detour>();
        List<Vector3> MemoryPosList = new List<Vector3>();
        TransformRecording PathRecording;
        int currentListIndex = 0;
        float cacheFOV = 0;
        public enum attachTypes {
            lookAt,
            orbit,
            parented,
        }
        
        public static string AttachSpecific = "";
        GameObject AttachedTarget;

        public static float raycastDistance = 100f;
        public static string hitObjectName;

        public static float focusAdjustmentSpeed = 2f;
        public static float verticalFocusAdjustment = 2f;

        float currentFOV = Plugin.CameraFOV.Value;
        float targetFOV;

        public static float zoomVelocity = 0.8f;
        public static float zoomSmoothTime = 0.3f;

        public static bool CamViewInControl { get; set; } = true;
        Player player { get => gameWorld.MainPlayer; }
        GameWorld gameWorld { get => Singleton<GameWorld>.Instance; }
        float MovementSpeed { get => Plugin.MovementSpeed.Value; }
        float CameraSensitivity { get => Plugin.CameraSensitivity.Value; }
        float CameraSmoothing { get => Plugin.CameraSmoothing.Value; }

        GameObject commonUI { get => MonoBehaviourSingleton<CommonUI>.Instance.gameObject; }
        GameObject preloaderUI { get => MonoBehaviourSingleton<PreloaderUI>.Instance.gameObject; }
        GameObject gameScene { get => MonoBehaviourSingleton<GameUI>.Instance.gameObject; }

        private bool cullingIsDisabled = false;
        private DisablerCullingObjectBase[] allDisablerObjects;
        private readonly List<PerfectCullingBakeGroup> previouslyEnabledBakeGroups = new List<PerfectCullingBakeGroup>();

        bool GamespeedChanged {
            get => Time.timeScale != 1f;
            set {
                Time.timeScale = value ? Plugin.Gamespeed.Value : 1f;
            }
        }

        bool UIEnabled {
            get => commonUI.activeSelf && preloaderUI.activeSelf && gameScene.activeSelf;
            set {
                commonUI.SetActive(value);
                preloaderUI.SetActive(value);
                gameScene.SetActive(value);
            }
        }

        bool playerAirborne {
            get => !player.CharacterController.isGrounded;
        }

        bool CamUnsnapped {
            get => mCamUnsnapped;
            set {
                
                if (SceneManager.GetSceneByName("bunker_2") != null)
                {
                    IEnumerable<Behaviour> comps = Camera.main.GetComponentsInChildren<Behaviour>();
                    foreach (Behaviour comp in comps)
                    {
                        string comp_type = comp.GetType().ToString();
                        Debug.Log(comp_type);
                        if (comp.GetType().FullName == "Cinemachine.CinemachineBrain")
                        {
                            comp.enabled = !value;
                            break;
                        }
                    }
                }

                if (!value) {
                    if (!Plugin.OverrideGameRestriction.Value) {
                        if (Ready()) {
                            player.PointOfView = EPointOfView.FirstPerson;
                        }
                        if (Detours.Any())
                            Detours.ForEach((Detour det) => det.Dispose());
                        Detours.Clear();

                        if (!UIEnabled) {
                            try {
                                commonUI.SetActive(true);
                                preloaderUI.SetActive(true);
                                gameScene.SetActive(true);
                            } catch (Exception e) {
                                Plugin.logger.LogError($"bruh\n{e}");
                            }
                            UIEnabled = true;
                        }
                        Camera.main.fieldOfView = cacheFOV;
                    }
                } else {
                    if (player != null) {
                        player.PointOfView = EPointOfView.FreeCamera;
                        player.PointOfView = EPointOfView.ThirdPerson;
                    }
                    cacheFOV = Camera.main.fieldOfView;
                    if (Plugin.OverrideGameRestriction.Value)
                        SendNotification("Session Override is enabled, player and positioning options are ignored, and controlling the camera outside of a raid may cause issues.\nYou've been warned...");
                }
                mCamUnsnapped = value;
            }
        }

        void OnGUI() {
            if (CamUnsnapped && Plugin.AttachableCrosshair.Value) {
                float crosshairSize = 3;
                Vector2 centerPoint = new Vector2(Screen.width / 2, Screen.height / 2);
                GUI.DrawTexture(new Rect(centerPoint.x - (crosshairSize / 2), centerPoint.y - (crosshairSize / 2), crosshairSize, crosshairSize), Texture2D.whiteTexture);
                GUI.Label(new Rect(centerPoint.x - 50, centerPoint.y + 20, 100, 20), hitObjectName);
            }
        }

        void Update() {
            if (Input.GetKeyDown(Plugin.ToggleCameraSnap.Value.MainKey)) {

                CamUnsnapped = !CamUnsnapped;
            }

            if (Input.GetKeyDown(Plugin.CameraMouse.Value.MainKey))
                CamViewInControl = !CamViewInControl;

            if (Input.GetKeyDown(Plugin.ChangeGamespeed.Value.MainKey))
                GamespeedChanged = !GamespeedChanged;

            if (Input.GetKeyDown(Plugin.HideUI.Value.MainKey))
                UIEnabled = !UIEnabled;

            try { if (Plugin.Godmode.Value == true) { player.ActiveHealthController.SetDamageCoeff(Plugin.Godmode.Value ? 0 : player.ActiveHealthController.DamageCoeff != 1 && !playerAirborne ? 1 : 0); } } catch { }


            if (CamUnsnapped) {

                try {

                    float fastMove = Input.GetKey(Plugin.FastMove.Value.MainKey) ? Plugin.FastMoveMult.Value : 1f;
                    gameCamera = Camera.main.gameObject;

                    if (!Plugin.OverrideGameRestriction.Value && Ready()) {
                        if (Input.GetKeyDown(Plugin.disableCulling.Value.MainKey)) {
                            if (allDisablerObjects == null || allDisablerObjects.Length == 0) {
                                allDisablerObjects = FindObjectsOfType<DisablerCullingObjectBase>();
                                if (allDisablerObjects == null || allDisablerObjects.Length == 0) {
                                    Debug.LogWarning("Could not find any DisablerCullingObjectBase. Are we in a raid?");
                                    return;
                                }
                            }
                            Helpers.ToggleCulling(ref cullingIsDisabled, allDisablerObjects, previouslyEnabledBakeGroups);
                        }

                        // ------ RECORDING STUFF ------
                        if (PathRecording.Target == null) {
                            PathRecording = new TransformRecording(gameCamera); 
                        }

                        if (Input.GetKeyDown(Plugin.GoToPos.Value.MainKey)) {
                            if (!MemoryPos.HasValue)
                                SendNotification("No memory pos to move camera to.");
                            else
                                gameCamera.transform.position = MemoryPos.Value;
                        }

                        if (Input.GetKeyDown(Plugin.PlayRecord.Value.MainKey))
                            playingPath = true;

                        if (Recording)
                            PathRecording.Capture();

                        if (playingPath) {
                            Vector3[] transformFrame = PathRecording[currentRecordingIndex];
                            gameCamera.transform.position = transformFrame[0];
                            gameCamera.transform.localEulerAngles = transformFrame[1];

                            currentRecordingIndex++;

                            if (currentRecordingIndex > PathRecording.Length) // fuckers took my .Length cant have shit with VS
                            {
                                currentRecordingIndex = 0;
                                playingPath = false;
                                return;
                            }

                            return;
                        }

                        if (Input.GetKeyDown(Plugin.MovePlayerToCam.Value.MainKey))
                            MovePlayer();

                        if (Input.GetKeyDown(Plugin.BeginRecord.Value.MainKey)) {
                            Recording = true;
                            PathRecording.Clear();
                            SendNotification("Recording Started", false);
                        }

                        if (Input.GetKeyDown(Plugin.ResumeRecord.Value.MainKey)) {
                            if (PathRecording.Any()) {
                                Recording = true;
                                SendNotification("Recording Resumed", false);
                            } else SendNotification($"Cannot resume recording\nNo previous recording exists, press '{Plugin.BeginRecord.Value}' to start a new one");
                        }

                        if (Input.GetKeyDown(Plugin.StopRecord.Value.MainKey)) {
                            Recording = false;
                            SendNotification("Recording Stopped", false);
                        }





                        // ------ PLAYER OVERRIDES & RANDO ------

                        if (Input.GetKeyDown(Plugin.RememberPos.Value.MainKey))
                            MemoryPos = gameCamera.transform.position;

                        if (Input.GetKeyDown(Plugin.LockPlayerMovement.Value.MainKey)) {
                            if (!Detours.Any())
                                Detours = new List<Detour>()
                                {
                                    new Detour(typeof(Player).GetMethod(nameof(Player.Move)).CreateDelegate(player), (Action)BlankOverride),
                                    new Detour(typeof(Player).GetMethod(nameof(Player.Rotate)).CreateDelegate(player), (Action)BlankOverride),
                                    new Detour(typeof(Player).GetMethod(nameof(Player.SlowLean)).CreateDelegate(player), (Action)BlankOverride),
                                    new Detour(typeof(Player).GetMethod(nameof(Player.ChangePose)).CreateDelegate(player), (Action)BlankOverride),
                                    new Detour(typeof(Player).GetMethod(nameof(Player.Jump)).CreateDelegate(player), (Action)BlankOverride),
                                    new Detour(typeof(Player).GetMethod(nameof(Player.ToggleProne)).CreateDelegate(player), (Action)BlankOverride)
                                };
                            else {
                                Detours.ForEach((Detour det) => det.Dispose());
                                Detours.Clear();
                            };
                        }

                        if (Input.GetKeyDown(Plugin.AddToMemPosList.Value.MainKey))
                            MemoryPosList.Add(gameCamera.transform.position);

                        if (Input.GetKeyDown(Plugin.AdvanceList.Value.MainKey)) {
                            if (MemoryPosList[currentListIndex + 1] != null) {
                                currentListIndex++;
                                gameCamera.transform.position = MemoryPosList[currentListIndex];
                            } else if (MemoryPosList.First() != null) {
                                currentListIndex = 0;
                                gameCamera.transform.position = MemoryPosList.First();
                            } else {
                                currentListIndex = 0;
                                SendNotification("No valid Vector3 in Memory Position List to move to.");
                            }
                        }

                        if (Input.GetKeyDown(Plugin.ClearList.Value.MainKey))
                            MemoryPosList.Clear();

                    } else if (!Ready() && MemoryPosList.Any()) {
                        MemoryPosList.Clear();
                        CamUnsnapped = false;
                        return;
                    }

                    // ------- INPUT CONTROLS -------

                    if (Input.GetKey(Plugin.SpeedKey.Value.MainKey)) {
                        float scroll = Input.GetAxis("Mouse ScrollWheel");
                        if (scroll > 0) {
                            Plugin.MovementSpeed.Value += Plugin.speedAdjustmentFactor.Value;
                        } else if (scroll < 0) {
                            Plugin.MovementSpeed.Value = Mathf.Max(0.1f, Plugin.MovementSpeed.Value - Plugin.speedAdjustmentFactor.Value);
                        }
                    }

                    float delta = !GamespeedChanged ? Time.deltaTime : Time.fixedDeltaTime;
                    Vector3 targetVelocity = Vector3.zero;

                    if (Input.GetKey(Plugin.CamLeft.Value.MainKey))
                        targetVelocity += -gameCamera.transform.right * MovementSpeed * fastMove;

                    if (Input.GetKey(Plugin.CamRight.Value.MainKey))
                        targetVelocity += gameCamera.transform.right * MovementSpeed * fastMove;

                    if (Input.GetKey(Plugin.CamForward.Value.MainKey))
                        targetVelocity += gameCamera.transform.forward * MovementSpeed * fastMove;

                    if (Input.GetKey(Plugin.CamBack.Value.MainKey))
                        targetVelocity += -gameCamera.transform.forward * MovementSpeed * fastMove;

                    if (Input.GetKey(Plugin.CamUp.Value.MainKey))
                        targetVelocity += gameCamera.transform.up * MovementSpeed * fastMove;

                    if (Input.GetKey(Plugin.CamDown.Value.MainKey))
                        targetVelocity += -gameCamera.transform.up * MovementSpeed * fastMove;

                    currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1 - Plugin.Friction.Value);
                    gameCamera.transform.position += currentVelocity * delta;





                    /// ------- MOUSE MOVEMENT -------
                    if (CamViewInControl) {
                        currentMouseDelta.x = Input.GetAxis("Mouse X") * CameraSensitivity;
                        currentMouseDelta.y = Input.GetAxis("Mouse Y") * CameraSensitivity;

                        smoothedMouseDelta = Vector2.Lerp(smoothedMouseDelta, currentMouseDelta, CameraSmoothing);

                        // Accumulate the smoothed delta to total rotation
                        totalRotationX += smoothedMouseDelta.x;
                        totalRotationY -= smoothedMouseDelta.y;

                        // was issues with looking directly up, allowing the camera to feely spin around also allows for cooler shots
                        gameCamera.transform.rotation = Quaternion.Euler(totalRotationY, totalRotationX, 0f);
                    }

                    // ------ ZOOM FOV STUFF ------
                    if (!Input.GetKey(Plugin.ZoomKey.Value.MainKey)) {
                        targetFOV = Plugin.CameraFOV.Value;
                    } else {
                        float scrollDelta = -Input.GetAxis("Mouse ScrollWheel");
                        if (scrollDelta != 0) {
                            float zoomFactor = Mathf.Exp(Plugin.ZoomSpeed.Value);
                            targetFOV *= zoomFactor;
                            targetFOV = Mathf.Clamp(targetFOV, 5, 170);
                        }
                    }
                    currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref zoomVelocity, zoomSmoothTime);
                    Camera.main.fieldOfView = currentFOV;

                    // ------- ATTACH TO OBJECT -------
                    if (Input.GetKey(Plugin.DetatchCameraFollow.Value.MainKey) && AttachedTarget != null) {
                        gameCamera.transform.SetParent(null);
                        AttachedTarget = null;
                        AttachSpecific = "";
                        if (Plugin.SelectedAttachType.Value != attachTypes.parented) {
                            CamViewInControl = true;
                        }
                        SendNotification("Detached");
                    } else if (Input.GetKey(Plugin.AttachCameraFollow.Value.MainKey) && AttachedTarget == null) {
                        Ray ray = new Ray(gameCamera.transform.position, gameCamera.transform.forward);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, raycastDistance)) {
                            AttachedTarget = hit.collider.gameObject;
                            if (Plugin.SelectedAttachType.Value == attachTypes.parented) {
                                CamViewInControl = true;
                            } else { CamViewInControl = false; }
                            SendNotification("Attached to: " + hit.collider.gameObject.name);
                        }
                    }

                    if (UIEnabled && AttachedTarget == null) {
                        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
                        RaycastHit hit;
                        if (Physics.Raycast(ray, out hit, raycastDistance)) {
                            hitObjectName = hit.collider.gameObject.name;
                        } else {
                            hitObjectName = "";
                        }
                    }

                    if (!string.IsNullOrEmpty(AttachSpecific)) {
                        GameObject targetObject = GameObject.Find(AttachSpecific);
                        if (targetObject != null && AttachedTarget == null) {
                            AttachedTarget = targetObject;
                            if (Plugin.SelectedAttachType.Value == attachTypes.parented) {
                                CamViewInControl = true;
                            } else { CamViewInControl = false; }
                            SendNotification("Attached to: " + AttachedTarget.gameObject.name, false);
                        } else if (targetObject == null) {
                            AttachSpecific = "";
                            SendNotification("Could not find the object given in the path");
                        }
                    }

                    if (AttachedTarget != null && AttachedTarget.transform.position != null) {
                        if (Plugin.SelectedAttachType.Value == attachTypes.orbit) {
                            gameCamera.transform.RotateAround(AttachedTarget.transform.position, Vector3.up, Time.deltaTime * MovementSpeed);


                        } else if (Plugin.SelectedAttachType.Value == attachTypes.lookAt && AttachedTarget != null) {
                            Vector3 targetPosition = AttachedTarget.gameObject.transform.position + new Vector3(0.0f, verticalFocusAdjustment, 0.0f);
                            Vector3 directionToTarget = targetPosition - gameCamera.transform.position;
                            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                            gameCamera.transform.rotation = Quaternion.Lerp(gameCamera.transform.rotation, targetRotation, Plugin.LookAtDamp.Value * Time.deltaTime);
                            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(targetPosition);
                            bool isNearEdge = viewportPoint.x <= Plugin.LookAtEscapeThreshold.Value
                                              || viewportPoint.x >= 1.0f - Plugin.LookAtEscapeThreshold.Value
                                              || viewportPoint.y <= Plugin.LookAtEscapeThreshold.Value
                                              || viewportPoint.y >= 1.0f - Plugin.LookAtEscapeThreshold.Value;
                            if (isNearEdge) {
                                // Adjust the rotation more quickly if the target is near the edge of the screen
                                float correctionFactor = 1.0f - Mathf.Min(viewportPoint.x, 1.0f - viewportPoint.x,viewportPoint.y, 1.0f - viewportPoint.y)                                                                          / Plugin.LookAtEscapeThreshold.Value;
                                gameCamera.transform.rotation = Quaternion.Lerp(gameCamera.transform.rotation, targetRotation, correctionFactor * Time.deltaTime);
                            }

                        } else if (Plugin.SelectedAttachType.Value == attachTypes.parented) {
                            Vector3 cameraWorldPosition = gameCamera.transform.position;
                            Quaternion cameraWorldRotation = gameCamera.transform.rotation;

                            gameCamera.transform.SetParent(AttachedTarget.transform, true);

                            gameCamera.transform.position = cameraWorldPosition;
                            gameCamera.transform.rotation = cameraWorldRotation;
                        } else {
                            gameCamera.transform.LookAt(AttachedTarget.gameObject.transform.position + new Vector3(0.0f, verticalFocusAdjustment, 0.0f));
                        }

                        if (Input.GetKey(Plugin.RotateLeft.Value.MainKey)) verticalFocusAdjustment += focusAdjustmentSpeed * Time.deltaTime;
                        if (Input.GetKey(Plugin.RotateRight.Value.MainKey)) verticalFocusAdjustment -= focusAdjustmentSpeed * Time.deltaTime;
                    }



                    // ------ ROTATION -------
                    if (Input.GetKey(Plugin.RotateLeft.Value.MainKey))
                        gameCamera.transform.localEulerAngles += new Vector3(0, 0, Plugin.RotateUsesSens.Value ? 1f * CameraSensitivity : 1f);

                    if (Input.GetKey(Plugin.RotateRight.Value.MainKey))
                        gameCamera.transform.localEulerAngles += new Vector3(0, 0, Plugin.RotateUsesSens.Value ? -1f * CameraSensitivity : -1f);

                } catch (Exception e) {
                    SendNotification($"Camera machine broke =>\n{e.Message}");
                    Plugin.logger.LogError(e);
                    CamUnsnapped = false;
                }

            }

        }

        async void MovePlayer() {
            player.Transform.position = gameCamera.transform.position;
            player.ActiveHealthController.SetDamageCoeff(0);
            while (playerAirborne) {
                await Task.Yield();
            }
            player.ActiveHealthController.SetDamageCoeff(1);
        }

        void SendNotification(string message, bool warn = true) => NotificationManagerClass.DisplayMessageNotification(message, ENotificationDurationType.Long, warn ? ENotificationIconType.Alert : ENotificationIconType.Default);

        public static void BlankOverride() { } // override so player doesn't move

        bool Ready() => gameWorld != null && gameWorld.MainPlayer != null && gameWorld.AllAlivePlayersList.Count > 0;
    }
}