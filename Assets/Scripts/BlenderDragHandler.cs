using UnityEngine;
using UnityEngine.InputSystem;

public class BlenderDragHandler : MonoBehaviour
{
    [Header("✨ Drag & Follow Smoothing")]
    [SerializeField] private float followSmoothing = 25f;

    [Header("🍂 Gravity & Floor Settings")]
    [SerializeField] private float gravity = 25f;
    private float counterFloorY; // Captured dynamically on awake to perfectly match initial placement

    // Core Component Cache
    private BlenderController blenderController;
    private Collider2D myCollider;
    private Camera mainCamera;

    // Interaction & State Tracking Locks
    private bool canBePickedUp = false;
    private bool isBeingDragged = false;
    private bool isFallingToCounter = false;
    private Vector3 dragOffset;
    private float verticalVelocity = 0f;
    private Vector3 initialStartingPosition;

    void Awake()
    {
        blenderController = GetComponent<BlenderController>();
        myCollider = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        // Automatically pin the countertop target height based on where you place it in your scene layout
        initialStartingPosition = transform.position;
        counterFloorY = transform.position.y;

    }

    void Update()
    {
        // 🌟 RULE 1: Only monitor drag logic if a shake has been successfully prepared!
        if (!canBePickedUp) return;

        HandleInputAndDrag();

        if (isFallingToCounter && !isBeingDragged)
        {
            ApplyGravity();
        }
    }

    private void HandleInputAndDrag()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // ── 1. CLICK TO PICK UP ──
        if (InputPressed() && !isBeingDragged)
        {
            if (myCollider != null && myCollider.OverlapPoint(new Vector2(mouseWorldPos.x, mouseWorldPos.y)))
            {
                isBeingDragged = true;
                isFallingToCounter = false;
                verticalVelocity = 0f;
                dragOffset = transform.position - mouseWorldPos;
                dragOffset.z = 0f;

                // Stop the camera from sliding around while carrying the shake drink
                CameraPan.IsHoldingAnyItem = true;
                // 🌟 SORTING LAYER FIX: Force the blender in front of Lumi while carrying it!
                if (TryGetComponent<SpriteRenderer>(out var sr))
                {
                    sr.sortingLayerName = "UI_WorldSpace"; // Or "Ingredients"
                    sr.sortingOrder = 100; // High order guarantees it sits on top of Lumi's layer (15)
                }
            }
        }

        // ── 2. HOLD AND SMOOTH GLIDE ──
        if (isBeingDragged)
        {
            Vector3 targetPos = mouseWorldPos + dragOffset;
            transform.position = Vector3.Lerp(transform.position, new Vector3(targetPos.x, targetPos.y, transform.position.z), Time.deltaTime * followSmoothing);
        }

        // ── 3. RELEASE TO DROP ──
        if (isBeingDragged && InputReleased())
        {
            isBeingDragged = false;
            CameraPan.IsHoldingAnyItem = false;

            if (TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.sortingLayerName = "Containers_Back"; // Adjust this to match your normal kitchen counter layer
                sr.sortingOrder = 5;
            }
            // Check if dropped near Lumi's mouth before executing regular gravity drop
            if (CheckIfNearLumiFace())
            {
                // Lumi consumed it! Empty the jar and snap it back home instantly
                ResetBlenderBackToCounter();
            }
            else
            {
                // Missed Lumi: let the gravity system drop it back to the kitchen counter surface safely
                isFallingToCounter = true;
            }
        }
    }

    private bool CheckIfNearLumiFace()
    {
        LumiController lumi = FindFirstObjectByType<LumiController>();
        if (lumi != null)
        {
            float distanceToFace = Vector2.Distance(transform.position, lumi.transform.position);
            if (distanceToFace <= 1.5f)
            {
                // 🎬 Play her cute thinking loop sequence
                Animator lumiAnimator = lumi.GetComponent<Animator>();
                if (lumiAnimator != null)
                {
                    lumiAnimator.SetBool("isFlying", false);
                    lumiAnimator.Play("LumiThinking");
                }

                // 🌟 NEW HOOK: Tell her personality script to analyze the food and award the stars!
                if (lumi.TryGetComponent<CharacterPersonality>(out var personality))
                {
                    // Passes the custom recipe name (e.g. "Strawberry Shake") to compute the score
                    personality.ProcessFedFoodItem("Strawberry Shake");
                }

                Debug.Log("Lumi happily drank the completed milkshake blend!");
                return true;
            }
        }
        return false;
    }   

    private void ApplyGravity()
    {
        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 currentPos = transform.position;
        currentPos.y += verticalVelocity * Time.deltaTime;
        transform.position = currentPos;

        // Floor boundary collision lock check
        if (transform.position.y <= counterFloorY)
        {
            transform.position = new Vector3(transform.position.x, counterFloorY, transform.position.z);
            isFallingToCounter = false;
            verticalVelocity = 0f;
        }
    }

    /// <summary>
    /// 🚀 THE UNLOCK SWITCH: Called by BlenderController when the blend successfully completes!
    /// </summary>
    public void EnableBlenderDraggability(bool status)
    {
        canBePickedUp = status;
        if (!status)
        {
            isBeingDragged = false;
            isFallingToCounter = false;
        }
    }

    private void ResetBlenderBackToCounter()
    {
        EnableBlenderDraggability(false);

        if (blenderController != null)
        {
            blenderController.ClearBlender();
        }

        // 🌟 SNAP BACK TO THE EXACT ORIGINAL RESTING SLOT INSTEAD OF JUST THE HEIGHT!
        transform.position = initialStartingPosition;
    }

    #region ── INPUT VECTOR UTILITIES ──

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

    #endregion
}