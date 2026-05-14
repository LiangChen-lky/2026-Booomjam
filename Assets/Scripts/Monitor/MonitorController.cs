using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

/// <summary>
/// Monitor terminal controller. Press R to toggle monitor mode and Z/X to cycle rooms.
/// Uses CameraRoomManager room bindings first, then falls back to RoomBounds_* data.
/// </summary>
public class MonitorController : MonoBehaviour
{
    public static MonitorController Instance { get; private set; }

    public enum MonitorMode
    {
        Camera,
        Image
    }

    [System.Serializable]
    private class MonitorImageFeed
    {
        public string roomName;
        public Sprite image;
        public string imagePath;
        public bool enabled = true;
    }

    private static readonly string[] FixedMonitorRooms =
    {
        "Hall",
        "Dorm",
        "Classroom",
        "Toilet",
        "Guardroom"
    };

    private static readonly string[] DefaultImageFeedPaths =
    {
        "Assets/Sprites/\u573A\u666F/Hall.png",
        "Assets/Sprites/\u573A\u666F/Dorm.png",
        "Assets/Sprites/\u573A\u666F/Classroom.png",
        "Assets/Sprites/\u573A\u666F/Toilet.jpg",
        "Assets/Sprites/\u573A\u666F/\u65E0\u95E8/Cuardroom.png"
    };
    private static readonly Dictionary<string, Sprite> loadedPathSprites = new Dictionary<string, Sprite>();

    public struct MonitorTrackedBlip
    {
        public Vector2 normalizedPosition;
        public Color color;
        public float size;
    }

    [Header("Monitor Settings")]
    [SerializeField] private float cameraHeight = 30f;
    [SerializeField] private float cameraOrthoSize = 12f;
    [SerializeField] private float cooldownDuration = 5f;
    [SerializeField] private float signalLostChance = 0.1f;
    [SerializeField] private float signalRecoverTime = 3f;
    [SerializeField] private MonitorMode currentMode = MonitorMode.Image;
    [SerializeField] private string[] includedCameraRooms = { "Hall", "Dorm", "Classroom", "Toilet" };
    [SerializeField, Min(0f)] private float roomBoundsPadding = 1f;

    [Header("Image Feeds")]
    [SerializeField] private MonitorImageFeed[] imageFeeds = new MonitorImageFeed[0];

    [Header("Switch Static")]
    [SerializeField] private Sprite switchStaticSprite;
    [SerializeField] private string switchStaticImagePath;
    [SerializeField, Min(0f)] private float switchStaticDuration = 0.2f;
    [SerializeField, Min(0f)] private float cameraTransitionDuration = 0.35f;

    [Header("UI")]
    [SerializeField] private MonitorCameraUI monitorUIPrefab;
    private MonitorCameraUI monitorUIInstance;

    private struct MonitorView
    {
        public string roomName;
        public Vector3 position;
        public Quaternion rotation;
        public bool orthographic;
        public float orthographicSize;
        public float fieldOfView;
        public Sprite image;
        public bool usesImage;
        public string imageSourcePath;
        public Bounds roomBounds;
        public bool hasRoomBounds;
    }

    private readonly List<MonitorView> monitorViews = new List<MonitorView>();
    private int currentCameraIndex;
    private bool isMonitorOpen;
    private bool isOnCooldown;
    private bool isSignalLost;
    private float lastCloseTime = -999f;
    private string savedRoom;
    private PlayerController playerRef;
    private MonsterController monsterRef;
    private PlayerController trackedPlayer;
    private MonsterController trackedMonster;
    private TravelBag[] trackedBags;
    private Camera mainCamera;
    private Camera monitorCamera;
    private GameObject monitorCameraObject;
    private bool savedCursorVisible;
    private CursorLockMode savedCursorLockState;
    private Coroutine switchStaticCoroutine;
    private bool hasLoggedBuiltViews;

    public bool IsMonitorOpen => isMonitorOpen;
    public MonitorMode CurrentMode => currentMode;
    public int CurrentCameraIndex => currentCameraIndex;
    public int CurrentCameraCount => monitorViews.Count;
    public string CurrentCameraRoomName => GetCameraRoomName(currentCameraIndex);
    public bool IsOnCooldown => isOnCooldown;
    public float CooldownDuration => cooldownDuration;
    public float CooldownRemaining => isOnCooldown ? Mathf.Max(0f, cooldownDuration - (Time.unscaledTime - lastCloseTime)) : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (isMonitorOpen)
            RestoreCursorAfterMonitor();

        CleanupUI();
        CleanupMonitorCamera();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleMonitor();
            return;
        }

        if (!isMonitorOpen || isSignalLost)
            return;

        if (Input.GetKeyDown(KeyCode.Z))
            PrevCamera();
        else if (Input.GetKeyDown(KeyCode.X))
            NextCamera();
    }

    public void ToggleMonitor()
    {
        if (isMonitorOpen)
            CloseMonitor();
        else
            OpenMonitor();
    }

    public void OpenMonitor()
    {
        if (isOnCooldown)
        {
            return;
        }

        BuildViews();
        if (monitorViews.Count == 0)
        {
            Debug.LogWarning("[MonitorController] No monitor rooms available.");
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null && HasCameraViews())
        {
            Debug.LogWarning("[MonitorController] Main Camera not found.");
            return;
        }

        isMonitorOpen = true;
        AudioManager.Instance.Play(SFX.MonitorOpen);

        var camRoomManager = FindObjectOfType<CameraRoomManager>();
        if (camRoomManager != null)
            savedRoom = camRoomManager.currentRoom;

        CacheTrackedObjects();

        monsterRef = trackedMonster;
        if (monsterRef != null)
            monsterRef.enabled = false;

        playerRef = trackedPlayer;
        if (playerRef != null)
            playerRef.Input.DisablePlayerMoveInput();

        SetVisionMaskEnabled(false);
        ShowCursorForMonitor();

        ShowUI();

        currentCameraIndex = 0;
        ShowCamera(0);

        if (Random.value < signalLostChance)
            StartCoroutine(SignalLostCoroutine());
    }

    public void CloseMonitor()
    {
        if (!isMonitorOpen)
            return;

        isMonitorOpen = false;
        isSignalLost = false;
        AudioManager.Instance.Play(SFX.MonitorClose);
        StopSwitchStatic();

        HideUI();
        RestoreCursorAfterMonitor();
        SetVisionMaskEnabled(true);
        DisableMonitorCamera();

        var camRoomManager = FindObjectOfType<CameraRoomManager>();
        if (camRoomManager != null && !string.IsNullOrEmpty(savedRoom))
            camRoomManager.SwitchRoom(savedRoom);

        if (monsterRef != null)
            monsterRef.enabled = true;
        monsterRef = null;

        if (playerRef != null)
            playerRef.Input.EnablePlayerMoveInput();
        playerRef = null;

        isOnCooldown = true;
        lastCloseTime = Time.unscaledTime;
        StartCoroutine(CooldownCoroutine());
    }

    private void LateUpdate()
    {
        if (!isMonitorOpen || isSignalLost || monitorUIInstance == null)
            return;

        UpdateTrackedBlips();
    }

    public void NextCamera()
    {
        if (!isMonitorOpen || isSignalLost || monitorViews.Count == 0)
            return;

        currentCameraIndex = (currentCameraIndex + 1) % monitorViews.Count;
        AudioManager.Instance.Play(SFX.MonitorStatic);
        ShowCameraWithStatic(currentCameraIndex);
    }

    public void PrevCamera()
    {
        if (!isMonitorOpen || isSignalLost || monitorViews.Count == 0)
            return;

        currentCameraIndex = (currentCameraIndex - 1 + monitorViews.Count) % monitorViews.Count;
        AudioManager.Instance.Play(SFX.MonitorStatic);
        ShowCameraWithStatic(currentCameraIndex);
    }

    private void ShowCameraWithStatic(int index)
    {
        StopSwitchStatic();

        Sprite staticSprite = GetSwitchStaticSprite();
        if (staticSprite == null || switchStaticDuration <= 0f || monitorUIInstance == null)
        {
            ShowCamera(index);
            return;
        }

        switchStaticCoroutine = StartCoroutine(SwitchStaticCoroutine(index, staticSprite));
    }

    private IEnumerator SwitchStaticCoroutine(int index, Sprite staticSprite)
    {
        monitorUIInstance.SetSwitchStatic(staticSprite, true);

        yield return new WaitForSecondsRealtime(switchStaticDuration);

        if (!isMonitorOpen || isSignalLost)
        {
            switchStaticCoroutine = null;
            yield break;
        }

        ShowCamera(index);

        if (monitorUIInstance != null)
        {
            if (cameraTransitionDuration > 0f)
                yield return FadeSwitchStaticOut();
            else
                monitorUIInstance.SetSwitchStatic(null, false);
        }

        switchStaticCoroutine = null;
    }

    private IEnumerator FadeSwitchStaticOut()
    {
        float duration = Mathf.Max(0.01f, cameraTransitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            if (monitorUIInstance != null)
                monitorUIInstance.SetSwitchStaticAlpha(alpha);

            yield return null;
        }

        if (monitorUIInstance != null)
            monitorUIInstance.SetSwitchStatic(null, false);
    }

    private void StopSwitchStatic()
    {
        if (switchStaticCoroutine != null)
        {
            StopCoroutine(switchStaticCoroutine);
            switchStaticCoroutine = null;
        }

        if (monitorUIInstance != null)
            monitorUIInstance.SetSwitchStatic(null, false);
    }

    private Sprite GetSwitchStaticSprite()
    {
        if (switchStaticSprite != null)
            return switchStaticSprite;

        return LoadSpriteFromPath(switchStaticImagePath);
    }

    private void ShowCamera(int index)
    {
        if (index < 0 || index >= monitorViews.Count)
            return;

        var view = monitorViews[index];

        if (view.usesImage)
        {
            DisableMonitorCamera();
            if (monitorUIInstance != null)
            {
                LogShownView(index, view);
                monitorUIInstance.SetCameraFeed(view.image);
                UpdateTrackedBlips();
            }

            return;
        }

        if (monitorUIInstance != null)
            monitorUIInstance.SetCameraFeed(null);

        if (mainCamera == null)
        {
            Debug.LogWarning("[MonitorController] Cannot show camera view because Main Camera is missing.");
            return;
        }

        CreateOrEnableMonitorCamera();
        if (monitorCamera == null)
            return;

        ApplyMonitorCameraView(view);
    }

    private void ApplyMonitorCameraView(MonitorView view)
    {
        monitorCamera.transform.SetPositionAndRotation(view.position, view.rotation);
        monitorCamera.orthographic = view.orthographic;
        if (view.orthographic)
            monitorCamera.orthographicSize = view.orthographicSize;
        else
            monitorCamera.fieldOfView = view.fieldOfView;
    }

    private void BuildViews()
    {
        monitorViews.Clear();

        BuildDefaultImageViews();
        SortMonitorViews();

        if (monitorViews.Count > 0)
        {
            LogBuiltViewsOnce();
            return;
        }

        Debug.LogWarning("[MonitorController] No fixed monitor rooms could be built.");
    }

    private void LogBuiltViewsOnce()
    {
        if (hasLoggedBuiltViews)
            return;

        hasLoggedBuiltViews = true;
        Debug.Log($"[MonitorController] Built {monitorViews.Count} monitor views: {string.Join(", ", GetBuiltViewNames())}");
    }

    private void LogShownView(int index, MonitorView view)
    {
        string spriteInfo = view.image != null
            ? $"{view.image.name} {view.image.rect.width}x{view.image.rect.height}"
            : "<null>";
        Debug.Log($"[MonitorController] Showing view {index + 1}/{monitorViews.Count}: {view.roomName}, sprite={spriteInfo}, source={view.imageSourcePath}");
    }

    private string[] GetBuiltViewNames()
    {
        var names = new string[monitorViews.Count];
        for (int i = 0; i < monitorViews.Count; i++)
            names[i] = monitorViews[i].roomName;

        return names;
    }

    private void BuildViewsFromImageFeeds()
    {
        if (imageFeeds == null || imageFeeds.Length == 0)
            return;

        var roomBounds = BuildRoomBoundsMap();
        foreach (var feed in imageFeeds)
        {
            if (feed == null || !feed.enabled)
                continue;

            if (!ShouldIncludeCameraRoom(feed.roomName))
                continue;

            Sprite feedImage = feed.image != null ? feed.image : LoadSpriteFromPath(feed.imagePath);
            if (feedImage == null)
            {
                Debug.LogWarning($"[MonitorController] Image feed '{feed.roomName}' has no sprite: {feed.imagePath}");
                continue;
            }

            string roomName = string.IsNullOrWhiteSpace(feed.roomName) ? feedImage.name : feed.roomName;
            AddImageMonitorView(roomName, feedImage, roomBounds, feed.imagePath);
        }
    }

    private void BuildDefaultImageViews()
    {
        var roomBounds = BuildRoomBoundsMap();
        int roomCount = FixedMonitorRooms.Length;
        for (int i = 0; i < roomCount && i < DefaultImageFeedPaths.Length; i++)
        {
            string roomName = FixedMonitorRooms[i];
            Sprite feedImage = LoadSpriteFromPath(DefaultImageFeedPaths[i]);
            if (feedImage == null)
            {
                Debug.LogWarning($"[MonitorController] Default image feed '{roomName}' has no sprite: {DefaultImageFeedPaths[i]}");
                feedImage = CreateFallbackRoomSprite(roomName);
            }

            AddImageMonitorView(roomName, feedImage, roomBounds, DefaultImageFeedPaths[i]);
        }
    }

    private void AddImageMonitorView(string roomName, Sprite feedImage, Dictionary<string, Bounds> roomBounds, string imageSourcePath)
    {
        bool hasRoomBounds = roomBounds.TryGetValue(roomName, out Bounds bounds);
        monitorViews.Add(new MonitorView
        {
            roomName = roomName,
            image = feedImage,
            usesImage = true,
            rotation = Quaternion.identity,
            orthographic = true,
            orthographicSize = cameraOrthoSize,
            fieldOfView = 60f,
            imageSourcePath = imageSourcePath,
            roomBounds = bounds,
            hasRoomBounds = hasRoomBounds
        });
    }

    /// <summary>
    /// Maps an asset path basename to <c>Resources/MonitorRooms/&lt;name&gt;</c> (e.g. Cuardroom.png → Guardroom).
    /// </summary>
    private static string GetMonitorRoomResourceName(string imagePath)
    {
        string baseName = Path.GetFileNameWithoutExtension(imagePath);
        if (string.IsNullOrEmpty(baseName))
            return null;

        if (string.Equals(baseName, "Cuardroom", System.StringComparison.OrdinalIgnoreCase))
            return "Guardroom";

        return baseName;
    }

    private static Sprite LoadSpriteFromPath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return null;

        if (loadedPathSprites.TryGetValue(imagePath, out var cachedSprite))
            return cachedSprite;

        string resourceRoomName = GetMonitorRoomResourceName(imagePath);
        if (!string.IsNullOrEmpty(resourceRoomName))
        {
            var resSprite = Resources.Load<Sprite>($"MonitorRooms/{resourceRoomName}");
            if (resSprite != null)
            {
                loadedPathSprites[imagePath] = resSprite;
                return resSprite;
            }
        }

#if UNITY_EDITOR
        var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
        if (sprite != null)
        {
            loadedPathSprites[imagePath] = sprite;
            return sprite;
        }

        var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(imagePath);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] is Sprite childSprite)
            {
                loadedPathSprites[imagePath] = childSprite;
                return childSprite;
            }
        }
#endif

        string fullPath = Path.Combine(Application.dataPath, imagePath.StartsWith("Assets/") ? imagePath.Substring("Assets/".Length) : imagePath);
        if (!File.Exists(fullPath))
            return null;

        byte[] bytes = File.ReadAllBytes(fullPath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
            return null;

        texture.name = Path.GetFileNameWithoutExtension(fullPath);
        var runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        runtimeSprite.name = texture.name;
        loadedPathSprites[imagePath] = runtimeSprite;
        return runtimeSprite;
    }

    private static Sprite CreateFallbackRoomSprite(string roomName)
    {
        string cacheKey = $"fallback:{roomName}";
        if (loadedPathSprites.TryGetValue(cacheKey, out var cachedSprite))
            return cachedSprite;

        const int width = 512;
        const int height = 288;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(18, 24, 28, 255);

        texture.SetPixels32(pixels);
        texture.Apply();
        texture.name = roomName;

        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = roomName;
        loadedPathSprites[cacheKey] = sprite;
        return sprite;
    }

    private bool TryBuildViewsFromRoomManager()
    {
        var camRoomManager = FindObjectOfType<CameraRoomManager>();
        if (camRoomManager == null)
        {
            return false;
        }

        var roomCameras = camRoomManager.GetValidRoomCameras();
        if (roomCameras == null || roomCameras.Length == 0)
        {
            return false;
        }

        foreach (var roomCamera in roomCameras)
        {
            if (roomCamera == null || roomCamera.vcam == null)
                continue;

            if (!ShouldIncludeCameraRoom(roomCamera.roomName))
                continue;

            var vcam = roomCamera.vcam;
            monitorViews.Add(new MonitorView
            {
                roomName = roomCamera.roomName,
                position = vcam.transform.position,
                rotation = vcam.transform.rotation,
                orthographic = vcam.m_Lens.Orthographic,
                orthographicSize = vcam.m_Lens.OrthographicSize,
                fieldOfView = vcam.m_Lens.FieldOfView
            });
        }

        return monitorViews.Count > 0;
    }

    private void AddViewsFromRoomBounds()
    {
        var roomBounds = GetSceneRoomBounds();
        foreach (var col in roomBounds)
        {
            string roomName = col.name.Replace("RoomBounds_", "");
            if (!ShouldIncludeCameraRoom(roomName))
                continue;

            Bounds bounds = col.bounds;
            Vector2 center = bounds.center;
            monitorViews.Add(new MonitorView
            {
                roomName = roomName,
                position = new Vector3(center.x, center.y, -cameraHeight),
                rotation = Quaternion.identity,
                orthographic = true,
                orthographicSize = GetOrthographicSizeForBounds(bounds),
                fieldOfView = 60f
            });
        }
    }

    private float GetOrthographicSizeForBounds(Bounds bounds)
    {
        float aspect = mainCamera != null ? mainCamera.aspect : (16f / 9f);
        float halfHeight = bounds.extents.y + roomBoundsPadding;
        float halfWidthAsHeight = (bounds.extents.x + roomBoundsPadding) / Mathf.Max(0.01f, aspect);
        return Mathf.Max(cameraOrthoSize, halfHeight, halfWidthAsHeight);
    }

    private bool ShouldIncludeCameraRoom(string roomName)
    {
        for (int i = 0; i < FixedMonitorRooms.Length; i++)
        {
            if (string.Equals(FixedMonitorRooms[i], roomName, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private bool HasMonitorViewForRoom(string roomName)
    {
        for (int i = 0; i < monitorViews.Count; i++)
        {
            if (string.Equals(monitorViews[i].roomName, roomName, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private void SortMonitorViews()
    {
        monitorViews.Sort((a, b) =>
        {
            int aIndex = GetIncludedRoomIndex(a.roomName);
            int bIndex = GetIncludedRoomIndex(b.roomName);
            if (aIndex != bIndex)
                return aIndex.CompareTo(bIndex);

            return string.Compare(a.roomName, b.roomName, System.StringComparison.Ordinal);
        });
    }

    private int GetIncludedRoomIndex(string roomName)
    {
        for (int i = 0; i < FixedMonitorRooms.Length; i++)
        {
            if (string.Equals(FixedMonitorRooms[i], roomName, System.StringComparison.Ordinal))
                return i;
        }

        return int.MaxValue;
    }

    private void CacheTrackedObjects()
    {
        trackedPlayer = FindObjectOfType<PlayerController>();
        trackedMonster = FindObjectOfType<MonsterController>();
        trackedBags = FindObjectsOfType<TravelBag>(true);
    }

    private void UpdateTrackedBlips()
    {
        if (currentCameraIndex < 0 || currentCameraIndex >= monitorViews.Count)
            return;

        var view = monitorViews[currentCameraIndex];
        if (!view.usesImage || !view.hasRoomBounds)
        {
            monitorUIInstance.SetTrackedBlips(null, 0);
            return;
        }

        var blips = new List<MonitorTrackedBlip>();
        AddTrackedBlip(blips, trackedPlayer != null ? trackedPlayer.transform : null, view, new Color(0.15f, 0.85f, 1f, 1f), 18f);
        AddTrackedBlip(blips, trackedMonster != null ? trackedMonster.transform : null, view, new Color(1f, 0.08f, 0.08f, 1f), 22f);

        if (trackedBags == null || trackedBags.Length == 0)
            trackedBags = FindObjectsOfType<TravelBag>(true);

        for (int i = 0; i < trackedBags.Length; i++)
        {
            var bag = trackedBags[i];
            if (bag == null || bag.IsOpened)
                continue;

            AddTrackedBlip(blips, bag.transform, view, new Color(1f, 0.82f, 0.15f, 1f), 14f);
        }

        monitorUIInstance.SetTrackedBlips(blips, blips.Count);
    }

    private void AddTrackedBlip(List<MonitorTrackedBlip> blips, Transform target, MonitorView view, Color color, float size)
    {
        if (target == null)
            return;

        Bounds bounds = view.roomBounds;
        Vector3 min = bounds.min;
        Vector3 size3 = bounds.size;
        if (target.position.x < min.x || target.position.x > min.x + size3.x ||
            target.position.y < min.y || target.position.y > min.y + size3.y)
        {
            return;
        }

        Vector2 normalized = new Vector2(
            Mathf.InverseLerp(min.x, min.x + size3.x, target.position.x),
            Mathf.InverseLerp(min.y, min.y + size3.y, target.position.y));

        blips.Add(new MonitorTrackedBlip
        {
            normalizedPosition = normalized,
            color = color,
            size = size
        });
    }

    private static Dictionary<string, Bounds> BuildRoomBoundsMap()
    {
        var map = new Dictionary<string, Bounds>();
        var roomBounds = GetSceneRoomBounds();
        foreach (var col in roomBounds)
        {
            if (col == null)
                continue;

            string roomName = col.name.Replace("RoomBounds_", "");
            map[roomName] = col.bounds;
        }

        return map;
    }

    private static PolygonCollider2D[] GetSceneRoomBounds()
    {
        var roomBounds = new List<PolygonCollider2D>();

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            var scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
                continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null)
                    continue;

                var colliders = root.GetComponentsInChildren<PolygonCollider2D>(true);
                foreach (var col in colliders)
                {
                    if (col == null || !col.name.StartsWith("RoomBounds_"))
                        continue;

                    roomBounds.Add(col);
                }
            }
        }

        return roomBounds.ToArray();
    }

    private bool HasCameraViews()
    {
        for (int i = 0; i < monitorViews.Count; i++)
        {
            if (!monitorViews[i].usesImage)
                return true;
        }

        return false;
    }

    private void CreateOrEnableMonitorCamera()
    {
        if (monitorCameraObject == null)
        {
            monitorCameraObject = new GameObject("[MonitorViewCamera]");
            monitorCamera = monitorCameraObject.AddComponent<Camera>();
        }
        else if (monitorCamera == null)
        {
            monitorCamera = monitorCameraObject.GetComponent<Camera>();
            if (monitorCamera == null)
                monitorCamera = monitorCameraObject.AddComponent<Camera>();
        }

        monitorCamera.CopyFrom(mainCamera);
        monitorCamera.depth = mainCamera.depth + 100f;
        monitorCamera.cullingMask = ~0;
        monitorCamera.targetTexture = null;
        monitorCamera.enabled = true;
        monitorCameraObject.SetActive(true);
    }

    private void DisableMonitorCamera()
    {
        if (monitorCamera != null)
            monitorCamera.enabled = false;

        if (monitorCameraObject != null)
            monitorCameraObject.SetActive(false);
    }

    private void CleanupMonitorCamera()
    {
        if (monitorCameraObject == null)
            return;

        Destroy(monitorCameraObject);
        monitorCameraObject = null;
        monitorCamera = null;
    }

    private void SetVisionMaskEnabled(bool enabled)
    {
        var mask = FindObjectOfType<PlayerVisionMaskSystem>();
        if (mask == null)
            return;

        mask.SetForceHidden(!enabled);
    }

    private string GetCameraRoomName(int index)
    {
        if (index < 0 || index >= monitorViews.Count)
            return "<out_of_range>";

        return monitorViews[index].roomName;
    }

    private void ShowUI()
    {
        if (monitorUIPrefab == null)
        {
            Debug.LogWarning("[MonitorController] monitorUIPrefab is null.");
            return;
        }

        if (monitorUIInstance == null)
        {
            var canvasGo = new GameObject("[MonitorCanvas]");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            monitorUIInstance = Instantiate(monitorUIPrefab, canvas.transform);
        }

        monitorUIInstance.Show();
    }

    private void HideUI()
    {
        if (monitorUIInstance != null)
            monitorUIInstance.Hide();
    }

    private void ShowCursorForMonitor()
    {
        savedCursorVisible = Cursor.visible;
        savedCursorLockState = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestoreCursorAfterMonitor()
    {
        if (PauseMenu.IsPaused || ConfirmPanel.IsShowing)
            return;

        Cursor.visible = savedCursorVisible;
        Cursor.lockState = savedCursorLockState;
    }

    private void CleanupUI()
    {
        if (monitorUIInstance == null)
            return;

        Destroy(monitorUIInstance.gameObject);
        monitorUIInstance = null;
    }

    private IEnumerator SignalLostCoroutine()
    {
        isSignalLost = true;
        AudioManager.Instance.Play(SFX.MonitorSignalLost);

        yield return new WaitForSecondsRealtime(signalRecoverTime);

        if (!isMonitorOpen)
            yield break;

        isSignalLost = false;
        ShowCamera(currentCameraIndex);
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSecondsRealtime(cooldownDuration);
        isOnCooldown = false;
        AudioManager.Instance.Play(SFX.MonitorCooldownDone);
    }
}
