using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KappaCam.Lights {
    public class LightController {
        public GameObject selectedLightGameObject = null;
        public Light selectedLight = null;
        public Vector3 lightPosition = Vector3.zero;
        public Vector3 lightRotation = Vector3.zero;
        public float lightIntensity = 5f;
        public float lightRange = 10f;
        public Color lightColor = Color.white;
        public UnityEngine.LightType selectedLightType = UnityEngine.LightType.Point;

        public List<GameObject> lights = new List<GameObject>();

        public void SelectLight(GameObject lightGameObject) {
            selectedLightGameObject = lightGameObject;
            selectedLight = lightGameObject.GetComponent<Light>();
            lightPosition = selectedLightGameObject.transform.position;
            lightRotation = selectedLightGameObject.transform.eulerAngles;
            lightIntensity = selectedLight.intensity;
            lightRange = selectedLight.range;
            lightColor = selectedLight.color;
            selectedLightType = selectedLight.type;
        }
       
        public void CreateLight(string name, UnityEngine.LightType type, Color color, float intensity, float range) {
            GameObject lightGameObject = new GameObject(name);
            Light lightComp = lightGameObject.AddComponent<Light>();
            lightComp.type = type;
            lightComp.color = color;
            lightComp.intensity = intensity;
            lightComp.range = range;

            lightGameObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 5;
            lightGameObject.transform.LookAt(Camera.main.transform);

            lights.Add(lightGameObject);
            SelectLight(lightGameObject);
        }
    }
}
