using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("Shop Panel")]
    public GameObject shopPanel;

    [Header("Item Template")]
    public GameObject shopItemButtonPrefab; // Renamed for clarity
    public Transform itemContainer;

    [Header("Item Details Panel")]
    public Image itemDetailImage;
    public TextMeshProUGUI itemDetailNameText;
    public TextMeshProUGUI itemDetailDescriptionText;
    public TextMeshProUGUI itemDetailPriceText;
    public Button buyButton;

    private List<GameObject> currentItemButtonObjects = new List<GameObject>();
    private ShopManager shopManager;
    private List<ShopItemData> currentDisplayedItems; // Keep track of items displayed
    private int selectedItemIndex = -1;

    private void Awake()
    {
        // Get reference to ShopManager (assuming it's on the same GameObject or parent)
        shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager == null)
        {
            Debug.LogError("ShopManager not found by ShopUI!");
        }

        // Ensure the template is inactive initially
        if (shopItemButtonPrefab != null)
        {
            shopItemButtonPrefab.SetActive(false);
        }
        else
        {
            Debug.LogError("ShopItemButtonPrefab is not assigned in ShopUI!");
        }

        // Hide shop panel at start
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("ShopPanel is not assigned in ShopUI!");
        }

        // Add listener to the buy button
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(BuySelectedItem);
        }
        else
        {
            Debug.LogError("BuyButton is not assigned in ShopUI!");
        }
    }

    // Called by ShopManager when the shop opens
    public void ShowShop(List<ShopItemData> items)
    {
        currentDisplayedItems = items; // Store the list of items being shown
        selectedItemIndex = -1; // Reset selection

        if (shopPanel == null) return;
        shopPanel.SetActive(true);

        ClearItemButtons();
        CreateItemButtons(items);

        // Select the first item by default if available
        if (items.Count > 0)
        {
            SelectItem(0);
        }
        else
        {
            ClearItemDetails(); // Clear details if shop is empty
        }
    }

    // Called by ShopManager after an item is purchased and replaced
    public void UpdateShop(List<ShopItemData> items)
    {
        currentDisplayedItems = items;
        selectedItemIndex = -1; // Reset selection

        if (shopPanel == null || !shopPanel.activeSelf) return; // Don't update if panel is hidden

        ClearItemButtons();
        CreateItemButtons(items);

        // Select the first item by default if available
        if (items.Count > 0)
        {
            SelectItem(0);
        }
        else
        {
            ClearItemDetails();
        }
    }

    private void CreateItemButtons(List<ShopItemData> items)
    {
        if (itemContainer == null || shopItemButtonPrefab == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            ShopItemData item = items[i];
            if (item == null) continue;

            // Instantiate button from prefab
            GameObject itemButtonObject = Instantiate(shopItemButtonPrefab, itemContainer);
            itemButtonObject.SetActive(true); // Make sure the instantiated object is active

            // Get components (assuming prefab structure)
            Image itemImageComponent = itemButtonObject.transform.Find("ItemImage").GetComponent<Image>(); // Adjust name if needed
            TextMeshProUGUI itemNameComponent = itemButtonObject.transform.Find("ItemName").GetComponent<TextMeshProUGUI>(); // Adjust name if needed
            Button buttonComponent = itemButtonObject.GetComponent<Button>();

            // Configure button UI
            if (itemImageComponent != null) itemImageComponent.sprite = item.itemSprite;
            if (itemNameComponent != null) itemNameComponent.text = item.itemName;

            // Add click listener
            if (buttonComponent != null)
            {
                int itemIndex = i; // Capture index for the closure
                buttonComponent.onClick.RemoveAllListeners(); // Clear previous listeners
                buttonComponent.onClick.AddListener(() => SelectItem(itemIndex));
            }

            currentItemButtonObjects.Add(itemButtonObject);
        }
    }

    private void ClearItemButtons()
    {
        foreach (GameObject buttonObj in currentItemButtonObjects)
        {
            Destroy(buttonObj);
        }
        currentItemButtonObjects.Clear();
    }

    // Called when an item button is clicked
    private void SelectItem(int index)
    {
        if (currentDisplayedItems == null || index < 0 || index >= currentDisplayedItems.Count)
        {
            ClearItemDetails();
            return;
        }

        selectedItemIndex = index;
        ShopItemData item = currentDisplayedItems[index];

        // Update Item Details Panel
        if (itemDetailImage != null) itemDetailImage.sprite = item.itemSprite;
        if (itemDetailNameText != null) itemDetailNameText.text = item.itemName;
        if (itemDetailPriceText != null) itemDetailPriceText.text = $"${item.price}";

        // Generate description based on item type
        string description = "";
        switch (item.type)
        {
            case ItemType.Weapon:
                if (item.weaponData != null)
                {
                    description = $"Damage: {item.weaponData.damage:F1}\nAttack Speed: {item.weaponData.attackSpeed:F1}";
                    if (item.weaponData.type == WeaponType.Melee)
                    {
                        description += $"\nRadius: {item.weaponData.radius:F1}\nRotation Speed: {item.weaponData.rotationSpeed:F1}";
                    }
                    if (item.weaponData.projectileSpeed > 0)
                    {
                        description += $"\nProjectile Speed: {item.weaponData.projectileSpeed:F1}";
                    }
                }
                else { description = "Weapon data missing!"; }
                break;

            case ItemType.Upgrade:
                // **MODIFIED: Show which weapon is upgraded**
                string targetWeaponName = (item.weaponData != null) ? item.weaponData.weaponName : "Unknown Weapon";
                description = $"Upgrade for: {targetWeaponName}\nDamage Multiplier: x{item.damageMultiplier:F2}\nAttack Speed Multiplier: x{item.attackSpeedMultiplier:F2}";
                break;

            case ItemType.Health:
                description = $"Restore {item.healthRestoreAmount} Health";
                break;

            default:
                description = "Unknown item type.";
                break;
        }
        if (itemDetailDescriptionText != null) itemDetailDescriptionText.text = description;

        // Update Buy Button interactability
        if (buyButton != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                PlayerStats playerStats = playerObj.GetComponent<PlayerStats>();
                buyButton.interactable = (playerStats != null && playerStats.money >= item.price);
            }
            else
            {
                buyButton.interactable = false; // Cannot buy if player doesn't exist
            }
        }
    }

    private void ClearItemDetails()
    {
        selectedItemIndex = -1;
        if (itemDetailImage != null) itemDetailImage.sprite = null;
        if (itemDetailNameText != null) itemDetailNameText.text = "";
        if (itemDetailDescriptionText != null) itemDetailDescriptionText.text = "";
        if (itemDetailPriceText != null) itemDetailPriceText.text = "";
        if (buyButton != null) buyButton.interactable = false;
    }

    // Called by the Buy Button's OnClick event
    public void BuySelectedItem()
    {
        if (selectedItemIndex >= 0 && selectedItemIndex < currentDisplayedItems.Count)
        {
            if (shopManager != null)
            {
                shopManager.PurchaseItem(selectedItemIndex);
                // ShopManager will call UpdateShop if purchase is successful
            }
            else
            {
                Debug.LogError("ShopManager reference missing in BuySelectedItem!");
            }
        }
        else
        {
            Debug.LogWarning("BuySelectedItem called with no valid item selected.");
        }
    }

    // Called by a Close Button in the UI
    public void CloseShop()
    {
        if (shopPanel != null) shopPanel.SetActive(false);

        // Notify GameManager that the shop is closed (resumes game)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CloseShop();
        }
        else
        {
            Debug.LogError("GameManager instance not found when closing shop!");
        }
    }
}

