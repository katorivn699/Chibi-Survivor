using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Properties")]
    public float damage;
    public float speed;
    public float lifetime;
    public bool isPlayerProjectile;

    private Rigidbody2D rb;
    private float timer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(float damage, float speed, float lifetime, bool isPlayerProjectile)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.isPlayerProjectile = isPlayerProjectile;

        timer = 0f;

        // Reset velocity nếu object pool giữ lại từ trước
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Đặt hướng bay theo transform.right (nơi Weapon đã xoay sẵn)
        rb.linearVelocity = transform.right * speed;
    }

    private void OnEnable()
    {
        timer = 0f;

        // Reset Rigidbody để tránh lỗi từ lần trước
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }
    }

    private void Update()
    {
        // Cộng thời gian
        timer += Time.deltaTime;

        if (timer >= lifetime)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlayerProjectile && other.CompareTag("Enemy"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 knockbackDirection = rb.linearVelocity.normalized;
                float knockbackForce = 3f;

                damageable.TakeDamage(damage, knockbackDirection, knockbackForce);
                DamagePopup.Create(other.transform.position, damage);
            }

            gameObject.SetActive(false);
        }
        else if (!isPlayerProjectile && other.CompareTag("Player"))
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            gameObject.SetActive(false);
        }
    }
}
