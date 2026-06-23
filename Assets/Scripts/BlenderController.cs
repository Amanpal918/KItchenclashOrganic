using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class BlenderController : MonoBehaviour, IDropHandler
{
    [Header("Data Configuration")]
    public List<RecipeData> allRecipes;
    public TMPro.TextMeshProUGUI uiFeedbackText;

    [Header("Blender Visuals")]
    public Image blenderImage;

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

    // IDropHandler - fires when ingredient is dropped on blender
    public void OnDrop(PointerEventData eventData)
    {
        UIDraggableIngredient dragged = eventData.pointerDrag.GetComponent<UIDraggableIngredient>();
        if (dragged != null)
        {
            AddIngredient(dragged.ingredientData);
        }
    }

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
            blenderImage.sprite = emptyBlenderSprite;
            return;
        }

        string check = "";
        foreach (var item in ingredientsInBlender)
            check += item.ingredientName.ToLower() + " ";

        if ((check.Contains("chocolate") || check.Contains("choclate")) && check.Contains("milk"))
            blenderImage.sprite = chocolateAndMilkInJarSprite;
        else if (check.Contains("chocolate") || check.Contains("choclate"))
            blenderImage.sprite = chocolateInJarSprite;
        else if (check.Contains("strawberry"))
            blenderImage.sprite = strawberryInJarSprite;
        else if (check.Contains("kiwi"))
            blenderImage.sprite = kiwiInJarSprite;
        else if (check.Contains("pineapple"))
            blenderImage.sprite = pineappleInJarSprite;
        else if (check.Contains("watermelon"))
            blenderImage.sprite = watermelonInJarSprite;
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
        blenderImage.sprite = lastSuccessfulRecipe.resultSprite;
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
        if (blenderImage != null) blenderImage.sprite = emptyBlenderSprite;
    }
}