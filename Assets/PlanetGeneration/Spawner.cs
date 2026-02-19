using UnityEngine;
using System.Collections.Generic;


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

    [Header("Spawning")]
    public GameObject waterSpawn;
    public GameObject landSpawn;
    public int spawnCount = 50;
    public int maxAttemptsPerSpawn = 50;

    public List<GameObject> spawned = new List<GameObject>();


    [Tooltip("Push spawned prefab outward so it doesn't clip into the surface.")]
    public float surfaceOffset = 0.05f;

    [Tooltip("If true, align object's up to surface normal.")]
    public bool alignUpToNormal = true;

    void Reset()
    {
        center = transform;
    }

    void Start()
    {
        Spawn();
    }

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        if (!center || !landSpawn || !waterSpawn)
        {
            Debug.LogWarning("Missing center or prefab.");
            return;
        }

        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            if (TrySpawnOne())
                spawned++;
        }

        Debug.Log($"Spawned {spawned}/{spawnCount}");
    }

    bool TrySpawnOne()
    {
        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            Vector3 origin = RandomPointOnSphere(center.position, radius + startPadding);
            Vector3 dir = (center.position - origin).normalized; // inward

            float maxDist = radius * maxDistanceMultiplier;

            int combinedMask = waterMask | landMask;
            RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDist, combinedMask, QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0)
                continue;

            // Sort by distance so we process the first surface hit outward->inward
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                int hitLayerMask = 1 << hit.collider.gameObject.layer;

                // if we hit water, we skip it and keep going inward
                if ((hitLayerMask & waterMask.value) != 0)
                {
                    SpawnAtHit(hit, -dir, waterSpawn); // face towards where the ray came from (outward)
                    return true;
                }

                // if we hit land, spawn and stop
                if ((hitLayerMask & landMask.value) != 0)
                {
                    SpawnAtHit(hit, -dir, landSpawn); // face towards where the ray came from (outward)
                    return true;
                }
            }

        }

        return false;
    }

    void SpawnAtHit(RaycastHit hit, Vector3 outwardDirection, GameObject prefab)
    {
        Vector3 pos = hit.point + hit.normal * surfaceOffset;

        Quaternion rot;
        if (alignUpToNormal)
        {
            // forward points outward toward ray origin, up aligns to surface normal
            rot = Quaternion.LookRotation(outwardDirection, hit.normal);
        }
        else
        {
            rot = Quaternion.LookRotation(outwardDirection);
        }

        spawned.Add(Instantiate(prefab, pos, rot, transform));
    }

    static Vector3 RandomPointOnSphere(Vector3 center, float r)
    {
        return center + Random.onUnitSphere * r;
    }
}
