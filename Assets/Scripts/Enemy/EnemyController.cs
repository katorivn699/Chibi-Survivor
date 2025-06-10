using UnityEngine;
using System.Collections;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Enemy Data")]
    public EnemyData enemyData;

    [Header("State")]
    public float currentHealth;
    public bool isDead = false;
    public float attackCooldown = 0f;

    [Header("Components")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private Transform playerTransform;
    private bool isInAttackRange = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Removed Start() method, initialization logic moved to Initialize()

    public void Initialize(EnemyData data)
    {
        enemyData = data;

        if (enemyData == null)
        {
            Debug.LogError($"EnemyData is not assigned on {gameObject.name}! Disabling enemy.");
            isDead = true;
            gameObject.SetActive(false);
            return;
        }

        currentHealth = enemyData.maxHealth;

        // Assign Sprite
        if (spriteRenderer != null && enemyData.enemySprite != null)
        {
            spriteRenderer.sprite = enemyData.enemySprite;
        }
        else if (spriteRenderer != null)
        {
            Debug.LogWarning($"SpriteRenderer found on {gameObject.name}, but EnemyData \'{enemyData.name}\' has no sprite assigned!");
        }
        else
        {
            Debug.LogError($"SpriteRenderer component missing on {gameObject.name}!");
        }

        // Assign Animator Controller
        if (animator != null && enemyData.animatorController != null)
        {
            animator.runtimeAnimatorController = enemyData.animatorController;
        }
        else if (animator != null)
        {
            Debug.LogWarning($"Animator found on {gameObject.name}, but EnemyData \'{enemyData.name}\' has no Animator Controller assigned!");
        }
        else
        {
            Debug.LogError($"Animator component missing on {gameObject.name}!");
        }

        // Find Player Transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError($"Enemy {gameObject.name} could not find Player object! Make sure Player has 'Player' tag.");
            isDead = true; // Prevent AI execution
                           // Optionally disable the enemy: gameObject.SetActive(false);
            return; // Stop initialization if player not found
        }

        // Reset state for pooling / reactivation
        isDead = false;
        attackCooldown = 0f; // Reset attack cooldown
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = true;
        rb.linearVelocity = Vector2.zero; // Reset velocity
    }

    private void OnEnable()
    {
        // If using object pooling, re-initialization might be needed here
        // or ensure Initialize is called when spawning from pool.
        // For now, we assume Initialize is called by the spawner.
    }
    private void Update()
    {
        if (isDead) return;

        // Giảm cooldown
        if (attackCooldown > 0)
        {
            attackCooldown -= Time.deltaTime;
        }

        // Kiểm tra khoảng cách đến người chơi
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        isInAttackRange = distanceToPlayer <= enemyData.attackRange;

        // Xử lý hành vi theo loại kẻ địch
        switch (enemyData.type)
        {
            case EnemyType.Melee:
                HandleMeleeEnemy();
                break;
            case EnemyType.Fast:
                HandleFastEnemy();
                break;
            case EnemyType.Ranged:
                HandleRangedEnemy();
                break;
            case EnemyType.Boss:
                HandleBossEnemy();
                break;
        }

        // Flip sprite theo hướng di chuyển
        if (rb.linearVelocity.x > 0)
        {
            spriteRenderer.flipX = false;
        }
        else if (rb.linearVelocity.x < 0)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void HandleMeleeEnemy()
    {
        if (isInAttackRange)
        {
            // Dừng lại và tấn công
            //rb.linearVelocity = Vector2.zero;
            Attack();
        }
        else
        {
            // Di chuyển đến người chơi
            MoveTowardsPlayer();
        }
    }

    private void HandleFastEnemy()
    {
        // Di chuyển nhanh đến người chơi
        MoveTowardsPlayer();

        // Tấn công khi trong tầm
        if (isInAttackRange)
        {
            Attack();
        }
    }

    private void HandleRangedEnemy()
    {
        if (isInAttackRange)
        {
            // Dừng lại và tấn công từ xa
            //rb.linearVelocity = Vector2.zero;
            RangedAttack();
        }
        else
        {
            // Di chuyển đến khoảng cách tấn công
            MoveTowardsPlayer();
        }
    }

    private void HandleBossEnemy()
    {
        bool isPhase2 = currentHealth <= enemyData.maxHealth * enemyData.phase2HealthPercentage;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        bool inAttackRange = distanceToPlayer <= enemyData.attackRange;
        bool inRangedRange = distanceToPlayer <= enemyData.rangedRange;

        MoveTowardsPlayer(isPhase2);

        if (attackCooldown <= 0 && (inAttackRange || inRangedRange))
        {
            // Chọn kỹ năng dựa trên khoảng cách
            System.Action[] skills;
            float[] weights;

            if (inAttackRange)
            {
                skills = new System.Action[] { Attack };
                weights = new float[] { 1f }; // chỉ cận chiến
            }
            else if (inRangedRange)
            {
                skills = new System.Action[] {
                RangedAttack,
                () => SpawnProjectileSpread(12, 180f),
                SpawnProjectileCircle,
                SpecialChargeAttack,
                () => SpawnProjectileSpiral(20, 100f, 0.1f)
            };

                weights = isPhase2
                    ? new float[] { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f }
                    : new float[] { 0.25f, 0.25f, 0.25f, 0.25f, 0f }; // Chỉ dùng skill xoắn ốc ở giai đoạn 2
            }
            else return;

            // Weighted random
            float totalWeight = 0f;
            foreach (var w in weights) totalWeight += w;

            float randomValue = Random.Range(0f, totalWeight);
            float sum = 0f;
            int selectedIndex = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                sum += weights[i];
                if (randomValue <= sum)
                {
                    selectedIndex = i;
                    break;
                }
            }

            skills[selectedIndex]?.Invoke();

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            attackCooldown = 1f / enemyData.attackSpeed;
        }
    }


    private void SpawnProjectileSpread(int projectileCount, float spreadAngle)
    {
        if (attackCooldown > 0) return;

        attackCooldown = 1f / enemyData.attackSpeed;

        float baseAngle = 0f; // Bạn có thể lấy hướng về player hoặc góc bất kỳ

        Vector2 centerDir = (playerTransform.position - transform.position).normalized;
        baseAngle = Mathf.Atan2(centerDir.y, centerDir.x) * Mathf.Rad2Deg;

        float startAngle = baseAngle - spreadAngle / 2f;
        float angleStep = spreadAngle / (projectileCount - 1);

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);

            GameObject projectileObj = ObjectPooler.Instance.SpawnFromPool("EnemyProjectile", transform.position, rotation);

            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                float damage = enemyData.damage;

                if (enemyData.type == EnemyType.Boss && currentHealth <= enemyData.maxHealth * enemyData.phase2HealthPercentage)
                {
                    damage *= enemyData.phase2DamageMultiplier;
                }

                projectile.Initialize(damage, enemyData.projectileSpeed, 5f, false);
            }
        }
    }

    private void SpawnProjectileSpiral(int projectileCount = 20, float rotationSpeed = 100f, float spiralRadiusIncrease = 0.1f)
    {
        if (attackCooldown > 0) return;

        attackCooldown = 1f / enemyData.attackSpeed;

        StartCoroutine(SpiralCoroutine(projectileCount, rotationSpeed, spiralRadiusIncrease));
    }

    private IEnumerator SpiralCoroutine(int projectileCount, float rotationSpeed, float spiralRadiusIncrease)
    {
        float angleStep = 360f / projectileCount;
        float currentRadius = 0f;
        float radiusIncrement = spiralRadiusIncrease;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = angleStep * i;
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);

            // Tính toán vị trí bắt đầu của đạn theo bán kính xoắn ốc
            Vector3 offset = rotation * Vector3.right * currentRadius;
            Vector3 spawnPosition = transform.position + offset;

            GameObject projectileObj = ObjectPooler.Instance.SpawnFromPool("EnemyProjectile", spawnPosition, rotation);

            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                float damage = enemyData.damage;

                if (enemyData.type == EnemyType.Boss && currentHealth <= enemyData.maxHealth * enemyData.phase2HealthPercentage)
                {
                    damage *= enemyData.phase2DamageMultiplier;
                }

                projectile.Initialize(damage, enemyData.projectileSpeed, 5f, false);

                // Thêm chuyển động xoay cho đạn (nếu cần)
                Rigidbody2D rbProj = projectileObj.GetComponent<Rigidbody2D>();
                if (rbProj != null)
                {
                    Vector2 direction = (spawnPosition - transform.position).normalized;
                    rbProj.linearVelocity = direction * enemyData.projectileSpeed;
                }
            }

            // Tăng bán kính xoắn ốc cho viên đạn tiếp theo
            currentRadius += radiusIncrement;

            // Đợi một khoảng thời gian nhỏ để tạo hiệu ứng xoắn ốc dần dần
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void SpawnProjectileCircle()
    {
        int projectileCount = 16;
        float angleStep = 360f / projectileCount;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = angleStep * i;
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);

            GameObject projectileObj = ObjectPooler.Instance.SpawnFromPool("EnemyProjectile", transform.position, rotation);
            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                float damage = enemyData.damage;
                if (enemyData.type == EnemyType.Boss && currentHealth <= enemyData.maxHealth * enemyData.phase2HealthPercentage)
                    damage *= enemyData.phase2DamageMultiplier;

                projectile.Initialize(damage, enemyData.projectileSpeed, 5f, false);
            }
        }
    }

    private void SpecialChargeAttack()
    {
        // Boss lao nhanh về phía player trong 0.5s, gây sát thương khi chạm

        StartCoroutine(ChargeCoroutine());
    }

    private IEnumerator ChargeCoroutine()
    {
        float chargeDuration = 0.5f;
        float chargeSpeed = enemyData.moveSpeed * 3f;
        Vector2 direction = (playerTransform.position - transform.position).normalized;

        float timer = 0f;
        while (timer < chargeDuration && !isDead)
        {
            rb.linearVelocity = direction * chargeSpeed;
            timer += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector2.zero;
    }

    private Vector2 PredictPlayerPosition(float predictionTime)
    {
        Vector2 playerVelocity = (playerTransform.position - transform.position).normalized;
        return (Vector2)playerTransform.position + playerVelocity * predictionTime;
    }


    private void MoveTowardsPlayer(bool isPhase2 = false)
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        float speed = enemyData.moveSpeed;

        PredictPlayerPosition(0.5f);

        // Tăng tốc độ trong giai đoạn 2 của boss
        if (isPhase2)
        {
            speed *= enemyData.phase2SpeedMultiplier;
        }

        rb.linearVelocity = direction * speed;
    }

    private void Attack()
    {
        if (attackCooldown <= 0)
        {
            // Reset cooldown
            attackCooldown = 1f / enemyData.attackSpeed;

            // Gây sát thương cho người chơi
            PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                float damage = enemyData.damage;

                // Tăng sát thương trong giai đoạn 2 của boss
                if (enemyData.type == EnemyType.Boss && currentHealth <= enemyData.maxHealth * enemyData.phase2HealthPercentage)
                {
                    damage *= enemyData.phase2DamageMultiplier;
                }

                playerStats.TakeDamage(damage);
            }
        }
    }

    private void RangedAttack()
    {
        if (attackCooldown <= 0)
        {
            // Reset cooldown
            attackCooldown = 1f / enemyData.attackSpeed;

            // Tính toán hướng bắn
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

            // Bắn đạn
            GameObject projectileObj = ObjectPooler.Instance.SpawnFromPool("EnemyProjectile", transform.position, rotation);

            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();

                float damage = enemyData.damage;

                // Tăng sát thương trong giai đoạn 2 của boss
                if (enemyData.type == EnemyType.Boss && currentHealth <= enemyData.maxHealth * enemyData.phase2HealthPercentage)
                {
                    damage *= enemyData.phase2DamageMultiplier;
                }

                projectile.Initialize(damage, enemyData.projectileSpeed, 5f, false);
            }
        }
    }

    private IEnumerator FlashRedCoroutine(float duration = 0.1f)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(duration);
            if (!isDead) // Nếu chưa chết thì trả lại màu trắng
                spriteRenderer.color = Color.white;
        }
    }


    public void TakeDamage(float damage, Vector2 knockbackDirection = default, float knockbackForce = 0f)
    {
        if (isDead) return;

        currentHealth -= damage;
        StartCoroutine(FlashRedCoroutine());

        // Áp dụng knockback nếu có
        if (knockbackForce > 0f && knockbackDirection != Vector2.zero)
        {
            // Chỉ áp dụng knockback cho quái thường, không áp dụng cho boss
            if (enemyData.type != EnemyType.Boss)
            {
                AudioController.Instance.PlaySFX("EnemyHit");
                // Áp dụng lực đẩy tức thời
                rb.linearVelocity = Vector2.zero; // Reset velocity hiện tại
                rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);

                // Có thể thêm hiệu ứng visual hoặc âm thanh khi bị đẩy lùi
                // Ví dụ: tạo particle effect, thay đổi màu sprite tạm thời, v.v.

                // Tùy chọn: Tạm thời vô hiệu hóa AI trong một khoảng thời gian ngắn
                StartCoroutine(KnockbackStunCoroutine(0.2f)); // Choáng 0.2 giây
                Dodge();
            }
        }

        // Kiểm tra nếu chết
        if (currentHealth <= 0)
        {
            spriteRenderer.color = Color.white; // Đảm bảo màu trở về bình thường trước khi chết
            Die();
        }

        if (currentHealth <= 0 && enemyData.type == EnemyType.Boss)
        {
            AudioController.Instance.PlayBGM("BGM1");
        }


    }

    private void Dodge()
    {
        if (attackCooldown <= 0)
        {
            Vector2 dodgeDirection = Random.value > 0.5f ? Vector2.left : Vector2.right;
            rb.linearVelocity = dodgeDirection * enemyData.moveSpeed * 1.5f;
            StartCoroutine(StopDodgeAfterTime(0.5f));
        }
    }

    private IEnumerator StopDodgeAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        rb.linearVelocity = Vector2.zero;
    }


    private IEnumerator KnockbackStunCoroutine(float stunDuration)
    {
        // Lưu trạng thái di chuyển hiện tại
        bool wasMoving = rb.linearVelocity.magnitude > 0;

        // Tạm dừng di chuyển và tấn công
        attackCooldown = Mathf.Max(attackCooldown, stunDuration);

        // Đợi hết thời gian choáng
        yield return new WaitForSeconds(stunDuration);

        // Khôi phục di chuyển nếu trước đó đang di chuyển
        if (wasMoving && !isDead)
        {
            // AI sẽ tự động tiếp tục di chuyển trong Update
        }
    }

    public void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        // Chạy animation chết
        animator.SetTrigger("Die");

        // Vô hiệu hóa collider
        GetComponent<Collider2D>().enabled = false;

        // Rơi tiền
        DropMoney();

        // Hủy GameObject sau khi animation chết kết thúc
        Destroy(gameObject, 1f);
    }

    private void DropMoney()
    {
        if (Random.value <= enemyData.moneyDropChance)
        {
            int moneyAmount = Random.Range(enemyData.moneyDropMin, enemyData.moneyDropMax + 1);

            // Tìm người chơi và thêm tiền
            PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddMoney(moneyAmount);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Tầm phát hiện
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.rangedRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);


        // Vẽ đường đến Player
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
            Gizmos.DrawSphere(playerTransform.position, 0.1f);
        }
    }
}
