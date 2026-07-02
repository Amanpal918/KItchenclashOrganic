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
        Debug.Log($"🎯 [Trash Check] Checking position {worldPos}. Overlap detected? {isOver}");
        return isOver;
    }
    /// <summary>
    /// Pulls a recycled item from the pool, or instantiates a new one if empty.
    /// </summary>
    public GameObject GetPooledIngredient(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        string poolKey = prefab.name.Replace("(Clone)", "");

        // Initialize pool list if it doesn't exist yet
        if (!poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary[poolKey] = new List<GameObject>();
        }

        // Search for an inactive object in the pool
        foreach (GameObject obj in poolDictionary[poolKey])
        {
            if (obj != null && !obj.activeInHierarchy)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
                return obj;
            }
        }

        // If no inactive object was found, create a new one and cache it
        GameObject newObj = Instantiate(prefab, position, rotation);
        newObj.name = poolKey; // Keep naming clean for keys
        poolDictionary[poolKey].Add(newObj);
        return newObj;
    }

    /// <summary>
    /// Recycles an item back into the pool by deactivating it.
    /// </summary>
    public void RecycleToPool(GameObject obj)
    {
        // 🔍 DEBUG PRINT: Find out exactly what name key the item is trying to use to register
        string poolKey = obj.name.Replace("(Clone)", "");
        Debug.Log($"🗑️ [Recycle Request] Attempting to trash gameobject named: '{obj.name}' using Key: '{poolKey}'");

        var milkPourer = obj.GetComponentInChildren<MilkPourer>();
        var waterPourer = obj.GetComponentInChildren<WaterPourer>();
        if (milkPourer != null) milkPourer.ResetPourState();
        if (waterPourer != null) waterPourer.ResetPourState();

        obj.transform.SetParent(null);
        obj.SetActive(false);

        Debug.Log($"✅ [Recycle Success] '{obj.name}' successfully deactivated and stored in pool under key '{poolKey}'");
    }
}