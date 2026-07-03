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

    [Header("📐 Individual Placement Tuning")]
    [Tooltip("Adjust this per-prefab to nudge items up (+) or down (-) when landing on the counter.")]
    [SerializeField] private float counterLandingOffset = 0f;

    [Header("🔄 Dispenser Settings")]
    [SerializeField] private bool isSourceDispenser = false; // Check this true ONLY for the source items sitting permanently on the fridge door shelf!

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

        // ── 1. PRESS TRIGGER (Direct Overlap Point check) ──
        if (InputPressed() && !isDragging)
        {
            if (myCollider != null && myCollider.OverlapPoint(new Vector2(worldPos.x, worldPos.y)))
            {
                StartDrag(worldPos);
            }
        }

        // ── 2. HOLD TRACKING ──
        if (isDragging)
        {
            transform.position = new Vector3(worldPos.x + dragOffset.x, worldPos.y + dragOffset.y, 0f);

            // Dynamically search for specialized child pourers
            var milkPourer = GetComponentInChildren<MilkPourer>();
            var waterPourer = GetComponentInChildren<WaterPourer>();

            if (milkPourer != null || waterPourer != null)
            {
                Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(transform.position.x, transform.position.y));
                Collider2D containerCollider = null;

                foreach (Collider2D hit in hits)
                {
                    if (hit != myCollider)
                    {
                        // 🌟 MULTI-CONTAINER SUPPORT: Trigger pour animations if hovering over Blender, Glass, or any Bowl!
                        bool isValidContainer = hit.GetComponent<BlenderController>() != null ||
                                                hit.gameObject.name.ToLower().Contains("glass") ||
                                                hit.gameObject.name.ToLower().Contains("bowl") ||
                                                hit.gameObject.tag == "Container";

                        if (isValidContainer)
                        {
                            containerCollider = hit;
                            break;
                        }
                    }
                }

                // Send interactions to whichever specialized pourer component script is active on this prefab instance
                if (containerCollider != null)
                {
                    if (milkPourer != null) milkPourer.UpdatePourInteraction(containerCollider);
                    if (waterPourer != null) waterPourer.UpdatePourInteraction(containerCollider);
                }
                else
                {
                    if (milkPourer != null) milkPourer.ResetPourState();
                    if (waterPourer != null) waterPourer.ResetPourState();
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

    void CheckFridgeShelfHover()
    {
        if (!isDragging) return;

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
            // Keep it cleanly over background elements while dragging around the cabinet rows
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

    /// <summary>
    /// Invoked automatically on the recycled clone object by the source button initialization script.
    /// </summary>
    public void StartDragFromSource(Vector3 clickWorldPos)
    {
        isDragging = true;
        justStartedDrag = true;
        isFallingToFloor = false;
        bounceCount = 0;
        customVelocity = Vector2.zero;

        // Force the drag offset to zero initially since the source spawned it exactly on your cursor position
        dragOffset = Vector3.zero;

        CameraPan.IsHoldingAnyItem = true;
        transform.SetParent(null);

        if (sr != null)
        {
            sr.sortingLayerName = "Ingredients";
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

    void StartDrag(Vector3 clickWorldPos)
    {
        // 🌟 OBJECT POOLING SPARK: If this item is designated as an infinite dispenser source button, pull from pool!
        if (isSourceDispenser)
        {
            GameObject cloneObj = TrashAndPoolManager.Instance.GetPooledIngredient(gameObject, transform.position, transform.rotation);

            IngredientController cloneController = cloneObj.GetComponent<IngredientController>();
            if (cloneController != null)
            {
                cloneController.isSourceDispenser = false;
                cloneController.isSlottedInFridge = false;
                cloneController.overlappingFridgeShelf = null;
                cloneController.StartDragFromSource(clickWorldPos);
            }
            return; // Abort remaining drag routines on this source template so it stays permanently locked inside the fridge!
        }

        isDragging = true;
        justStartedDrag = true;
        isFallingToFloor = false;
        bounceCount = 0;
        customVelocity = Vector2.zero;

        dragOffset = transform.position - clickWorldPos;
        dragOffset.z = 0f;

        CameraPan.IsHoldingAnyItem = true;
        transform.SetParent(null);

        if (sr != null)
        {
            sr.sortingLayerName = "Ingredients";
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

        // Shut off active pour emissions immediately on release
        var milkPourer = GetComponentInChildren<MilkPourer>();
        var waterPourer = GetComponentInChildren<WaterPourer>();
        if (milkPourer != null) milkPourer.ResetPourState();
        if (waterPourer != null) waterPourer.ResetPourState();

        // 🌟 TRASH CAN OVERRIDE: Uses geometric bounds center to calculate alignment hits perfectly
        if (myCollider != null && TrashAndPoolManager.Instance.IsOverTrashCan(myCollider.bounds.center))
        {
            TrashAndPoolManager.Instance.RecycleToPool(gameObject);
            return; // Abort remainder of sorting layouts or dropping physics calculations!
        }

        // ── 1. IF RELEASED INSIDE A VALID FRIDGE GRID ROW ──
        if (overlappingFridgeShelf != null)
        {
            Vector3 targetSnapPos = overlappingFridgeShelf.AddAndRearrangeShelf(this);

            // Instantly updates position coordinates to prevent floating or drops
            transform.position = targetSnapPos;

            // Recycles the object clean out of the active scene instantly!
            TrashAndPoolManager.Instance.RecycleToPool(gameObject);
            return;
        }
        // ── 2. IF DRAGGED OUTSIDE OF THE REFRIGERATOR COMPLETELY ──
        else
        {
            if (isSlottedInFridge)
            {
                isSlottedInFridge = false;
            }
            transform.SetParent(null);
        }

        // ── 3. RUN ORIGINAL BLENDER OVERLAP & COUNTER LANDING PHYSICS ──
        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(transform.position.x, transform.position.y));
        foreach (Collider2D hit in hits)
        {
            if (hit == myCollider) continue;

            if (hit.TryGetComponent<BlenderController>(out var blender))
            {
                if (milkPourer == null && waterPourer == null)
                {
                    blender.SnapAndAddIngredient(this);
                    return;
                }
                else
                {
                    if (milkPourer != null) milkPourer.HandleReleaseOverBlender();
                    if (waterPourer != null) waterPourer.HandleReleaseOverBlender();
                    return;
                }
            }
        }

        ExecuteNormalFloorDrop();
    }

    private void ExecuteNormalFloorDrop()
    {
        bool landedOnSurface = false;

        // ── 1. CHECK THE KITCHEN TABLE COUNTER BOUNDARIES ──
        GameObject tableObj = GameObject.Find("KitchenTable");
        if (tableObj != null)
        {
            Collider2D tableCollider = tableObj.GetComponent<Collider2D>();
            if (tableCollider != null)
            {
                // Rule A: Is the item horizontally between the left and right edges of the table?
                bool withinHorizontalBounds = transform.position.x >= tableCollider.bounds.min.x &&
                                              transform.position.x <= tableCollider.bounds.max.x;

                // Rule B: Is the item physically ABOVE the table surface when dropped?
                bool isAboveTable = transform.position.y >= tableCollider.bounds.max.y - 0.2f;

                if (withinHorizontalBounds && isAboveTable)
                {
                    targetFloorY = tableCollider.bounds.max.y + counterLandingOffset;
                    landedOnSurface = true;
                }
            }
        }

        // ── 2. CHECK THE OVEN BOUNDARIES ──
        if (!landedOnSurface)
        {
            GameObject ovenObj = GameObject.Find("Oven");
            if (ovenObj != null)
            {
                Collider2D ovenCollider = ovenObj.GetComponent<Collider2D>();
                if (ovenCollider != null)
                {
                    bool withinOvenHorizontal = transform.position.x >= ovenCollider.bounds.min.x &&
                                                transform.position.x <= ovenCollider.bounds.max.x;

                    bool isAboveOven = transform.position.y >= -1.35f - 0.2f;

                    if (withinOvenHorizontal && isAboveOven)
                    {
                        targetFloorY = -1.35f + counterLandingOffset;
                        landedOnSurface = true;
                    }
                }
            }
        }

        // ── 3. FALLBACK: IF DROPPED BELOW COUNTERS OR OVER OPEN AIR ──
        if (!landedOnSurface)
        {
            // Lands naturally on the lower floorboard line without snapping back up!
            targetFloorY = Mathf.Clamp(transform.position.y, floorBottomY, floorTopY);
        }

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
        if (Pointer.current != null) screenPos = Pointer.current.position.ReadValue();
        if (screenPos == Vector2.zero) return transform.position;
        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        world.z = 0f;
        return world;
    }

    bool InputPressed() => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
    bool InputReleased() => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;
}