using UnityEngine;
using System;

/// <summary>
/// Central game manager responsible for global state such as pause/resume.
/// Uses a singleton pattern to ensure only one instance exists across scenes.
/// Also handles centralized saving logic for dirty data.
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    /// <summary>
    /// Indicates whether the game is currently paused.
    /// </summary>
    public bool IsPaused { get; private set; }

    // "Dirty Flag" for optimized saving
    private bool isDataDirty = false;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    #region Pause Methods

    /// <summary>
    /// Toggles the paused state of the game.
    /// Pausing sets Time.timeScale = 0, resuming sets it back to 1.
    /// </summary>
    public void TogglePause() {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    /// <summary>
    /// Forcefully pause the game (if not already).
    /// </summary>
    public void PauseGame() {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log("[GameManager] Game Paused");
    }

    /// <summary>
    /// Forcefully resume the game (if paused).
    /// </summary>
    public void ResumeGame() {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        Debug.Log("[GameManager] Game Resumed");
    }

    #endregion

    #region Saving Logic

    /// <summary>
    /// Marks the game data as "dirty" so it will be saved on pause or quit.
    /// </summary>
    public void MarkDataDirty() {
        isDataDirty = true;
    }

    private void OnApplicationPause(bool pauseStatus) {
        if (pauseStatus) SaveIfDirty();
    }

    private void OnApplicationQuit() {
        SaveIfDirty();
    }

    /// <summary>
    /// Saves player data if anything is dirty.
    /// </summary>
    private void SaveIfDirty() {
        if (!isDataDirty) return;

        Debug.Log("[GameManager] Saving data before exit...");

        if (InventoryManager.Instance != null) {
            InventoryManager.Instance.playerData.lastSessionEndTimeTicks = DateTime.UtcNow.Ticks;
            SaveSystem.Save(InventoryManager.Instance.playerData);
        }

        isDataDirty = false;
    }

    #endregion
}