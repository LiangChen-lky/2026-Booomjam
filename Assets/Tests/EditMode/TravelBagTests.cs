using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TravelBagTests
{
    [Test]
    public void PickupKey_OpenedBag_DisablesInteractionAndSwitchesSprite()
    {
        var audioType = FindType("AudioManager");
        Assert.IsNotNull(audioType, "Could not find AudioManager type.");
        var audioObject = new GameObject("AudioManager");
        audioObject.AddComponent(audioType);

        var bagObject = new GameObject("TravelBag");
        var bagRenderer = bagObject.AddComponent<SpriteRenderer>();
        var bagCollider = bagObject.AddComponent<CircleCollider2D>();
        var interactableType = FindType("InteractableItem");
        Assert.IsNotNull(interactableType, "Could not find InteractableItem type.");
        var interactable = (Behaviour)bagObject.AddComponent(interactableType);

        var keyVisual = new GameObject("Key");
        keyVisual.transform.SetParent(bagObject.transform, false);

        var bagType = FindType("TravelBag");
        Assert.IsNotNull(bagType, "Could not find TravelBag type.");
        var bag = (MonoBehaviour)bagObject.AddComponent(bagType);

        var closedSprite = CreateSprite(Color.red);
        var openedSprite = CreateSprite(Color.green);

        bagRenderer.sprite = closedSprite;
        SetPrivateField(bag, "closedSprite", closedSprite);
        SetPrivateField(bag, "openedSprite", openedSprite);
        SetPrivateField(bag, "emptyBagHintText", string.Empty);

        SetPrivateField(bag, "hasKey", true);
        InvokeMethod(bag, "PickupKey", null);

        Assert.AreEqual(openedSprite, bagRenderer.sprite);
        Assert.IsFalse(interactable.enabled);
        Assert.IsFalse(bagCollider.enabled);
        Assert.IsFalse(keyVisual.activeSelf);
    }

    private static Sprite CreateSprite(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
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
}
