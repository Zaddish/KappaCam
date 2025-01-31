using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PrefabMenu : MonoBehaviour {
    private Dictionary<string, GameObject> scenePrefabs = new Dictionary<string, GameObject>();
    private List<string> allPrefabNames = new List<string>();
    private List<string> filteredPrefabNames = new List<string>();

    private Vector2 scrollPosition;
    private string searchText = "";
    private Coroutine refreshRoutine;
    private bool isRefreshing;
    private float itemHeight = 22f;
    private float scrollViewHeight = 500f;
    private float scrollViewWidth = 600f;
    private GameObject spawnedInstance;

    void Start() {
        StartRefresh();
    }

    private void StartRefresh() {
        if (isRefreshing && refreshRoutine != null) {
            StopCoroutine(refreshRoutine);
        }
        refreshRoutine = StartCoroutine(CollectSceneObjectsCoroutine());
    }

    private IEnumerator CollectSceneObjectsCoroutine() {
        isRefreshing = true;
        scenePrefabs.Clear();
        allPrefabNames.Clear();
        filteredPrefabNames.Clear();

        for (int s = 0; s < SceneManager.sceneCount; s++) {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject rootObj in roots) {
                Stack<GameObject> stack = new Stack<GameObject>();
                stack.Push(rootObj);

                while (stack.Count > 0) {
                    GameObject current = stack.Pop();
                    if (HasVisibleMesh(current) && !scenePrefabs.ContainsKey(current.name)) {
                        scenePrefabs[current.name] = current;
                        allPrefabNames.Add(current.name);
                    }

                    for (int i = 0; i < current.transform.childCount; i++) {
                        stack.Push(current.transform.GetChild(i).gameObject);
                    }
                }
                yield return null;
            }
        }

        allPrefabNames.Sort();
        filteredPrefabNames = new List<string>(allPrefabNames);
        isRefreshing = false;
    }

    private bool HasVisibleMesh(GameObject obj) {
        Renderer meshRenderer = obj.GetComponent<MeshRenderer>();
        SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
        Collider collider = obj.GetComponent<Collider>();
        return (meshRenderer != null && meshRenderer.enabled)
            || (skinnedMeshRenderer != null && skinnedMeshRenderer.enabled)
            || (collider != null && collider.enabled);
    }

    public void Menu() {
        GUILayout.Label("Select a Prefab to Spawn (Chunked Refresh).");

        if (isRefreshing) {
            GUILayout.Label("...Refreshing Scene Objects...");
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        string newSearch = GUILayout.TextField(searchText, GUILayout.Width(530));
        GUILayout.EndHorizontal();
        if (newSearch != searchText) {
            searchText = newSearch;
            FilterSearch();
        }

        if (GUILayout.Button("Refresh") && !isRefreshing) {
            StartRefresh();
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(scrollViewWidth), GUILayout.Height(scrollViewHeight));
        int startIndex = Mathf.FloorToInt(scrollPosition.y / itemHeight);
        int visibleCount = Mathf.CeilToInt(scrollViewHeight / itemHeight) + 1;
        int endIndex = Mathf.Min(startIndex + visibleCount, filteredPrefabNames.Count);

        GUILayout.Space(startIndex * itemHeight);

        for (int i = startIndex; i < endIndex; i++) {
            string prefabName = filteredPrefabNames[i];
            if (GUILayout.Button(prefabName, GUILayout.Height(itemHeight), GUILayout.Width(scrollViewWidth - 20))) {
                SpawnPrefab(prefabName);
            }
        }

        GUILayout.Space((filteredPrefabNames.Count - endIndex) * itemHeight);
        GUILayout.EndScrollView();

        if (spawnedInstance != null) {
            GUILayout.Label("Spawned: " + spawnedInstance.name);
        }
    }

    private void FilterSearch() {
        if (string.IsNullOrEmpty(searchText)) {
            filteredPrefabNames = new List<string>(allPrefabNames);
        } else {
            string lower = searchText.ToLower();
            filteredPrefabNames = allPrefabNames
                .Where(n => n.ToLower().Contains(lower))
                .ToList();
        }
    }

    private void SpawnPrefab(string prefabName) {
        if (!scenePrefabs.ContainsKey(prefabName)) {
            Debug.LogError($"[PrefabMenu] Prefab not found: {prefabName}");
            return;
        }

        GameObject original = scenePrefabs[prefabName];
        if (original == null) {
            Debug.LogError($"[PrefabMenu] Original GameObject is null for: {prefabName}");
            return;
        }

        Camera cam = Camera.main;
        Vector3 spawnPos = new Vector3(0, 1, 5);
        Quaternion spawnRot = Quaternion.identity;
        if (cam != null) {
            spawnPos = cam.transform.position + cam.transform.forward * 5f;
            spawnRot = cam.transform.rotation;
        }

        spawnedInstance = Instantiate(original, spawnPos, spawnRot);
        spawnedInstance.name = prefabName + "_Spawned";
        spawnedInstance.isStatic = true;
        Debug.Log($"[PrefabMenu] Spawned: {spawnedInstance.name} at {spawnPos}");
    }
}
