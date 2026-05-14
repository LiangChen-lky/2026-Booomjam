using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TravelBagTests
{
    [Test]
    public void PickupKey_OpenedBag_DisablesInteractionAndKeyVisual()
    {
        EnsureAudioManager();

        var bagObject = new GameObject("TravelBag");
        var bagCollider = bagObject.AddComponent<CircleCollider2D>();
        var interactableType = FindType("InteractableItem");
        Assert.IsNotNull(interactableType, "Could not find InteractableItem type.");
        var interactable = (Behaviour)bagObject.AddComponent(interactableType);

        var keyVisual = new GameObject("Key");
        keyVisual.transform.SetParent(bagObject.transform, false);

        var bagType = FindType("TravelBag");
        Assert.IsNotNull(bagType, "Could not find TravelBag type.");
        var bag = (MonoBehaviour)bagObject.AddComponent(bagType);

        SetPrivateField(bag, "hasKey", true);
        InvokeMethod(bag, "PickupKey", new object[] { null });

        Assert.IsFalse(interactable.enabled);
        Assert.IsFalse(bagCollider.enabled);
        Assert.IsFalse(keyVisual.activeSelf);
    }

    [Test]
    public void PickupKey_WithStateRenderers_TogglesOpenedKeyVisual()
    {
        EnsureAudioManager();

        var bagObject = new GameObject("TravelBag");
        bagObject.AddComponent<SpriteRenderer>();
        bagObject.AddComponent<CircleCollider2D>();
        var interactableType = FindType("InteractableItem");
        Assert.IsNotNull(interactableType, "Could not find InteractableItem type.");
        bagObject.AddComponent(interactableType);

        var closedRenderer = CreateChildRenderer(bagObject.transform, "Closed");
        var openedKeyRenderer = CreateChildRenderer(bagObject.transform, "OpenedKey");
        var openedEmptyRenderer = CreateChildRenderer(bagObject.transform, "OpenedEmpty");

        var bagType = FindType("TravelBag");
        Assert.IsNotNull(bagType, "Could not find TravelBag type.");
        var bag = (MonoBehaviour)bagObject.AddComponent(bagType);

        SetPrivateField(bag, "closedVisualRenderer", closedRenderer);
        SetPrivateField(bag, "openedWithKeyVisualRenderer", openedKeyRenderer);
        SetPrivateField(bag, "openedEmptyVisualRenderer", openedEmptyRenderer);
        SetPrivateField(bag, "hasKey", true);

        InvokeMethod(bag, "PickupKey", new object[] { null });

        Assert.IsFalse(closedRenderer.gameObject.activeSelf);
        Assert.IsTrue(openedKeyRenderer.gameObject.activeSelf);
        Assert.IsFalse(openedEmptyRenderer.gameObject.activeSelf);
    }

    [Test]
    public void PickupKey_WithStateRenderers_TogglesOpenedEmptyVisual()
    {
        EnsureAudioManager();

        var bagObject = new GameObject("TravelBag");
        bagObject.AddComponent<SpriteRenderer>();
        bagObject.AddComponent<CircleCollider2D>();
        var interactableType = FindType("InteractableItem");
        Assert.IsNotNull(interactableType, "Could not find InteractableItem type.");
        bagObject.AddComponent(interactableType);

        var closedRenderer = CreateChildRenderer(bagObject.transform, "Closed");
        var openedKeyRenderer = CreateChildRenderer(bagObject.transform, "OpenedKey");
        var openedEmptyRenderer = CreateChildRenderer(bagObject.transform, "OpenedEmpty");

        var bagType = FindType("TravelBag");
        Assert.IsNotNull(bagType, "Could not find TravelBag type.");
        var bag = (MonoBehaviour)bagObject.AddComponent(bagType);

        SetPrivateField(bag, "closedVisualRenderer", closedRenderer);
        SetPrivateField(bag, "openedWithKeyVisualRenderer", openedKeyRenderer);
        SetPrivateField(bag, "openedEmptyVisualRenderer", openedEmptyRenderer);
        SetPrivateField(bag, "hasKey", false);

        InvokeMethod(bag, "PickupKey", new object[] { null });

        Assert.IsFalse(closedRenderer.gameObject.activeSelf);
        Assert.IsFalse(openedKeyRenderer.gameObject.activeSelf);
        Assert.IsTrue(openedEmptyRenderer.gameObject.activeSelf);
    }

    private static SpriteRenderer CreateChildRenderer(Transform parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.AddComponent<SpriteRenderer>();
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field '{fieldName}'.");
        field.SetValue(target, value);
    }

    private static void InvokeMethod(object target, string methodName, object[] parameters)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing method '{methodName}'.");
        method.Invoke(target, parameters);
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

    private static void EnsureAudioManager()
    {
        var audioType = FindType("AudioManager");
        Assert.IsNotNull(audioType, "Could not find AudioManager type.");

        if (UnityEngine.Object.FindObjectOfType(audioType) == null)
        {
            var audioObject = new GameObject("AudioManager");
            audioObject.AddComponent(audioType);
        }
    }
}
