using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene-level allocator that spreads a fixed number of keys across all travel bags.
/// </summary>
public class TravelBagAllocator : MonoBehaviour
{
    private static TravelBagAllocator instance;

    [SerializeField, Min(0)] private int totalSceneKeys = KeyGameConfig.DefaultKeyCount;

    private bool distributionQueued;

    public static TravelBagAllocator Instance => instance;

    public static void RequestDistribution()
    {
        EnsureInstance();

        if (instance != null)
        {
            instance.QueueDistribution();
        }
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        var allocatorObject = new GameObject("[TravelBagAllocator]");
        instance = allocatorObject.AddComponent<TravelBagAllocator>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        QueueDistribution();
    }

    private void OnEnable()
    {
        QueueDistribution();
    }

    private void QueueDistribution()
    {
        if (distributionQueued)
        {
            return;
        }

        distributionQueued = true;
        StartCoroutine(DistributeNextFrame());
    }

    private IEnumerator DistributeNextFrame()
    {
        yield return null;
        DistributeKeys();
        distributionQueued = false;
    }

    private void DistributeKeys()
    {
        var bags = FindObjectsOfType<TravelBag>(true);
        if (bags == null || bags.Length == 0)
        {
            return;
        }

        var bagList = new List<TravelBag>(bags);
        for (int i = 0; i < bagList.Count; i++)
        {
            bagList[i].SetHasKey(false);
        }

        Shuffle(bagList);

        int keysToPlace = Mathf.Min(Mathf.Max(0, totalSceneKeys), bagList.Count);
        for (int i = 0; i < keysToPlace; i++)
        {
            bagList[i].SetHasKey(true);
        }

        if (totalSceneKeys > bagList.Count)
        {
            // Scene has fewer bags than configured keys; extra keys are skipped.
        }
    }

    private void Shuffle<T>(List<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            T temp = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = temp;
        }
    }
}
