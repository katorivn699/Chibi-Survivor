using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Player Data")]
    public PlayerData playerData;
    
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public int money = 0;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;

    private GameObject firePoint;
    
    [Header("State")]
    public bool isDead = false;
    
    private Animator animator;
    private Rigidbody2D rb;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        firePoint = GetComponentInChildren<WeaponManager>().firePoint.gameObject;
    }
    
    private void Start()
    {
        // Khởi tạo giá trị mặc định
        currentHealth = maxHealth;
        
        // Thông báo chỉ số ban đầu
        EventManager.Instance.PlayerHealthChanged(currentHealth);
        EventManager.Instance.MoneyChanged(money);
    }
    
    public void InitializeFromPlayerData(PlayerData data)
    {
        if (data == null) return;
        
        playerData = data;
        maxHealth = data.maxHealth;
        currentHealth = maxHealth;
        damageMultiplier = data.damageMultiplier;
        attackSpeedMultiplier = data.attackSpeedMultiplier;
        
        // Thông báo chỉ số ban đầu
        EventManager.Instance.PlayerHealthChanged(currentHealth);
        EventManager.Instance.MoneyChanged(money);
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection = default, float knockbackForce = 0f)
    {
        if (isDead) return;

        currentHealth -= damage;

        // Gây knockback nếu có lực và hướng
        if (rb != null && knockbackForce > 0f && knockbackDirection != Vector2.zero)
        {
            rb.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }

        // Thông báo HP thay đổi
        EventManager.Instance.PlayerHealthChanged(currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");

        firePoint.SetActive(false); 

        StartCoroutine(WaitForDieAnimation());
    }

    private IEnumerator WaitForDieAnimation()
    {
        yield return new WaitForSeconds(1.2f);

        GameManager.Instance.GameOver();
    }


    public void AddMoney(int amount)
    {
        money += amount;
        
        EventManager.Instance.MoneyChanged(money);
    }
    
    public bool SpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            
            EventManager.Instance.MoneyChanged(money);
            
            return true;
        }
        
        return false;
    }
    
    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        EventManager.Instance.PlayerHealthChanged(currentHealth);
    }
    
    public float GetActualDamage(float baseDamage)
    {
        return baseDamage * damageMultiplier;
    }
    
    public float GetActualAttackSpeed(float baseAttackSpeed)
    {
        return baseAttackSpeed * attackSpeedMultiplier;
    }
}
