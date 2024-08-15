using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrefabMenu : MonoBehaviour {
    private GameObject previewInstance;
    private string[] prefabNames;
    private string[] filteredPrefabNames;
    private Vector2 scrollPosition;
    private string searchText = "";
    private bool showAdvanced = false; 
    private string bundlePath = "bundle path";

    void Start() {
        LoadPrefabs();
    }

    void LoadPrefabs() {
        var allGameObjectNames = new HashSet<string>();

        for (int i = 0; i < SceneManager.sceneCount; i++) {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) {
                foreach (GameObject obj in scene.GetRootGameObjects()) {
                    CollectGameObjectNames(obj, allGameObjectNames);
                }
            }
        }

        prefabNames = allGameObjectNames.ToArray();
        filteredPrefabNames = prefabNames;
    }
    private void CollectGameObjectNames(GameObject gameObject, HashSet<string> names) {
        names.Add(gameObject.name);
        foreach (Transform child in gameObject.transform) {
            CollectGameObjectNames(child.gameObject, names);
        }
    }
    public void Menu() {
        GUILayout.Label("Select a Prefab to Spawn:");

        // Search field
        string newSearchText = GUILayout.TextField(searchText, GUILayout.Width(580));
        if (newSearchText != searchText) {
            searchText = newSearchText;
            filteredPrefabNames = prefabNames.Where(name => name.ToLower().Contains(searchText.ToLower())).ToArray();
        }

        // Refresh button
        if (GUILayout.Button("Refresh")) {
            LoadPrefabs();
        }

        // Scroll view for prefab selection
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(600), GUILayout.Height(500));
        foreach (string prefabName in filteredPrefabNames) {
            if (GUILayout.Button(prefabName)) {
                LoadAndPreviewPrefab(prefabName);
            }
        }
        GUILayout.EndScrollView();

        // Toggle for showing advanced settings
        showAdvanced = GUILayout.Toggle(showAdvanced, "Advanced Options");
        if (showAdvanced) {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Bundle Path:");
            bundlePath = GUILayout.TextField(bundlePath, GUILayout.Width(500));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Load from Bundle")) {
                LoadFromBundle(bundlePath);
            }
        }

        // Preview Section
        if (previewInstance != null) {
            GUILayout.Label("Previewing: " + previewInstance.name);
        }
    }

    private void LoadAndPreviewPrefab(string prefabName) {
        if (previewInstance != null) {
            Destroy(previewInstance);
        }

        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        if (prefab != null) {
            Vector3 previewPosition = new Vector3(0, 1, 5);
            Quaternion previewRotation = Quaternion.Euler(0, 180, 0);

            previewInstance = Instantiate(prefab, previewPosition, previewRotation);
            previewInstance.name = prefabName;
        } else {
            Debug.LogError("Prefab not found: " + prefabName);
        }
    }

    private void LoadFromBundle(string path) {
        AssetBundle bundle = AssetBundle.LoadFromFile(path);
        if (bundle != null) {
            GameObject[] allAssets = bundle.LoadAllAssets<GameObject>();
            if (allAssets.Length > 0) {
                prefabNames = allAssets.Select(asset => asset.name).ToArray();
                filteredPrefabNames = prefabNames;
            }
            bundle.Unload(false);
        } else {
            Debug.LogError("Failed to load AssetBundle from given path.");
        }
    }

}
