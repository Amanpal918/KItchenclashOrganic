using UnityEngine;
using UnityEngine.InputSystem;

public class FridgeController : MonoBehaviour
{
    [Header("🖼️ Sprite Render Allocation")]
    [SerializeField] private SpriteRenderer mainFridgeRenderer;
    public Sprite spriteFullyClosed;
    public Sprite spriteUpperOnlyOpen;
    public Sprite spriteLowerOnlyOpen;
    public Sprite spriteFullyOpen;

    [Header("🎯 Dual-State Clickable Targets")]
    [Tooltip("Trigger to open the Upper Door (Active when closed)")]
    public Collider2D upperClosedTrigger;
    [Tooltip("Trigger to close the Upper Door (Active when open)")]
    public Collider2D upperOpenTrigger;

    [Tooltip("Trigger to open the Lower Door (Active when closed)")]
    public Collider2D lowerClosedTrigger;
    [Tooltip("Trigger to close the Lower Door (Active when open)")]
    public Collider2D lowerOpenTrigger;

    [Header("📁 Separated Hierarchy Containers")]
    [SerializeField] private GameObject upperDoorContainer;
    [SerializeField] private GameObject lowerDoorMainShelvesContainer;

    [Header("🚪 Separated Door Wing Pockets")]
    [Tooltip("Assign Upper_Door_Shelves container here")]
    [SerializeField] private GameObject upperDoorShelves;

    [Tooltip("Assign Lower_Door_Shelves container here")]
    [SerializeField] private GameObject lowerDoorShelves;

    private bool isUpperOpen = false;
    private bool isLowerOpen = false;
    private Camera cam;
    private FridgeShelfZone[] allShelves;

    void Awake()
    {
        cam = Camera.main;
        if (mainFridgeRenderer == null) mainFridgeRenderer = GetComponent<SpriteRenderer>();

        // Cache all child shelf zones dynamically to update internal item visibilities
        allShelves = GetComponentsInChildren<FridgeShelfZone>(true);

        ApplyFridgeState();
    }

    void Update()
    {
        if (!InputPressed()) return;

        // CRITICAL GUARD: Prioritize ingredient interaction and abort door updates entirely if holding an item
        if (CameraPan.IsHoldingAnyItem) return;

        Vector2 worldPos = GetWorldPos();

        // 🔍 DEBUG STEP 1: Let's see every single collider under our cursor position!
        RaycastHit2D[] hits = Physics2D.RaycastAll(worldPos, Vector2.zero);

        Debug.Log($"[Fridge Update] Click detected at {worldPos}. Total objects hit by raycast: {hits.Length}");

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCol = hits[i].collider;
            if (hitCol != null)
            {
                Debug.Log($" -> Hit [{i}]: GameObject name = '{hitCol.gameObject.name}', Layer = '{LayerMask.LayerToName(hitCol.gameObject.layer)}', IsTrigger = {hitCol.isTrigger}");
            }
        }

        // Secondary layer verification: Prioritize ingredient raycasts so door clicks don't steal focus
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider.GetComponent<IngredientController>() != null)
            {
                Debug.Log($"🔍 [Kiwi/Ingredient Priority Block]: Raycast caught an IngredientController on '{hit.collider.gameObject.name}'. Aborting door click checks so item can be grabbed!");
                return;
            }
        }

        // Process door click detection depending on active state colliders
        foreach (RaycastHit2D hit in hits)
        {
            // --- UPPER DOOR LOGIC ---
            if (!isUpperOpen && hit.collider == upperClosedTrigger)
            {
                Debug.Log("🚪 [Fridge Action] Clicked UPPER CLOSED trigger. Opening top half.");
                isUpperOpen = true;
                ApplyFridgeState();
                break;
            }
            if (isUpperOpen && hit.collider == upperOpenTrigger)
            {
                Debug.Log("🚪 [Fridge Action] Clicked UPPER OPEN trigger. Closing top half.");
                isUpperOpen = false;
                ApplyFridgeState();
                break;
            }

            // --- LOWER DOOR LOGIC ---
            if (!isLowerOpen && hit.collider == lowerClosedTrigger)
            {
                Debug.Log("🚪 [Fridge Action] Clicked LOWER CLOSED trigger. Opening bottom half.");
                isLowerOpen = true;
                ApplyFridgeState();
                break;
            }
            if (isLowerOpen && hit.collider == lowerOpenTrigger)
            {
                Debug.Log("🚪 [Fridge Action] Clicked LOWER OPEN trigger. Closing bottom half.");
                isLowerOpen = false;
                ApplyFridgeState();
                break;
            }
        }
    }

    void ApplyFridgeState()
    {
        if (mainFridgeRenderer == null) return;

        // 1. Update visual sprite state variations
        if (!isUpperOpen && !isLowerOpen) mainFridgeRenderer.sprite = spriteFullyClosed;
        else if (isUpperOpen && !isLowerOpen) mainFridgeRenderer.sprite = spriteUpperOnlyOpen;
        else if (!isUpperOpen && isLowerOpen) mainFridgeRenderer.sprite = spriteLowerOnlyOpen;
        else if (isUpperOpen && isLowerOpen) mainFridgeRenderer.sprite = spriteFullyOpen;

        // 2. Toggle main cabinet compartments visibility
        if (upperDoorContainer != null) upperDoorContainer.SetActive(isUpperOpen);
        if (lowerDoorMainShelvesContainer != null) lowerDoorMainShelvesContainer.SetActive(isLowerOpen);

        // 3. Toggle the exact door pockets matching only the open halves
        if (upperDoorShelves != null) upperDoorShelves.SetActive(isUpperOpen);
        if (lowerDoorShelves != null) lowerDoorShelves.SetActive(isLowerOpen);

        // 4. Update the inner item visibility loops via cached shelf references
        UpdateShelfItemsVisibility();

        // 5. Swap colliders cleanly so you can only trigger them in the proper context
        if (upperClosedTrigger != null) upperClosedTrigger.gameObject.SetActive(!isUpperOpen);
        if (upperOpenTrigger != null) upperOpenTrigger.gameObject.SetActive(isUpperOpen);

        if (lowerClosedTrigger != null) lowerClosedTrigger.gameObject.SetActive(!isLowerOpen);
        if (lowerOpenTrigger != null) lowerOpenTrigger.gameObject.SetActive(isLowerOpen);
    }

    private void UpdateShelfItemsVisibility()
    {
        if (allShelves == null) return;

        foreach (var shelf in allShelves)
        {
            if (shelf == null) continue;

            // Determine if this specific shelf belongs to the upper or lower segment of the fridge
            bool isUpperShelf = shelf.transform.IsChildOf(upperDoorContainer.transform) ||
                                (upperDoorShelves != null && shelf.transform.IsChildOf(upperDoorShelves.transform));

            bool isLowerShelf = shelf.transform.IsChildOf(lowerDoorMainShelvesContainer.transform) ||
                                (lowerDoorShelves != null && shelf.transform.IsChildOf(lowerDoorShelves.transform));

            if (isUpperShelf)
            {
                shelf.SetShelfItemsVisibility(isUpperOpen);
            }
            else if (isLowerShelf)
            {
                shelf.SetShelfItemsVisibility(isLowerOpen);
            }
        }
    }

    Vector2 GetWorldPos()
    {
        Vector2 screenPos = Vector2.zero;
        if (Pointer.current != null) screenPos = Pointer.current.position.ReadValue();
        if (screenPos == Vector2.zero) return transform.position;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        world.z = 0f;
        return world;
    }

    bool InputPressed()
    {
        return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
    }
}