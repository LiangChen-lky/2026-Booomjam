using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

public class MonitorRoomCameraTests
{
    [Test]
    public void GetValidRoomCameras_FiltersNullEntriesAndKeepsOrder()
    {
        var managerType = FindType("CameraRoomManager");
        Assert.IsNotNull(managerType, "Could not find CameraRoomManager type.");

        var managerObject = new GameObject("CameraRoomManager");
        var manager = managerObject.AddComponent(managerType);

        var vcamType = FindType("CinemachineVirtualCamera");
        Assert.IsNotNull(vcamType, "Could not find CinemachineVirtualCamera type.");

        var hallCamera = new GameObject("HallCamera").AddComponent(vcamType);
        var classroomCamera = new GameObject("ClassroomCamera").AddComponent(vcamType);

        var roomCameraType = managerType.GetNestedType("RoomCamera", BindingFlags.Public);
        Assert.IsNotNull(roomCameraType, "Could not find RoomCamera nested type.");

        var roomCameras = Array.CreateInstance(roomCameraType, 4);
        roomCameras.SetValue(CreateRoomCamera(roomCameraType, "Hall", hallCamera), 0);
        roomCameras.SetValue(null, 1);
        roomCameras.SetValue(CreateRoomCamera(roomCameraType, "Toilet", null), 2);
        roomCameras.SetValue(CreateRoomCamera(roomCameraType, "Classroom", classroomCamera), 3);

        SetPrivateFieldOrProperty(manager, "roomCameras", roomCameras);

        var validRoomCameras = (Array)InvokeMethod(manager, "GetValidRoomCameras", null);

        Assert.AreEqual(2, validRoomCameras.Length);
        Assert.AreEqual("Hall", GetStringField(validRoomCameras.GetValue(0), "roomName"));
        Assert.AreEqual("Classroom", GetStringField(validRoomCameras.GetValue(1), "roomName"));
    }

    [Test]
    public void BuildCameras_UsesRegisteredRoomCamerasBeforeScanningBounds()
    {
        var monitorType = FindType("MonitorController");
        Assert.IsNotNull(monitorType, "Could not find MonitorController type.");

        var managerType = FindType("CameraRoomManager");
        Assert.IsNotNull(managerType, "Could not find CameraRoomManager type.");

        var vcamType = FindType("CinemachineVirtualCamera");
        Assert.IsNotNull(vcamType, "Could not find CinemachineVirtualCamera type.");

        var monitorObject = new GameObject("MonitorController");
        var monitor = monitorObject.AddComponent(monitorType);

        var managerObject = new GameObject("CameraRoomManager");
        var manager = managerObject.AddComponent(managerType);

        var hallCamera = new GameObject("HallCamera").AddComponent(vcamType);
        var classroomCamera = new GameObject("ClassroomCamera").AddComponent(vcamType);

        var roomCameraType = managerType.GetNestedType("RoomCamera", BindingFlags.Public);
        Assert.IsNotNull(roomCameraType, "Could not find RoomCamera nested type.");

        var roomCameras = Array.CreateInstance(roomCameraType, 2);
        roomCameras.SetValue(CreateRoomCamera(roomCameraType, "Hall", hallCamera), 0);
        roomCameras.SetValue(CreateRoomCamera(roomCameraType, "Classroom", classroomCamera), 1);
        SetPrivateFieldOrProperty(manager, "roomCameras", roomCameras);

        InvokeMethod(monitor, "BuildCameras", null);

        var monitorCameras = (ICollection)GetPrivateField(monitor, "monitorCameras");

        Assert.AreEqual(2, monitorCameras.Count);
    }

    [Test]
    public void GetSceneRoomBounds_IncludesInactiveRoomBoundsObjects()
    {
        var monitorType = FindType("MonitorController");
        Assert.IsNotNull(monitorType, "Could not find MonitorController type.");

        CreateRoomBound("RoomBounds_TestActive", true);
        CreateRoomBound("RoomBounds_TestInactive", false);

        var roomBounds = (Array)InvokeStaticMethod(monitorType, "GetSceneRoomBounds", null);

        Assert.IsTrue(ContainsRoomBound(roomBounds, "RoomBounds_TestActive"));
        Assert.IsTrue(ContainsRoomBound(roomBounds, "RoomBounds_TestInactive"));
    }

    private static object CreateRoomCamera(Type roomCameraType, string roomName, Component vcam)
    {
        var roomCamera = Activator.CreateInstance(roomCameraType);
        SetPrivateFieldOrProperty(roomCamera, "roomName", roomName);
        SetPrivateFieldOrProperty(roomCamera, "vcam", vcam);
        return roomCamera;
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{fieldName}'.");
        return field.GetValue(target);
    }

    private static string GetStringField(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{fieldName}'.");
        return (string)field.GetValue(target);
    }

    private static void SetPrivateFieldOrProperty(object target, string memberName, object value)
    {
        var field = target.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }

        var property = target.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(property, $"Missing field or property '{memberName}'.");
        property.SetValue(target, value);
    }

    private static object InvokeMethod(object target, string methodName, object[] parameters)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing method '{methodName}'.");
        return method.Invoke(target, parameters);
    }

    private static object InvokeStaticMethod(Type targetType, string methodName, object[] parameters)
    {
        var method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing static method '{methodName}'.");
        return method.Invoke(null, parameters);
    }

    private static GameObject CreateRoomBound(string name, bool active)
    {
        var go = new GameObject(name);
        go.SetActive(active);
        var collider = go.AddComponent<PolygonCollider2D>();
        collider.pathCount = 1;
        collider.SetPath(0, new[]
        {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f),
            new Vector2(-1f, 1f)
        });
        return go;
    }

    private static bool ContainsRoomBound(Array roomBounds, string expectedName)
    {
        foreach (var item in roomBounds)
        {
            if (item is PolygonCollider2D collider && collider.name == expectedName)
                return true;
        }

        return false;
    }

    private static Type FindType(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }
}
