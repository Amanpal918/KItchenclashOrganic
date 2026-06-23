using UnityEngine;

[CreateAssetMenu(fileName = "New Ingredient", menuName = "KitchenActivity/Ingredient")]
public class IngredientData : ScriptableObject
{
    public string ingredientName;
    public Sprite icon;
}