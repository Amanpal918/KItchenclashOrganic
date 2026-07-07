using UnityEngine;
using UnityEngine.InputSystem;

public class PlacedIngredient : MonoBehaviour
{
    public bool IsDragging => isDragging;

    [Header("2.5D Floor Settings")]
    [SerializeField] private float floorTopY = -1.64f;
    [SerializeField] private float floorBottomY = -8.69f;

    [Header("Juicy Drop Settings")]
    [SerializeField] private float gravity = 25f;       // How fast it pulls down
    [SerializeField] private float baseBounceForce = 5f; // Power of the first bounce
    [SerializeField] private float bounciness = 0.5f;    // Multiplier for next bounce (0.5 = half height)

    private float targetFloorY;
    private int bounceCount = 0;
    private const int MAX_BOUNCES = 2;

    // Manual Physics Tracking Variables
    private Vector2 customVelocity;
    private bool isFallingToFloor = false;

    private Collider2D myCollider;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool isDragging = false;
    private bool justStartedDrag = false;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Always start fully manageable
        SetKinematic();
    }

    void Update()
    {
        if (justStartedDrag)
        {
            justStartedDrag = false;
            return;
        }

        // ── PRESS ──
        if (InputPressed() && !isDragging)
        {
            Vector3 worldPos = GetWorldPos();
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(worldPos.x, worldPos.y), Vector2.zero);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == myCollider)
                {
                    StartDrag();
                    break;
                }
            }
        }

        // ── HOLD ──
        if (isDragging)
        {
            Vector3 worldPos = GetWorldPos();
            transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
        }

        // ── RELEASE ──
        if (isDragging && InputReleased())
        {
            StopDrag();
        }
    }

    void FixedUpdate()
    {
        // Handle all custom bounce/fall movement safely inside FixedUpdate
        if (isFallingToFloor && !isDragging)
        {
            HandleManualGravityAndBounce();
        }
    }

    void StartDrag()
    {
        isDragging = true;
        justStartedDrag = true;
        isFallingToFloor = false;
        bounceCount = 0;

        // CRITICAL FIX: Completely wipe out any previous movement speeds
        customVelocity = Vector2.zero;

        SetKinematic();

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

        // Calculate ground target line based on current height
        targetFloorY = Mathf.Clamp(transform.position.y, floorBottomY, floorTopY);

        // CRITICAL FIX: Reset velocity again so it falls cleanly straight down from zero speed
        customVelocity = Vector2.zero;
        isFallingToFloor = true;

        SetKinematic();

        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
            UpdateSortingOrder();
        }
    }

    void HandleManualGravityAndBounce()
    {
        // Apply falling acceleration over time
        customVelocity.y -= gravity * Time.fixedDeltaTime;

        // Apply temporary movement step
        Vector3 currentPos = transform.position;
        currentPos.x += customVelocity.x * Time.fixedDeltaTime;
        currentPos.y += customVelocity.y * Time.fixedDeltaTime;
        transform.position = currentPos;

        // Floor collision check
        if (transform.position.y <= targetFloorY)
        {
            // Lock position cleanly directly to the calculated perspective line
            transform.position = new Vector3(transform.position.x, targetFloorY, transform.position.z);

            if (bounceCount < MAX_BOUNCES)
            {
                bounceCount++;

                // Diminish bounce force linearly each bounce iteration
                float appliedBounce = baseBounceForce * Mathf.Pow(bounciness, bounceCount);
                float sideRoll = Random.Range(-1.5f, 1.5f);

                customVelocity = new Vector2(sideRoll, appliedBounce);
            }
            else
            {
                // Finished bounce cycle, come to absolute rest
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

    void SetKinematic()
    {
        if (rb == null) return;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    // ─────────────────────────────────────────────
    // INPUT + WORLD POS HELPERS
    // ─────────────────────────────────────────────
    Vector3 GetWorldPos()
    {
        Vector2 screenPos = Vector2.zero;

        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.isInProgress)
                {
                    screenPos = touch.position.ReadValue();
                    break;
                }
            }
        }

        if (screenPos == Vector2.zero && Mouse.current != null)
            screenPos = Mouse.current.position.ReadValue();

        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        world.z = 0f;
        return world;
    }

    bool InputPressed()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                    return true;
            }
        }

        if (Mouse.current != null)
            return Mouse.current.leftButton.wasPressedThisFrame;

        return false;
    }

    bool InputReleased()
    {
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended)
                    return true;
            }
        }

        if (Mouse.current != null)
            return Mouse.current.leftButton.wasReleasedThisFrame;

        return false;
    }
}