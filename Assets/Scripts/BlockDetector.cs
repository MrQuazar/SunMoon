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

    // Stores progress time for each block
    private Dictionary<GameObject, float> blockTimers = new Dictionary<GameObject, float>();

    // Tracks which blocks are currently being touched
    private HashSet<GameObject> currentlyTouching = new HashSet<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (!IsValidBlock(other.gameObject))
            return;

        GameObject block = other.gameObject;

        currentlyTouching.Add(block);

        if (!blockTimers.ContainsKey(block))
        {
            blockTimers.Add(block, 0f);
            Debug.Log($"Started tracking block: {block.name}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject block = other.gameObject;

        if (currentlyTouching.Contains(block))
        {
            currentlyTouching.Remove(block);
            Debug.Log($"Stopped touching block: {block.name}");
        }
    }

    private void Update()
    {
        if (blockTimers.Count == 0)
            return;

        List<GameObject> keys = new List<GameObject>(blockTimers.Keys);

        foreach (GameObject block in keys)
        {
            if (block == null)
            {
                blockTimers.Remove(block);
                currentlyTouching.Remove(block);
                continue;
            }

            // If touching, increase timer
            if (currentlyTouching.Contains(block))
            {
                blockTimers[block] += Time.deltaTime;

                if (blockTimers[block] >= convertTime)
                {
                    ConvertBlock(block);
                    blockTimers.Remove(block);
                    currentlyTouching.Remove(block);
                }
            }
            // If not touching, decrease timer (fade progress)
            else
            {
                blockTimers[block] -= Time.deltaTime;

                if (blockTimers[block] <= 0f)
                {
                    blockTimers.Remove(block);
                }
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

                Instantiate(replacementPrefabs[i], pos, rot, parent);
                return;
            }
        }
    }
}
