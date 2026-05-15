using UnityEngine;
using System.Collections.Generic;

public class RoomDoorTrigger : MonoBehaviour
{
    public CameraRoomManager roomManager;
    public string targetRoom;
    [SerializeField, Min(0f)]
    private float roomDetectionFallbackDistance = 6f;

    private readonly Dictionary<Collider2D, DoorVisit> activeVisits = new Dictionary<Collider2D, DoorVisit>();

    private class DoorVisit
    {
        public string entryRoom;
        public bool switchedOnEnter;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (roomManager == null) return;

        string entryRoom = ResolveCurrentRoom(other.transform.position);
        bool switchedOnEnter = false;

        if (!string.IsNullOrEmpty(targetRoom) && entryRoom != targetRoom)
        {
            roomManager.SwitchRoomFromDoor(targetRoom);
            switchedOnEnter = true;
        }

        activeVisits[other] = new DoorVisit
        {
            entryRoom = entryRoom,
            switchedOnEnter = switchedOnEnter
        };
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (roomManager == null) return;

        activeVisits.TryGetValue(other, out var visit);
        activeVisits.Remove(other);

        string entryRoom = visit != null ? visit.entryRoom : ResolveCurrentRoom(other.transform.position);
        string exitRoom = roomManager.FindRoomAtPosition(other.transform.position);
        if (string.IsNullOrEmpty(exitRoom))
        {
            exitRoom = !string.IsNullOrEmpty(entryRoom) && entryRoom == targetRoom
                ? roomManager.FindClosestRoomAtPosition(other.transform.position, roomDetectionFallbackDistance, entryRoom)
                : roomManager.FindClosestRoomAtPosition(other.transform.position, roomDetectionFallbackDistance);
        }

        if (string.IsNullOrEmpty(exitRoom) && !string.IsNullOrEmpty(targetRoom) && entryRoom != targetRoom)
            exitRoom = targetRoom;

        if (string.IsNullOrEmpty(exitRoom))
            return;

        if (exitRoom != entryRoom || exitRoom != roomManager.currentRoom || exitRoom == targetRoom || (visit != null && !visit.switchedOnEnter))
            roomManager.SwitchRoomFromDoor(exitRoom);
    }

    private string ResolveCurrentRoom(Vector2 playerPosition)
    {
        if (!string.IsNullOrEmpty(roomManager.currentRoom))
            return roomManager.currentRoom;

        string room = roomManager.FindRoomAtPosition(playerPosition);
        if (!string.IsNullOrEmpty(room))
            return room;

        return roomManager.FindClosestRoomAtPosition(playerPosition, roomDetectionFallbackDistance);
    }
}
