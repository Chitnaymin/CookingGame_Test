using UnityEngine;
using UnityEditor; // This is essential for all editor scripts
using System.Collections.Generic;
using System.IO;

public static class RecipeCreator {
    private const string SAVE_PATH = "Assets/Resources/ScriptableObjects/Foods";

    private const int TOTAL_RECIPES_TO_CREATE = 26;

    [MenuItem("Tools/Cooking Game/Create Dummy Recipes")]
    public static void CreateDummyRecipes() {
        string[] guids = AssetDatabase.FindAssets("t:IngredientSO");
        if (guids.Length == 0) {
            Debug.LogError("Recipe Creator Error: No IngredientSO assets found in the project. Please create some ingredients first.");
            return;
        }

        List<IngredientSO> allIngredients = new List<IngredientSO>();
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            allIngredients.Add(AssetDatabase.LoadAssetAtPath<IngredientSO>(path));
        }

        if (!Directory.Exists(SAVE_PATH)) {
            Directory.CreateDirectory(SAVE_PATH);
        }

        for (int i = 1; i <= TOTAL_RECIPES_TO_CREATE; i++) {
            FoodSO newFood = ScriptableObject.CreateInstance<FoodSO>();

            newFood.foodName = $"Food {i}";
            newFood.starCount = Random.Range(1, 4);
            newFood.cookingTime = Random.Range(10, 121);

            newFood.requirements = new List<IngredientRequirement>();
            int ingredientCount = Random.Range(1, Mathf.Min(4, allIngredients.Count + 1));

            List<IngredientSO> availableIngredients = new List<IngredientSO>(allIngredients);

            for (int j = 0; j < ingredientCount; j++) {
                int randIndex = Random.Range(0, availableIngredients.Count);
                IngredientSO chosenIngredient = availableIngredients[randIndex];
                availableIngredients.RemoveAt(randIndex);
                newFood.requirements.Add(new IngredientRequirement {
                    ingredient = chosenIngredient, amount = Random.Range(1, 6)
                });
            }

            if (newFood.requirements.Count > 0) {
                newFood.foodSprite = newFood.requirements[0].ingredient.icon;
            }

            string assetPath = $"{SAVE_PATH}/Food_{i}.asset";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(newFood, uniquePath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully created {TOTAL_RECIPES_TO_CREATE} new dummy FoodSO assets in the folder: {SAVE_PATH}");
    }
}