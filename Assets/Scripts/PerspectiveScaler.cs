using UnityEngine;

public class PerspectiveScaler : MonoBehaviour
{
    [Header("📐 Perspective Limits")]
    [SerializeField] private float floorTopY = -1.64f;
    [SerializeField] private float floorBottomY = -8.69f;

    [Header("🔍 Multiplier Tuning")]
    [SerializeField] private float minScaleMultiplier = 0.4f;
    [SerializeField] private float maxScaleMultiplier = 1.0f;

    private Vector3 baseLocalScale;
    private IngredientController ingredientController;

    void Awake()
    {
        baseLocalScale = transform.localScale;
        ingredientController = GetComponent<IngredientController>();
    }

    void Update()
    {
        if (ingredientController != null)
        {
            // 🌟 THE FIX: If the item is currently inside the fridge, OR it is NOT being dragged/falling,
            // shut down this world space perspective script completely!
            var slottedField = ingredientController.GetType().GetField("isSlottedInFridge", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool isSlotted = slottedField != null && (bool)slottedField.GetValue(ingredientController);

            // Access the public property we have in your script to check if it's being dragged
            bool isDragging = ingredientController.IsDragging;

            if (isSlotted || !isDragging)
            {
                return; // Do nothing! Let the fridge control the scale completely.
            }
        }

        // ── PERSPECTIVE RUNS ONLY WHEN DRAGGING HERE & THERE ──
        float t = Mathf.InverseLerp(floorTopY, floorBottomY, transform.position.y);
        float currentScaleMultiplier = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, t);

        if (currentScaleMultiplier < 1.0f)
        {
            currentScaleMultiplier = 1.0f;
        }

        transform.localScale = baseLocalScale * currentScaleMultiplier;
    }
}