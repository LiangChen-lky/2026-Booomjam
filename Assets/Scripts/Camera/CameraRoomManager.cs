using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

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
        cameraMap = new Dictionary<string, CinemachineVirtualCamera>();
        if (roomCameras != null)
        {
            foreach (var rc in roomCameras)
            {
                if (rc.vcam != null)
                {
                    cameraMap[rc.roomName] = rc.vcam;
                    // Debug.Log($"[CameraRoom] 注册房间相机: {rc.roomName}, Priority={rc.vcam.Priority}");
                }
            }
        }

        // Debug.Log($"[CameraRoom] 共 {cameraMap.Count} 个房间相机");

        // 根据玩家位置设置初始房间
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            string initRoom = FindRoomAtPosition(player.transform.position);
            // Debug.Log($"[CameraRoom] 玩家位置={player.transform.position}, 初始房间={initRoom}");
            if (!string.IsNullOrEmpty(initRoom))
                SwitchRoom(initRoom);
        }
        else
        {
            // Debug.LogWarning("[CameraRoom] 找不到 Player");
        }
    }

    public void SwitchRoom(string newRoom)
    {
        if (string.IsNullOrEmpty(newRoom) || newRoom == currentRoom) return;
        if (cameraMap == null || !cameraMap.ContainsKey(newRoom)) return;

        // Debug.Log($"[CameraRoom] 切换: {currentRoom} → {newRoom}");

        // 禁用所有房间相机
        foreach (var kvp in cameraMap)
        {
            kvp.Value.Priority = 0;
        }

        // 激活目标房间相机
        cameraMap[newRoom].Priority = 100;
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
        // 用场景中的 RoomBounds_Xxx 检测
        var roomBounds = FindObjectsOfType<PolygonCollider2D>();
        foreach (var col in roomBounds)
        {
            if (!col.name.StartsWith("RoomBounds_")) continue;
            if (col.OverlapPoint(worldPos))
                return col.name.Replace("RoomBounds_", "");
        }
        return null;
    }
}
