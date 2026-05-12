using UnityEngine;
using Cinemachine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class CameraRoomManager : MonoBehaviour
{
    [System.Serializable]
    public class RoomCamera
    {
        public string roomName;
        public CinemachineVirtualCamera vcam;
    }

    public RoomCamera[] roomCameras;
    public string currentRoom;

    [Header("Startup Camera Control")]
    [SerializeField, Tooltip("Switch to the room camera that contains the player when the scene starts. Leave off when the player should keep using the normal follow camera.")]
    private bool switchToPlayerRoomOnStart = false;
    [SerializeField, Tooltip("Let this manager raise/lower Cinemachine room camera priorities. Leave off when the player should keep using the normal follow camera.")]
    private bool controlRoomCameraPriorities = false;

    [Header("Room Camera Auto Setup")]
    [SerializeField, Tooltip("Automatically place configured room cameras at matching RoomBounds centers and clear Follow/LookAt.")]
    private bool autoAlignRoomCamerasToBounds = true;
    [SerializeField, Tooltip("Z position used when auto-aligning room cameras.")]
    private float roomCameraZ = -30f;

    [Header("环境音映射")]
    [Tooltip("房间名到环境音的映射。未映射的房间不播放环境音。")]
    public RoomAmbientMapping[] ambientMappings;

    [System.Serializable]
    public class RoomAmbientMapping
    {
        public string roomName;
        public AmbientRoom ambient;
    }

    private Dictionary<string, CinemachineVirtualCamera> cameraMap;

    private void Start()
    {
        cameraMap = BuildCameraMap(roomCameras);

        if (switchToPlayerRoomOnStart)
            SwitchToPlayerRoomAtStart();
    }

    private void SwitchToPlayerRoomAtStart()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        string initRoom = FindRoomAtPosition(player.transform.position);
        if (!string.IsNullOrEmpty(initRoom))
            SwitchRoom(initRoom);
    }

    public RoomCamera[] GetValidRoomCameras()
    {
        AlignRoomCamerasToBoundsIfNeeded();

        if (roomCameras == null || roomCameras.Length == 0)
        {
            return new RoomCamera[0];
        }

        var validRoomCameras = new List<RoomCamera>();
        foreach (var roomCamera in roomCameras)
        {
            if (roomCamera == null)
                continue;

            if (string.IsNullOrEmpty(roomCamera.roomName))
                continue;

            if (roomCamera.vcam == null)
                continue;

            validRoomCameras.Add(roomCamera);
        }

        return validRoomCameras.ToArray();
    }

    public void AlignRoomCamerasToBoundsIfNeeded()
    {
        if (!autoAlignRoomCamerasToBounds)
            return;

        AlignRoomCamerasToBounds();
    }

    public void AlignRoomCamerasToBounds()
    {
        if (roomCameras == null || roomCameras.Length == 0)
            return;

        var boundsByRoom = BuildRoomBoundsMap();
        if (boundsByRoom.Count == 0)
        {
            return;
        }

        foreach (var roomCamera in roomCameras)
        {
            if (roomCamera == null || roomCamera.vcam == null || string.IsNullOrEmpty(roomCamera.roomName))
                continue;

            if (!boundsByRoom.TryGetValue(roomCamera.roomName, out var boundary) || boundary == null)
            {
                Debug.LogWarning($"[CameraRoomManager] Room camera '{roomCamera.roomName}' has no matching RoomBounds_{roomCamera.roomName}.");
                continue;
            }

            var vcam = roomCamera.vcam;
            Vector2 center = boundary.bounds.center;
            vcam.transform.position = new Vector3(center.x, center.y, roomCameraZ);
            vcam.m_Follow = null;
            vcam.m_LookAt = null;
            vcam.Priority = 0;

            var confiner = vcam.GetComponent<CinemachineConfiner2D>();
            if (confiner == null)
                confiner = vcam.gameObject.AddComponent<CinemachineConfiner2D>();

            confiner.m_BoundingShape2D = boundary;
            confiner.m_Damping = 0f;
            confiner.m_MaxWindowSize = 0;
            confiner.InvalidateCache();
        }
    }

    public void SwitchRoom(string newRoom)
    {
        if (string.IsNullOrEmpty(newRoom)) return;
        if (cameraMap == null)
            cameraMap = BuildCameraMap(roomCameras);

        if (controlRoomCameraPriorities && cameraMap.ContainsKey(newRoom))
        {
            // 禁用所有房间相机
            foreach (var kvp in cameraMap)
            {
                kvp.Value.Priority = 0;
            }

            // 激活目标房间相机
            cameraMap[newRoom].Priority = 100;
        }

        currentRoom = newRoom;

        // 切换环境音
        SwitchAmbientForRoom(newRoom);
    }

    private void SwitchAmbientForRoom(string roomName)
    {
        if (ambientMappings == null) return;

        foreach (var mapping in ambientMappings)
        {
            if (mapping.roomName == roomName)
            {
                AudioManager.Instance.PlayAmbient(mapping.ambient);
                return;
            }
        }
    }

    public string FindRoomAtPosition(Vector2 worldPos)
    {
        // 用场景中的 RoomBounds_Xxx 检测，包括失活对象
        var roomBounds = GetSceneRoomBounds();
        foreach (var col in roomBounds)
        {
            if (col.OverlapPoint(worldPos))
            {
                return col.name.Replace("RoomBounds_", "");
            }
        }
        return null;
    }

    private Dictionary<string, CinemachineVirtualCamera> BuildCameraMap(RoomCamera[] cameras)
    {
        var map = new Dictionary<string, CinemachineVirtualCamera>();
        if (cameras == null)
            return map;

        foreach (var roomCamera in cameras)
        {
            if (roomCamera == null)
                continue;

            if (string.IsNullOrEmpty(roomCamera.roomName))
                continue;

            if (roomCamera.vcam == null)
                continue;

            map[roomCamera.roomName] = roomCamera.vcam;
        }

        return map;
    }

    private static Dictionary<string, PolygonCollider2D> BuildRoomBoundsMap()
    {
        var map = new Dictionary<string, PolygonCollider2D>();
        var roomBounds = GetSceneRoomBounds();
        foreach (var col in roomBounds)
        {
            if (col == null)
                continue;

            string roomName = col.name.Replace("RoomBounds_", "");
            map[roomName] = col;
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
                    if (col == null)
                        continue;

                    if (!col.name.StartsWith("RoomBounds_"))
                        continue;
                    roomBounds.Add(col);
                }
            }
        }

        return roomBounds.ToArray();
    }
}
