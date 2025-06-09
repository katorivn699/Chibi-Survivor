using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Shop Settings")]
    public int itemsPerShop = 3;
    public List<ShopItemData> allItems;

    [Header("References")]
    public ShopUI shopUI;

    // Renamed for clarity, as it's used internally by ShopManager
    private List<ShopItemData> currentShopOfferings = new List<ShopItemData>();

    private void Start()
    {
        // Ensure EventManager exists before subscribing
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShopOpened += OnShopOpened;
        }
        else
        {
            Debug.LogError("EventManager instance not found in ShopManager Start!");
        }
    }

    private void OnDestroy()
    {
        // Ensure EventManager exists before unsubscribing
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnShopOpened -= OnShopOpened;
        }
    }

    private void OnShopOpened()
    {
        // Generate a new set of items for the shop
        GenerateShopItems();

        // Display the shop UI with the generated items
        if (shopUI != null)
        {
            shopUI.ShowShop(currentShopOfferings);
        }
        else
        {
            Debug.LogError("ShopUI reference is not set in ShopManager!");
        }
    }

    private void GenerateShopItems()
    {
        currentShopOfferings.Clear();

        // Create a temporary list of items available for selection
        List<ShopItemData> availableItemsPool = new List<ShopItemData>(allItems);

        // Select items randomly based on rarity
        for (int i = 0; i < itemsPerShop; i++)
        {
            if (availableItemsPool.Count == 0) break; // Stop if no more items to choose from

            // Calculate total rarity for weighted random selection
            float totalRarity = 0f;
            foreach (ShopItemData item in availableItemsPool)
            {
                totalRarity += item.rarity;
            }

            // Select an item based on weighted rarity
            float randomValue = Random.Range(0f, totalRarity);
            float currentSum = 0f;
            ShopItemData selectedItem = null;

            foreach (ShopItemData item in availableItemsPool)
            {
                currentSum += item.rarity;
                if (randomValue <= currentSum)
                {
                    selectedItem = item;
                    break;
                }
            }

            // Add the selected item to the current shop offerings and remove from the pool
            if (selectedItem != null)
            {
                currentShopOfferings.Add(selectedItem);
                availableItemsPool.Remove(selectedItem);
            }
            else if (availableItemsPool.Count > 0)
            {
                // Fallback: if calculation fails somehow, pick the first available
                selectedItem = availableItemsPool[0];
                currentShopOfferings.Add(selectedItem);
                availableItemsPool.Remove(selectedItem);
                Debug.LogWarning("Weighted random selection failed, picked first available item.");
            }
        }
    }

    // Called by ShopUI when the buy button is clicked
    public void PurchaseItem(int index)
    {
        if (index < 0 || index >= currentShopOfferings.Count)
        {
            Debug.LogError($"PurchaseItem called with invalid index: {index}");
            return;
        }

        ShopItemData item = currentShopOfferings[index];

        // Find Player components safely
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("Player object not found in PurchaseItem!");
            return;
        }
        PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats component not found on Player object!");
            return;
        }

        // Check if player has enough money
        if (playerStats.SpendMoney(item.price))
        {
            // Process the purchase based on item type
            bool purchaseSuccessful = ProcessPurchase(item, playerObject);

            // Only replace item if purchase was processed successfully
            if (purchaseSuccessful)
            {
                // Remove the purchased item from the current offerings
                currentShopOfferings.RemoveAt(index);

                // Add a new random item to replace the purchased one
                AddNewItemToShop();

                // Update the shop UI
                if (shopUI != null)
                {
                    shopUI.UpdateShop(currentShopOfferings);
                }
            }
            else
            {
                // Refund money if purchase processing failed (e.g., trying to upgrade a weapon player doesn't have)
                playerStats.AddMoney(item.price);
                Debug.LogWarning($"Purchase of {item.itemName} failed processing. Money refunded.");
                // Optionally notify the player via UI
            }
        }
        else
        {
            Debug.Log("Not enough money to purchase " + item.itemName);
            // Optionally notify the player via UI
        }
    }

    // Handles the logic for applying the purchased item's effect
    private bool ProcessPurchase(ShopItemData item, GameObject playerObject)
    {
        PlayerStats playerStats = playerObject.GetComponent<PlayerStats>();
        WeaponManager weaponManager = playerObject.GetComponent<WeaponManager>();

        if (playerStats == null || weaponManager == null)
        {
            Debug.LogError("Missing PlayerStats or WeaponManager on Player object in ProcessPurchase!");
            return false; // Purchase failed
        }

        Debug.Log($"Processing purchase of {item.itemName} of type {item.type}");

        switch (item.type)
        {
            case ItemType.Weapon:
                if (item.weaponData != null)
                {
                    bool added = weaponManager.AddWeapon(item.weaponData);
                    Debug.Log($"AddWeapon result for {item.weaponData.weaponName}: {added}");
                    return added;
                }
                else
                {
                    Debug.LogError($"Weapon item {item.itemName} has no WeaponData assigned!");
                    return false; // Purchase failed
                }

            case ItemType.Upgrade:
                if (item.weaponData != null)
                {
                    bool upgraded = weaponManager.UpgradeSpecificWeapon(item.weaponData, item.damageMultiplier, item.attackSpeedMultiplier);
                    Debug.Log($"UpgradeSpecificWeapon result for {item.weaponData.weaponName}: {upgraded}");
                    if (!upgraded)
                    {
                        Debug.Log($"Player does not have {item.weaponData.weaponName} to upgrade.");
                    }
                    return upgraded;
                }
                else
                {
                    Debug.LogError($"Upgrade item {item.itemName} has no upgradeTargetWeapon assigned!");
                    return false; // Purchase failed
                }

            case ItemType.Health:
                playerStats.RestoreHealth(item.healthRestoreAmount);
                return true;

            default:
                Debug.LogError($"Unknown item type: {item.type}");
                return false;
        }
    }

    // Adds a new random item to the shop offerings, replacing one that was bought
    private void AddNewItemToShop()
    {
        // Nếu bạn muốn có thể bán lại vật phẩm đã có trong shop, thì bỏ qua việc lọc trùng:
        // Chỉ lấy allItems làm pool, không loại bỏ item đang hiện

        if (allItems.Count == 0) return;

        // Tính tổng rarity
        float totalRarity = 0f;
        foreach (ShopItemData item in allItems)
        {
            totalRarity += item.rarity;
        }

        float randomValue = Random.Range(0f, totalRarity);
        float currentSum = 0f;
        ShopItemData selectedItem = null;

        foreach (ShopItemData item in allItems)
        {
            currentSum += item.rarity;
            if (randomValue <= currentSum)
            {
                selectedItem = item;
                break;
            }
        }

        if (selectedItem != null)
        {
            currentShopOfferings.Add(selectedItem);
        }
        else if (allItems.Count > 0)
        {
            selectedItem = allItems[0];
            currentShopOfferings.Add(selectedItem);
            Debug.LogWarning("Weighted random selection failed in AddNewItemToShop, picked first available item.");
        }
    }
}

