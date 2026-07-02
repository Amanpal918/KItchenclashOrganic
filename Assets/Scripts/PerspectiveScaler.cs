using UnityEngine;

public class PerspectiveScaler : MonoBehaviour
{
    [Header("📐 Perspective Limits")]
    [SerializeField] private float floorTopY = -1.64f;
    [SerializeField] private float floorBottomY = -8.69f;

    [Header("🔍 Multiplier Tuning")]
    [SerializeField] private float minScaleMultiplier = 0.4f; // Smallest size at the back wall
    [SerializeField] private float maxScaleMultiplier = 1.0f; // Largest size at the front counter

    private Vector3 baseLocalScale;
    private IngredientController ingredientController;
    private bool isBaseScaleCaptured = false;

    void Awake()
    {
        CaptureBaseScale();
        ingredientController = GetComponent<IngredientController>();
    }

    // Public method so the IngredientSource script can force capture the unscaled asset size instantly on instantiation
    public void CaptureBaseScale()
    {
        if (isBaseScaleCaptured) return;
        baseLocalScale = transform.localScale;
        isBaseScaleCaptured = true;
    }

    void Update()
    {
        if (ingredientController != null)
        {
            var slottedField = ingredientController.GetType().GetField("isSlottedInFridge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isSlotted = slottedField != null && (bool)slottedField.GetValue(ingredientController);

            // 🌟 FIXED: REMOVED !isDragging from the return check!
            // We ONLY turn off the script if the item is physically snapped inside the fridge.
            if (isSlotted)
            {
                return;
            }
        }

        // ── PERSPECTIVE MATH RUNS CONTINUOUSLY IN THE ROOM NOW ──
        float t = Mathf.InverseLerp(floorTopY, floorBottomY, transform.position.y);
        float currentScaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);

        // Keep it safely clamped within your tuning thresholds
        currentScaleMultiplier = Mathf.Clamp(currentScaleMultiplier, minScaleMultiplier, maxScaleMultiplier);

        transform.localScale = baseLocalScale * currentScaleMultiplier;
    }
}