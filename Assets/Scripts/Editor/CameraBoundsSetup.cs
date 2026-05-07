using UnityEditor;
using UnityEngine;
using Cinemachine;
using System.Collections.Generic;
using System.Linq;

public class CameraBoundsSetup : EditorWindow
{
    private PolygonCollider2D boundsCollider;
    private string prefabFolder = "Assets/Prefabs";
    private string[] wallKeywords = { "boundary", "Wall", "wall" };
    private float cameraPadding = 0.5f;
    private float maxWallSize = 50f;

    [MenuItem("Tools/相机边界设置")]
    public static void ShowWindow()
    {
        GetWindow<CameraBoundsSetup>("相机边界设置");
    }

    private void OnGUI()
    {
        GUILayout.Label("相机边界自动配置", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("第0步：给 Prefab 墙体添加碰撞体", EditorStyles.boldLabel);
        prefabFolder = EditorGUILayout.TextField("Prefab 文件夹", prefabFolder);
        if (GUILayout.Button("扫描并添加碰撞体到 Prefab"))
        {
            AddCollidersToPrefabs();
        }

        EditorGUILayout.Space(10);

        GUILayout.Label("第1步：生成相机边界", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "每个房间/区域生成一个矩形路径，\n" +
            "合并到 PolygonCollider2D 中。",
            MessageType.Info);

        cameraPadding = EditorGUILayout.FloatField("边界内缩量", cameraPadding);
        maxWallSize = EditorGUILayout.FloatField("最大墙体尺寸", maxWallSize);
        EditorGUILayout.HelpBox("超过此尺寸的 boundary sprite 会被跳过（避免超大 sprite 拉大矩形）", MessageType.None);

        if (GUILayout.Button("一键生成相机边界"))
        {
            GenerateCameraBounds();
        }

        EditorGUILayout.Space(10);
        boundsCollider = (PolygonCollider2D)EditorGUILayout.ObjectField(
            "当前边界碰撞体", boundsCollider, typeof(PolygonCollider2D), true);
    }

    private void AddCollidersToPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolder });
        if (prefabGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", $"在 {prefabFolder} 中未找到任何 Prefab", "确定");
            return;
        }

        int totalAdded = 0;
        int totalSkipped = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            Transform[] allChildren = prefab.GetComponentsInChildren<Transform>(true);
            bool prefabModified = false;

            foreach (Transform child in allChildren)
            {
                if (child == prefab.transform) continue;
                string objName = child.gameObject.name;

                bool isWall = false;
                foreach (string keyword in wallKeywords)
                {
                    if (objName.Contains(keyword)) { isWall = true; break; }
                }
                if (!isWall) continue;

                var sr = child.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                var existingCol = child.GetComponent<BoxCollider2D>();
                if (existingCol != null) { totalSkipped++; continue; }

                Sprite sprite = sr.sprite;
                Vector2 spriteSize;
                if (sprite != null && sprite.rect.width > 0 && sprite.rect.height > 0)
                {
                    spriteSize = new Vector2(
                        sprite.rect.width / sprite.pixelsPerUnit,
                        sprite.rect.height / sprite.pixelsPerUnit);
                }
                else
                {
                    spriteSize = sr.localBounds.size;
                }

                if (spriteSize.x < 0.01f || spriteSize.y < 0.01f)
                {
                    totalSkipped++;
                    continue;
                }

                var col = child.gameObject.AddComponent<BoxCollider2D>();
                col.size = spriteSize;
                col.offset = Vector2.zero;
                prefabModified = true;
                totalAdded++;
            }

            if (prefabModified) EditorUtility.SetDirty(prefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成",
            $"新增: {totalAdded}，已有: {totalSkipped}", "确定");
    }

    private void GenerateCameraBounds()
    {
        // 1. 收集墙体碰撞体，按所在 prefab 实例的根对象分组
        var allBoxCols = FindObjectsOfType<BoxCollider2D>();
        var groups = new Dictionary<Transform, List<Rect>>();

        foreach (var col in allBoxCols)
        {
            if (col.isTrigger) continue;
            if (col.GetComponentInParent<PlayerController>() != null) continue;
            if (col.GetComponentInParent<MonsterController>() != null) continue;

            string name = col.gameObject.name;
            bool isWall = false;
            foreach (string kw in wallKeywords)
            {
                if (name.Contains(kw)) { isWall = true; break; }
            }
            if (!isWall) continue;

            Vector2 size = col.bounds.size;
            if (size.x < 0.01f || size.y < 0.01f) continue;

            // 跳过尺寸过大的 boundary sprite（视觉装饰用，不适合做碰撞边界）
            if (size.x > maxWallSize || size.y > maxWallSize)
            {
                Debug.Log($"[跳过-过大] {name} root={col.transform.root.name} size={size}");
                continue;
            }

            // 找到所属 prefab 实例的根对象
            Transform root = col.transform.root;

            if (!groups.ContainsKey(root))
                groups[root] = new List<Rect>();

            groups[root].Add(new Rect(
                col.bounds.min.x, col.bounds.min.y,
                size.x, size.y));

            Debug.Log($"[墙体] {name} root={root.name} center={col.bounds.center} size={size}");
        }

        Debug.Log($"[相机边界] 有效墙体: {groups.Values.Sum(g => g.Count)}，分组: {groups.Count}");

        if (groups.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "未找到有效墙体。请先执行第0步。", "确定");
            return;
        }

        // 2. 每个 prefab 实例计算 AABB 矩形
        var allPaths = new List<Vector2[]>();

        foreach (var kvp in groups)
        {
            string rootName = kvp.Key.name;
            var rects = kvp.Value;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var r in rects)
            {
                if (r.xMin < minX) minX = r.xMin;
                if (r.yMin < minY) minY = r.yMin;
                if (r.xMax > maxX) maxX = r.xMax;
                if (r.yMax > maxY) maxY = r.yMax;
            }

            minX += cameraPadding;
            minY += cameraPadding;
            maxX -= cameraPadding;
            maxY -= cameraPadding;

            if (maxX - minX < 0.1f || maxY - minY < 0.1f)
            {
                Debug.LogWarning($"[{rootName}] 矩形太小，跳过");
                continue;
            }

            var rect = new Vector2[]
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(maxX, maxY),
                new Vector2(minX, maxY)
            };

            allPaths.Add(rect);
            Debug.Log($"[{rootName}] 矩形: ({minX:F1},{minY:F1}) - ({maxX:F1},{maxY:F1}) size=({maxX-minX:F1},{maxY-minY:F1})");
        }

        if (allPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("错误", "未能生成有效矩形", "确定");
            return;
        }

        // 4. 创建 PolygonCollider2D（多路径）
        var boundsGo = GameObject.Find("CameraBounds");
        if (boundsGo == null)
        {
            boundsGo = new GameObject("CameraBounds");
            Undo.RegisterCreatedObjectUndo(boundsGo, "Create CameraBounds");
        }

        boundsCollider = boundsGo.GetComponent<PolygonCollider2D>();
        if (boundsCollider == null)
            boundsCollider = Undo.AddComponent<PolygonCollider2D>(boundsGo);

        boundsCollider.isTrigger = true;
        boundsCollider.pathCount = allPaths.Count;
        for (int i = 0; i < allPaths.Count; i++)
        {
            boundsCollider.SetPath(i, allPaths[i]);
        }

        // 5. 配置 VirtualCamera
        var vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam == null)
        {
            EditorUtility.DisplayDialog("错误", "场景中未找到 CinemachineVirtualCamera", "确定");
            return;
        }

        var confiner = vcam.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
            confiner = Undo.AddComponent<CinemachineConfiner2D>(vcam.gameObject);

        confiner.m_BoundingShape2D = boundsCollider;
        confiner.m_Damping = 0.5f;
        confiner.m_MaxWindowSize = 0;
        confiner.InvalidateCache();

        // 6. 清理旧 Composite
        var old = GameObject.Find("CameraComposite");
        if (old != null) Undo.DestroyObjectImmediate(old);

        Selection.activeGameObject = boundsGo;

        EditorUtility.DisplayDialog("完成",
            $"相机边界生成完成！\n\n" +
            $"房间区域: {allPaths.Count} 个\n\n" +
            $"请在 Scene 视图中查看 CameraBounds。",
            "确定");
    }

}
