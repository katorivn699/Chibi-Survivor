using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged
}

[CreateAssetMenu(fileName = "New Weapon", menuName = "ScriptableObjects/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public Sprite weaponSprite;
    public WeaponType type;
    public float damage;
    public float attackSpeed;
    public float radius; 
    public float rotationSpeed; 
    public int price;
    
    [Header("Ranged Weapon Properties")]
    public GameObject projectilePrefab;
    [Tooltip("Speed of the projectile when fired. For example, a bullet might travel at 20 units per second.")]
    public float projectileSpeed;
    [Tooltip("Lifetime of the projectile in seconds. After this time, the projectile will be destroyed.")]
    public float projectileLifetime;
    [Tooltip("Number of projectiles fired in a single attack. For example, a shotgun might have 5 projectiles.")]
    public int projectileCount = 1;
    [Tooltip("Angle spread for projectiles. For example, a shotgun might have a spread angle of 30 degrees.")]
    public float spreadAngle = 0f;
}
