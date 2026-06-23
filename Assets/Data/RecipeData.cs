using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Recipe", menuName = "KitchenActivity/Recipe")]
public class RecipeData : ScriptableObject
{
    public string resultName;
    public Sprite resultSprite;
    public List<IngredientData> requiredIngredients;
}