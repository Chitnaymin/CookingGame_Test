using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
    public static InventoryManager Instance { get; private set; }

    [Header("Data")]
    public PlayerData playerData;

    public event Action OnInventoryChanged;
    public event Action<int, int> OnEnergyChanged;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
            return;
        }
    }

    private void Start() {
        // Load player data
        playerData = SaveSystem.Load();

        // Calculate offline progress before starting the normal timer
        CalculateOfflineProgress();

        // Start the normal regeneration coroutine
        StartCoroutine(RegenerateEnergy());

        // Update UI with initial values
        OnEnergyChanged?.Invoke(playerData.currentEnergy, playerData.maxEnergy);
    }

    #region Inventory Methods

    /// <summary>
    /// Gets the current count of a specific ingredient.
    /// </summary>
    /// <returns>The count, or 0 if the player has none.</returns>
    public int GetIngredientCount(string id) {
        var entry = playerData.ingredients.Find(i => i.id == id);
        return entry?.count ?? 0;
    }

    /// <summary>
    /// Checks if the player possesses all ingredients for a recipe.
    /// </summary>
    public bool HasIngredients(List<IngredientRequirement> requirements) {
        foreach (var req in requirements) {
            if (GetIngredientCount(req.ingredient.ingredientId) < req.amount)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Deducts ingredients from the player's inventory.
    /// </summary>
    public void UseIngredients(List<IngredientRequirement> requirements) {
        foreach (var req in requirements) {
            var entry = playerData.ingredients.Find(i => i.id == req.ingredient.ingredientId);
            if (entry != null) {
                entry.count -= req.amount;
            }
        }

        OnInventoryChanged?.Invoke();

        // Flag for saving, don't save immediately
        GameManager.Instance.MarkDataDirty();
    }

    /// <summary>
    /// Consumes a set amount of energy.
    /// </summary>
    public void ConsumeEnergy(int amount) {
        playerData.currentEnergy = Mathf.Max(0, playerData.currentEnergy - amount);
        OnEnergyChanged?.Invoke(playerData.currentEnergy, playerData.maxEnergy);

        // Flag for saving
        GameManager.Instance.MarkDataDirty();
    }

    #endregion

    #region Offline & Regeneration Logic

    /// <summary>
    /// Calculates energy earned while the game was closed.
    /// </summary>
    private void CalculateOfflineProgress() {
        if (playerData.lastSessionEndTimeTicks == 0) return;

        DateTime lastTime = new DateTime(playerData.lastSessionEndTimeTicks);
        TimeSpan timeOffline = DateTime.UtcNow - lastTime;

        if (timeOffline.TotalSeconds <= 0) return;

        Debug.Log($"Player was offline for {timeOffline.TotalMinutes:F1} minutes.");

        // Add the previously saved timer progress to the total offline time
        double totalSecondsToProcess = timeOffline.TotalSeconds + playerData.energyRegenTimer;
        int energyTicksEarned = (int)(totalSecondsToProcess / 5.0);

        if (energyTicksEarned > 0) {
            playerData.currentEnergy = Mathf.Min(playerData.maxEnergy, playerData.currentEnergy + energyTicksEarned);
            Debug.Log($"Awarded {energyTicksEarned} offline energy.");
        }

        // Store the remainder for the next tick
        playerData.energyRegenTimer = (float)(totalSecondsToProcess % 5.0);
        GameManager.Instance.MarkDataDirty();
    }

    /// <summary>
    /// Coroutine to regenerate energy over time.
    /// </summary>
    private IEnumerator RegenerateEnergy() {
        const float regenInterval = 5f;

        while (true) {
            // Only run if the game is not paused
            if (Time.timeScale > 0) {
                playerData.energyRegenTimer += Time.deltaTime;

                if (playerData.energyRegenTimer >= regenInterval) {
                    playerData.energyRegenTimer -= regenInterval;

                    if (playerData.currentEnergy < playerData.maxEnergy) {
                        playerData.currentEnergy++;
                        OnEnergyChanged?.Invoke(playerData.currentEnergy, playerData.maxEnergy);

                        // Mark as dirty for saving
                        GameManager.Instance.MarkDataDirty();
                    }
                }
            }

            yield return null; // Wait for the next frame
        }
    }

    #endregion
}