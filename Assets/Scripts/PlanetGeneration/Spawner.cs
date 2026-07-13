using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[System.Serializable]
public class WeightedLandPrefab
{
    public GameObject prefab;

    [Tooltip("Exactly how many of this prefab should exist on the planet. " +
             "No longer a random weight — this is the fixed count baked at edit time.")]
    [Min(0)] public int targetCount = 25;
}

[System.Serializable]
public struct BakedSpawnPoint
{
    public Vector3 localPosition;   // relative to `center`, so it still works if the planet moves/rotates as a whole
    public Vector3 localNormal;     // relative to `center`
    public int prefabIndex;         // index into landSpawns
    public bool isMonster;          // baked state, replaces the old runtime 50/50 roll
}

public class Spawner : MonoBehaviour
{
    public RoundManager roundManager;

    [Header("Raycast (used only when baking in the Editor)")]
    public Transform center;
    [Min(0.001f)] public float radius = 10f;
    public float startPadding = 0.5f;
    public float maxDistanceMultiplier = 2.5f;
    public int maxAttemptsPerPoint = 200;

    [Header("Layers")]
    public LayerMask waterMask;
    public LayerMask landMask;

    [Header("Land Prefabs (fixed counts)")]
    public WeightedLandPrefab[] landSpawns;

    [Header("Surface Settings")]
    public float surfaceOffset = 0.05f;

    [Header("Baked Data — do not hand-edit, use 'Bake Spawn Points'")]
    public BakedSpawnPoint[] bakedSpawns;

    public List<GameObject> spawned = new List<GameObject>();

    void Reset()
    {
        center = transform;
    }

    void Start()
    {
        SpawnFromBakedData();

        if (roundManager != null)
            roundManager.GetPlants(spawned);
    }

    /// <summary>
    /// Runtime entry point. Purely reads pre-baked data — no RNG, no
    /// Physics calls. Every device runs this exact same loop over the
    /// exact same serialized array, so the result is identical everywhere.
    /// </summary>
    public void SpawnFromBakedData()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();

        if (bakedSpawns == null || bakedSpawns.Length == 0)
        {
            Debug.LogWarning("Spawner: no baked spawn points. Right-click the component and choose 'Bake Spawn Points' in the Editor first.");
            return;
        }

        foreach (var bp in bakedSpawns)
        {
            if (bp.prefabIndex < 0 || bp.prefabIndex >= landSpawns.Length)
                continue;

            GameObject prefab = landSpawns[bp.prefabIndex].prefab;
            if (prefab == null)
                continue;

            Vector3 worldPos = center.TransformPoint(bp.localPosition);
            Vector3 worldNormal = center.TransformDirection(bp.localNormal).normalized;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, worldNormal);

            GameObject instance = Instantiate(prefab, worldPos, rot, transform);

            PlantHandler ph = instance.GetComponent<PlantHandler>();
            if (ph != null)
            {
                ph.currentState = bp.isMonster ? PlantHandler.PlantState.monster : PlantHandler.PlantState.dead;

                // spawned.Count here is this plant's index BEFORE it's added
                // below, so it doubles as a stable network ID. Both clients
                // walk the exact same bakedSpawns array in the exact same
                // order (no RNG at runtime), so this index lines up 1:1 with
                // the other client's copy of the same plant - that's what
                // plant_convert messages use to target the right plant.
                ph.plantId = spawned.Count;
            }

            spawned.Add(instance);
        }

        Debug.Log($"Spawned {spawned.Count}/{bakedSpawns.Length} baked plants.");
    }

    /// <summary>
    /// Deterministic-by-construction re-roll. Instead of a live 50/50 RNG
    /// call (which would diverge across clients), this just flips every
    /// plant to the opposite of its baked state. Same input state → same
    /// output state on every device, no randomness involved.
    /// </summary>
    public void ResetPlants()
    {
        for (int i = 0; i < spawned.Count && i < bakedSpawns.Length; i++)
        {
            GameObject plant = spawned[i];
            if (plant == null) continue;

            PlantHandler ph = plant.GetComponent<PlantHandler>();
            if (ph == null) continue;

            bool wasMonster = bakedSpawns[i].isMonster;
            ph.SetStateLocalOnly(wasMonster ? PlantHandler.PlantState.dead : PlantHandler.PlantState.monster);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR-ONLY. Runs the raycast search once, records exact results
    /// (local position/normal/prefab/state) into bakedSpawns, and marks
    /// the scene/prefab dirty so Unity saves that data. This is the ONLY
    /// place randomness or Physics.RaycastAll is used — it never runs in
    /// a build, so every player ships with the same fixed array.
    /// </summary>
    [ContextMenu("Bake Spawn Points")]
    void BakeSpawnPoints()
    {
        if (!center)
        {
            Debug.LogWarning("Spawner: missing center transform, can't bake.");
            return;
        }
        if (landSpawns == null || landSpawns.Length == 0)
        {
            Debug.LogWarning("Spawner: no land prefabs assigned, can't bake.");
            return;
        }

        // Fixed bake-time seed. Doesn't need to match anything at runtime —
        // it only controls the search order while baking, and the *result*
        // of the search is what gets saved and shipped.
        System.Random bakeRng = new System.Random(12345);

        var results = new List<BakedSpawnPoint>();
        int combinedMask = waterMask | landMask;
        float maxDist = radius * maxDistanceMultiplier;

        for (int prefabIndex = 0; prefabIndex < landSpawns.Length; prefabIndex++)
        {
            int needed = landSpawns[prefabIndex].targetCount;
            int found = 0;
            int attempts = 0;
            int maxAttempts = needed * maxAttemptsPerPoint;

            while (found < needed && attempts < maxAttempts)
            {
                attempts++;

                Vector3 origin = RandomPointOnSphere(center.position, radius + startPadding, bakeRng);
                Vector3 dir = (center.position - origin).normalized;

                RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDist, combinedMask, QueryTriggerInteraction.Ignore);
                if (hits == null || hits.Length == 0)
                    continue;

                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    int hitLayerMask = 1 << hit.collider.gameObject.layer;

                    if ((hitLayerMask & waterMask.value) != 0)
                        break; // water is closest hit, skip this ray entirely

                    if ((hitLayerMask & landMask.value) != 0)
                    {
                        Vector3 worldPos = hit.point + hit.collider.transform.up * surfaceOffset;
                        Vector3 worldNormal = -dir;

                        results.Add(new BakedSpawnPoint
                        {
                            localPosition = center.InverseTransformPoint(worldPos),
                            localNormal = center.InverseTransformDirection(worldNormal),
                            prefabIndex = prefabIndex,
                            isMonster = bakeRng.NextDouble() < 0.5
                        });

                        found++;
                        break;
                    }
                }
            }

            if (found < needed)
                Debug.LogWarning($"Spawner bake: only found {found}/{needed} spots for '{landSpawns[prefabIndex].prefab?.name}'.");
        }

        bakedSpawns = results.ToArray();

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log($"Spawner: baked {bakedSpawns.Length} spawn points. Save the scene/prefab to keep this data.");
    }

    static Vector3 RandomPointOnSphere(Vector3 center, float r, System.Random rng)
    {
        double z = rng.NextDouble() * 2.0 - 1.0;
        double t = rng.NextDouble() * (System.Math.PI * 2.0);
        double xy = System.Math.Sqrt(1.0 - z * z);

        float x = (float)(xy * System.Math.Cos(t));
        float y = (float)z;
        float z2 = (float)(xy * System.Math.Sin(t));

        return center + new Vector3(x, y, z2) * r;
    }
#endif
}