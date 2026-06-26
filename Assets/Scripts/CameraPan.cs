using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPan : MonoBehaviour
{
    [Header("Background Reference")]
    [Tooltip("Drag your Backgmd-Img sprite game object here")]
    [SerializeField] private SpriteRenderer backgroundSprite;

    [Header("Edge Scroll Settings")]
    [Tooltip("Percentage of screen width that triggers auto-scroll (e.g., 0.10f = outer 10% of screen)")]
    [SerializeField] private float edgeThreshold = 0.10f;
    [Tooltip("How fast the camera moves when holding an item near the edge")]
    [SerializeField] private float autoScrollSpeed = 8f;

    // 🌟 GLOBAL TRACKING STATUS (Tells camera if an item is being held by any script)
    public static bool IsHoldingAnyItem = false;

    private Vector3 touchStart;
    private bool isPanning = false;
    private bool isHoldingItem = false;

    private float minWorldX;
    private float maxWorldX;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        if (backgroundSprite == null)
        {
            Debug.LogError("CameraPan: Please assign the Background Sprite in the Inspector!");
            return;
        }

        CalculateLimits();
    }

    void CalculateLimits()
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float bgMinX = backgroundSprite.bounds.min.x;
        float bgMaxX = backgroundSprite.bounds.max.x;

        minWorldX = bgMinX + camWidth;
        maxWorldX = bgMaxX - camWidth;

        if (minWorldX > maxWorldX)
        {
            float center = (bgMinX + bgMaxX) / 2f;
            minWorldX = center;
            maxWorldX = center;
        }
    }

    void Update()
    {
        if (backgroundSprite == null) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();

        // ─────────────────────────────────────────
        // 1. INPUT PRESS START
        // Check what player pressed on
        // ─────────────────────────────────────────
        if (Pointer.current.press.wasPressedThisFrame)
        {
            // 🌟 Check our global safety toggle state first
            if (IsHoldingAnyItem)
            {
                isPanning = false;
                isHoldingItem = true;
                return;
            }

            Vector2 mouseRay = cam.ScreenToWorldPoint(screenPos);
            RaycastHit2D hit = Physics2D.Raycast(mouseRay, Vector2.zero);

            // ── UPDATED CHECK ──
            // Triggers edge pan for:
            // - WorldDraggableIngredient (old drag system)
            // - IngredientSource (dragging from source)
            // - PlacedIngredient (moving placed clone)
            bool hitIngredient = hit.collider != null &&
            (
                hit.collider.GetComponent<WorldDraggableIngredient>() != null ||
                hit.collider.GetComponent<IngredientSource>() != null ||
                hit.collider.GetComponent<PlacedIngredient>() != null
            );

            if (hitIngredient)
            {
                // Player grabbed ingredient
                // Disable manual pan, enable edge pan
                isPanning = false;
                isHoldingItem = true;
                IsHoldingAnyItem = true; // Sync state globally
                return;
            }

            // Player pressed on background
            // Enable normal manual panning
            touchStart = cam.ScreenToWorldPoint(screenPos);
            isPanning = true;
            isHoldingItem = false;
            IsHoldingAnyItem = false;
        }

        // ─────────────────────────────────────────
        // 2. INPUT HELD STATE
        // Either panning manually or edge scrolling
        // ─────────────────────────────────────────
        if (Pointer.current.press.isPressed)
        {
            // 🌟 Use global variable fallback logic to verify edge scroll triggering
            if (isHoldingItem || IsHoldingAnyItem)
            {
                // Edge pan while carrying ingredient
                HandleEdgeScrolling(screenPos);
            }
            else if (isPanning)
            {
                // Standard manual panning
                Vector3 currentWorldPos = cam.ScreenToWorldPoint(screenPos);
                Vector3 direction = touchStart - currentWorldPos;
                Vector3 targetPos = transform.position +
                    new Vector3(direction.x, 0, 0);

                targetPos.x = Mathf.Clamp(targetPos.x, minWorldX, maxWorldX);
                transform.position = targetPos;
            }
        }

        // ─────────────────────────────────────────
        // 3. INPUT RELEASED
        // Reset all states
        // ─────────────────────────────────────────
        if (Pointer.current.press.wasReleasedThisFrame)
        {
            isPanning = false;
            isHoldingItem = false;
            IsHoldingAnyItem = false; // Reset global status
        }
    }

    // ─────────────────────────────────────────────
    // HANDLE EDGE SCROLLING
    // Called every frame while holding ingredient
    // Moves camera if finger near screen edge
    // Works on all screen sizes because it uses
    // Screen.width which adapts automatically
    // ─────────────────────────────────────────────
    private void HandleEdgeScrolling(Vector2 screenPosition)
    {
        float screenWidth = Screen.width;
        float leftEdgeBounds = screenWidth * edgeThreshold;
        float rightEdgeBounds = screenWidth * (1f - edgeThreshold);

        float moveDirection = 0f;

        // Finger in right edge zone
        if (screenPosition.x >= rightEdgeBounds)
        {
            moveDirection = 1f;
        }
        // Finger in left edge zone
        else if (screenPosition.x <= leftEdgeBounds)
        {
            moveDirection = -1f;
        }

        // Move camera if in edge zone
        if (moveDirection != 0f)
        {
            Vector3 targetPos = transform.position + new Vector3(
                moveDirection * autoScrollSpeed * Time.deltaTime,
                0,
                0
            );

            // Clamp to background limits
            targetPos.x = Mathf.Clamp(targetPos.x, minWorldX, maxWorldX);
            transform.position = targetPos;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && cam != null && backgroundSprite != null)
        {
            CalculateLimits();
        }
    }
#endif
}