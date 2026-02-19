using UnityEngine;
using System.Collections.Generic;

public class BlockDetector : MonoBehaviour
{
    [Header("Conversion Settings")]
    public float convertTime = 3f;

    [Header("Tags (5 Block Types)")]
    public string[] blockTags = new string[5];

    [Header("Replacement Prefabs (Same Order As Tags)")]
    public GameObject[] replacementPrefabs = new GameObject[5];

    // Tracks blocks currently being touched + timer
    private Dictionary<GameObject, float> blockTimers = new Dictionary<GameObject, float>();

    private void OnTriggerEnter(Collider other)
    {
        if (IsValidBlock(other.gameObject))
        {
            if (!blockTimers.ContainsKey(other.gameObject))
            {
                blockTimers.Add(other.gameObject, 0f);
                Debug.Log($"Started touching block: {other.gameObject.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (blockTimers.ContainsKey(other.gameObject))
        {
            blockTimers.Remove(other.gameObject);
            Debug.Log($"Stopped touching block: {other.gameObject.name}");
        }
    }

    private void Update()
    {
        if (blockTimers.Count == 0)
            return;

        // We copy keys because we can't modify dictionary while looping
        List<GameObject> keys = new List<GameObject>(blockTimers.Keys);

        foreach (GameObject block in keys)
        {
            if (block == null)
            {
                blockTimers.Remove(block);
                continue;
            }

            blockTimers[block] += Time.deltaTime;

            if (blockTimers[block] >= convertTime)
            {
                ConvertBlock(block);
                blockTimers.Remove(block);
            }
        }
    }

    bool IsValidBlock(GameObject obj)
    {
        foreach (string tag in blockTags)
        {
            if (!string.IsNullOrEmpty(tag) && obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    void ConvertBlock(GameObject block)
    {
        Debug.Log($"Converting block: {block.name}");
        // Find which tag it currently has
        for (int i = 0; i < blockTags.Length; i++)
        {
            if (!string.IsNullOrEmpty(blockTags[i]) && block.CompareTag(blockTags[i]))
            {
                if (replacementPrefabs[i] == null)
                {
                    Debug.LogWarning($"No replacement prefab assigned for tag: {blockTags[i]}");
                    return;
                }

                Vector3 pos = block.transform.position;
                Quaternion rot = block.transform.rotation;
                Transform parent = block.transform.parent;

                Destroy(block);

                GameObject newBlock = Instantiate(replacementPrefabs[i], pos, rot, parent);

                return;
            }
        }
    }
}
