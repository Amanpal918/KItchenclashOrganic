using UnityEngine;
using UnityEngine.InputSystem;

public class IngredientSource : MonoBehaviour
{
    public GameObject ingredientPrefab;

    private Collider2D myCollider;
    private GameObject activeClone;
    private bool isDraggingClone = false;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        // ── PRESS ──
        if (InputPressed() && !isDraggingClone)
        {
            Vector2 inputPos = GetInputPosition();
            Vector3 worldPos = GetWorldPos(inputPos);

            // FIX: Only detect a click on the actual background source asset, ignoring existing items
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(worldPos.x, worldPos.y),
                Vector2.zero
            );

            // Make sure we hit the source container collider, AND we are NOT clicking an already spawned item
            if (hit.collider == myCollider)
            {
                SpawnCloneAtFinger(worldPos);
            }
        }

        // ── HOLD ──
        if (isDraggingClone && activeClone != null)
        {
            Vector3 worldPos = Vector3.zero;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                worldPos = GetWorldPos(Mouse.current.position.ReadValue());
            }
            else if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.isInProgress)
                    {
                        worldPos = GetWorldPos(touch.position.ReadValue());
                        break;
                    }
                }
            }

            if (worldPos != Vector3.zero)
            {
                activeClone.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
            }
        }

        // ── RELEASE ──
        if (isDraggingClone && InputReleased())
        {
            DropCloneHere();
        }
    }

    void SpawnCloneAtFinger(Vector3 fingerPos)
    {
        isDraggingClone = true;

        activeClone = Instantiate(
            ingredientPrefab,
            new Vector3(fingerPos.x, fingerPos.y, 0f),
            Quaternion.identity
        );

        // Remove conflicting old legacy scripts if they exist on the prefab
        var oldDrag = activeClone.GetComponent<WorldDraggableIngredient>();
        if (oldDrag != null) Destroy(oldDrag);

        var oldPlaced = activeClone.GetComponent<PlacedIngredient>();
        if (oldPlaced != null) Destroy(oldPlaced);

        // Access our clean master controller script
        IngredientController master = activeClone.GetComponent<IngredientController>();
        if (master == null)
        {
            master = activeClone.AddComponent<IngredientController>();
        }

        // Turn transparency on manually while the source is dragging it initial setup
        SpriteRenderer sr = activeClone.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
            Color c = sr.color;
            c.a = 0.7f;
            sr.color = c;
        }
    }

    void DropCloneHere()
    {
        isDraggingClone = false;

        if (activeClone != null)
        {
            IngredientController master = activeClone.GetComponent<IngredientController>();
            if (master == null)
            {
                master = activeClone.AddComponent<IngredientController>();
            }

            // Let go and run clean drop calculations
            master.StopDrag();
        }

        activeClone = null;
    }
    Vector3 GetWorldPos(Vector2 screenPos)
    {
        if (screenPos == Vector2.zero) return transform.position;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        worldPos.z = 0f;
        return worldPos;
    }

    Vector2 GetInputPosition()
    {
        if (Touchscreen.current != null)
        {
            var activeTouches = Touchscreen.current.touches;
            for (int i = 0; i < activeTouches.Count; i++)
            {
                if (activeTouches[i].press.isPressed)
                    return activeTouches[i].position.ReadValue();
            }
        }
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
    }

    bool InputPressed()
    {
        if (Touchscreen.current != null)
        {
            var activeTouches = Touchscreen.current.touches;
            for (int i = 0; i < activeTouches.Count; i++)
            {
                if (activeTouches[i].press.wasPressedThisFrame) return true;
            }       
        }
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    bool InputReleased()
    {
        if (Touchscreen.current != null)
        {
            var activeTouches = Touchscreen.current.touches;
            for (int i = 0; i < activeTouches.Count; i++)
            {
                if (activeTouches[i].press.wasReleasedThisFrame) return true;
            }
        }
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
    }
}