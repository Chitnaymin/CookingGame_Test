using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodItem : MonoBehaviour {
    public TMP_Text txtName;
    public Image imgFood;
    public Image imgBG;
    public Button btnFood;
    public Sprite imgSelected_1s, imgSelected_2s, imgSelected_3s;
    public Sprite imgUnselected_1s, imgUnselected_2s, imgUnselected_3s;

    FoodSO foodData;
    private Action<FoodItem> onSelectCallback;

    public void BindFoodData(FoodSO foodSO, Action<FoodItem> callback) {
        foodData = foodSO;
        onSelectCallback = callback;

        txtName.text = foodData.foodName;
        imgFood.sprite = foodData.foodSprite;
        if (foodData.starCount == 1) {
            imgBG.sprite = imgUnselected_1s;
        }else if (foodData.starCount == 2) {
            imgBG.sprite = imgUnselected_2s;
        }else if (foodData.starCount == 3) {
            imgBG.sprite = imgUnselected_3s;
        }

        btnFood.onClick.AddListener(HandleClick);
        SetSelected(false);
    }

    public FoodSO GetFoodSO() {
        return foodData;
    }

    public void SetSelected(bool isSelected) {
        int stars = foodData.starCount;

        switch (stars) {
            case 1:
                imgBG.sprite = isSelected ? imgSelected_1s : imgUnselected_1s;
                break;
            case 2:
                imgBG.sprite = isSelected ? imgSelected_2s : imgUnselected_2s;
                break;
            case 3:
                imgBG.sprite = isSelected ? imgSelected_3s : imgUnselected_3s;
                break;
        }
    }

    private void HandleClick() {
        onSelectCallback?.Invoke(this);
    }
}