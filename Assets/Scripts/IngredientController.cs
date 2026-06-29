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
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(worldPos.x, worldPos.y), Vector2.zero);
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
            ApplyPerspectiveScale();
            UpdateSortingOrder();
        }

        // ── 3. RELEASE TRIGGER ──
        if (isDragging && InputReleased())
        {
            StopDrag();
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

        if (sr != null)
        {
            sr.sortingOrder = 100;
            Color c = sr.color;
            c.a = 0.7f;
            sr.color = c;
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

        // ── BLENDER OVERLAP CHECK ──
        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(transform.position.x, transform.position.y));
        foreach (Collider2D hit in hits)
        {
            if (hit == myCollider) continue;

            if (hit.TryGetComponent<BlenderController>(out var blender))
            {
                blender.SnapAndAddIngredient(this);
                return;
            }
        }

        // ── 🛠️ SMART VERTICAL DROP LAYER DETECTION ──
        bool overTableHorizontalSpace = false;
        float tableSurfaceY = floorTopY;

        // Find the KitchenTable collider in your active scene workspace
        GameObject tableObj = GameObject.Find("KitchenTable");
        if (tableObj != null)
        {
            Collider2D tableCollider = tableObj.GetComponent<Collider2D>();
            if (tableCollider != null)
            {
                float leftEdge = tableCollider.bounds.min.x;
                float rightEdge = tableCollider.bounds.max.x;

                // Is the ingredient's current X position directly within the table's left and right sides?
                if (transform.position.x >= leftEdge && transform.position.x <= rightEdge)
                {
                    // If we are dropped above the table surface line, the table is our new floor floor!
                    tableSurfaceY = tableCollider.bounds.max.y;

                    if (transform.position.y >= tableSurfaceY - 0.5f)
                    {
                        overTableHorizontalSpace = true;
                    }
                }
            }
        }

        // Determine where this item should eventually land and bounce
        if (overTableHorizontalSpace)
        {
            targetFloorY = tableSurfaceY; // Land right on top of the counter surface line!
        }
        else
        {
            // Missed the table completely! Fall all the way down to the kitchen floorboards
            targetFloorY = Mathf.Clamp(transform.position.y, floorBottomY, floorTopY);
        }

        // Start the manual gravity falling physics sequence down to our chosen landing target floor
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

        ApplyPerspectiveScale();
    }

    // ── TASK 2: BLUEY PERSPECTIVE SCALE CALCULATION ──
    void ApplyPerspectiveScale()
    {
        // Calculate percentage tracking between top wall line and bottom screen edge line
        float t = Mathf.InverseLerp(floorTopY, floorBottomY, transform.position.y);
        float currentScaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);

        transform.localScale = baseLocalScale * currentScaleMultiplier;
    }

    void UpdateSortingOrder()
    {
        if (sr == null) return;
        float t = Mathf.InverseLerp(floorTopY, floorBottomY, transform.position.y);
        sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(5, 50, t));
    }

    Vector3 GetWorldPos()
    {
        Vector2 screenPos = Vector2.zero;

        // Pointer tracks BOTH Mouse position and Touchscreen primary finger position automatically
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
            // Triggers on left-mouse-click OR mobile screen press down
            return Pointer.current.press.wasPressedThisFrame;
        }
        return false;
    }

    bool InputReleased()
    {
        if (Pointer.current != null)
        {
            // Triggers when lifting mouse click OR lifting finger off screen
            return Pointer.current.press.wasReleasedThisFrame;
        }
        return false;
    }
}