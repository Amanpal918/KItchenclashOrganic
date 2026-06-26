using UnityEngine;
using UnityEngine.InputSystem;

public class FridgeController : MonoBehaviour
{
    [Header("Main Renderer")]
    [Tooltip("The main SpriteRenderer component on your Fridge GameObject")]
    public SpriteRenderer mainFridgeRenderer;

    [Header("Fridge Sprite Assets")]
    [Tooltip("Sprite for when everything is closed (complete removed)")]
    public Sprite spriteFullyClosed;

    [Tooltip("Sprite for when ONLY the upper door is open (fridge upper open)")]
    public Sprite spriteUpperOnlyOpen;

    [Tooltip("Sprite for when ONLY the lower door is open (fridge lower open)")]
    public Sprite spriteLowerOnlyOpen;

    [Tooltip("Sprite for when BOTH doors are open (both open)")]
    public Sprite spriteFullyOpen;

    [Header("Door Click Zones")]
    [Tooltip("BoxCollider2D covering upper door area")]
    public Collider2D upperDoorCollider;

    [Tooltip("BoxCollider2D covering lower door area")]
    public Collider2D lowerDoorCollider;

    // ── COMMENTED OUT UNTIL YOU ADD FRIDGE INTERIOR CONTENT ──
    /*
    [Header("Interior Contents")]
    [Tooltip("Parent of all upper shelf ingredient sources")]
    public GameObject upperContents;

    [Tooltip("Parent of all lower shelf ingredient sources")]
    public GameObject lowerContents;
    */

    private bool upperOpen = false;
    private bool lowerOpen = false;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;

        if (mainFridgeRenderer == null)
            mainFridgeRenderer = GetComponent<SpriteRenderer>();

        // Set the initial visual state
        ApplyFridgeVisuals();
    }

    void Update()
    {
        if (!InputPressed()) return;

        Vector2 worldPos = GetWorldPos();
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider == null) return;

        // ── UPPER DOOR CLICKED ──
        if (hit.collider == upperDoorCollider)
        {
            upperOpen = !upperOpen;
            ApplyFridgeVisuals();
            return;
        }

        // ── LOWER DOOR CLICKED ──
        if (hit.collider == lowerDoorCollider)
        {
            lowerOpen = !lowerOpen;
            ApplyFridgeVisuals();
            return;
        }
    }

    void ApplyFridgeVisuals()
    {
        if (mainFridgeRenderer == null) return;

        // 1. Swap out the texture asset based on the combined door states
        if (!upperOpen && !lowerOpen)
        {
            mainFridgeRenderer.sprite = spriteFullyClosed;
        }
        else if (upperOpen && !lowerOpen)
        {
            mainFridgeRenderer.sprite = spriteUpperOnlyOpen;
        }
        else if (!upperOpen && lowerOpen)
        {
            mainFridgeRenderer.sprite = spriteLowerOnlyOpen;
        }
        else if (upperOpen && lowerOpen)
        {
            mainFridgeRenderer.sprite = spriteFullyOpen;
        }

        // 2. Toggle the interactive shelf content groups (Commented Out)
        /*
        if (upperContents != null) upperContents.SetActive(upperOpen);
        if (lowerContents != null) lowerContents.SetActive(lowerOpen);
        */
    }

    // ─────────────────────────────────────────────
    // STABILIZED INPUT HELPERS (Matches your IngredientController layout)
    // ─────────────────────────────────────────────
    Vector2 GetWorldPos()
    {
        Vector2 screenPos = Vector2.zero;
        bool foundTouch = false;

        if (Touchscreen.current != null)
        {
            var activeTouches = Touchscreen.current.touches;
            for (int i = 0; i < activeTouches.Count; i++)
            {
                if (activeTouches[i].press.isPressed)
                {
                    screenPos = activeTouches[i].position.ReadValue();
                    foundTouch = true;
                    break;
                }
            }
        }

        if (!foundTouch && Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
        }

        if (screenPos == Vector2.zero) return transform.position;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        world.z = 0f;
        return new Vector2(world.x, world.y);
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
}