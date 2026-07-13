using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class BlenderController : MonoBehaviour
{
    [Header("Data Configuration")]
    public List<RecipeData> allRecipes;
    public TMPro.TextMeshProUGUI uiFeedbackText;

    [Header("Blender Visuals & Snapping")]
    public SpriteRenderer blenderSpriteRenderer;
    public Transform snapPoint;
    [SerializeField] private float snapSpeed = 8f;

    [Header("Blender Sprites")]
    public Sprite emptyBlenderSprite;
    public Sprite chocolateInJarSprite;
    public Sprite chocolateAndMilkInJarSprite;
    public Sprite strawberryInJarSprite;
    public Sprite kiwiInJarSprite;
    public Sprite pineappleInJarSprite;
    public Sprite watermelonInJarSprite;

    [Header("🥛 Custom Liquid Data References")]
    [Tooltip("Drag your raw Data_Milk ScriptableObject into this slot via the Inspector!")]
    public IngredientData milkIngredientData;

    private List<IngredientData> ingredientsInBlender = new List<IngredientData>();
    private RecipeData lastSuccessfulRecipe;
    private Collider2D myCollider;

    // 🌟 PROTECTED LAYOUT SYSTEM
    private Vector3 lockedLocalScale;
    private Transform lockedParentTransform;

    void Awake()
    {
        myCollider = GetComponent<Collider2D>();

        // 🌟 Cache the perfect layout scale and structure parameters on startup!
        lockedLocalScale = transform.localScale;
        lockedParentTransform = transform.parent;
    }

    void Update()
    {
        // ── BLUEY STYLE: TAP BLENDER TO BLEND ──
        if (InputPressed())
        {
            Vector3 worldPos = GetWorldPos();
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(worldPos.x, worldPos.y), Vector2.zero);

            if (hit.collider != null && hit.collider == myCollider)
            {
                if (ingredientsInBlender.Count > 0)
                {
                    OnBlendButtonPressed();
                }
            }
        }
    }

    public void SnapAndAddIngredient(IngredientController ingredient)
    {
        ingredient.enabled = false;
        StartCoroutine(AnimateSnapToMouth(ingredient));
    }

    private IEnumerator AnimateSnapToMouth(IngredientController ingredient)
    {
        Vector3 targetPos = snapPoint != null ? snapPoint.position : transform.position;
        targetPos.z = 0f;

        while (Vector3.Distance(ingredient.transform.position, targetPos) > 0.05f)
        {
            ingredient.transform.position = Vector3.Lerp(
                ingredient.transform.position,
                targetPos,
                Time.deltaTime * snapSpeed
            );
            yield return null;
        }

        if (ingredient.ingredientData != null)
        {
            AddIngredient(ingredient.ingredientData);
        }
        else if (ingredient.gameObject.name.ToLower().Contains("milk") && milkIngredientData != null)
        {
            AddIngredient(milkIngredientData);
        }

        if (ingredient.gameObject.name.Contains("(Clone)"))
        {
            Destroy(ingredient.gameObject);
        }
        else
        {
            ingredient.enabled = true;
            ingredient.transform.position += new Vector3(-1.5f, -0.5f, 0f);

            // 🌟 FORCE LOCK BEFORE THE CALL: Keep the blender parented and correctly scaled
            transform.SetParent(lockedParentTransform);
            transform.localScale = lockedLocalScale;

            ingredient.StopDrag();

            // 🌟 RE-LOCK AFTER THE CALL: Fixes any external unparenting leaks instantly!
            transform.SetParent(lockedParentTransform);
            transform.localScale = lockedLocalScale;
        }
    }

    public void AddLiquidIngredientDirect(string liquidName)
    {
        if (liquidName.ToLower() == "milk" && milkIngredientData != null)
        {
            if (!ingredientsInBlender.Contains(milkIngredientData))
            {
                AddIngredient(milkIngredientData);
            }
        }
    }

    public void AddIngredient(IngredientData data)
    {
        if (data == null) return;
        ingredientsInBlender.Add(data);
        Debug.Log("Added to Blender: " + data.ingredientName + " (Total: " + ingredientsInBlender.Count + ")");
        UpdateBlenderVisualProgress();
    }

    private void UpdateBlenderVisualProgress()
    {
        if (ingredientsInBlender.Count == 0)
        {
            blenderSpriteRenderer.sprite = emptyBlenderSprite;
            return;
        }

        string check = "";
        foreach (var item in ingredientsInBlender)
            if (item != null) check += item.ingredientName.ToLower() + " ";

        if ((check.Contains("chocolate") || check.Contains("choclate")) && check.Contains("milk"))
            blenderSpriteRenderer.sprite = chocolateAndMilkInJarSprite;
        else if (check.Contains("chocolate") || check.Contains("choclate"))
            blenderSpriteRenderer.sprite = chocolateInJarSprite;
        else if (check.Contains("strawberry"))
            blenderSpriteRenderer.sprite = strawberryInJarSprite;
        else if (check.Contains("kiwi"))
            blenderSpriteRenderer.sprite = kiwiInJarSprite;
        else if (check.Contains("pineapple"))
            blenderSpriteRenderer.sprite = pineappleInJarSprite;
        else if (check.Contains("watermelon"))
            blenderSpriteRenderer.sprite = watermelonInJarSprite;
    }

    public void OnBlendButtonPressed()
    {
        RecipeData matched = ValidateRecipe();
        if (matched != null)
        {
            if (uiFeedbackText != null) uiFeedbackText.text = "Success!";
            lastSuccessfulRecipe = matched;
            Invoke(nameof(DisplayFinalDrink), 1.5f);
        }
        else
        {
            if (uiFeedbackText != null) uiFeedbackText.text = "Recipe Failed!";
            ClearBlender();
        }
    }

    private void DisplayFinalDrink()
    {
        ingredientsInBlender.Clear();
        if (lastSuccessfulRecipe != null) blenderSpriteRenderer.sprite = lastSuccessfulRecipe.resultSprite;

        if (TryGetComponent<BlenderDragHandler>(out var dragHandler))
        {
            dragHandler.EnableBlenderDraggability(true);
        }
    }

    private RecipeData ValidateRecipe()
    {
        foreach (RecipeData recipe in allRecipes)
            if (IsMatchingRecipe(recipe)) return recipe;
        return null;
    }

    private bool IsMatchingRecipe(RecipeData recipe)
    {
        if (recipe.requiredIngredients.Count != ingredientsInBlender.Count) return false;
        List<IngredientData> checklist = new List<IngredientData>(ingredientsInBlender);
        foreach (var req in recipe.requiredIngredients)
        {
            if (checklist.Contains(req)) checklist.Remove(req);
            else return false;
        }
        return true;
    }

    public void ClearBlender()
    {
        ingredientsInBlender.Clear();
        if (blenderSpriteRenderer != null) blenderSpriteRenderer.sprite = emptyBlenderSprite;
    }

    private Vector3 GetWorldPos()
    {
        Vector2 screenPos = Vector2.zero;
        if (Pointer.current != null)
        {
            screenPos = Pointer.current.position.ReadValue();
        }
        if (screenPos == Vector2.zero) return transform.position;

        Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        world.z = 0f;
        return world;
    }

    private bool InputPressed()
    {
        if (Pointer.current != null)
        {
            return Pointer.current.press.wasPressedThisFrame;
        }
        return false;
    }
}