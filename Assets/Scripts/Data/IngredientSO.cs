using UnityEngine;

[CreateAssetMenu(fileName = "Ingredient", menuName = "CookingGame/Ingredient")]
public class IngredientSO : ScriptableObject {
    public string ingredientId;
    public Sprite icon;
}