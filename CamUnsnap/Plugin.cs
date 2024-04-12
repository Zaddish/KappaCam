using BepInEx;
using UnityEngine;
using BepInEx.Configuration;
using BepInEx.Logging;
using CamUnsnap.Menu;

namespace CamUnsnap
{
    [BepInPlugin("com.kobrakon.camunsnap", "CamUnsnap", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private GameObject Hook;
        private const string KeybindsSection = "Keybinds";
        private const string CameraSection = "Camera Control Settings";
        private const string UtilitySection = "Utility Settings";
        private const string GameSection = "Game Settings";
        private const string RecordingSection = "Recording Settings";

        internal static ManualLogSource logger;
        internal static ConfigEntry<KeyboardShortcut> ToggleCameraSnap;
        internal static ConfigEntry<KeyboardShortcut> CameraMouse;
        internal static ConfigEntry<KeyboardShortcut> ChangeGamespeed;
        internal static ConfigEntry<KeyboardShortcut> CamForward;
        internal static ConfigEntry<KeyboardShortcut> AttachCameraFollow;
        internal static ConfigEntry<KeyboardShortcut> DetatchCameraFollow;
        internal static ConfigEntry<KeyboardShortcut> ZoomKey;
        internal static ConfigEntry<KeyboardShortcut> SpeedKey;
        internal static ConfigEntry<KeyboardShortcut> CamBack;
        internal static ConfigEntry<KeyboardShortcut> CamLeft;
        internal static ConfigEntry<KeyboardShortcut> CamRight;
        internal static ConfigEntry<KeyboardShortcut> CamUp;
        internal static ConfigEntry<KeyboardShortcut> CamDown;
        internal static ConfigEntry<KeyboardShortcut> RememberPos;
        internal static ConfigEntry<KeyboardShortcut> GoToPos;
        internal static ConfigEntry<KeyboardShortcut> MovePlayerToCam;
        internal static ConfigEntry<KeyboardShortcut> LockPlayerMovement;
        internal static ConfigEntry<KeyboardShortcut> HideUI;
        internal static ConfigEntry<KeyboardShortcut> FastMove;
        internal static ConfigEntry<KeyboardShortcut> RotateLeft;
        internal static ConfigEntry<KeyboardShortcut> RotateRight;
        internal static ConfigEntry<KeyboardShortcut> ResetRotation;
        internal static ConfigEntry<KeyboardShortcut> AddToMemPosList;
        internal static ConfigEntry<KeyboardShortcut> AdvanceList;
        internal static ConfigEntry<KeyboardShortcut> ClearList;
        internal static ConfigEntry<KeyboardShortcut> BeginRecord;
        internal static ConfigEntry<KeyboardShortcut> ResumeRecord;
        internal static ConfigEntry<KeyboardShortcut> StopRecord;
        internal static ConfigEntry<KeyboardShortcut> PlayRecord;
        internal static ConfigEntry<KeyboardShortcut> MenuButton;
        internal static ConfigEntry<KeyboardShortcut> CreateKeyframe;

        internal static ConfigEntry<float> Gamespeed;
        internal static ConfigEntry<float> MovementSpeed;
        internal static ConfigEntry<float> ZoomSpeed;
        internal static ConfigEntry<float> CameraSensitivity;
        internal static ConfigEntry<float> CameraSmoothing;
        internal static ConfigEntry<float> FastMoveMult;
        internal static ConfigEntry<float> raycastDistance;
        internal static ConfigEntry<float> zoomVelocity;
        internal static ConfigEntry<float> zoomSmoothTime;
        internal static ConfigEntry<float> speedAdjustmentFactor;
        internal static ConfigEntry<float> Friction;
        
        internal static ConfigEntry<float> LookAtDamp;
        internal static ConfigEntry<float> LookAtEscapeThreshold;
        
        internal static ConfigEntry<int> CameraFOV;

        internal static ConfigEntry<bool> Godmode;
        internal static ConfigEntry<bool> PlayerFollowCamera;
        internal static ConfigEntry<bool> RotateUsesSens;
        internal static ConfigEntry<bool> OverrideGameRestriction;



        private void Awake()
        {
            logger = Logger;
            
            /// CameraSection
            ToggleCameraSnap = Config.Bind(CameraSection, "Toggle Camera Snap", new KeyboardShortcut(KeyCode.C, KeyCode.LeftControl),"Allows you to unsnap the camera at will");
            CameraMouse = Config.Bind(CameraSection, "Switch camera control to mouse", new KeyboardShortcut(KeyCode.Equals), "Lets you contol the camera viewport with the mouse, switch between enabling to pose your character");
            AttachCameraFollow = Config.Bind(CameraSection, "Attach to object", new KeyboardShortcut(KeyCode.F6), "Will attach to an object");
            DetatchCameraFollow = Config.Bind(CameraSection, "Detach from object", new KeyboardShortcut(KeyCode.F7), "Will detach from object");
            ZoomKey = Config.Bind(CameraSection, "Zoom Key", new KeyboardShortcut(KeyCode.Mouse4), "While holding the button down, it will allow you to zoom the FOV in and out with your mouse wheel.");
            SpeedKey = Config.Bind(CameraSection, "Speed Key", new KeyboardShortcut(KeyCode.Mouse5), "While holding the button down, it will allow you to change the speed of the camera with your mouse wheel");
            CamForward = Config.Bind(CameraSection, "Move Forward", new KeyboardShortcut(KeyCode.UpArrow), "Moves the camera forwards");
            CamBack = Config.Bind(CameraSection, "Move Back", new KeyboardShortcut(KeyCode.DownArrow), "Moves the camera backwards");
            CamLeft = Config.Bind(CameraSection, "Move Left", new KeyboardShortcut(KeyCode.LeftArrow), "Moves the camera left");
            CamRight = Config.Bind(CameraSection, "Move Right", new KeyboardShortcut(KeyCode.RightArrow), "Moves the camera right");
            CamUp = Config.Bind(CameraSection, "Move Up", new KeyboardShortcut(KeyCode.Space), "Moves the camera up");
            CamDown = Config.Bind(CameraSection, "Move Down", new KeyboardShortcut(KeyCode.LeftControl), "Moves the camera down");
            RotateLeft = Config.Bind(CameraSection, "Rotate Left", new KeyboardShortcut(KeyCode.Q), "Rotates/tilts the camera to the left");
            RotateRight = Config.Bind(CameraSection, "Rotate Right", new KeyboardShortcut(KeyCode.E), "Rotates/tilts the camera to the right");
            ResetRotation = Config.Bind(CameraSection, "Reset Rotation", new KeyboardShortcut(KeyCode.Minus), "Resets camera rotation back to 0");
            FastMove = Config.Bind(CameraSection, "Move Fast", new KeyboardShortcut(KeyCode.LeftShift), "Makes the camera move faster when held\nBasically like sprinting");
            CameraFOV = Config.Bind(CameraSection, "Camera FOV", 75, new ConfigDescription("The FOV value of the camera while unsnaped", new AcceptableValueRange<int>(1, 200)));
            zoomVelocity = Config.Bind(CameraSection, "Zoom Velocity", 0.8f, new ConfigDescription("used to smooth out the zoom transition", new AcceptableValueRange<float>(0.00001f, 5.0f)));
            zoomSmoothTime = Config.Bind(CameraSection, "Zoom Time", 0.3f, new ConfigDescription("Time taken to smooth the transition", new AcceptableValueRange<float>(0.000001f, 5.0f)));
            speedAdjustmentFactor = Config.Bind(CameraSection, "Movement Speed Mouse Wheel", 3.0f, new ConfigDescription("When you have the Speed Key Held, how fast do you want the speed to change when scrolling the mouse?", new AcceptableValueRange<float>(0.1f, 10.0f)));
            Friction = Config.Bind(CameraSection, "Movement Smoothing", 0.992f, new ConfigDescription("The amount of smoothing applied to the movement of the camera when unsnapped", new AcceptableValueRange<float>(0.00001f, 0.99999f)));
            MovementSpeed = Config.Bind(CameraSection, "CameraMoveSpeed", 10f, new ConfigDescription("How fast you want the camera to move", new AcceptableValueRange<float>(0.01f, 100f)));
            ZoomSpeed = Config.Bind(CameraSection, "Camera FOV Zoom speed", 50f, new ConfigDescription("How fast you want to zoom in whilst the zoom key is held", new AcceptableValueRange<float>(0.01f, 1000f)));
            CameraSensitivity = Config.Bind(CameraSection, "Camera Sensitivity", 10f, new ConfigDescription("How fast you want the camera viewport to move while slaved", new AcceptableValueRange<float>(0.0f, 100f)));
            CameraSmoothing = Config.Bind(CameraSection, "Mouse Smoothing", 10f, new ConfigDescription("The amount of smoothing you want applied to the mouse in camera mode. (Lower is smoother)", new AcceptableValueRange<float>(0.0001f, 1f)));
            RotateUsesSens = Config.Bind(UtilitySection, "Rotation speed inherits Camera Sensitivity", false, "If true, the camera rotation speed is multiplied by the Camera Sensitivity value, otherwise, the camera is rotated by only 1 degree per frame");

            /// GameSection
            ChangeGamespeed = Config.Bind(GameSection, "Change Gamespeed", new KeyboardShortcut(KeyCode.Tilde), "Toggle that sets the gamespeed to what you set using the gamespeed slider");
            Gamespeed = Config.Bind(GameSection, "Set Gamespeed", 1f, new ConfigDescription("What gamespeed you want to set the gameworld to when pressing the Change Gamespeed bind !WARNING! Changing the gamespeed for too long can cause weird (but temporary) side effects", new AcceptableValueRange<float>(0f, 1f)));

            /// UtilitySection
            RememberPos = Config.Bind(UtilitySection, "Remember Camera Position", new KeyboardShortcut(KeyCode.O), "Save the camera's current Vector3 position.");
            GoToPos = Config.Bind(UtilitySection, "Go to Memory Position", new KeyboardShortcut(KeyCode.P), "Moves the camera to the last remembered Vector3 position.");
            MovePlayerToCam = Config.Bind(UtilitySection, "Move Player to Camera Position", new KeyboardShortcut(KeyCode.RightAlt), "Moves the player to the camera's position.");
            LockPlayerMovement = Config.Bind(UtilitySection, "Lock Player Movement", new KeyboardShortcut(KeyCode.RightControl), "Locks player body movement when pressed");
            HideUI = Config.Bind(UtilitySection, "Hide UI", new KeyboardShortcut(KeyCode.Keypad7), "Hides the game UI");
            AddToMemPosList = Config.Bind(UtilitySection, "Add Position To Camera Memory Position List", new KeyboardShortcut(KeyCode.Plus), "Adds the current camera position into the Memory Position List");
            AdvanceList = Config.Bind(UtilitySection, "Advance Memory List Position", new KeyboardShortcut(KeyCode.Greater), "Changes the camera's position to the next position contained in the list");
            ClearList = Config.Bind(UtilitySection, "Clear Camera Memory Position List", new KeyboardShortcut(KeyCode.Less), "Empties the current Camera Memory Position List");
            raycastDistance = Config.Bind(UtilitySection, "Attachment Raycast Distance", 100f, new ConfigDescription("The distance at which the attachment will look for objects to attach to", new AcceptableValueRange<float>(0.001f, 1000.0f)));
            RotateUsesSens = Config.Bind(UtilitySection, "Rotation speed inherits Camera Sensitivity", false, "If true, the camera rotation speed is multiplied by the Camera Sensitivity value, otherwise, the camera is rotated by only 1 degree per frame");
            Godmode = Config.Bind(UtilitySection, "God mode", true, "Makes the player unkillable");
            FastMoveMult = Config.Bind(UtilitySection, "Set Fast Movement Multiplier", 2f, new ConfigDescription("The value that the camera movement speed is multiplied by while the move fast key is held", new AcceptableValueRange<float>(0f, 100f)));
            PlayerFollowCamera = Config.Bind(UtilitySection, "Player Follows Camera (culling)", false, "The player will be put behind the camera so that culling works for the camera pos");
            
            /// RecordingSection
            BeginRecord = Config.Bind(RecordingSection, "Begin Path Recording", new KeyboardShortcut(KeyCode.LeftBracket), "Begins recording camera movement");
            ResumeRecord = Config.Bind(RecordingSection, "Continue Path Recording", new KeyboardShortcut(KeyCode.Backslash), "Resumes recording and appends it to the previous recording");
            StopRecord = Config.Bind(RecordingSection, "End Path Recording", new KeyboardShortcut(KeyCode.RightBracket), "Ends and saves the currently recording camera path");
            PlayRecord = Config.Bind(RecordingSection, "Play Path Recording", new KeyboardShortcut(KeyCode.Slash), "Plays the currently saved camera path recording");
            MenuButton = Config.Bind(RecordingSection, "Menu Button", new KeyboardShortcut(KeyCode.F10), "Advanced options menu for tweaking settings");
            LookAtDamp = Config.Bind(RecordingSection, "LookAt Smoothing", 0.2f, new ConfigDescription("Smmothing on the camera when you are attached to an object and the \"lookAt\" type is selected. 1 is instant look-at and 0 is mega smooth", new AcceptableValueRange<float>(0.0001f, 0.999f)));
            LookAtEscapeThreshold = Config.Bind(RecordingSection, "LookAt Escape Threshold", 0.1f, new ConfigDescription("Threshold for screen edges, if the attached object is closer to edge, it will keep it in frame. Smaller means closer to edge before correcting.", new AcceptableValueRange<float>(0, 1)));

            /// 
            CreateKeyframe = Config.Bind("Pathing", "Create Keyframe", new KeyboardShortcut(KeyCode.KeypadPlus), "Create a keybind with the smooth pathing");

            OverrideGameRestriction = Config.Bind("Unsafe Options", "Override Session Restriction", false, "When enabled, the requirement for the player to be in a session is overridden, allowing access to the main camera whenever it's in use. However, options regarding player values and the such (immune in camera, move player, memory pos etc) are ignored until this option is disabled.\nThis option can result in bugs and artifacts, and while exceptions thrown by the CUS script are automatically handled, EFT is not so well programmed, and may react unpredictably.");


            Hook = new GameObject("CUS");
            Hook.AddComponent<CUSController>();
            Hook.AddComponent<CUSMenu>();
            DontDestroyOnLoad(Hook);
            Logger.LogInfo($"Camera Unsnap Loaded");
        }

    }
}