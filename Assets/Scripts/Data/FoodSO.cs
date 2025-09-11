using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct IngredientRequirement
{
    public IngredientSO ingredient;
    public int amount;
}

[CreateAssetMenu(fileName = "Food", menuName = "CookingGame/Food")]
public class FoodSO : ScriptableObject {
    public string foodName;
    public Sprite foodSprite;
    public int starCount;
    public float cookingTime = 10;
    
    public List<IngredientRequirement> requirements;
}