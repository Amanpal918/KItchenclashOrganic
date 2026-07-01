using UnityEngine;
using UnityEngine.InputSystem;

public class IngredientController : MonoBehaviour
{
    [Header("Ingredient Data Reference")]
    public IngredientData ingredientData;

    [Header("2.5D Floor Settings")]
    [SerializeField] private float floorTopY = -1.64f;
    [SerializeField] private float floorBottomY = -8.69f;

    [Header("Perspective Scale Settings")]
    [SerializeField] private float minScaleMultiplier = 0.4f; // Size at the very back (wall)
    [SerializeField] private float maxScaleMultiplier = 1.0f; // Size at the very front (screen edge)
    private Vector3 baseLocalScale;

    [Header("Juicy Drop Settings")]
    [SerializeField] private float gravity = 25f;
    [SerializeField] private float baseBounceForce = 5f;
    [SerializeField] private float bounciness = 0.5f;

    private bool isDragging = false;
    private bool justStartedDrag = false;
    private bool isFallingToFloor = false;
    private float targetFloorY;
    private int bounceCount = 0;
    private const int MAX_BOUNCES = 2;

    private Vector2 customVelocity;
    private Vector3 dragOffset;
    private Collider2D myCollider;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    public bool IsDragging => isDragging;
    private FridgeShelfZone overlappingFridgeShelf;
    private bool isSlottedInFridge = false;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Record the starting scale set in the Unity Editor
        baseLocalScale = transform.localScale;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void Update()
    {
        if (justStartedDrag)
        {
            justStartedDrag = false;
            return;
        }

        Vector3 worldPos = GetWorldPos();

        // ── 1. PRESS TRIGGER ──
        if (InputPressed() && !isDragging)
        {
            // Create a mask targeting only the Ingredients layer (Layer 6, 7, etc.)
            int ingredientLayerMask = LayerMask.GetMask("Ingredients");

            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(worldPos.x, worldPos.y), Vector2.zero, Mathf.Infinity, ingredientLayerMask);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == myCollider)
                {
                    StartDrag(worldPos);
                    break;
                }
            }
        }

        // ── 2. HOLD TRACKING ──
        if (isDragging)
        {
            transform.position = new Vector3(worldPos.x + dragOffset.x, worldPos.y + dragOffset.y, 0f);

            var milkPourer = GetComponentInChildren<MilkPourer>();
            if (milkPourer != null)
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(transform.position.x, transform.position.y));
                Collider2D containerCollider = null;

                foreach (Collider2D hit in hits)
                {
                    if (hit != myCollider && (hit.GetComponent<BlenderController>() != null || hit.gameObject.name.ToLower().Contains("glass")))
                    {
                        containerCollider = hit;
                        break;
                    }
                }

                // CRITICAL CHANGE: If we are near a container, update interaction. If we moved away, reset it!
                if (containerCollider != null)
                {
                    milkPourer.UpdatePourInteraction(containerCollider);
                }
                else
                {
                    milkPourer.ResetPourState();
                }
            }

            CheckFridgeShelfHover();
            UpdateSortingOrder();
        }

        // ── 3. RELEASE TRIGGER ──
        if (isDragging && InputReleased())
        {
            StopDrag();
        }
    }

    // 🌟 EXTRACTED CORRECTLY OUT OF UPDATE SCOPE
    void CheckFridgeShelfHover()
    {
        if (!isDragging) return;

        // Sweep across all triggers under your finger/cursor position
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        overlappingFridgeShelf = null;

        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<FridgeShelfZone>(out var shelf))
            {
                overlappingFridgeShelf = shelf;
                break;
            }
        }

        if (overlappingFridgeShelf != null && sr != null)
        {
            // 🌟 Layer Sandwich: Keep it cleanly over the back walls while dragging
            sr.sortingLayerName = "Ingredients";
            sr.sortingOrder = 50;
        }
    }

    void FixedUpdate()
    {
        if (isFallingToFloor && !isDragging)
        {
            HandleManualGravityAndBounce();
        }
    }

    public void StartDragFromSource(Vector3 clickWorldPos)
    {
        isDragging = true;
        isFallingToFloor = false;
        bounceCount = 0;
        customVelocity = Vector2.zero;
        dragOffset = transform.position - clickWorldPos;
        dragOffset.z = 0f;
    }

    // Update these specific methods inside your IngredientController.cs

    void StartDrag(Vector3 clickWorldPos)
    {
        isDragging = true;
        justStartedDrag = true;
        isFallingToFloor = false;
        bounceCount = 0;
        customVelocity = Vector2.zero;

        dragOffset = transform.position - clickWorldPos;
        dragOffset.z = 0f;

        CameraPan.IsHoldingAnyItem = true;

        // CRITICAL: Unparent from moving fridge doors instantly when grabbed
        transform.SetParent(null);

        if (sr != null)
        {
            sr.sortingOrder = 100;
            Color c = sr.color;
            c.a = 0.7f;
            sr.color = c;
        }
        if (isSlottedInFridge && overlappingFridgeShelf != null)
        {
            overlappingFridgeShelf.DeregisterLeavingItem(this);
            isSlottedInFridge = false;
        }
    }
    public void StopDrag()
    {
        isDragging = false;
        CameraPan.IsHoldingAnyItem = false;

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        // CRITICAL SAFETY FIX: Ensure pour states shut off immediately when letting go of the item anywhere
        var milkPourer = GetComponentInChildren<MilkPourer>();
        if (milkPourer != null)
        {
            milkPourer.ResetPourState();
        }

        // ── 1. IF RELEASED INSIDE A VALID FRIDGE GRID ROW ──
        if (overlappingFridgeShelf != null)
        {
            Vector3 targetSnapPos = overlappingFridgeShelf.AddAndRearrangeShelf(this);
            if (sr != null)
            {
                sr.sortingLayerName = "Ingredients";
                sr.sortingOrder = 5;
            }

            isSlottedInFridge = true;
            isFallingToFloor = false;
            return;
        }
        // ── 2. IF DRAGGED OUTSIDE OF THE REFRIGERATOR COMPLETELY ──
        else
        {
            if (isSlottedInFridge)
            {
                isSlottedInFridge = false;
            }

            transform.localScale = new Vector3(1f, 1f, 1f);
            transform.SetParent(null);
        }

        // ── 3. RUN ORIGINAL BLENDER OVERLAP & COUNTER LANDING PHYSICS ──
        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(transform.position.x, transform.position.y));
        foreach (Collider2D hit in hits)
        {
            if (hit == myCollider) continue;

            if (hit.TryGetComponent<BlenderController>(out var blender))
            {
                // Liquid containers pour out but don't drop inside the blender directly
                if (milkPourer == null)
                {
                    blender.SnapAndAddIngredient(this);
                    return;
                }
                else
                {
                    milkPourer.HandleReleaseOverBlender();
                    return;
                }
            }
        }

        ExecuteNormalFloorDrop();
    }

    private void ExecuteNormalFloorDrop()
    {
        bool overTableHorizontalSpace = false;
        float tableSurfaceY = floorTopY;

        // 🌟 CHECK OVEN TOP BASELINE AREA FIRST
        GameObject ovenObj = GameObject.Find("Oven");
        if (ovenObj != null)
        {
            Collider2D ovenCollider = ovenObj.GetComponent<Collider2D>();
            if (ovenCollider != null)
            {
                if (transform.position.x >= ovenCollider.bounds.min.x && transform.position.x <= ovenCollider.bounds.max.x)
                {
                    tableSurfaceY = -1.35f; // Stove burner line height
                    overTableHorizontalSpace = true;
                }
            }
        }

        // 🌟 CHECK MAIN KITCHEN TABLE SECOND
        if (!overTableHorizontalSpace)
        {
            GameObject tableObj = GameObject.Find("KitchenTable");
            if (tableObj != null)
            {
                Collider2D tableCollider = tableObj.GetComponent<Collider2D>();
                if (tableCollider != null)
                {
                    if (transform.position.x >= tableCollider.bounds.min.x && transform.position.x <= tableCollider.bounds.max.x)
                    {
                        tableSurfaceY = tableCollider.bounds.max.y;
                        if (transform.position.y >= tableSurfaceY - 0.5f)
                        {
                            overTableHorizontalSpace = true;
                        }
                    }
                }
            }
        }

        targetFloorY = overTableHorizontalSpace ? tableSurfaceY : Mathf.Clamp(transform.position.y, floorBottomY, floorTopY);
        customVelocity = Vector2.zero;
        isFallingToFloor = true;
    }

    void HandleManualGravityAndBounce()
    {
        customVelocity.y -= gravity * Time.fixedDeltaTime;

        Vector3 currentPos = transform.position;
        currentPos.x += customVelocity.x * Time.fixedDeltaTime;
        currentPos.y += customVelocity.y * Time.fixedDeltaTime;
        transform.position = currentPos;

        if (transform.position.y <= targetFloorY)
        {
            transform.position = new Vector3(transform.position.x, targetFloorY, transform.position.z);

            if (bounceCount < MAX_BOUNCES)
            {
                bounceCount++;
                float appliedBounce = baseBounceForce * Mathf.Pow(bounciness, bounceCount);
                float sideRoll = Random.Range(-1.5f, 1.5f);

                customVelocity = new Vector2(sideRoll, appliedBounce);
            }
            else
            {
                isFallingToFloor = false;
                customVelocity = Vector2.zero;
                UpdateSortingOrder();
            }
        }

    }

   

    void UpdateSortingOrder()
    {
        if (sr == null || isSlottedInFridge) return;
        float t = Mathf.InverseLerp(floorTopY, floorBottomY, transform.position.y);
        sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(5, 50, t));
    }

    Vector3 GetWorldPos()
    {
        Vector2 screenPos = Vector2.zero;

        if (Pointer.current != null)
        {
            screenPos = Pointer.current.position.ReadValue();
        }

        if (screenPos == Vector2.zero) return transform.position;

        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        world.z = 0f;
        return world;
    }

    bool InputPressed()
    {
        if (Pointer.current != null)
        {
            return Pointer.current.press.wasPressedThisFrame;
        }
        return false;
    }

    bool InputReleased()
    {
        if (Pointer.current != null)
        {
            return Pointer.current.press.wasReleasedThisFrame;
        }
        return false;
    }
}