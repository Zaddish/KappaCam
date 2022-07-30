using UnityEngine;
using Comfort.Common;
using EFT;
using EFT.UI;

namespace CamUnSnap
{
    public class CUSController : MonoBehaviour
    {
        public static bool isSnapped { get; set; } = false;
        public static bool CamViewInControl { get; set; } = false;
        public static bool GamespeedChanged { get; set; } = false;
        public static float MovementSpeed;
        public static GameObject gameCamera;
        public void Update()
        {
            MovementSpeed = Plugin.CameraMoveSpeed.Value;

            if (Plugin.ToggleCameraSnap.Value.IsDown())
                SnapCam();

            if (Plugin.CameraMouse.Value.IsDown())
            {
                switch (CamViewInControl) // sick ass switch statements
                {
                    case true:
                        CamViewInControl = false;
                        break;
                    case false:
                        CamViewInControl = true;
                        break;
                }
            }

            if (Plugin.ChangeGamespeed.Value.IsDown())
            {
                switch (GamespeedChanged)
                {
                    case false:
                        Time.timeScale = Plugin.Gamespeed.Value;
                        GamespeedChanged = true;
                        break;
                    case true:
                        Time.timeScale = 1;
                        GamespeedChanged = false;
                        break;
                }
            }

            if (isSnapped && Ready())
            {
                gameCamera = GameObject.Find("FPS Camera");

                if (Input.GetKey(Plugin.CamLeft.Value.MainKey))
                {
                    gameCamera.transform.position += (-gameCamera.transform.right * MovementSpeed * ApplicableTimeDelta());
                }

                if (Input.GetKey(Plugin.CamRight.Value.MainKey))
                {
                    gameCamera.transform.position += (gameCamera.transform.right * MovementSpeed * ApplicableTimeDelta());
                }

                if (Input.GetKey(Plugin.CamForward.Value.MainKey))
                {
                    gameCamera.transform.position += (gameCamera.transform.forward * MovementSpeed * ApplicableTimeDelta());
                }

                if (Input.GetKey(Plugin.CamBack.Value.MainKey))
                {
                    gameCamera.transform.position += (-gameCamera.transform.forward * MovementSpeed * ApplicableTimeDelta());
                }

                if (Input.GetKey(Plugin.CamUp.Value.MainKey))
                {
                    gameCamera.transform.position += (gameCamera.transform.up * MovementSpeed * ApplicableTimeDelta());
                }

                if (Input.GetKey(Plugin.CamDown.Value.MainKey))
                {
                    gameCamera.transform.position += (-gameCamera.transform.up * MovementSpeed * ApplicableTimeDelta());
                }

                if (CamViewInControl)
                {
                    float newRotationX = gameCamera.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * Plugin.CameraSensitivity.Value;
                    float newRotationY = gameCamera.transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * Plugin.CameraSensitivity.Value;
                    gameCamera.transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
                }
                
            }
        }

        private static float ApplicableTimeDelta() => !GamespeedChanged ? Time.deltaTime : Time.fixedDeltaTime;

        private static void SnapCam()
        {
            var gameWorld = Singleton<GameWorld>.Instance;

            if (gameWorld == null || gameWorld.AllPlayers == null)
            {
                if (isSnapped) isSnapped = !isSnapped;
                return;
            }

            if (!isSnapped)
            {
                gameWorld.AllPlayers[0].PointOfView = EPointOfView.FreeCamera;
                gameWorld.AllPlayers[0].PointOfView = EPointOfView.ThirdPerson;
            }
            else
                gameWorld.AllPlayers[0].PointOfView = EPointOfView.FirstPerson;

            isSnapped = !isSnapped;

            return;
        }

        private static bool Ready()
        {
            var gameWorld = Singleton<GameWorld>.Instance;

            if (gameWorld.AllPlayers == null || gameWorld.AllPlayers[0] == null)
            {
                return false;
            }
            return true;
        }
    }
}
