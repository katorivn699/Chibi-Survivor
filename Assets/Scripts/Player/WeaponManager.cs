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
    public float recoilForce = 5f;

    private Camera mainCamera;
    private Rigidbody2D playerRigidbody;

    private float fireRate = 0.5f;
    private float lastShotTime = 0f;

    private void Start()
    {
        mainCamera = Camera.main;
        playerRigidbody = GetComponent<Rigidbody2D>();
        if (playerRigidbody == null)
        {
            Debug.LogError("Player Rigidbody2D not found! Recoil will not work.");
        }
    }

    private void Update()
    {
        if (rangedWeapon != null && !GameManager.Instance.isShopOpen && !GameManager.Instance.isGameOver && !GameManager.Instance.isGamePaused)
        {
            RotateTowardsMouse();

            if (Input.GetMouseButtonDown(0) && Time.time - lastShotTime >= fireRate)
            {
                rangedWeapon.Attack();
                ApplyRecoil();
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


        Vector3 offset = new Vector3(isMouseOnLeft ? -0.2f : 0.2f, -0.2f, 0); 
        firePoint.localPosition = offset;
    }

    private void ApplyRecoil()
    {
        if (playerRigidbody != null && rangedWeapon != null && firePoint != null)
        {
            Vector2 shootDirection = firePoint.right; // Direction the firePoint is facing
            Vector2 recoilDirection = -shootDirection.normalized; // Opposite direction
            playerRigidbody.AddForce(recoilDirection * recoilForce, ForceMode2D.Impulse);
            Debug.Log($"Applying recoil: Direction={recoilDirection}, Force={recoilForce}, Total={recoilDirection * recoilForce}");
        }
        else
        {
            Debug.LogWarning("Cannot apply recoil: Missing playerRigidbody, rangedWeapon, or firePoint.");
        }
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
                Debug.Log($"Replacing ranged weapon {rangedWeapon.baseWeaponData.weaponName} with {weaponData.weaponName}.");
                Destroy(rangedWeapon.gameObject); // Destroy the existing ranged weapon
                rangedWeapon = null;
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
            weapon.GetComponent<SpriteRenderer>().sortingLayerName = "Player";
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
            //foreach (Weapon w in equippedWeapons)
            //{
            //    if (w.baseWeaponData == weaponData)
            //    {
            //        Debug.Log($"Weapon {weaponData.weaponName} is already equipped.");
            //        // Optionally, implement upgrade logic here if adding the same weapon means upgrading
            //        return false; // For now, don't add duplicates
            //    }
            //}

            // Create and initialize the melee weapon
            GameObject weaponObject = new GameObject(weaponData.weaponName + " (Melee)");
            // Parent to the WeaponManager (which should be on the Player)
            weaponObject.transform.SetParent(transform);

            Weapon weapon = weaponObject.AddComponent<Weapon>();
            weapon.Initialize(weaponData, transform); // Pass player transform
            weapon.GetComponent<SpriteRenderer>().sortingLayerName = "Player";

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
        if (targetWeaponData == null)
        {
            Debug.LogError("UpgradeSpecificWeapon called with null targetWeaponData!");
            return false;
        }

        bool upgraded = false;
        int matchingWeaponCount = 0;

        // Count matching melee weapons
        foreach (Weapon weapon in equippedWeapons)
        {
            if (weapon.baseWeaponData == targetWeaponData)
            {
                matchingWeaponCount++;
            }
        }

        // Apply upgrade to matching melee weapons
        if (matchingWeaponCount > 0)
        {
            // Distribute the upgrade evenly across all matching weapons
            float distributedDamageMultiplier = Mathf.Pow(damageMultiplier, 1f / matchingWeaponCount);
            float distributedAttackSpeedMultiplier = Mathf.Pow(attackSpeedMultiplier, 1f / matchingWeaponCount);

            foreach (Weapon weapon in equippedWeapons)
            {
                if (weapon.baseWeaponData == targetWeaponData)
                {
                    weapon.UpgradeStats(distributedDamageMultiplier, distributedAttackSpeedMultiplier);
                    Debug.Log($"Upgraded melee weapon: {targetWeaponData.weaponName} (distributed upgrade)");
                    upgraded = true;
                }
            }
        }

        // Check and upgrade the primary ranged weapon
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
