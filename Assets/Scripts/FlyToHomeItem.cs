using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class FlyToHomeItem : MonoBehaviour
{
    [Header("📐 Target Landing Configuration")]
    [SerializeField] private Vector3 permanentHomePosition; // For Table Items (Toaster, Bread)

    [Header("🗂️ Fridge Nesting Setup")]
    [SerializeField] private Transform fridgeShelfTargetParent; // Drag "Shelf_top" here for the Butter
    [SerializeField] private Vector3 targetLocalPosition = new Vector3(-0.2f, 0f, 0f); // Assign the exact destination coordinates here!

    private bool hasFlownHome = false;
    private Vector3 savedScale;

    // Core components needed for click detection
    private Collider2D myCollider;
    private Camera mainCamera;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
        savedScale = transform.localScale;
    }

    void Update()
    {
        // ── NEW INPUT SYSTEM CLICK DETECTOR ──
        // Only listen for clicks if the script is enabled and it hasn't flown home yet
        if (!hasFlownHome && InputPressed())
        {
            Vector3 worldPos = GetMouseWorldPosition();
            if (myCollider != null && myCollider.OverlapPoint(new Vector2(worldPos.x, worldPos.y)))
            {
                hasFlownHome = true;
                DetermineAndExecuteFlightPath();
            }
        }
    }

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

    private void DetermineAndExecuteFlightPath()
    {
        // ------------------------------------------------------------
        // 🧈 TYPE A: FRIDGE ITEMS (If fridgeShelfTargetParent is assigned)
        // ------------------------------------------------------------
        if (fridgeShelfTargetParent != null)
        {
            Debug.Log($"🚀 [Fridge Nesting] Flying {gameObject.name} straight inside shelf. Target Local Pos: {targetLocalPosition}");

            // Instantly parent to the fridge so it belongs to the hierarchy right away
            transform.SetParent(fridgeShelfTargetParent);
            gameObject.SetActive(true);

            // Parabolic jump directly to the exact target local position next to the cheese
            transform.DOLocalJump(targetLocalPosition, 2.5f, 1, 1.2f)
                .SetEase(Ease.OutCubic)
                .OnComplete(FinishFlightSortingState);
        }
        // ------------------------------------------------------------
        // 🍞 TYPE B: TABLE ITEMS (Standard World Space Flight)
        // ------------------------------------------------------------
        else
        {
            Debug.Log($"🚀 [World Flight] Flying {gameObject.name} directly to perfect Table World Pos: {permanentHomePosition}");

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

        Debug.Log($"✅ {gameObject.name} arrived safely at final position.");

        // 🌟 ARCHITECTURE BRIDGE: Notify HootCutsceneManager to turn this item into its permanent workstate!
        HootCutsceneManager cutsceneManager = FindFirstObjectByType<HootCutsceneManager>();
        if (cutsceneManager != null)
        {
            cutsceneManager.OnItemArrivedAtPermanentHome(gameObject);
        }
    }

    #region ── INPUT VECTOR UTILITIES ──

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 pointerPos = Vector2.zero;
        if (Pointer.current != null) pointerPos = Pointer.current.position.ReadValue();
        if (pointerPos == Vector2.zero) return transform.position;

        Vector3 worldCoordinate = mainCamera.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, 0f));
        worldCoordinate.z = 0f;
        return worldCoordinate;
    }

    private bool InputPressed() => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;

    #endregion
}