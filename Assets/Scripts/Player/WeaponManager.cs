using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    public List<Weapon> equippedWeapons = new List<Weapon>();
    public int maxWeapons = 6;

    [Header("Ranged Weapon")]
    public Transform firePoint;
    public Weapon rangedWeapon;

    private Camera mainCamera;

    private float fireRate = 0.5f;
    private float lastShotTime = 0f;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Xoay vũ khí tầm xa theo hướng chuột
        if (rangedWeapon != null && !GameManager.Instance.isShopOpen && !GameManager.Instance.isGameOver && !GameManager.Instance.isGamePaused)
        {
            RotateTowardsMouse();

            // Bắn khi nhấn chuột
            if (Input.GetMouseButtonDown(0) && Time.time - lastShotTime >= fireRate)
            {
                rangedWeapon.Attack();
                lastShotTime = Time.time;
            }
        }
    }

    private void RotateTowardsMouse()
    {
        Vector3 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        Vector3 direction = mousePosition - firePoint.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Xoay súng
        firePoint.rotation = Quaternion.Euler(0, 0, angle);

        // Lật sprite nếu chuột ở bên trái
        bool isMouseOnLeft = mousePosition.x < transform.position.x;

        // Lật trục Y của sprite
        Vector3 localScale = firePoint.localScale;
        localScale.y = isMouseOnLeft ? -Mathf.Abs(localScale.y) : Mathf.Abs(localScale.y);
        firePoint.localScale = localScale;

        // Nếu bạn muốn cả vị trí súng đổi bên theo hướng chuột (ví dụ: chuyển sang bên trái nhân vật),
        // bạn có thể điều chỉnh vị trí của firePoint như bên dưới:

        Vector3 offset = new Vector3(isMouseOnLeft ? -0.2f : 0.2f, -0.2f, 0); // Khoảng cách từ trung tâm nhân vật đến firePoint
        firePoint.localPosition = offset;
    }

    public bool AddWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            Debug.LogError("Attempted to add a null WeaponData!");
            return false;
        }

        // Check if it's a ranged weapon
        if (weaponData.type == WeaponType.Ranged)
        {
            // If already have a ranged weapon, potentially replace or ignore
            if (rangedWeapon != null)
            {
                Debug.Log($"Already have a ranged weapon ({rangedWeapon.baseWeaponData.weaponName}). Cannot add {weaponData.weaponName}.");
                // Optionally, implement logic to replace or upgrade the existing ranged weapon
                return false; // For now, don't add if one exists
            }

            if (firePoint == null)
            {
                Debug.LogError("FirePoint transform is not assigned in WeaponManager!");
                return false;
            }

            // Create and initialize the ranged weapon
            GameObject weaponObject = new GameObject(weaponData.weaponName + " (Ranged)");
            weaponObject.transform.SetParent(firePoint); // Parent to the firePoint for rotation
            weaponObject.transform.localPosition = Vector3.zero;
            weaponObject.transform.localRotation = Quaternion.identity;


            Weapon weapon = weaponObject.AddComponent<Weapon>();
            weapon.Initialize(weaponData, transform); // Pass player transform for reference
            rangedWeapon = weapon;
            weapon.GetComponent<SpriteRenderer>().sortingOrder = 2;
            Debug.Log($"Added ranged weapon: {weaponData.weaponName}");
            return true;
        }
        else if (weaponData.type == WeaponType.Melee)
        {
            // Check if max orbiting weapons reached
            if (equippedWeapons.Count >= maxWeapons)
            {
                Debug.Log("Maximum number of orbiting weapons reached.");
                return false;
            }

            // Check if this specific melee weapon is already equipped
            foreach (Weapon w in equippedWeapons)
            {
                if (w.baseWeaponData == weaponData)
                {
                    Debug.Log($"Weapon {weaponData.weaponName} is already equipped.");
                    // Optionally, implement upgrade logic here if adding the same weapon means upgrading
                    return false; // For now, don't add duplicates
                }
            }

            // Create and initialize the melee weapon
            GameObject weaponObject = new GameObject(weaponData.weaponName + " (Melee)");
            // Parent to the WeaponManager (which should be on the Player)
            weaponObject.transform.SetParent(transform);

            Weapon weapon = weaponObject.AddComponent<Weapon>();
            weapon.Initialize(weaponData, transform); // Pass player transform

            equippedWeapons.Add(weapon);

            // Distribute weapons evenly in orbit
            UpdateOrbitingWeaponPositions();
            Debug.Log($"Added melee weapon: {weaponData.weaponName}. Total: {equippedWeapons.Count}");
            return true;
        }
        else
        {
            Debug.LogError($"Unknown weapon type for {weaponData.weaponName}");
            return false;
        }
    }

    public bool UpgradeSpecificWeapon(WeaponData targetWeaponData, float damageMultiplier, float attackSpeedMultiplier)
    {
        bool upgraded = false;
        if (targetWeaponData == null)
        {
            Debug.LogError("UpgradeSpecificWeapon called with null targetWeaponData!");
            return false;
        }

        // Check orbiting melee weapons
        foreach (Weapon weapon in equippedWeapons)
        {
            if (weapon.baseWeaponData == targetWeaponData)
            {
                weapon.UpgradeStats(damageMultiplier, attackSpeedMultiplier);
                Debug.Log($"Upgraded melee weapon: {targetWeaponData.weaponName}");
                upgraded = true;
                // Don't break here, in case multiple instances could exist (though current logic prevents duplicates)
            }
        }

        // Check the primary ranged weapon
        if (rangedWeapon != null && rangedWeapon.baseWeaponData == targetWeaponData)
        {
            rangedWeapon.UpgradeStats(damageMultiplier, attackSpeedMultiplier);
            Debug.Log($"Upgraded ranged weapon: {targetWeaponData.weaponName}");
            upgraded = true;
        }

        if (!upgraded)
        {
            Debug.LogWarning($"Attempted to upgrade {targetWeaponData.weaponName}, but player does not have it equipped.");
        }

        return upgraded;
    }



    private void UpdateOrbitingWeaponPositions()
    {
        int count = equippedWeapons.Count;
        if (count == 0) return;

        float angleIncrement = 360f / count;
        for (int i = 0; i < count; i++)
        {
            equippedWeapons[i].SetOrbitAngle(i * angleIncrement);
        }
    }

}
