using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class WeightedLandPrefab
{
    public GameObject prefab;
    [Min(0f)] public float weight = 1f;
}

public class Spawner : MonoBehaviour
{
    [Header("Raycast")]
    public Transform center;
    [Min(0.001f)] public float radius = 10f;

    [Tooltip("Optional extra distance outside the radius to start the ray.")]
    public float startPadding = 0.5f;

    [Tooltip("How far inward to raycast. Usually radius * 2 is plenty.")]
    public float maxDistanceMultiplier = 2.5f;

    [Header("Layers")]
    public LayerMask waterMask;
    public LayerMask landMask;

    [Header("Weighted Land Prefabs")]
    public WeightedLandPrefab[] landSpawns;

    [Header("Spawn Settings")]
    public int spawnCount = 50;
    public int maxAttemptsPerSpawn = 50;

    public List<GameObject> spawned = new List<GameObject>();

    [Header("Surface Settings")]
    public float surfaceOffset = 0.05f;
    public bool alignUpToNormal = true;
    
    public int seed = 12345;
    System.Random rng;

    void Reset()
    {
        center = transform;
    }

    void Awake()
    {
        rng = new System.Random(seed);
    }

    void Start()
    {
        Spawn();
    }

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        if (!center)
        {
            Debug.LogWarning("Missing center transform.");
            return;
        }

        if (landSpawns == null || landSpawns.Length == 0)
        {
            Debug.LogWarning("No land prefabs assigned.");
            return;
        }

        int successCount = 0;
        int totalAttempts = 0;
        int maxTotalAttempts = spawnCount * maxAttemptsPerSpawn;

        while (successCount < spawnCount && totalAttempts < maxTotalAttempts)
        {
            totalAttempts++;

            if (TrySpawnOne() == 1)
            {
                successCount++;
            }
        }

        Debug.Log($"Spawned {successCount}/{spawnCount} after {totalAttempts} attempts");
    }

    int TrySpawnOne()
    {
        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            Vector3 origin = RandomPointOnSphere(center.position, radius + startPadding, rng);
            Vector3 dir = (center.position - origin).normalized;

            Debug.Log(origin);

            float maxDist = radius * maxDistanceMultiplier;

            int combinedMask = waterMask | landMask;

            RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDist, combinedMask, QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
                continue;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                int hitLayerMask = 1 << hit.collider.gameObject.layer;

                // Skip water hits completely
                if ((hitLayerMask & waterMask.value) != 0)
                {
                    Debug.Log("Hit water, skipping.");
                    return 0;
                }

                // Spawn ONLY if we hit land
                if ((hitLayerMask & landMask.value) != 0)
                {
                    GameObject prefab = GetWeightedRandomPrefab(landSpawns);

                    if (prefab == null)
                        return 0;

                    SpawnAtHit(hit, -dir, prefab);
                    Debug.Log($"Spawned on land at {prefab.name}");
                    return 1;
                }
            }
        }

        return 0;
    }

    void SpawnAtHit(RaycastHit hit, Vector3 outwardDirection, GameObject prefab)
    {
        Vector3 pos = hit.point + hit.collider.transform.up * surfaceOffset;

        // Align prefab's UP (Y) to the hit object's UP axis
        Quaternion rot = Quaternion.FromToRotation(Vector3.up, outwardDirection);

        spawned.Add(Instantiate(prefab, pos, rot, transform));
    }

    GameObject GetWeightedRandomPrefab(WeightedLandPrefab[] prefabs)
    {
        float totalWeight = 0f;

        foreach (var item in prefabs)
        {
            if (item.prefab != null && item.weight > 0f)
                totalWeight += item.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue = (float)rng.NextDouble() * totalWeight;
        float currentWeight = 0f;

        foreach (var item in prefabs)
        {
            if (item.prefab == null || item.weight <= 0f)
                continue;

            currentWeight += item.weight;

            if (randomValue <= currentWeight)
                return item.prefab;
        }

        return null;
    }

    static Vector3 RandomPointOnSphere(Vector3 center, float r, System.Random rng)
    {
        // uniform point on unit sphere
        double z = rng.NextDouble() * 2.0 - 1.0;
        double t = rng.NextDouble() * (System.Math.PI * 2.0);
        double xy = System.Math.Sqrt(1.0 - z * z);

        float x = (float)(xy * System.Math.Cos(t));
        float y = (float)z;
        float z2 = (float)(xy * System.Math.Sin(t));

        return center + new Vector3(x, y, z2) * r;
    }
}
