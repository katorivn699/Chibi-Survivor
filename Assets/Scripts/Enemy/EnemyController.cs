using UnityEngine;
using System.Collections;
using UnityEngine.AI;

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
    private NavMeshAgent navAgent;

    private Transform playerTransform;
    private bool isInAttackRange = false;
    private int lastSkillIndex = -1;
    private float dodgeCooldown = 0f;
    private Vector3 lastPosition; // To detect if stuck
    private float stuckCheckTimer = 0f;
    private const float STUCK_CHECK_INTERVAL = 1f; // Check every 1 second
    private const float STUCK_DISTANCE_THRESHOLD = 0.1f; // Distance to consider as stuck

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        navAgent = GetComponent<NavMeshAgent>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        navAgent.updateRotation = false;
        navAgent.updateUpAxis = false;
    }

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

        navAgent.speed = enemyData.moveSpeed;
        navAgent.acceleration = enemyData.moveSpeed * 4f;
        navAgent.angularSpeed = 720f;

        // Assign Sprite
        if (spriteRenderer != null && enemyData.enemySprite != null)
        {
            spriteRenderer.sprite = enemyData.enemySprite;
        }
        else if (spriteRenderer != null)
        {
            Debug.LogWarning($"SpriteRenderer found on {gameObject.name}, but EnemyData '{enemyData.name}' has no sprite assigned!");
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
            Debug.LogWarning($"Animator found on {gameObject.name}, but EnemyData '{enemyData.name}' has no Animator Controller assigned!");
        }
        else
        {
            Debug.LogError($"Animator component missing on {gameObject.name}!");
        }

        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError($"Enemy {gameObject.name} could not find Player object! Make sure Player has 'Player' tag.");
            isDead = true;
            gameObject.SetActive(false);
            return;
        }

        isDead = false;
        attackCooldown = 0f;
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = true;
        rb.linearVelocity = Vector2.zero;

        lastPosition = transform.position;
        stuckCheckTimer = 0f;

        EnsureOnNavMesh();
    }

    private void OnEnable()
    {
        EnsureOnNavMesh();
    }

    private void Update()
    {
        if (isDead) return;

        if (attackCooldown > 0) attackCooldown -= Time.deltaTime;
        if (dodgeCooldown > 0) dodgeCooldown -= Time.deltaTime;

        stuckCheckTimer += Time.deltaTime;
        if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
        {
            CheckIfStuck();
            stuckCheckTimer = 0f;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        isInAttackRange = distanceToPlayer <= enemyData.attackRange;

        if (!isDead && dodgeCooldown <= 0 && Random.value < 0.005f && distanceToPlayer < 3f)
        {
            StartCoroutine(DodgeRoutine());
            dodgeCooldown = 2f;
        }

        if (!isInAttackRange)
        {
            MoveTowardsPlayer();
        }

        switch (enemyData.type)
        {
            case EnemyType.Melee: HandleMeleeEnemy(); break;
            case EnemyType.Fast: HandleFastEnemy(); break;
            case EnemyType.Ranged: HandleRangedEnemy(); break;
            case EnemyType.Boss: HandleBossEnemy(); break;
        }

        // Apply NavMesh movement
        if (navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.Move(navAgent.desiredVelocity * Time.deltaTime);
            Vector2 vel = navAgent.desiredVelocity;
            if (vel.x > 0.1f) spriteRenderer.flipX = false;
            else if (vel.x < -0.1f) spriteRenderer.flipX = true;
        }

        if (playerTransform == null)
        {
            if (enemyData.type != EnemyType.Boss && !isDead)
            {
                StartCoroutine(IdleLookAround());
            }
            return;
        }
    }

    private void EnsureOnNavMesh()
    {
        if (!navAgent.enabled)
        {
            navAgent.enabled = true;
        }

        NavMeshHit hit;
        if (!navAgent.isOnNavMesh)
        {
            int maxAttempts = 3;
            float searchRadius = 10f;
            for (int i = 0; i < maxAttempts; i++)
            {
                if (NavMesh.SamplePosition(transform.position, out hit, searchRadius, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    navAgent.Warp(hit.position);
                    Debug.Log($"Enemy {gameObject.name} successfully placed on NavMesh at {hit.position}");
                    return;
                }
                searchRadius *= 2; 
            }
            Debug.LogError($"Enemy {gameObject.name} could not find a valid NavMesh position after {maxAttempts} attempts!");
            isDead = true;
            gameObject.SetActive(false);
        }
    }

    private void CheckIfStuck()
    {
        if (Vector3.Distance(transform.position, lastPosition) < STUCK_DISTANCE_THRESHOLD)
        {
            Debug.Log($"Enemy {gameObject.name} appears stuck, recalculating path...");
            EnsureOnNavMesh();
            MoveTowardsPlayer(); // Force recalculate path
        }
        lastPosition = transform.position;
    }

    private IEnumerator DodgeRoutine()
    {
        if (navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
        navAgent.enabled = false;

        Vector2 dodgeDir = Random.value > 0.5f ? Vector2.left : Vector2.right;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dodgeDir, 1f, LayerMask.GetMask("Obstacle"));
        if (hit.collider != null) dodgeDir *= -1;

        float dodgeSpeed = enemyData.moveSpeed * 2f;
        float dodgeDuration = 0.25f;
        float timer = 0f;

        while (timer < dodgeDuration)
        {
            transform.position += (Vector3)(dodgeDir * dodgeSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }

        navAgent.enabled = true;
        EnsureOnNavMesh();
        if (navAgent.enabled && navAgent.isOnNavMesh)
        {
            MoveTowardsPlayer();
        }
    }

    private IEnumerator IdleLookAround()
    {
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        NavMeshHit hit;
        Vector3 targetPos = transform.position + (Vector3)(randomDir * enemyData.moveSpeed * 0.5f);
        if (NavMesh.SamplePosition(targetPos, out hit, 2f, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
        }
        yield return new WaitForSeconds(0.5f);
        rb.linearVelocity = Vector2.zero;
    }

    private void HandleMeleeEnemy()
    {
        if (isInAttackRange)
        {
            Attack();
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    private void HandleFastEnemy()
    {
        MoveTowardsPlayer();
        if (isInAttackRange)
        {
            Attack();
        }
    }

    private void HandleRangedEnemy()
    {
        if (isInAttackRange)
        {
            RangedAttack();
        }
        else
        {
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

        if (attackCooldown > 0 || (!inAttackRange && !inRangedRange)) return;

        foreach (var skill in enemyData.bossSkills)
        {
            switch (skill.skillName.ToString())
            {
                case "RangedAttack":
                    skill.skillAction = RangedAttack;
                    break;
                case "SpreadProjectile":
                    skill.skillAction = () => SpawnProjectileSpread(12, 180f);
                    break;
                case "ProjectileCircle":
                    skill.skillAction = SpawnProjectileCircle;
                    break;
                case "ChargeAttack":
                    skill.skillAction = SpecialChargeAttack;
                    break;
                case "Spiral":
                    skill.skillAction = () => SpawnProjectileSpiral(20, 100f, 0.1f);
                    break;
                case "MeleeAttack":
                    skill.skillAction = Attack;
                    break;
                default:
                    Debug.LogWarning($"[BossSkill] Unknown skill name: {skill.skillName}");
                    break;
            }
        }

        var skills = enemyData.bossSkills.FindAll(s => s.skillAction != null);
        if (skills.Count == 0) return;

        float[] weights = skills.ConvertAll(s => isPhase2 ? s.weightPhase2 : s.weightPhase1).ToArray();
        float totalWeight = 0f;
        foreach (var w in weights) totalWeight += w;

        float rnd = Random.Range(0f, totalWeight);
        float accum = 0f;
        int selectedIndex = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            accum += weights[i];
            if (rnd <= accum && i != lastSkillIndex)
            {
                selectedIndex = i;
                break;
            }
        }

        lastSkillIndex = selectedIndex;

        Debug.Log($"[BossSkill] Using skill: {skills[selectedIndex].skillName}");

        skills[selectedIndex].skillAction?.Invoke();

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        attackCooldown = 1f / enemyData.attackSpeed;
    }

    private void SpawnProjectileSpread(int projectileCount, float spreadAngle)
    {
        if (attackCooldown > 0) return;

        attackCooldown = 1f / enemyData.attackSpeed;

        float baseAngle = 0f;
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

                Rigidbody2D rbProj = projectileObj.GetComponent<Rigidbody2D>();
                if (rbProj != null)
                {
                    Vector2 direction = (spawnPosition - transform.position).normalized;
                    rbProj.linearVelocity = direction * enemyData.projectileSpeed;
                }
            }

            currentRadius += radiusIncrement;
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

    private void MoveTowardsPlayer(bool isPhase2 = false)
    {
        if (!navAgent.enabled || !navAgent.isOnNavMesh)
        {
            EnsureOnNavMesh();
            if (!navAgent.enabled || !navAgent.isOnNavMesh) return;
        }

        float predictionTime = 0.4f;
        if (enemyData.type == EnemyType.Fast) predictionTime = 0.6f;
        else if (enemyData.type == EnemyType.Ranged) predictionTime = 0.3f;

        Vector3 predictedPos = (Vector2)playerTransform.position +
                              ((Vector2)playerTransform.position - (Vector2)transform.position).normalized * predictionTime;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(predictedPos, out hit, 10f, NavMesh.AllAreas))
        {
            navAgent.speed = enemyData.moveSpeed * (isPhase2 ? enemyData.phase2SpeedMultiplier : 1f);
            navAgent.SetDestination(hit.position);
        }
        else
        {
            if (NavMesh.SamplePosition(playerTransform.position, out hit, 20f, NavMesh.AllAreas))
            {
                navAgent.speed = enemyData.moveSpeed * (isPhase2 ? enemyData.phase2SpeedMultiplier : 1f);
                navAgent.SetDestination(hit.position);
            }
            else
            {
                if (NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas))
                {
                    navAgent.SetDestination(hit.position);
                    Debug.LogWarning($"Enemy {gameObject.name} could not path to player, moving to closest NavMesh edge.");
                }
                else
                {
                    Debug.LogError($"Enemy {gameObject.name} cannot find any valid NavMesh position to move!");
                }
            }
        }
    }

    private void Attack()
    {
        if (attackCooldown <= 0)
        {
            attackCooldown = 1f / enemyData.attackSpeed;

            PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                float damage = enemyData.damage;
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
            attackCooldown = 1f / enemyData.attackSpeed;

            Vector2 direction = (playerTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, 0, angle);

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

    private IEnumerator FlashRedCoroutine(float duration = 0.1f)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(duration);
            if (!isDead)
                spriteRenderer.color = Color.white;
        }
    }

    public void TakeDamage(float damage, Vector2 knockbackDirection = default, float knockbackForce = 0f)
    {
        if (isDead) return;

        currentHealth -= damage;
        StartCoroutine(FlashRedCoroutine());

        if (knockbackForce > 0f && knockbackDirection != Vector2.zero)
        {
            if (enemyData.type != EnemyType.Boss)
            {
                AudioController.Instance.PlaySFX("EnemyHit");
                StartCoroutine(KnockbackCoroutine(knockbackDirection, knockbackForce));
                StartCoroutine(KnockbackStunCoroutine(0.2f));
                Dodge();
            }
        }

        if (currentHealth <= 0)
        {
            spriteRenderer.color = Color.white;
            Die();
        }

        if (currentHealth <= 0 && enemyData.type == EnemyType.Boss)
        {
            AudioController.Instance.PlayBGM("BGM1", true, 5);
        }
    }

    private IEnumerator KnockbackCoroutine(Vector2 knockbackDirection, float knockbackForce)
    {
        if (navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
        }
        navAgent.enabled = false;

        float knockbackDuration = 0.2f;
        float timer = 0f;
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + knockbackDirection.normalized * knockbackForce * 0.1f;

        // Di chuyển bằng transform thay vì lực
        while (timer < knockbackDuration)
        {
            float t = timer / knockbackDuration;
            transform.position = Vector2.Lerp(startPos, targetPos, t);
            timer += Time.deltaTime;
            yield return null;
        }

        navAgent.enabled = true;
        EnsureOnNavMesh();
        if (navAgent.enabled && navAgent.isOnNavMesh)
        {
            MoveTowardsPlayer();
        }
    }

    private void Dodge()
    {
        if (attackCooldown <= 0)
        {
            StartCoroutine(DodgeRoutine());
        }
    }


    private IEnumerator KnockbackStunCoroutine(float stunDuration)
    {
        bool wasMoving = rb.linearVelocity.magnitude > 0;
        attackCooldown = Mathf.Max(attackCooldown, stunDuration);
        yield return new WaitForSeconds(stunDuration);
        if (wasMoving && !isDead)
        {
            // AI will resume in Update
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;

        StopAllCoroutines();

        if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.ResetPath();
            navAgent.enabled = false;
        }
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        animator.SetTrigger("Die");
        GetComponent<Collider2D>().enabled = false;

        if (enemyData.type == EnemyType.Boss)
        {
            HUD hud = FindFirstObjectByType<HUD>();
            hud.HideBossHealth();
        }

        DropMoney();
        Destroy(gameObject, 1f);
    }

    private void DropMoney()
    {
        if (Random.value <= enemyData.moneyDropChance)
        {
            int moneyAmount = Random.Range(enemyData.moneyDropMin, enemyData.moneyDropMax + 1);
            PlayerStats playerStats = playerTransform.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.AddMoney(moneyAmount);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, enemyData.rangedRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyData.attackRange);

        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
            Gizmos.DrawSphere(playerTransform.position, 0.1f);
        }
    }
}