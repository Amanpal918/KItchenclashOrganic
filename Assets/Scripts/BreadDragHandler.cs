using UnityEngine;
using UnityEngine.InputSystem; // 🌟 Modern Input System Hook

public class BreadDragHandler : MonoBehaviour
{
    [Header("🔍 Drag Settings")]
    [SerializeField] private bool isRawBread = true;
    [SerializeField] private float activationDistance = 2.5f; // Increased slightly for comfort

    private bool isBeingDragged = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private Collider2D myCollider;

    void Awake()
    {
        mainCamera = Camera.main;
        myCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        HandleInputAndDrag();
    }

    private void HandleInputAndDrag()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
        mouseWorldPos.z = 0f;

        // 1. Pick Up
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (myCollider != null && myCollider.OverlapPoint(new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
            {
                StartDragging(mouseWorldPos);
            }
        }

        // 2. Dragging Loop
        if (isBeingDragged)
        {
            transform.position = mouseWorldPos + dragOffset;

            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingOrder = 50; // Fly above the kitchen counter while carrying
            }
        }

        // 3. Drop Drop
        if (isBeingDragged && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isBeingDragged = false;

            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingOrder = 5; // Revert to shelf/counter sorting depth
            }

            if (isRawBread)
            {
                CheckToasterProximity();
            }
        }
    }

    private void StartDragging(Vector3 mousePos)
    {
        isBeingDragged = true;
        dragOffset = transform.position - mousePos;
        dragOffset.z = 0f;
    }

    public void ForceStartDragOnSpawn()
    {
        mainCamera = Camera.main;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
        mouseWorldPos.z = 0f;
        StartDragging(mouseWorldPos);
    }

    private void CheckToasterProximity()
    {
        ToasterController toaster = FindFirstObjectByType<ToasterController>();
        if (toaster != null && toaster.currentState == ToasterController.ToasterState.Empty)
        {
            float distanceToToaster = Vector2.Distance(transform.position, toaster.transform.position);
            Debug.Log($"[Distance Check] Distance: {distanceToToaster:F2} (Target <= {activationDistance})");

            if (distanceToToaster <= activationDistance)
            {
                if (toaster.InsertRawBread())
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}