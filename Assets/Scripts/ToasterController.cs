using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class ToasterController : MonoBehaviour
{
    public enum ToasterState { Empty, HasRawBread, Cooking, HasCookedToast }

    [Header("📊 Current System State")]
    public ToasterState currentState = ToasterState.Empty;

    [Header("🖼️ Sprite Configuration")]
    [SerializeField] private SpriteRenderer toasterSpriteRenderer;
    [SerializeField] private Sprite emptyToasterSprite;       // 1st Image (Empty)
    [SerializeField] private Sprite rawBreadToasterSprite;     // 2nd Image (With Raw Bread)
    [SerializeField] private Sprite cookedToastToasterSprite;  // 3rd Image (With Toasted Bread)

    [Header("⏳ Cooking Settings")]
    [SerializeField] private float cookingDuration = 2f;      // Fixed precisely to 2 seconds!

    [Header("🍞 Spawnable Prefab")]
    [SerializeField] private GameObject cookedToastPrefab;    // Your Toast Ingredient Prefab

    private Camera mainCamera;
    private Collider2D myCollider;
    private bool isExtracting = false; // 🌟 FLAG: Prevents the toaster from re-absorbing the toast instantly on spawn!

    void Start()
    {
        mainCamera = Camera.main;
        myCollider = GetComponent<Collider2D>();
        if (toasterSpriteRenderer == null) toasterSpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateToasterVisuals();
    }

    void Update()
    {
        // ── HANDLE TOASTER CLICKS (NEW INPUT SYSTEM) ──
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Pointer.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));

            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);
            if (hitCollider != null && hitCollider == myCollider)
            {
                OnToasterClicked();
            }
        }
    }

    private void OnToasterClicked()
    {
        if (currentState == ToasterState.HasRawBread)
        {
            StartCoroutine(CookBreadRoutine());
        }
        else if (currentState == ToasterState.HasCookedToast)
        {
            ExtractAndDragCookedToast();
        }
    }

    // ── TRIGGER ZONE OVERLAP INTERACTION ──
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 🌟 SAFETY CHECK: Block insertion if the toaster isn't empty OR if we are currently spawning toast out
        if (currentState != ToasterState.Empty || isExtracting) return;

        if (other.TryGetComponent<IngredientController>(out var ingredient))
        {
            if (ingredient.gameObject.name.ToLower().Contains("bread"))
            {
                if (ingredient.IsDragging)
                {
                    ingredient.StopDrag();
                }

                Destroy(ingredient.gameObject);
                InsertRawBread();
            }
        }
    }

    public void InsertRawBread()
    {
        currentState = ToasterState.HasRawBread;
        UpdateToasterVisuals();
        Debug.Log("🎯 Raw bread inserted! Click toaster to start the 2-second timer.");
    }

    private IEnumerator CookBreadRoutine()
    {
        currentState = ToasterState.Cooking;
        UpdateToasterVisuals();

        yield return new WaitForSeconds(cookingDuration);

        currentState = ToasterState.HasCookedToast;
        UpdateToasterVisuals();
        Debug.Log("🔔 *DING!* Toast popped up! Drag it to extract.");
    }

    private void ExtractAndDragCookedToast()
    {
        if (cookedToastPrefab != null)
        {
            StartCoroutine(ExtractionSafetyRoutine()); // 🌟 Run the safety gate handling logic
        }
        else
        {
            // Reset to empty safely if prefab reference was dropped
            currentState = ToasterState.Empty;
            UpdateToasterVisuals();
        }
    }

    // 🌟 COROUTINE SAFETY GATE: Handles spawning, pointer injection, and temporary trigger safety windows cleanly
    private IEnumerator ExtractionSafetyRoutine()
    {
        isExtracting = true; // Lock the trigger entry gate

        Vector3 spawnPos = transform.position + new Vector3(0f, 0.75f, 0f);
        GameObject freshToast = Instantiate(cookedToastPrefab, spawnPos, Quaternion.identity);

        if (freshToast.TryGetComponent<IngredientController>(out var ingredientController))
        {
            ingredientController.isSourceDispenser = false;

            Vector2 pointerScreen = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
            Vector3 pointerWorld = mainCamera.ScreenToWorldPoint(new Vector3(pointerScreen.x, pointerScreen.y, 0f));

            ingredientController.StartDragFromSource(pointerWorld);
        }

        // Return toaster back to the 1st Empty Sprite layout frame instantly
        currentState = ToasterState.Empty;
        UpdateToasterVisuals();

        // Wait a tiny fraction of a second (1 frame or 0.1s) for the toast to be dragged safely out of the collider zone
        yield return new WaitForSeconds(0.1f);

        isExtracting = false; // Re-open the trigger gate for next use
    }

    private void UpdateToasterVisuals()
    {
        if (toasterSpriteRenderer == null) return;

        switch (currentState)
        {
            case ToasterState.Empty:
                toasterSpriteRenderer.sprite = emptyToasterSprite;
                break;
            case ToasterState.HasRawBread:
            case ToasterState.Cooking:
                toasterSpriteRenderer.sprite = rawBreadToasterSprite;
                break;
            case ToasterState.HasCookedToast:
                toasterSpriteRenderer.sprite = cookedToastToasterSprite;
                break;
        }
    }
}