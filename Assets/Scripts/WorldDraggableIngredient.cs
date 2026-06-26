using UnityEngine;
using UnityEngine.InputSystem;

public class WorldDraggableIngredient : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // ASSIGN IN INSPECTOR
    // ─────────────────────────────────────────────
    public IngredientData ingredientData;

    // ─────────────────────────────────────────────
    // PRIVATE VARIABLES
    // ─────────────────────────────────────────────
    private bool isDragging = false;
    private Vector3 offset;
    private Vector3 originalPosition;
    private Transform originalParent;
    private SpriteRenderer spriteRenderer;
    private Collider2D myCollider;
    private int originalSortingOrder;

    // References to our floor script components
    private PlacedIngredient placedIngredientScript;

    // ─────────────────────────────────────────────
    // AWAKE
    // Saves starting state
    // ─────────────────────────────────────────────
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        placedIngredientScript = GetComponent<PlacedIngredient>();

        originalPosition = transform.localPosition;
        originalParent = transform.parent;

        if (spriteRenderer != null)
            originalSortingOrder = spriteRenderer.sortingOrder;
    }

    // ─────────────────────────────────────────────
    // UPDATE
    // Runs every frame - handles all drag input
    // ─────────────────────────────────────────────
    void Update()
    {
        // CRITICAL: If the other script is currently handling a drag, skip this one
        if (placedIngredientScript != null && placedIngredientScript.IsDragging)
            return;

        Vector2 inputPos = GetInputPosition();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(inputPos);
        worldPos.z = 0f;

        // PRESS - did we tap this ingredient?
        if (InputPressed())
        {
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(worldPos.x, worldPos.y),
                Vector2.zero
            );

            if (hit.collider == myCollider)
            {
                StartDrag(worldPos);
            }
        }

        // HOLD - move with finger/mouse
        if (isDragging && InputHeld())
        {
            transform.position = new Vector3(
                worldPos.x + offset.x,
                worldPos.y + offset.y,
                0f
            );
        }

        // RELEASE - drop it
        if (isDragging && InputReleased())
        {
            EndDrag();
        }
    }

    // ─────────────────────────────────────────────
    // START DRAG
    // ─────────────────────────────────────────────
    void StartDrag(Vector3 worldPos)
    {
        isDragging = true;
        CameraPan.IsHoldingAnyItem = true;

        offset = transform.position - worldPos;
        offset.z = 0f;

        // Tell the floor script to stop any active falling calculations while we drag it around
        if (placedIngredientScript != null)
        {
            placedIngredientScript.enabled = false;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 100;
            Color c = spriteRenderer.color;
            c.a = 0.7f;
            spriteRenderer.color = c;
        }
    }

    // ─────────────────────────────────────────────
    // END DRAG
    // ─────────────────────────────────────────────
    void EndDrag()
    {
        isDragging = false;
        CameraPan.IsHoldingAnyItem = false;

        // Reset look
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
            spriteRenderer.sortingOrder = originalSortingOrder;
        }

        // Check for blender interaction
        Collider2D[] hits = Physics2D.OverlapPointAll(
            new Vector2(transform.position.x, transform.position.y)
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == myCollider) continue;

            if (hit.TryGetComponent<BlenderController>(out var blender))
            {
                blender.AddIngredient(this.ingredientData);
                Destroy(gameObject);
                return; // Exit early if eaten by blender
            }
        }

        // If it wasn't dropped in the blender, pass total control back to the floor drop system!
        if (placedIngredientScript != null)
        {
            placedIngredientScript.enabled = true;
            placedIngredientScript.StopDrag(); // Triggers clean floor calculations and resets speeds
        }
    }

    public void ReturnToStorage()
    {
        transform.SetParent(originalParent);
        transform.localPosition = originalPosition;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    Vector2 GetInputPosition()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.isInProgress) return touch.position.ReadValue();
            }
        }
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    bool InputPressed()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began) return true;
            }
        }
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    bool InputHeld()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.isInProgress) return true;
            }
        }
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
    }

    bool InputReleased()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended) return true;
            }
        }
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }
}