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

    [Header("📐 Individual Placement Tuning")]
    [SerializeField] private float counterLandingOffset = 0f;

    [Header("🔄 Dispenser Settings")]
    public bool isSourceDispenser = false;

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

    public bool IsBeingDragged => isDragging;
    public bool IsDragging => isDragging;
    private FridgeShelfZone overlappingFridgeShelf;
    private bool isSlottedInFridge = false;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

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

        // ====================================================================
        // 🌟 DYNAMIC FRIDGE VISIBILITY STATE (FIXED INVERSION)
        // ====================================================================
        if (isSourceDispenser && transform.parent != null && transform.parent.name == "Shelf_top")
        {
            GameObject fridgeDoor = GameObject.Find("Upper_Door_Container");

            if (fridgeDoor != null && sr != null)
            {
                // In your project setup, Upper_Door_Container is active when the door is OPEN.
                // Therefore, the butter should be ENABLED when the door container is active!
                bool isFridgeOpen = fridgeDoor.activeInHierarchy;

                // 🔒 Force the sprite and collider to match the fridge open state exactly
                if (sr.enabled != isFridgeOpen)
                {
                    sr.enabled = isFridgeOpen;
                    if (myCollider != null) myCollider.enabled = isFridgeOpen;
                }
            }
            else if (fridgeDoor == null && sr != null)
            {
                // If the game can't find the open door object, it means the door is closed!
                if (sr.enabled)
                {
                    sr.enabled = false;
                    if (myCollider != null) myCollider.enabled = false;
                }
            }
        }

        // ====================================================================
        // 🛠️ DRAG AND DROP DETECTION MECHANICS (NEW INPUT SYSTEM)
        // ====================================================================
        Vector3 worldPos = GetWorldPos();

        // Check for click/touch initialization
        if (InputPressed() && !isDragging)
        {
            if (myCollider != null && myCollider.OverlapPoint(new Vector2(worldPos.x, worldPos.y)))
            {
                StartDrag(worldPos);
            }
        }

        // Handle active movement frame-by-frame while dragging
        if (isDragging)
        {
            transform.position = new Vector3(worldPos.x + dragOffset.x, worldPos.y + dragOffset.y, 0f);
            CheckFridgeShelfHover();
            UpdateSortingOrder();
        }

        // Handle touch/click release termination
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
        justStartedDrag = true;
        isFallingToFloor = false;
        bounceCount = 0;
        customVelocity = Vector2.zero;
        dragOffset = Vector3.zero;

        CameraPan.IsHoldingAnyItem = true;
        transform.SetParent(null);

        if (sr != null)
        {
            sr.sortingLayerName = "Ingredients";
            sr.sortingOrder = 100;
            sr.color = Color.white;
        }
    }

    void StartDrag(Vector3 clickWorldPos)
    {
        if (isSourceDispenser)
        {
            GameObject cloneObj = Instantiate(gameObject, transform.position, transform.rotation);
            IngredientController cloneController = cloneObj.GetComponent<IngredientController>();
            if (cloneController != null)
            {
                cloneController.isSourceDispenser = false;
                cloneController.isSlottedInFridge = false;
                cloneController.overlappingFridgeShelf = null;
                cloneController.StartDragFromSource(clickWorldPos);
            }
            return;
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
            sr.color = new Color(1f, 1f, 1f, 0.7f);
        }

        if (isSlottedInFridge && overlappingFridgeShelf != null)
        {
            overlappingFridgeShelf.SendMessage("DeregisterLeavingItem", this, SendMessageOptions.DontRequireReceiver);
            isSlottedInFridge = false;
        }
    }

    public void StopDrag()
    {
        isDragging = false;
        CameraPan.IsHoldingAnyItem = false;

        if (sr != null)
        {
            sr.color = Color.white;
            sr.sortingOrder = 5;
        }

        if (myCollider != null && TrashAndPoolManager.Instance != null && TrashAndPoolManager.Instance.IsOverTrashCan(myCollider.bounds.center))
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(null);

        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(transform.position.x, transform.position.y));
        foreach (Collider2D hit in hits)
        {
            if (hit == myCollider) continue;

            if (hit.TryGetComponent<BlenderController>(out var blender))
            {
                blender.SnapAndAddIngredient(this);
                return;
            }

            if (hit.TryGetComponent<FridgeShelfZone>(out var shelf))
            {
                overlappingFridgeShelf = shelf;
                shelf.SendMessage("RegisterIncomingItem", this, SendMessageOptions.DontRequireReceiver);
                shelf.SendMessage("RegisterEnteringItem", this, SendMessageOptions.DontRequireReceiver);
                shelf.SendMessage("RegisterItem", this, SendMessageOptions.DontRequireReceiver);
                isSlottedInFridge = true;
                return;
            }
        }

        ExecuteNormalFloorDrop();
    }

    public void ClearFridgeShelfStatus()
    {
        isSlottedInFridge = false;
        overlappingFridgeShelf = null;
    }

    private void ExecuteNormalFloorDrop()
    {
        bool landedOnSurface = false;
        GameObject tableObj = GameObject.Find("KitchenTable");
        if (tableObj != null)
        {
            Collider2D tableCollider = tableObj.GetComponent<Collider2D>();
            if (tableCollider != null)
            {
                bool withinHorizontalBounds = transform.position.x >= tableCollider.bounds.min.x &&
                                              transform.position.x <= tableCollider.bounds.max.x;
                bool isAboveTable = transform.position.y >= tableCollider.bounds.max.y - 0.2f;

                if (withinHorizontalBounds && isAboveTable)
                {
                    targetFloorY = tableCollider.bounds.max.y + counterLandingOffset;
                    landedOnSurface = true;
                }
            }
        }

        if (!landedOnSurface)
        {
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