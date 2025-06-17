using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, Vector2 knockbackDirection = default, float knockbackForce = 1f);
    void Die();
}
