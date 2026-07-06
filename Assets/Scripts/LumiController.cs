using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class LumiController : MonoBehaviour
{
    [Header("🤖 Animation Settings")]
    [SerializeField] private string flyParameterName = "isFlying";
    [SerializeField] private string eatTriggerName = "Eat";
    [Tooltip("How many seconds the eating animation lasts before returning to normal.")]
    [SerializeField] private float eatAnimationDuration = 1.5f;

    [Header("✨ Drag & Follow Smoothing")]
    [SerializeField] private float followSmoothing = 25f;

    [Header("🍂 Gravity & Floor Settings")]
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float floorYCoord = -3.5f;

    [Header("🔴 Flight Threshold Zone")]
    [SerializeField] private float redLineYCoord = -0.5f;

    [Header("🍰 Eating Interaction Settings")]
    [Tooltip("How close an item needs to be to Lumi's center to trigger eating.")]
    [SerializeField] private float eatingDistanceThreshold = 1.2f;

    [Header("🎥 Edge Panning Settings")]
    [SerializeField] private float cameraScrollSpeed = 5f;

    // Core Component Cache
    private Animator myAnimator;
    private Collider2D myCollider;
    private Camera mainCamera;

    // Internal Drag & Physics States
    private bool isBeingDragged = false;
    private bool isFalling = false;
    private bool isEating = false;
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
            HandleEdgeScrolling();
        }

        if (isFalling && !isBeingDragged && !isEating)
        {
            ApplyGravity();
        }

        if (!isBeingDragged && !isEating)
        {
            CheckForFoodNearFace();
        }
    }

    private void HandleInputAndDrag()
    {
        if (isEating) return; // Ignore tracking while eating animation plays

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

    private void CheckForFoodNearFace()
    {
        IngredientController[] ingredients = FindObjectsByType<IngredientController>(FindObjectsSortMode.None);

        foreach (IngredientController item in ingredients)
        {
            if (!item.IsBeingDragged)
            {
                float distance = Vector2.Distance(transform.position, item.transform.position);
                if (distance <= eatingDistanceThreshold)
                {
                    StartCoroutine(EatItemRoutine(item.gameObject));
                    break;
                }
            }
        }
    }

    private IEnumerator EatItemRoutine(GameObject foodObject)
    {
        isEating = true;
        isFalling = false;
        verticalVelocity = 0f;

        // Extract the clean item name
        string foodName = foodObject.name.Replace("(Clone)", "").Trim();

        // 1. Recycle food instantly to your pool manager
        if (TrashAndPoolManager.Instance != null)
        {
            TrashAndPoolManager.Instance.RecycleToPool(foodObject);
        }
        else
        {
            Destroy(foodObject);
        }

        // 2. 🎬 PLAY THINKING ANIMATION DIRECTLY: Force-plays your custom state
        if (myAnimator != null)
        {
            // Turn off flying parameter so it doesn't fight the thinking loop
            myAnimator.SetBool(flyParameterName, false);

            // 🌟 Play the exact state name from your image_924ce0.png layout map!
            myAnimator.Play("LumiThinking");
        }

        // 3. 💾 PROCESS PERSONALITY STORAGE
        CharacterPersonality personalityEngine = GetComponent<CharacterPersonality>();
        if (personalityEngine != null)
        {
            personalityEngine.ProcessFedFoodItem(foodName);
        }

        // Wait here while she stands/floats thinking about her meal
        yield return new WaitForSeconds(eatAnimationDuration);

        isEating = false;

        // 4. Return to your regular state logic loops
        if (transform.position.y >= redLineYCoord)
        {
            if (myAnimator != null) myAnimator.SetBool(flyParameterName, true);
        }
        else
        {
            isFalling = true;
        }
    }

    private void HandleEdgeScrolling()
    {
        if (Pointer.current == null || mainCamera == null) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector3 viewportPos = mainCamera.ScreenToViewportPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector3 cameraPos = mainCamera.transform.position;

        float edgeThreshold = 0.12f;

        if (viewportPos.x >= 1.0f - edgeThreshold)
        {
            cameraPos.x += cameraScrollSpeed * Time.deltaTime;
        }
        else if (viewportPos.x <= edgeThreshold)
        {
            cameraPos.x -= cameraScrollSpeed * Time.deltaTime;
        }

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

        if (transform.position.y >= redLineYCoord)
        {
            isFalling = false;
            verticalVelocity = 0f;
            if (myAnimator != null) myAnimator.SetBool(flyParameterName, true);
        }
        else
        {
            isFalling = true;
        }
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