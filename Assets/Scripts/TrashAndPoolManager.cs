using System.Collections.Generic;
using UnityEngine;

public class TrashAndPoolManager : MonoBehaviour
{
    public static TrashAndPoolManager Instance { get; private set; }

    [Header("🗑️ Trash Can Setup")]
    [SerializeField] private Collider2D trashCanCollider;

    // Dictionary tracking lists of deactivated objects for each ingredient type
    private Dictionary<string, List<GameObject>> poolDictionary = new Dictionary<string, List<GameObject>>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Checks if a position is inside the trash can boundary.
    /// </summary>
    public bool IsOverTrashCan(Vector2 worldPos)
    {
        if (trashCanCollider == null)
        {
            Debug.LogError("❌ [Pool Manager] Trash Can Collider is MISSING in the Inspector slot!");
            return false;
        }

        bool isOver = trashCanCollider.OverlapPoint(worldPos);
        return isOver;
    }

    /// <summary>
    /// Pulls a recycled item from the pool, or instantiates a new one if empty.
    /// </summary>
    public GameObject GetPooledIngredient(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        string poolKey = prefab.name.Replace("(Clone)", "").Trim();

        if (!poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary[poolKey] = new List<GameObject>();
        }

        // Search for an inactive object in the pool
        for (int i = 0; i < poolDictionary[poolKey].Count; i++)
        {
            GameObject obj = poolDictionary[poolKey][i];
            if (obj != null && !obj.activeInHierarchy)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.name = poolKey;
        poolDictionary[poolKey].Add(newObj);
        return newObj;
    }

    /// <summary>
    /// Updated: Handles item trashing. Instantly DESTROYS clone objects to clean scene structure.
    /// </summary>
    public void RecycleToPool(GameObject obj)
    {
        if (obj == null) return;

        // 🌟 CHECK IF IT IS A CLONE OR TARGET OBJECT
        // If the item name contains "clone" or if you want ALL items put in the trash can to die instantly:
        Debug.Log($"🗑️ [Trash Collector] Actively destroying trashed object: '{obj.name}' from memory.");

        // Clean up tracking dictionary references before killing the object to prevent null exceptions
        string poolKey = obj.name.Replace("(Clone)", "").Trim();
        if (poolDictionary.ContainsKey(poolKey) && poolDictionary[poolKey].Contains(obj))
        {
            poolDictionary[poolKey].Remove(obj);
        }

        // 🚀 CRITICAL CHANGE: Completely vaporize the object from the scene framework!
        Destroy(obj);
    }
}