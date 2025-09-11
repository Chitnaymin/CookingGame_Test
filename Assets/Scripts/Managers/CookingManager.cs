using System;
using System.Collections;
using UnityEngine;

public class CookingManager : MonoBehaviour {
    public static CookingManager Instance { get; private set; }

    public bool IsCooking { get; private set; }

    public event Action<FoodSO> OnCookingStarted;
    public event Action<float> OnCookingTick;
    public event Action<FoodSO, bool> OnCookingFinished;

    private FoodSO currentFood;
    private Coroutine cookingRoutine;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        // Start a coroutine to wait until InventoryManager is ready
        StartCoroutine(WaitForInventoryManager());
    }

    private IEnumerator WaitForInventoryManager() {
        // Wait until InventoryManager.Instance is not null
        while (InventoryManager.Instance == null) {
            yield return null; // wait one frame
        }

        // Optional: wait one more frame to ensure Start() of InventoryManager is finished
        yield return null;

        // Now safe to access player data
        var data = InventoryManager.Instance.playerData;
        Debug.Log("Reached after InventoryManager initialized...");

        if (!string.IsNullOrEmpty(data.cookingRecipeId) && data.cookingRemainingTime > 0) {
            Debug.Log("Resuming cooking...");
            FoodSO foodToResume = Resources.Load<FoodSO>("ScriptableObjects/Foods/" + data.cookingRecipeId);
            if (foodToResume != null) {
                ResumeCooking(foodToResume, data.cookingRemainingTime);
            } else {
                Debug.LogWarning("FoodSO not found for ID: " + data.cookingRecipeId);
            }
        }
    }

    /// <summary>
    /// Attempts to start the cooking process for a given food.
    /// </summary>
    /// <returns>True if cooking started successfully, false otherwise.</returns>
    public bool StartCooking(FoodSO food) {
        if (IsCooking) return false;
        if (!InventoryManager.Instance.HasIngredients(food.requirements)) return false;
        if (InventoryManager.Instance.playerData.currentEnergy < 10) return false;

        // --- All checks passed, proceed ---

        InventoryManager.Instance.UseIngredients(food.requirements);
        InventoryManager.Instance.ConsumeEnergy(10);

        IsCooking = true;
        currentFood = food;

        // Save the cooking state immediately, this is a critical action.
        var data = InventoryManager.Instance.playerData;
        data.cookingRecipeId = food.name;
        data.cookingRemainingTime = food.cookingTime;
        SaveSystem.Save(data);

        OnCookingStarted?.Invoke(food);
        cookingRoutine = StartCoroutine(CookingRoutine(food.cookingTime));
        return true;
    }

    /// <summary>
    /// Resumes a previously saved cooking session.
    /// </summary>
    private void ResumeCooking(FoodSO food, float timeLeft) {
        IsCooking = true;
        currentFood = food;
        OnCookingStarted?.Invoke(food);
        cookingRoutine = StartCoroutine(CookingRoutine(timeLeft));
    }

    /// <summary>
    /// The main timer coroutine for the cooking process. Respects game pause.
    /// </summary>
    private IEnumerator CookingRoutine(float duration) {
        float remainingTime = duration;
        float tickTimer = 0f;

        while (remainingTime > 0) {
            // Using Time.deltaTime ensures this timer pauses when Time.timeScale is 0.
            tickTimer += Time.deltaTime;

            if (tickTimer >= 1f) {
                tickTimer -= 1f;
                remainingTime -= 1f;

                OnCookingTick?.Invoke(remainingTime);

                // Update data in memory and flag for saving, but don't save every second.
                InventoryManager.Instance.playerData.cookingRemainingTime = remainingTime;
                SaveSystem.Save(InventoryManager.Instance.playerData);
            }
            yield return null; // Wait for the next frame
        }

        // --- Cooking is Finished ---
        IsCooking = false;

        var data = InventoryManager.Instance.playerData;
        data.cookingRecipeId = null;
        data.cookingRemainingTime = 0;
        SaveSystem.Save(data);

        OnCookingTick?.Invoke(0);
        OnCookingFinished?.Invoke(currentFood, true);
        currentFood = null;
    }
}