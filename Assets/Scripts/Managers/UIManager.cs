using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Spine.Unity;

/// <summary>
/// Manages all UI elements, user input, and visual feedback for the cooking game.
/// Acts as the View-Controller, listening to events from other managers and updating the display.
/// </summary>
public class UIManager : MonoBehaviour {
    #region UI References

    [Header("Panels")]
    [SerializeField] private GameObject cookingPanel;
    [SerializeField] private GameObject filterPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Main Buttons")]
    [SerializeField] private Button btnOpenCookingPanel;
    [SerializeField] private Button btnCloseCookingPanel;
    [SerializeField] private Button btnCook;
    [SerializeField] private Button btnPauseGame;
    [SerializeField] private Button btnCloseResultPanel;

    [Header("Displays")]
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private Slider energySlider;
    [SerializeField] private TMP_Text txtCookingTimer;

    [Header("Food List & Pagination")]
    [SerializeField] private Transform foodListContainer;
    [SerializeField] private GameObject foodItemPrefab;
    [SerializeField] private Button btnLeftArrow;
    [SerializeField] private Button btnRightArrow;
    [SerializeField] private Transform pageIndicatorsContainer;
    [SerializeField] private GameObject pageIndicatorPrefab;
    [SerializeField] private Sprite activeIndicatorSprite;
    [SerializeField] private Sprite inactiveIndicatorSprite;

    [Header("Ingredient Display")]
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private GameObject ingredientPrefab;
    [SerializeField] private string sufficientColorHex = "#FFFFFF";
    [SerializeField] private string insufficientColorHex = "#F84960";

    [Header("Search & Filter")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button btnFilterToggle;
    [SerializeField] private Button btnFilter1Star;
    [SerializeField] private Button btnFilter2Star;
    [SerializeField] private Button btnFilter3Star;
    [SerializeField] private Image imgFilter1Star;
    [SerializeField] private Image imgFilter2Star;
    [SerializeField] private Image imgFilter3Star;
    [SerializeField] private Sprite selectedFilterSprite;
    [SerializeField] private Sprite unselectedFilterSprite;

    [Header("Result Panel")]
    [SerializeField] private Image imgFoodResult;
    [SerializeField] private TMP_Text txtResultFood;

    [Header("Pause GFX")]
    [SerializeField] private Sprite imgPause;
    [SerializeField] private Sprite imgUnpause;

    [Header("Spine Animation")]
    [SerializeField] private SkeletonGraphic cookingPotAnimation;

    #endregion

    #region State Variables

    private FoodSO selectedFood;
    private FoodItem currentSelectedFoodItem;
    private Coroutine cookingAnimationCoroutine;

    // Pooling
    private List<GameObject> ingredientPool = new List<GameObject>();
    private List<Image> pageIndicatorPool = new List<Image>();

    // Pagination
    private const int ITEMS_PER_PAGE = 4;
    private int currentPage = 0;
    private int totalPages = 0;
    private List<FoodItem> allFoodItems = new List<FoodItem>();

    // Filtering
    private List<FoodSO> allLoadedFoods = new List<FoodSO>();
    private string currentSearchQuery = "";
    private int currentStarFilter = 0;

    #endregion

    #region Unity Lifecycle

    private void Start() {
        setupButtonListeners();
        subscribeToEvents();
        setInitialState();
        loadAndDisplayInitialRecipes();
    }

    private void OnDestroy() {
        unsubscribeFromEvents();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Wires up all button OnClick events to their handler methods.
    /// </summary>
    private void setupButtonListeners() {
        btnOpenCookingPanel.onClick.AddListener(onClickedBtnShowCookingPanel);
        btnCloseCookingPanel.onClick.AddListener(onClickedBtnHideCookingPanel);
        btnCook.onClick.AddListener(onClickedBtnCook);
        btnLeftArrow.onClick.AddListener(onClickedBtnLeftArrow);
        btnRightArrow.onClick.AddListener(onClickedBtnRightArrow);
        btnCloseResultPanel.onClick.AddListener(() => resultPanel.SetActive(false));
        btnPauseGame.onClick.AddListener(onClickedBtnPause);

        searchInput.onValueChanged.AddListener(handleSearchQueryChanged);
        btnFilterToggle.onClick.AddListener(handleFilterButtonClicked);
        btnFilter1Star.onClick.AddListener(() => setStarFilter(1));
        btnFilter2Star.onClick.AddListener(() => setStarFilter(2));
        btnFilter3Star.onClick.AddListener(() => setStarFilter(3));
    }

    /// <summary>
    /// Subscribes to events from other managers to enable a decoupled architecture.
    /// </summary>
    private void subscribeToEvents() {
        InventoryManager.Instance.OnInventoryChanged += HandleInventoryChanged;
        InventoryManager.Instance.OnEnergyChanged += UpdateEnergyUI;
        CookingManager.Instance.OnCookingStarted += HandleCookingStarted;
        CookingManager.Instance.OnCookingTick += UpdateCookingTimer;
        CookingManager.Instance.OnCookingFinished += OnCookingFinished_Wrapper;
    }

    /// <summary>
    /// Unsubscribes from all events to prevent memory leaks when the object is destroyed.
    /// </summary>
    private void unsubscribeFromEvents() {
        if (InventoryManager.Instance != null) {
            InventoryManager.Instance.OnInventoryChanged -= HandleInventoryChanged;
            InventoryManager.Instance.OnEnergyChanged -= UpdateEnergyUI;
        }
        if (CookingManager.Instance != null) {
            CookingManager.Instance.OnCookingStarted -= HandleCookingStarted;
            CookingManager.Instance.OnCookingTick -= UpdateCookingTimer;
            CookingManager.Instance.OnCookingFinished -= OnCookingFinished_Wrapper;
        }
    }

    /// <summary>
    /// Sets the default state of all UI elements at the start of the game.
    /// </summary>
    private void setInitialState() {
        cookingPanel.SetActive(false);
        txtCookingTimer.text = FormatTime(0);
        btnCook.interactable = false;
        filterPanel.SetActive(false);
        resultPanel.SetActive(false);
        pausePanel.SetActive(false);

        if (cookingPotAnimation != null)
            cookingPotAnimation.AnimationState.SetAnimation(0, "idle", true);

        UpdateEnergyUI(InventoryManager.Instance.playerData.currentEnergy, InventoryManager.Instance.playerData.maxEnergy);
        UpdateFilterButtonGFX();
    }

    #endregion

    #region UI Population & Filtering

    /// <summary>
    /// Loads all FoodSO assets from the Resources folder and triggers the initial UI build.
    /// </summary>
    private void loadAndDisplayInitialRecipes() {
        allLoadedFoods = Resources.LoadAll<FoodSO>("ScriptableObjects/Foods").ToList();
        ApplyFiltersAndRefreshUI();
    }

    /// <summary>
    /// Applies the current search and star filters to the master list and rebuilds the UI display.
    /// This is the central method for refreshing the recipe list.
    /// </summary>
    private void ApplyFiltersAndRefreshUI() {
        IEnumerable<FoodSO> filteredList = allLoadedFoods;

        if (!string.IsNullOrEmpty(currentSearchQuery)) {
            string searchQueryLower = currentSearchQuery.ToLower();
            filteredList = filteredList.Where(food => food.foodName.ToLower().Contains(searchQueryLower));
        }

        if (currentStarFilter > 0) {
            filteredList = filteredList.Where(food => food.starCount == currentStarFilter);
        }

        filteredList = filteredList.OrderBy(food => food.starCount);
        PopulateUIFromList(filteredList.ToList());
    }

    /// <summary>
    /// Clears the existing recipe list and rebuilds it from a provided list of foods.
    /// This is what makes the system data-driven and scalable.
    /// </summary>
    private void PopulateUIFromList(List<FoodSO> foodsToDisplay) {
        // Clear old UI items.
        allFoodItems.Clear();
        foreach (Transform child in foodListContainer) Destroy(child.gameObject);

        // Instantiate a UI item for each food in the provided list.
        foreach (var foodSO in foodsToDisplay) {
            GameObject foodGO = Instantiate(foodItemPrefab, foodListContainer);
            FoodItem foodItem = foodGO.GetComponent<FoodItem>();
            if (foodItem != null) {
                foodItem.BindFoodData(foodSO, OnFoodItemSelected);
                allFoodItems.Add(foodItem);
                foodGO.SetActive(false); // Deactivate until its page is shown.
            }
        }

        // Recalculate pagination and display the first page.
        totalPages = Mathf.CeilToInt((float)allFoodItems.Count / ITEMS_PER_PAGE);
        UpdatePageIndicators();
        ShowPage(0);
    }

    #endregion

    #region Pagination
    /// <summary>
    /// Displays a specific page of recipes by activating/deactivating the UI items.
    /// </summary>
    private void ShowPage(int pageIndex) {
        currentPage = pageIndex;
        int startIndex = currentPage * ITEMS_PER_PAGE;
        int endIndex = startIndex + ITEMS_PER_PAGE;

        for (int i = 0; i < allFoodItems.Count; i++) {
            allFoodItems[i].gameObject.SetActive(i >= startIndex && i < endIndex);
        }

        btnLeftArrow.interactable = (currentPage > 0);
        btnRightArrow.interactable = (currentPage < totalPages - 1);

        // Update indicator visuals
        for (int i = 0; i < pageIndicatorPool.Count; i++) {
            if (i < totalPages) {
                pageIndicatorPool[i].gameObject.SetActive(true);
                pageIndicatorPool[i].sprite = (i == currentPage) ? activeIndicatorSprite : inactiveIndicatorSprite;
            } else {
                pageIndicatorPool[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Ensures the page indicator pool has enough items for the current number of pages.
    /// </summary>
    private void UpdatePageIndicators() {
        // Create new indicators if needed
        while (pageIndicatorPool.Count < totalPages) {
            GameObject indicatorGO = Instantiate(pageIndicatorPrefab, pageIndicatorsContainer);
            pageIndicatorPool.Add(indicatorGO.GetComponent<Image>());
        }
    }

    #endregion

    #region Animation Control
    /// <summary>
    /// A coroutine that loops through a sequence of cooking animations.
    /// </summary>
    private IEnumerator CookingAnimationLoop() {
        Spine.Animation idleAnim = cookingPotAnimation.Skeleton.Data.FindAnimation("idle");
        Spine.Animation idleBoiledAnim = cookingPotAnimation.Skeleton.Data.FindAnimation("idle-boiled");
        Spine.Animation idle2Anim = cookingPotAnimation.Skeleton.Data.FindAnimation("idle2");

        while (true) {
            cookingPotAnimation.AnimationState.SetAnimation(0, idleAnim, false);
            yield return new WaitForSeconds(idleAnim.Duration);

            cookingPotAnimation.AnimationState.SetAnimation(0, idleBoiledAnim, false);
            yield return new WaitForSeconds(idleBoiledAnim.Duration);

            cookingPotAnimation.AnimationState.SetAnimation(0, idle2Anim, false);
            yield return new WaitForSeconds(idle2Anim.Duration);
        }
    }

    #endregion

    #region Event Handlers
    /// <summary>
    /// A wrapper to start the HandleCookingFinished coroutine from an event.
    /// </summary>
    private void OnCookingFinished_Wrapper(FoodSO food, bool isSuccess) {
        StartCoroutine(HandleCookingFinished(food, isSuccess));
    }

    /// <summary>
    /// Called when the CookingManager successfully starts cooking a meal.
    /// </summary>
    private void HandleCookingStarted(FoodSO food) {
        btnCook.interactable = false;
        if (cookingPotAnimation != null) {
            if (cookingAnimationCoroutine != null) StopCoroutine(cookingAnimationCoroutine);
            cookingAnimationCoroutine = StartCoroutine(CookingAnimationLoop());
        }
    }

    /// <summary>
    /// A coroutine that handles the sequence of events when cooking is finished.
    /// </summary>
    private IEnumerator HandleCookingFinished(FoodSO food, bool isSuccess) {
        if (cookingAnimationCoroutine != null) {
            StopCoroutine(cookingAnimationCoroutine);
            cookingAnimationCoroutine = null;
        }

        txtCookingTimer.text = FormatTime(0);
        float animationDuration = 0;

        if (cookingPotAnimation != null) {
            var animToPlay = isSuccess ? "success" : "unsuccess";
            var trackEntry = cookingPotAnimation.AnimationState.SetAnimation(0, animToPlay, false);
            animationDuration = trackEntry.Animation.Duration;
            cookingPotAnimation.AnimationState.AddAnimation(0, "idle", true, 0);
        }

        if (animationDuration > 0) {
            yield return new WaitForSeconds(animationDuration);
        }

        if (isSuccess) {
            imgFoodResult.sprite = food.foodSprite;
            txtResultFood.text = $"{food.foodName} is ready!";
            resultPanel.SetActive(true);
        }

        HandleInventoryChanged();
    }

    private void HandleInventoryChanged() {
        if (selectedFood != null) {
            bool canAfford = InventoryManager.Instance.HasIngredients(selectedFood.requirements);
            btnCook.interactable = canAfford && !CookingManager.Instance.IsCooking;
            UpdateIngredientDisplay(selectedFood);
        }
    }

    #endregion

    #region Button Clicks

    private void onClickedBtnShowCookingPanel() {
        cookingPanel.SetActive(true);
        // Reset state when opening
        txtCookingTimer.text = FormatTime(0);
        ClearIngredientDisplay();
        setStarFilter(0);
        searchInput.text = string.Empty;
        ApplyFiltersAndRefreshUI(); // This calls ShowPage(0)
    }

    private void onClickedBtnHideCookingPanel() => cookingPanel.SetActive(false);

    private void onClickedBtnCook() {
        if (selectedFood != null && CookingManager.Instance.StartCooking(selectedFood)) {
            // Clear selection after successfully starting
            if (currentSelectedFoodItem != null) currentSelectedFoodItem.SetSelected(false);
            currentSelectedFoodItem = null;
            selectedFood = null;
        }
    }

    private void onClickedBtnPause() {
        GameManager.Instance.TogglePause();
        pausePanel.SetActive(GameManager.Instance.IsPaused);
        if (btnPauseGame != null && btnPauseGame.TryGetComponent<Image>(out var pauseImage)) {
            pauseImage.sprite = GameManager.Instance.IsPaused ? imgUnpause : imgPause;
        }
    }

    private void handleSearchQueryChanged(string query) {
        currentSearchQuery = query;
        ApplyFiltersAndRefreshUI();
    }

    private void handleFilterButtonClicked() => filterPanel.SetActive(!filterPanel.activeSelf);

    private void setStarFilter(int starValue) {
        currentStarFilter = (currentStarFilter == starValue) ? 0 : starValue;
        UpdateFilterButtonGFX();
        ApplyFiltersAndRefreshUI();
        filterPanel.SetActive(false);
    }

    private void onClickedBtnLeftArrow() {
        if (currentPage > 0) ShowPage(currentPage - 1);
    }

    private void onClickedBtnRightArrow() {
        if (currentPage < totalPages - 1) ShowPage(currentPage + 1);
    }

    #endregion

    #region UI Updates
    /// <summary>
    /// Handles the logic for selecting, deselecting, or switching between food items.
    /// </summary>
    private void OnFoodItemSelected(FoodItem clickedItem) {
        if (currentSelectedFoodItem == clickedItem) {
            clickedItem.SetSelected(false);
            currentSelectedFoodItem = null;
            selectedFood = null;
            btnCook.interactable = false;
            ClearIngredientDisplay();
            txtCookingTimer.text = FormatTime(0);
        } else {
            if (currentSelectedFoodItem != null) currentSelectedFoodItem.SetSelected(false);
            clickedItem.SetSelected(true);
            currentSelectedFoodItem = clickedItem;
            selectedFood = currentSelectedFoodItem.GetFoodSO();

            UpdateIngredientDisplay(selectedFood);
            txtCookingTimer.text = FormatTime(selectedFood.cookingTime);

            bool canAfford = InventoryManager.Instance.HasIngredients(selectedFood.requirements);
            btnCook.interactable = canAfford && !CookingManager.Instance.IsCooking;
        }
    }
    
    /// <summary>
    /// Populates the ingredient display using a pool of UI objects.
    /// </summary>
    private void UpdateIngredientDisplay(FoodSO food) {
        ClearIngredientDisplay();
        if (food == null) return;

        foreach (var requirement in food.requirements) {
            GameObject itemGO = GetIngredientFromPool();
            IngredientItem ingredientItem = itemGO.GetComponent<IngredientItem>();
            int ownCount = InventoryManager.Instance.GetIngredientCount(requirement.ingredient.ingredientId);

            ingredientItem.imgIngredient.sprite = requirement.ingredient.icon;
            ingredientItem.txtIngredientCount.text = ownCount < requirement.amount ?
                $"<color={insufficientColorHex}>{ownCount}</color>/{requirement.amount}" :
                $"<color={sufficientColorHex}>{ownCount}</color>/{requirement.amount}";

            itemGO.SetActive(true);
        }
    }

    /// <summary>
    /// Updates the visual state of the star filter buttons.
    /// </summary>
    private void UpdateFilterButtonGFX() {
        imgFilter1Star.sprite = (currentStarFilter == 1) ? selectedFilterSprite : unselectedFilterSprite;
        imgFilter2Star.sprite = (currentStarFilter == 2) ? selectedFilterSprite : unselectedFilterSprite;
        imgFilter3Star.sprite = (currentStarFilter == 3) ? selectedFilterSprite : unselectedFilterSprite;
    }

    private void UpdateEnergyUI(int current, int max) {
        if (energyText != null) energyText.text = $"{current}/{max}";
        if (energySlider != null) energySlider.value = (float)current / max;
    }

    private void UpdateCookingTimer(float remainingTime) {
        if (txtCookingTimer != null) txtCookingTimer.text = FormatTime(remainingTime);
    }

    #endregion

    #region Helper Methods
    /// <summary>
    /// Deactivates all items in the ingredient pool.
    /// </summary>
    private void ClearIngredientDisplay() {
        foreach (var item in ingredientPool) item.SetActive(false);
    }
    
    /// <summary>
    /// Gets a reusable ingredient UI object from the pool, creating one if necessary.
    /// </summary>
    private GameObject GetIngredientFromPool() {
        foreach (GameObject item in ingredientPool) {
            if (!item.activeInHierarchy) return item;
        }
        GameObject newItem = Instantiate(ingredientPrefab, ingredientsContainer);
        ingredientPool.Add(newItem);
        return newItem;
    }

    /// <summary>
    /// Formats a float time in seconds to a "m:ss" string.
    /// </summary>
    private string FormatTime(float timeInSeconds) {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return $"{minutes}:{seconds:00}";
    }

    #endregion
}