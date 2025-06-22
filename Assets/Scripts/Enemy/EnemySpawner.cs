using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Timing")]
    public float spawnDelayBetweenEnemies = 0.2f;
    public float spawnDelayBetweenGroups = 0.5f;
    public int enemiesPerGroup = 3;

    [Header("Boss Scaling")]
    public float bossHealthScaling = 1.2f;
    public float bossDamageScaling = 1.15f;

    [Header("Enemy Scaling")]
    public float enemyHealthScaling = 1.05f;
    public float enemyDamageScaling = 1.03f;

    private MapData currentMap;
    private Transform playerTransform;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void InitializeForMap(MapData map)
    {
        currentMap = map;
    }

    public int GetActiveEnemyCount()
    {
        return activeEnemies.Count;
    }

    public void SpawnWave(int waveNumber, int enemyCount)
    {
        StartCoroutine(SpawnWaveCoroutine(waveNumber, enemyCount));
    }

    private IEnumerator SpawnWaveCoroutine(int waveNumber, int enemyCount)
    {
        int meleeCount = Mathf.RoundToInt(enemyCount * 0.3f);
        int fastCount = Mathf.RoundToInt(enemyCount * 0.3f);
        int rangedCount = enemyCount - meleeCount - fastCount;

        List<EnemyType> enemiesToSpawn = new List<EnemyType>();
        for (int i = 0; i < meleeCount; i++) enemiesToSpawn.Add(EnemyType.Melee);
        for (int i = 0; i < fastCount; i++) enemiesToSpawn.Add(EnemyType.Fast);
        for (int i = 0; i < rangedCount; i++) enemiesToSpawn.Add(EnemyType.Ranged);

        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            EnemyType temp = enemiesToSpawn[i];
            int randomIndex = Random.Range(i, enemiesToSpawn.Count);
            enemiesToSpawn[i] = enemiesToSpawn[randomIndex];
            enemiesToSpawn[randomIndex] = temp;
        }

        foreach (EnemyType enemyType in enemiesToSpawn)
        {
            SpawnEnemy(enemyType);
            yield return new WaitForSeconds(spawnDelayBetweenEnemies);
        }
    }

    public void SpawnBoss()
    {
        if (currentMap.bossEnemies == null || currentMap.bossEnemies.Count == 0) return;
        EnemyData bossData = currentMap.bossEnemies[Random.Range(0, currentMap.bossEnemies.Count)];

        Vector2 spawnPosition = GetValidSpawnPosition(true);
        if (spawnPosition == Vector2.zero) return;

        GameObject bossObject = ObjectPooler.Instance.SpawnFromPool("Boss", spawnPosition, Quaternion.identity);
        if (bossObject == null) return;

        EnemyController bossController = bossObject.GetComponent<EnemyController>();
        if (bossController != null)
        {
            bossController.Initialize(bossData);

            int currentWave = GameManager.Instance.currentWave;
            int bossWaveCount = currentWave / 2;

            if (bossWaveCount > 1)
            {
                float healthMultiplier = Mathf.Pow(bossHealthScaling, bossWaveCount - 1);
                float damageMultiplier = Mathf.Pow(bossDamageScaling, bossWaveCount - 1);

                EnemyData scaledBossData = Instantiate(bossData);
                scaledBossData.damage *= damageMultiplier;
                scaledBossData.maxHealth *= healthMultiplier;

                bossController.enemyData = scaledBossData;
                bossController.currentHealth = scaledBossData.maxHealth;

                bossObject.transform.localScale *= 1 + (bossWaveCount * 0.05f);
            }

            AudioController.Instance.PlayBGM("BossBGM");
            activeEnemies.Add(bossObject);

            HUD hud = Object.FindFirstObjectByType<HUD>();
            hud.ShowBossHealth(bossController);

            DestroyCallback callback = bossObject.GetComponent<DestroyCallback>();
            if (callback == null) callback = bossObject.AddComponent<DestroyCallback>();
            callback.OnDestroyed = () => activeEnemies.Remove(bossObject);
        }
    }

    private void SpawnEnemy(EnemyType type)
    {
        if (activeEnemies.Count >= currentMap.maxEnemiesAtOnce) return;

        EnemyData enemyData = null;
        GameObject prefab = null;

        switch (type)
        {
            case EnemyType.Melee:
                if (currentMap.meleeEnemies == null || currentMap.meleeEnemies.Count == 0) return;
                enemyData = currentMap.meleeEnemies[Random.Range(0, currentMap.meleeEnemies.Count)];
                prefab = currentMap.meleeEnemyPrefab;
                break;
            case EnemyType.Fast:
                if (currentMap.fastEnemies == null || currentMap.fastEnemies.Count == 0) return;
                enemyData = currentMap.fastEnemies[Random.Range(0, currentMap.fastEnemies.Count)];
                prefab = currentMap.fastEnemyPrefab;
                break;
            case EnemyType.Ranged:
                if (currentMap.rangedEnemies == null || currentMap.rangedEnemies.Count == 0) return;
                enemyData = currentMap.rangedEnemies[Random.Range(0, currentMap.rangedEnemies.Count)];
                prefab = currentMap.rangedEnemyPrefab;
                break;
            default:
                return;
        }

        Vector2 spawnPosition = GetValidSpawnPosition(false);
        if (spawnPosition == Vector2.zero) return;

        GameObject enemyObject = ObjectPooler.Instance.SpawnFromPool(type.ToString(), spawnPosition, Quaternion.identity);
        if (enemyObject == null) return;

        EnemyController controller = enemyObject.GetComponent<EnemyController>();
        if (controller != null)
        {
            int currentWave = GameManager.Instance.currentWave;
            EnemyData scaledData = GetScaledEnemyData(enemyData, currentWave, enemyHealthScaling, enemyDamageScaling);
            controller.Initialize(scaledData);
            controller.currentHealth = scaledData.maxHealth;

            activeEnemies.Add(enemyObject);

            DestroyCallback callback = enemyObject.GetComponent<DestroyCallback>();
            if (callback == null) callback = enemyObject.AddComponent<DestroyCallback>();
            callback.OnDestroyed = () => activeEnemies.Remove(enemyObject);
        }
    }

    private Vector2 GetValidSpawnPosition(bool isBoss = false)
    {
        float checkRadius = isBoss ? currentMap.wallCheckRadius * 2f : currentMap.wallCheckRadius;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2 candidatePosition = GetRandomSpawnPosition();
            if (!IsPositionBlocked(candidatePosition, checkRadius))
            {
                return candidatePosition;
            }
        }

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2 candidatePosition = GetRandomSpawnPositionNearPlayer();
            if (!IsPositionBlocked(candidatePosition, checkRadius))
            {
                return candidatePosition;
            }
        }

        Debug.LogWarning("Could not find valid spawn position after all attempts!");
        return Vector2.zero;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        for (int i = 0; i < 30; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 candidatePos = (Vector2)playerTransform.position + direction * currentMap.spawnRadius;

            RaycastHit2D hit = Physics2D.Raycast(playerTransform.position, direction, currentMap.spawnRadius, LayerMask.GetMask("Wall"));
            if (hit.collider == null)
            {
                return candidatePos;
            }
        }
        return Vector2.zero;
    }

    private Vector2 GetRandomSpawnPositionNearPlayer()
    {
        float reducedRadius = currentMap.spawnRadius * 0.7f;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return (Vector2)playerTransform.position + direction * reducedRadius;
    }

    private bool IsPositionBlocked(Vector2 position, float checkRadius)
    {
        Collider2D[] wallColliders = Physics2D.OverlapCircleAll(position, checkRadius, LayerMask.GetMask("Wall"));
        foreach (Collider2D collider in wallColliders)
        {
            if (collider.CompareTag("Wall"))
            {
                return true;
            }
        }
        return false;
    }

    private void DrawWireCircle(Vector3 center, float radius, int segments = 36)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            DrawWireCircle(playerTransform.position, currentMap?.spawnRadius ?? 10f);
            Gizmos.color = Color.red;
            DrawWireCircle(playerTransform.position, currentMap?.wallCheckRadius ?? 1f);
        }
    }

    private EnemyData GetScaledEnemyData(EnemyData baseData, int wave, float healthScale, float damageScale)
    {
        EnemyData scaled = Instantiate(baseData);
        scaled.maxHealth *= Mathf.Pow(healthScale, wave - 1);
        scaled.damage *= Mathf.Pow(damageScale, wave - 1);
        return scaled;
    }
}

public class DestroyCallback : MonoBehaviour
{
    public System.Action OnDestroyed;

    private void OnDestroy()
    {
        OnDestroyed?.Invoke();
    }
}
