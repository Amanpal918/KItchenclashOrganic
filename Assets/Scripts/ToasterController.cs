using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; // 🌟 Modern Input System Hook

public class ToasterController : MonoBehaviour
{
    public enum ToasterState { Empty, HasRawBread, Cooking, HasCookedToast }

    [Header("📊 Current System State")]
    public ToasterState currentState = ToasterState.Empty;

    [Header("🖼️ Sprite Configuration")]
    [SerializeField] private SpriteRenderer toasterSpriteRenderer;
    [SerializeField] private Sprite emptyToasterSprite;
    [SerializeField] private Sprite rawBreadToasterSprite;
    [SerializeField] private Sprite cookedToastToasterSprite;

    [Header("⏳ Cooking Settings")]
    [SerializeField] private float cookingDuration = 3f;

    [Header("🍞 Spawnable Prefab")]
    [SerializeField] private GameObject cookedToastPrefab;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (toasterSpriteRenderer == null) toasterSpriteRenderer = GetComponent<SpriteRenderer>();
        UpdateToasterVisuals();
        Debug.Log($"[Toaster Initialization] Toaster started in state: {currentState}");
    }

    void Update()
    {
        // 🌟 NEW INPUT SYSTEM CLICK HANDLER: Replaces old OnMouseDown completely!
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));

            // Look directly at what collider your finger clicked on
            Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos);
            if (hitCollider != null && hitCollider.gameObject == gameObject)
            {
                OnToasterClicked();
            }
        }
    }

    private void OnToasterClicked()
    {
        Debug.Log($"[Toaster Click] Player clicked toaster. Current State: {currentState}");

        if (currentState == ToasterState.HasRawBread)
        {
            StartCoroutine(CookBreadRoutine());
        }
        else if (currentState == ToasterState.HasCookedToast)
        {
            ExtractCookedToast();
        }
        else if (currentState == ToasterState.Cooking)
        {
            Debug.Log("⏳ [Toaster Click] Toaster is busy cooking right now!");
        }
    }

    public bool InsertRawBread()
    {
        if (currentState != ToasterState.Empty) return false;

        currentState = ToasterState.HasRawBread;
        UpdateToasterVisuals();
        Debug.Log("🎯 [Toaster Action] Raw bread inserted! Click the toaster to cook.");
        return true;
    }

    private IEnumerator CookBreadRoutine()
    {
        currentState = ToasterState.Cooking;
        UpdateToasterVisuals();
        Debug.Log($"⏰ [Toaster Timer] Toasting... waiting {cookingDuration} seconds...");

        yield return new WaitForSeconds(cookingDuration);

        currentState = ToasterState.HasCookedToast;
        UpdateToasterVisuals();
        Debug.Log("🔔 [Toaster Timer] *DING!* Toast popped up! Click again to extract.");
    }

    private void ExtractCookedToast()
    {
        if (cookedToastPrefab != null)
        {
            Vector3 spawnPos = transform.position + new Vector3(0, 0.75f, 0);
            GameObject freshToast = Instantiate(cookedToastPrefab, spawnPos, Quaternion.identity);

            if (freshToast.TryGetComponent<BreadDragHandler>(out var dragHandler))
            {
                dragHandler.ForceStartDragOnSpawn();
            }
        }
        else
        {
            Debug.LogError("❌ [Toaster Error] Cooked Toast Prefab is missing in the Inspector!");
        }

        currentState = ToasterState.Empty;
        UpdateToasterVisuals();
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