using UnityEngine;
using DG.Tweening;

public class FlyToHomeItem : MonoBehaviour
{
    [Header("📐 Target Landing Configuration")]
    [SerializeField] private Vector3 permanentHomePosition; // For Table Items (Toaster, Bread)

    [Header("🗂️ Optional Fridge Nesting Setup")]
    [SerializeField] private Transform fridgeShelfTargetParent; // ONLY drag "Shelf_top" here for the Butter!
    [SerializeField] private Vector3 localOffsetNextToCheese = new Vector3(0.5f, 0f, 0f); // Local position inside fridge

    private bool hasFlownHome = false;
    private Vector3 savedScale;

    public void PrepareForTableDisplay()
    {
        savedScale = transform.localScale;

        // Break free from nested hierarchy temporarily for clean visual path math
        transform.SetParent(null);
        transform.localScale = savedScale;

        SpriteRenderer sRenderer = GetComponent<SpriteRenderer>();
        if (sRenderer != null)
        {
            sRenderer.sortingLayerName = "Ingredients";
            sRenderer.sortingOrder = 15; // Ensure it stays visible on top of the table foreground
        }
    }

    void OnMouseDown()
    {
        if (!hasFlownHome)
        {
            hasFlownHome = true;
            DetermineAndExecuteFlightPath();
        }
    }

    private void DetermineAndExecuteFlightPath()
    {
        // ------------------------------------------------------------
        // 🧈 TYPE A: FRIDGE ITEMS (If fridgeShelfTargetParent is assigned)
        // ------------------------------------------------------------
        if (fridgeShelfTargetParent != null)
        {
            Debug.Log("🚀 [Direct Nesting] Flying " + gameObject.name + " straight inside the fridge shelf container.");

            // Instantly parent to the fridge so it belongs to the hierarchy right away
            transform.SetParent(fridgeShelfTargetParent);
            gameObject.SetActive(true);

            // Parabolic jump directly to its local spot next to the cheese
            transform.DOLocalJump(localOffsetNextToCheese, 2.5f, 1, 1.2f)
                .SetEase(Ease.OutCubic)
                .OnComplete(FinishFlightSortingState);
        }
        // ------------------------------------------------------------
        // 🍞 TYPE B: TABLE ITEMS (Standard World Space Flight)
        // ------------------------------------------------------------
        else
        {
            Debug.Log("🚀 [World Flight] Flying " + gameObject.name + " directly to perfect Table World Pos: " + permanentHomePosition);

            // Parabolic jump directly to the exact world coordinates you assigned
            transform.DOJump(permanentHomePosition, 2.5f, 1, 1.2f)
                .SetEase(Ease.OutCubic)
                .OnComplete(FinishFlightSortingState);
        }
    }

    private void FinishFlightSortingState()
    {
        transform.localScale = savedScale;

        // Reset sorting layers to normal gameplay workspace depth
        SpriteRenderer sRenderer = GetComponent<SpriteRenderer>();
        if (sRenderer != null)
        {
            sRenderer.sortingLayerName = "Ingredients";
            sRenderer.sortingOrder = 5;
        }

        Debug.Log("✅ " + gameObject.name + " arrived safely at its final destination!");
    }
}