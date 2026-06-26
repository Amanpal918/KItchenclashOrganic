using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))] // Needs a 2D Trigger Collider to detect dropped ingredients
public class BlenderController : MonoBehaviour
{
    [Header("Data Configuration")]
    public List<RecipeData> allRecipes;
    public TMPro.TextMeshProUGUI uiFeedbackText;

    [Header("Blender Visuals")]
    public SpriteRenderer blenderSpriteRenderer; // Changed from UI Image to SpriteRenderer

    [Header("Blender Sprites")]
    public Sprite emptyBlenderSprite;
    public Sprite chocolateInJarSprite;
    public Sprite chocolateAndMilkInJarSprite;
    public Sprite strawberryInJarSprite;
    public Sprite kiwiInJarSprite;
    public Sprite pineappleInJarSprite;
    public Sprite watermelonInJarSprite;

    private List<IngredientData> ingredientsInBlender = new List<IngredientData>();
    private RecipeData lastSuccessfulRecipe;

    public void AddIngredient(IngredientData data)
    {
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
            check += item.ingredientName.ToLower() + " ";

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
        blenderSpriteRenderer.sprite = lastSuccessfulRecipe.resultSprite;
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
}
