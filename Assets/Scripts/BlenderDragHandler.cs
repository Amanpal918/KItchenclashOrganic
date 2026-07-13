using UnityEngine;
using UnityEngine.InputSystem;

public class BlenderDragHandler : MonoBehaviour
{
    [Header("✨ Drag & Follow Smoothing")]
    [SerializeField] private float followSmoothing = 25f;

    [Header("🍂 Gravity & Floor Settings")]
    [SerializeField] private float gravity = 25f;
    private float counterFloorY;

    [Header("⭐ Today's Task: Feedback Stars")]
    [SerializeField] private ParticleSystem starsParticleSystem;
    [SerializeField] private Vector3 starsOffset = new Vector3(0f, 2f, 0f); // Adjust to position perfectly over Lumi's head

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

        initialStartingPosition = transform.position;
        counterFloorY = transform.position.y;
    }

    void Update()
    {
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

                CameraPan.IsHoldingAnyItem = true;

                if (TryGetComponent<SpriteRenderer>(out var sr))
                {
                    sr.sortingLayerName = "UI_WorldSpace";
                    sr.sortingOrder = 100;
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
                sr.sortingLayerName = "Containers_Back";
                sr.sortingOrder = 5;
            }

            // Check if dropped near Lumi's mouth
            if (CheckIfNearLumiFace())
            {
                // Instantly clear juice, trigger stars, and snap back home!
                ResetBlenderBackToCounter();
            }
            else
            {
                isFallingToCounter = true;
            }
        }
    }

    private bool CheckIfNearLumiFace()
    {
        LumiController lumi = FindFirstObjectByType<LumiController>();
        if (lumi != null)
        {
            // Fallback World Distance check combined with Pixel Check for 100% accuracy
            float worldDistance = Vector3.Distance(transform.position, lumi.transform.position);

            Vector2 mouseScreenPos = Vector2.zero;
            if (Pointer.current != null) mouseScreenPos = Pointer.current.position.ReadValue();
            Vector2 lumiScreenPos = mainCamera.WorldToScreenPoint(lumi.transform.position);
            float distanceInPixels = Vector2.Distance(mouseScreenPos, lumiScreenPos);

            Debug.Log($"🎯 [Distance Check] World: {worldDistance} | Pixels: {distanceInPixels}");

            // Trigger if close either in World Space or Screen Pixel Space
            if (distanceInPixels <= 250f || worldDistance <= 3.5f)
            {
                Animator lumiAnimator = lumi.GetComponent<Animator>();
                if (lumiAnimator != null)
                {
                    lumiAnimator.SetBool("isFlying", false);
                    lumiAnimator.Play("LumiThinking");
                }

                if (lumi.TryGetComponent<CharacterPersonality>(out var personality))
                {
                    personality.ProcessFedFoodItem("Kiwi Shake");
                }

                // 🌟 TODAY'S TASK: Spawn stars directly over Lumi's head
                TriggerStarsOverHead(lumi.transform);

                Debug.Log("🔥 [SUCCESS] Lumi successfully fed!");
                return true;
            }
        }
        else
        {
            Debug.LogError("⚠️ Could not locate LumiController in the scene!");
        }
        return false;
    }

    private void TriggerStarsOverHead(Transform targetCharacter)
    {
        if (starsParticleSystem != null && targetCharacter != null)
        {
            starsParticleSystem.transform.position = targetCharacter.position + starsOffset;
            starsParticleSystem.Stop();
            starsParticleSystem.Play();
        }
    }

    private void ApplyGravity()
    {
        verticalVelocity -= gravity * Time.deltaTime;

        Vector3 currentPos = transform.position;
        currentPos.y += verticalVelocity * Time.deltaTime;
        transform.position = currentPos;

        if (transform.position.y <= counterFloorY)
        {
            transform.position = new Vector3(transform.position.x, counterFloorY, transform.position.z);
            isFallingToCounter = false;
            verticalVelocity = 0f;
        }
    }

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

        // Instantly snap back to the exact initial layout position
        transform.position = initialStartingPosition;
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