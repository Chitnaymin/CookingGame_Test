using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#region Data Models

/// <summary>
/// Represents one ingredient and its current count in the player's inventory.
/// </summary>
[Serializable]
public class IngredientEntry {
    public string id; // Unique identifier (e.g. "egg", "rice")
    public int count; // Current amount
}

/// <summary>
/// Serializable player profile containing energy, ingredients, and cooking state.
/// This acts as the single source of truth for persistent player data.
/// </summary>
[Serializable]
public class PlayerData {
    [Header("Energy")]
    public int currentEnergy;
    public int maxEnergy;
    public float energyRegenTimer = 0f; // Partial progress toward next energy point
    public long lastSessionEndTimeTicks; // Tracks when the player quit

    [Header("Inventory")]
    public List<IngredientEntry> ingredients;

    [Header("Cooking State")]
    public string cookingRecipeId; // Currently cooking recipe (null if none)
    public float cookingRemainingTime; // Remaining time in seconds

    /// <summary>
    /// Default constructor initializes new player with starter values.
    /// </summary>
    public PlayerData() {
        currentEnergy = 100;
        maxEnergy = 100;
        energyRegenTimer = 0f;

        cookingRecipeId = null;
        cookingRemainingTime = 0f;

        ingredients = new List<IngredientEntry> {
            new IngredientEntry {
                id = "egg", count = 50
            },
            new IngredientEntry {
                id = "vegetable", count = 50
            },
            new IngredientEntry {
                id = "rice", count = 50
            },
            new IngredientEntry {
                id = "carrot", count = 50
            }
        };
    }
}

#endregion

#region Save System

/// <summary>
/// Simple JSON-based persistence system for saving and loading PlayerData.
/// </summary>
public static class SaveSystem {
    private static string SavePath => Path.Combine(Application.persistentDataPath, "playerdata.json");

    /// <summary>
    /// Serializes PlayerData to JSON and writes it to persistent storage.
    /// </summary>
    public static void Save(PlayerData data) {
        Debug.Log("Saving player data"+SavePath);
        try {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveSystem] PlayerData saved to {SavePath}");
        } catch (Exception e) {
            Debug.LogError($"[SaveSystem] Failed to save data: {e.Message}");
        }
    }

    /// <summary>
    /// Loads PlayerData from persistent storage.
    /// If no save file exists or load fails, returns a fresh PlayerData instance.
    /// </summary>
    public static PlayerData Load() {
        if (File.Exists(SavePath)) {
            try {
                string json = File.ReadAllText(SavePath);
                return JsonUtility.FromJson<PlayerData>(json);
            } catch (Exception e) {
                Debug.LogError($"[SaveSystem] Failed to load data: {e.Message}");
                return new PlayerData();
            }
        } else {
            Debug.Log("[SaveSystem] No save file found. Creating new PlayerData.");
            return new PlayerData();
        }
    }
}

#endregion