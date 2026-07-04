using UnityEngine;
using UnityEngine.InputSystem;

public class FruitBasketManager : MonoBehaviour
{
    private Collider2D basketCollider;

    void Awake()
    {
        basketCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // Detect click input frames
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            worldPos.z = 0f;

            // Check if player clicked inside the basket area
            if (basketCollider != null && basketCollider.OverlapPoint(worldPos))
            {
                ProcessTopFruitClick(worldPos);
            }
        }
    }

    private void ProcessTopFruitClick(Vector3 clickPos)
    {
        // Find all colliders under the cursor click point
        Collider2D[] hits = Physics2D.OverlapPointAll(clickPos);

        LayeredBasketItem topFruit = null;
        int highestSortingOrder = -999;

        // Loop through the hits to find the fruit with the highest visual sorting layer order
        foreach (Collider2D hit in hits)
        {
            LayeredBasketItem fruitItem = hit.GetComponent<LayeredBasketItem>();
            if (fruitItem != null)
            {
                SpriteRenderer fruitSr = hit.GetComponent<SpriteRenderer>();
                if (fruitSr != null && fruitSr.enabled)
                {
                    if (fruitSr.sortingOrder > highestSortingOrder)
                    {
                        highestSortingOrder = fruitSr.sortingOrder;
                        topFruit = fruitItem;
                    }
                }
            }
        }

        // Extract the top fruit if one was found
        if (topFruit != null)
        {
            topFruit.TryExtractFruit(clickPos);
        }
    }
}