using UnityEngine;

public class LayeredBasketItem : MonoBehaviour
{
    [Header("Prefab Reference to Spawn")]
    [SerializeField] private GameObject ingredientPrefab;

    private Collider2D myCollider;
    private SpriteRenderer sr;
 

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Called when the player clicks directly on this fruit's sorting layer shape
    /// </summary>
    public void TryExtractFruit(Vector3 clickWorldPos)
    {
        // 🌟 FIX 1: Removed the 'if (isRemoved) return;' check so it can run infinitely!

        // 🌟 FIX 2: Removed 'sr.enabled = false;' so the visual fruit stays in the basket 
        // to act as a permanent dispenser button, just like the fridge!

        // Spawn the dynamic, draggable ingredient clone directly under the cursor
        if (ingredientPrefab != null)
        {
            GameObject cloneObj = Instantiate(ingredientPrefab, new Vector3(clickWorldPos.x, clickWorldPos.y, 0f), Quaternion.identity);

            IngredientController master = cloneObj.GetComponent<IngredientController>();
            if (master == null)
            {
                master = cloneObj.AddComponent<IngredientController>();
            }

            // Immediately hand off tracking control to the cursor drag routine
            master.StartDragFromSource(clickWorldPos);
        }
    }
}