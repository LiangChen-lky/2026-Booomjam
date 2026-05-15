using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PrefabDoorColliderSetup : EditorWindow
{
    private string prefabFolder = "Assets/Prefabs";

    [MenuItem("Tools/Prefab Door Collider Setup")]
    public static void ShowWindow()
    {
        GetWindow<PrefabDoorColliderSetup>("Door Collider Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Add missing colliders to prefab door objects", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        prefabFolder = EditorGUILayout.TextField("Prefab Folder", prefabFolder);

        EditorGUILayout.HelpBox(
            "Scans prefabs for objects with names containing door/Door.\n" +
            "Each door should have one trigger BoxCollider2D for interaction detection\n" +
            "and one non-trigger BoxCollider2D for physical blocking.\n\n" +
            "Existing colliders are preserved; the tool only adds the missing type.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Scan And Fix Door Colliders"))
        {
            AddMissingCollidersToDoors();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Remove Door BoxCollider2D Components"))
        {
            RemoveCollidersFromDoors();
        }
    }

    private void AddMissingCollidersToDoors()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
        if (prefabGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", $"No prefabs found in {prefabFolder}.", "OK");
            return;
        }

        int totalFixed = 0;
        int totalSkipped = 0;
        var log = new List<string>();

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null) continue;

            bool prefabModified = false;
            Transform[] allChildren = prefabRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                if (child == prefabRoot.transform) continue;
                if (!IsDoorObject(child)) continue;

                Vector2 size = GetColliderSize(child);
                if (size.x < 0.01f || size.y < 0.01f)
                {
                    log.Add($"  [Skipped] {path} -> {GetPath(child)} (no sprite or existing collider size)");
                    totalSkipped++;
                    continue;
                }

                BoxCollider2D[] existingColliders = child.GetComponents<BoxCollider2D>();
                bool hasTrigger = false;
                bool hasBlock = false;

                foreach (BoxCollider2D collider in existingColliders)
                {
                    if (collider.isTrigger) hasTrigger = true;
                    else hasBlock = true;
                }

                if (hasTrigger && hasBlock)
                {
                    log.Add($"  [OK] {path} -> {GetPath(child)}");
                    totalSkipped++;
                    continue;
                }

                if (!hasTrigger)
                {
                    BoxCollider2D triggerCol = child.gameObject.AddComponent<BoxCollider2D>();
                    triggerCol.isTrigger = true;
                    triggerCol.size = size;
                    triggerCol.offset = Vector2.zero;
                }

                if (!hasBlock)
                {
                    BoxCollider2D blockCol = child.gameObject.AddComponent<BoxCollider2D>();
                    blockCol.isTrigger = false;
                    blockCol.size = size;
                    blockCol.offset = Vector2.zero;
                }

                log.Add($"  [Fixed] {path} -> {GetPath(child)} (trigger: {!hasTrigger}, block: {!hasBlock}, size: {size:F2})");
                prefabModified = true;
                totalFixed++;
            }

            if (prefabModified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== Door Collider Setup Log ===\n" + string.Join("\n", log));
        EditorUtility.DisplayDialog(
            "Done",
            $"Scanned prefabs: {prefabGuids.Length}\nFixed doors: {totalFixed}\nSkipped/OK: {totalSkipped}\n\nSee Console for details.",
            "OK");
    }

    private void RemoveCollidersFromDoors()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
        if (prefabGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Info", $"No prefabs found in {prefabFolder}.", "OK");
            return;
        }

        int totalRemoved = 0;
        var log = new List<string>();

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null) continue;

            bool prefabModified = false;
            Transform[] allChildren = prefabRoot.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                if (child == prefabRoot.transform) continue;
                if (!IsDoorObject(child)) continue;

                BoxCollider2D[] colliders = child.GetComponents<BoxCollider2D>();
                if (colliders.Length == 0) continue;

                foreach (BoxCollider2D collider in colliders)
                {
                    Object.DestroyImmediate(collider);
                }

                log.Add($"  [Removed] {path} -> {GetPath(child)}");
                prefabModified = true;
                totalRemoved++;
            }

            if (prefabModified)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== Door Collider Remove Log ===\n" + string.Join("\n", log));
        EditorUtility.DisplayDialog("Done", $"Removed colliders from {totalRemoved} door objects.\n\nSee Console for details.", "OK");
    }

    private static bool IsDoorObject(Transform transform)
    {
        string objectName = transform.gameObject.name;
        if (!objectName.Contains("door") && !objectName.Contains("Door")) return false;
        if (objectName == "Doors") return false;

        return transform.GetComponent<SpriteRenderer>() != null
            || transform.GetComponent<Collider2D>() != null
            || transform.GetComponent<Door>() != null;
    }

    private static Vector2 GetColliderSize(Transform transform)
    {
        SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return new Vector2(
                spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit,
                spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit);
        }

        BoxCollider2D collider = transform.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            return collider.size;
        }

        return Vector2.zero;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        Transform parent = transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
