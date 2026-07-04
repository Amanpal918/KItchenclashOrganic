using UnityEngine;
using UnityEngine.InputSystem;

public class LumiController : MonoBehaviour
{
    [Header("🤖 Animation Settings")]
    [SerializeField] private string flyParameterName = "isFlying";

    [Header("✨ Drag & Follow Smoothing")]
    [SerializeField] private float followSmoothing = 25f;

    [Header("🍂 Gravity & Floor Settings")]
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float floorYCoord = -3.5f;

    [Header("🎥 Edge Panning Settings")]
    [Tooltip("Distance from screen edge in pixels to trigger a scroll (e.g., 100px).")]
    [SerializeField] private float edgeThresholdPixels = 100f;
    [Tooltip("How fast the camera moves when pushing the edge.")]
    [SerializeField] private float cameraScrollSpeed = 5f;

    // Core Component Cache
    private Animator myAnimator;
    private Collider2D myCollider;
    private Camera mainCamera;

    // Internal Drag & Physics States
    private bool isBeingDragged = false;
    private bool isFalling = false;
    private Vector3 dragOffset;
    private float verticalVelocity = 0f;

    void Awake()
    {
        myAnimator = GetComponent<Animator>();
        myCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleInputAndDrag();

        if (isBeingDragged)
        {
            // 🌟 NEW FEATURE: Check if Lumi is pushing screen edges to move the camera!
            HandleEdgeScrolling();
        }

        if (isFalling && !isBeingDragged)
        {
            ApplyGravity();
        }
    }

    private void HandleInputAndDrag()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        if (InputPressed() && !isBeingDragged)
        {
            if (myCollider != null && myCollider.OverlapPoint(new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
            {
                StartDraggingCharacter(mouseWorldPos);
            }
        }

        if (isBeingDragged)
        {
            Vector3 targetTargetPos = mouseWorldPos + dragOffset;
            transform.position = Vector3.Lerp(transform.position, new Vector3(targetTargetPos.x, targetTargetPos.y, transform.position.z), Time.deltaTime * followSmoothing);
        }

        if (isBeingDragged && InputReleased())
        {
            StopDraggingCharacter();
        }
    }

    private void HandleEdgeScrolling()
    {
        if (Pointer.current == null || mainCamera == null) return;

        // 1. Get raw screen position
        Vector2 screenPos = Pointer.current.position.ReadValue();

        // 2. Convert it to a normalized Viewport coordinate (0.0 to 1.0)
        Vector3 viewportPos = mainCamera.ScreenToViewportPoint(new Vector3(screenPos.x, screenPos.y, 0f));

        Vector3 cameraPos = mainCamera.transform.position;

        // 🌟 NEW THRESHOLD: 0.1f means "within 10% of the screen edge"
        float edgeThreshold = 0.12f;

        // ── A. PUSHING RIGHT EDGE (Viewport X is close to 1.0) ──
        if (viewportPos.x >= 1.0f - edgeThreshold)
        {
            cameraPos.x += cameraScrollSpeed * Time.deltaTime;
        }
        // ── B. PUSHING LEFT EDGE (Viewport X is close to 0.0) ──
        else if (viewportPos.x <= edgeThreshold)
        {
            cameraPos.x -= cameraScrollSpeed * Time.deltaTime;
        }

        // ── C. BOUNDARY CLAMP ──
        // 🌟 CRUCIAL: Make sure these limits (-5f, 5f) match your background limits!
        cameraPos.x = Mathf.Clamp(cameraPos.x, -10f, 28f);
        mainCamera.transform.position = cameraPos;
    }

    private void StartDraggingCharacter(Vector3 clickPos)
    {
        isBeingDragged = true;
        isFalling = false;
        verticalVelocity = 0f;
        dragOffset = transform.position - clickPos;
        dragOffset.z = 0f;

        // Tell your regular swipe camera system to freeze so it doesn't shake while moving
        CameraPan.IsHoldingAnyItem = true;

        if (myAnimator != null)
        {
            myAnimator.SetBool(flyParameterName, true);
        }
    }

    private void StopDraggingCharacter()
    {
        isBeingDragged = false;
        CameraPan.IsHoldingAnyItem = false;
        isFalling = true;
    }

    private void ApplyGravity()
    {
        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 currentPos = transform.position;
        currentPos.y += verticalVelocity * Time.deltaTime;
        transform.position = currentPos;

        if (transform.position.y <= floorYCoord)
        {
            transform.position = new Vector3(transform.position.x, floorYCoord, transform.position.z);
            isFalling = false;
            verticalVelocity = 0f;

            if (myAnimator != null)
            {
                myAnimator.SetBool(flyParameterName, false);
            }
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 pointerPos = Vector2.zero;
        if (Pointer.current != null) pointerPos = Pointer.current.position.ReadValue();
        if (pointerPos == Vector2.zero) return transform.position;

        Vector3 worldCoordinate = mainCamera.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, 0f));
        worldCoordinate.z = 0f;
        return worldCoordinate;
    }

    private bool InputPressed() => Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
    private bool InputReleased() => Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;
}