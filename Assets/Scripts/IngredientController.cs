using UnityEngine;
using UnityEngine.InputSystem;

public class IngredientController : MonoBehaviour
{
    [Header("Ingredient Data Reference")]
    public IngredientData ingredientData;

    [Header("2.5D Floor Settings")]
    [SerializeField] private float floorTopY = -1.64f;
    [SerializeField] private float floorBottomY = -8.69f;

    [Header("Juicy Drop Settings")]
    [SerializeField] private float gravity = 25f;
    [SerializeField] private float baseBounceForce = 5f;
    [SerializeField] private float bounciness = 0.5f;

    // State Tracking
    private bool isDragging = false;
    private bool justStartedDrag = false;
    private bool isFallingToFloor = false;
    private float targetFloorY;
    private int bounceCount = 0;
    private const int MAX_BOUNCES = 2;

    // Component References
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

        // Keep Rigidbody fully manual
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
        }

        // ── 3. RELEASE TRIGGER ──
        if (isDragging && InputReleased())
        {
            StopDrag();
        }
    }

    void FixedUpdate()
    {
        // ── 4. MANUAL ACCELERATION FALLOUT ──
        if (isFallingToFloor && !isDragging)
        {
            HandleManualGravityAndBounce();
        }
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

        // ── BLENDER OVERLAP OVERRIDE ──
        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(transform.position.x, transform.position.y));
        foreach (Collider2D hit in hits)
        {
            if (hit == myCollider) continue;

            if (hit.TryGetComponent<BlenderController>(out var blender))
            {
                blender.AddIngredient(this.ingredientData);
                Destroy(gameObject);
                return;
            }
        }

        // ── CALC DROP LIMIT GROUND DEPTH LINE ──
        targetFloorY = Mathf.Clamp(transform.position.y, floorBottomY, floorTopY);

        customVelocity = Vector2.zero;
        isFallingToFloor = true;
        UpdateSortingOrder();
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
        if (sr == null) return;
        float t = Mathf.InverseLerp(floorTopY, floorBottomY, transform.position.y);
        sr.sortingOrder = Mathf.RoundToInt(Mathf.Lerp(5, 50, t));
    }

    // ─────────────────────────────────────────────
    // STABILIZED INPUT HELPERS
    // ─────────────────────────────────────────────
    Vector3 GetWorldPos()
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

        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        world.z = 0f;
        return world;
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