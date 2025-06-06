using UnityEngine;
using System.Collections; // Required for IEnumerator

public class Weapon : MonoBehaviour
{
    [Header("Weapon Data")]
    public WeaponData baseWeaponData; // The original ScriptableObject
    private WeaponData runtimeWeaponData; // The instance data that gets upgraded

    [Header("State")]
    public float currentCooldown = 0f;
    public float orbitAngle = 0f;

    private float orbitSpeedMultipler = 36f;

    private SpriteRenderer spriteRenderer;
    private Transform playerTransform; // Reference to the player transform

    // Initialize using the base data and create a runtime copy
    public void Initialize(WeaponData data, Transform player)
    {
        baseWeaponData = data;
        // Create an instance copy of the WeaponData for runtime modifications
        runtimeWeaponData = Instantiate(baseWeaponData);
        // Optional: Rename the instance to avoid confusion in Inspector if needed
        runtimeWeaponData.name = baseWeaponData.name + " (Runtime)";

        playerTransform = player;

        // Setup SpriteRenderer
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = runtimeWeaponData.weaponSprite;
        spriteRenderer.sortingLayerName = "Weapons";

        // Setup Collider for Melee
        if (runtimeWeaponData.type == WeaponType.Melee)
        {
            CircleCollider2D collider = gameObject.GetComponent<CircleCollider2D>();
            if (collider == null) // Add collider only if it doesn't exist
            {
                collider = gameObject.AddComponent<CircleCollider2D>();
            }
            collider.radius = 0.5f; // Adjust as needed
            collider.isTrigger = true;
        }

        // Reset cooldown
        currentCooldown = 0f;
    }

    private void Update()
    {
        if (runtimeWeaponData == null) return; // Don't update if not initialized

        // Cooldown reduction
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
        }

        // Melee weapon orbiting logic
        if (runtimeWeaponData.type == WeaponType.Melee)
        {
            OrbitAroundPlayer();
        }
        // Ranged weapon aiming is handled by WeaponManager rotating the firePoint
    }

    private void OrbitAroundPlayer()
    {
        if (playerTransform == null || runtimeWeaponData == null) return;

        // Update orbit angle based on runtime attack speed (or rotation speed)
        orbitAngle += runtimeWeaponData.rotationSpeed * Time.deltaTime * orbitSpeedMultipler;
        orbitAngle %= 360f; // Keep angle within 0-360

        // Calculate new position based on runtime radius
        float x = Mathf.Cos(orbitAngle * Mathf.Deg2Rad) * runtimeWeaponData.radius;
        float y = Mathf.Sin(orbitAngle * Mathf.Deg2Rad) * runtimeWeaponData.radius;

        // Update position relative to player
        transform.position = playerTransform.position + new Vector3(x, y, 0);

        // Update rotation to point outwards
        float rotationZ = orbitAngle - 90f; // Adjust sprite orientation if needed
        transform.rotation = Quaternion.Euler(0, 0, rotationZ);
    }

    // Called by WeaponManager to set initial orbit position
    public void SetOrbitAngle(float angle)
    {
        orbitAngle = angle;
    }

    // Called by WeaponManager (for ranged) or triggered by cooldown
    public void Attack()
    {
        if (runtimeWeaponData == null) return;

        // Check cooldown
        if (currentCooldown <= 0)
        {
            // Reset cooldown based on runtime attack speed
            currentCooldown = 1f / runtimeWeaponData.attackSpeed;

            if (runtimeWeaponData.type == WeaponType.Ranged)
            {
                FireProjectile();
            }
            // Melee weapons deal damage via OnTriggerEnter2D
        }
    }

    private void FireProjectile()
    {
        if (runtimeWeaponData == null || ObjectPooler.Instance == null) return;

        int count = runtimeWeaponData.projectileCount;
        float spread = runtimeWeaponData.spreadAngle;

        // Tính góc bắt đầu
        float startAngle = transform.eulerAngles.z - spread / 2f;
        float angleStep = (count > 1) ? spread / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * angleStep;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            GameObject projectileObj = ObjectPooler.Instance.SpawnFromPool("Projectile", transform.position, rotation);
            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(runtimeWeaponData.damage, runtimeWeaponData.projectileSpeed, runtimeWeaponData.projectileLifetime, true);
                }
                else
                {
                    Debug.LogError("Projectile component missing!");
                }
            }
        }
    }

    // Handles collision for Melee weapons
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (runtimeWeaponData == null || runtimeWeaponData.type != WeaponType.Melee) return;

        // Check for collision with an enemy
        if (other.CompareTag("Enemy"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate knockback
                Vector2 knockbackDirection = (other.transform.position - playerTransform.position).normalized;
                float knockbackForce = 5f; // Base knockback force, potentially use runtimeWeaponData.knockbackForce if added

                // Apply damage using runtime damage value
                damageable.TakeDamage(runtimeWeaponData.damage, knockbackDirection, knockbackForce);

                // Show damage popup
                DamagePopup.Create(other.transform.position, runtimeWeaponData.damage);
            }
        }
    }

    // Method to apply upgrades to this weapon instance
    public void UpgradeStats(float damageMultiplier, float attackSpeedMultiplier)
    {
        if (runtimeWeaponData == null) return;

        // Apply multipliers to the runtime data
        runtimeWeaponData.damage *= damageMultiplier;
        runtimeWeaponData.attackSpeed *= attackSpeedMultiplier;

        // Optional: Adjust other stats like radius or rotation speed for melee if desired
        // runtimeWeaponData.radius *= someMultiplier;
        // runtimeWeaponData.rotationSpeed *= someMultiplier;

        Debug.Log($"Upgraded {runtimeWeaponData.weaponName}: Damage={runtimeWeaponData.damage}, AttackSpeed={runtimeWeaponData.attackSpeed}");

        // Optional: Visual feedback for upgrade
        StartCoroutine(UpgradeVisualEffect());
    }

    // Coroutine for simple visual feedback on upgrade
    private IEnumerator UpgradeVisualEffect()
    {
        if (spriteRenderer == null) yield break;

        float duration = 0.3f;
        int flashes = 2;
        float flashDuration = duration / (flashes * 2); // Mỗi nhấp nháy có 2 lần đổi màu

        Color originalColor = spriteRenderer.color;
        Color flashColor = Color.yellow;

        for (int i = 0; i < flashes; i++)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        spriteRenderer.color = originalColor;
    }

    // Public accessor for the runtime data if needed elsewhere
    public WeaponData GetRuntimeData()
    {
        return runtimeWeaponData;
    }
}

