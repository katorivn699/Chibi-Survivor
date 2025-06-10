using UnityEngine;

public enum ItemType
{
    Weapon,
    Upgrade,
    Health
}

[CreateAssetMenu(fileName = "New Shop Item", menuName = "ScriptableObjects/ShopItem")]
public class ShopItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemSprite;
    public ItemType type;
    public int price;
    public float rarity; // Tỉ lệ xuất hiện
    
    [Header("Weapon Item")]
    [Tooltip("Weapon can be upgrade include this type of data")]
    public WeaponData weaponData;

    [Header("Upgrade Item")]
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    
    [Header("Health Item")]
    public float healthRestoreAmount;
}
