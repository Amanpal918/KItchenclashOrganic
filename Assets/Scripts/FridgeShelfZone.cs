using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FridgeShelfZone : MonoBehaviour
{
    [Header("📐 Grid Configuration")]
    [SerializeField] private float horizontalSpacing = 0.4f;
    [SerializeField] private float insideDepthScale = 0.75f;
    [SerializeField] private bool isDoorShelf = false;

    [Tooltip("Fine-tune this to raise or lower items perfectly into your visual shelf art.")]
    [SerializeField] private float verticalOffset = -0.15f;

    private List<IngredientController> slottedItems = new List<IngredientController>();
    private BoxCollider2D shelfCollider;

    public float GetRequiredScale() => isDoorShelf ? 1.0f : insideDepthScale;
    public bool IsDoorShelf => isDoorShelf;

    void Awake()
    {
        shelfCollider = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        Invoke(nameof(InitializePrePlacedItems), 0.05f);
    }

    private void InitializePrePlacedItems()
    {
        slottedItems.Clear();
        IngredientController[] childIngredients = GetComponentsInChildren<IngredientController>(true);

        foreach (var ingredient in childIngredients)
        {
            if (!slottedItems.Contains(ingredient))
            {
                slottedItems.Add(ingredient);
            }

            float targetScale = GetRequiredScale();
            ingredient.transform.localScale = new Vector3(targetScale, targetScale, 1f);

            var isSlottedField = ingredient.GetType().GetField("isSlottedInFridge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isSlottedField != null) isSlottedField.SetValue(ingredient, true);

            var shelfField = ingredient.GetType().GetField("overlappingFridgeShelf", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (shelfField != null) shelfField.SetValue(ingredient, this);
        }

        if (slottedItems.Count > 0)
        {
            AddAndRearrangeShelf(null);
        }
    }

    public Vector3 AddAndRearrangeShelf(IngredientController newItem)
    {
        if (newItem != null)
        {
            newItem.transform.SetParent(this.transform);
            if (!slottedItems.Contains(newItem))
            {
                slottedItems.Add(newItem);
            }
        }

        slottedItems.RemoveAll(item => item == null);
        slottedItems.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        Vector3 shelfCenterLocal = shelfCollider != null ? (Vector3)shelfCollider.offset : Vector3.zero;

        float totalWidth = (slottedItems.Count - 1) * horizontalSpacing;
        float leftmostX = shelfCenterLocal.x - (totalWidth / 2f);

        Vector3 snapDestinationWorld = transform.position;

        for (int i = 0; i < slottedItems.Count; i++)
        {
            float targetX = leftmostX + (i * horizontalSpacing);
            Vector3 localTargetPos = new Vector3(targetX, shelfCenterLocal.y + verticalOffset, 0f);

            if (slottedItems[i] == newItem)
            {
                snapDestinationWorld = transform.TransformPoint(localTargetPos);
            }

            if (slottedItems[i] != null)
            {
                slottedItems[i].transform.localPosition = localTargetPos;
                slottedItems[i].transform.localRotation = Quaternion.identity;

                float targetScale = GetRequiredScale();
                slottedItems[i].transform.localScale = new Vector3(targetScale, targetScale, 1f);
            }
        }

        return snapDestinationWorld;
    }

    public void DeregisterLeavingItem(IngredientController item)
    {
        if (slottedItems.Contains(item))
        {
            slottedItems.Remove(item);
            if (slottedItems.Count > 0) AddAndRearrangeShelf(null);
        }
    }

    /// <summary>
    /// 🌟 FIXES ERROR CS1061: Dynamically handles turning off renderers and interaction colliders
    /// for items inside this zone when the fridge door is toggled shut.
    /// </summary>
    public void SetShelfItemsVisibility(bool visible)
    {
        IngredientController[] items = GetComponentsInChildren<IngredientController>(true);
        foreach (var item in items)
        {
            if (item != null)
            {
                var sr = item.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = visible;

                var col = item.GetComponent<Collider2D>();
                if (col != null) col.enabled = visible;
            }
        }
    }
}