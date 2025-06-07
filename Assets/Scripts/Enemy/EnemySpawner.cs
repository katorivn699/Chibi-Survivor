using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnRadius = 10f;
    public int maxEnemiesAtOnce = 50;
    public int maxSpawnAttempts = 30; // Số lần thử spawn tối đa
    public float wallCheckRadius = 1f; // Bán kính kiểm tra va chạm với tường

    [Header("Enemy Prefabs")]
    public GameObject meleeEnemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject rangedEnemyPrefab;
    public GameObject bossPrefab;

    [Header("Enemy Data")]
    public List<EnemyData> meleeEnemies;
    public List<EnemyData> fastEnemies;
    public List<EnemyData> rangedEnemies;
    public List<EnemyData> bossEnemies;

    [Header("Boss Scaling")]
    public float bossHealthScaling = 1.2f;
    public float bossDamageScaling = 1.15f;

    [Header("Enemy Scaling")]
    public float enemyHealthScaling = 1.05f;  // 5% mỗi wave
    public float enemyDamageScaling = 1.03f;  // 3% mỗi wave

    [Header("Spawn Boundaries")]
    public LayerMask wallLayerMask = -1; // Layer mask cho tường

    [Header("Spawn Timing")]
    public float spawnDelayBetweenEnemies = 0.2f; // Delay giữa mỗi enemy
    public float spawnDelayBetweenGroups = 0.5f; // Delay giữa các nhóm enemy
    public int enemiesPerGroup = 3; // Số enemy spawn cùng lúc trong 1 nhóm

    private Transform playerTransform;
    private List<GameObject> activeEnemies = new List<GameObject>();


    private void Awake()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public void SpawnWave(int waveNumber, int enemyCount)
    {
        StartCoroutine(SpawnWaveCoroutine(waveNumber, enemyCount));
    }

    private IEnumerator SpawnWaveCoroutine(int waveNumber, int enemyCount)
    {
        // Tính toán số lượng từng loại kẻ địch
        int meleeCount = Mathf.RoundToInt(enemyCount * 0.3f);
        int fastCount = Mathf.RoundToInt(enemyCount * 0.3f);
        int rangedCount = enemyCount - meleeCount - fastCount;

        // Tạo danh sách các enemy cần spawn
        List<EnemyType> enemiesToSpawn = new List<EnemyType>();

        for (int i = 0; i < meleeCount; i++)
            enemiesToSpawn.Add(EnemyType.Melee);
        for (int i = 0; i < fastCount; i++)
            enemiesToSpawn.Add(EnemyType.Fast);
        for (int i = 0; i < rangedCount; i++)
            enemiesToSpawn.Add(EnemyType.Ranged);

        // Xáo trộn danh sách để spawn ngẫu nhiên
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            EnemyType temp = enemiesToSpawn[i];
            int randomIndex = Random.Range(i, enemiesToSpawn.Count);
            enemiesToSpawn[i] = enemiesToSpawn[randomIndex];
            enemiesToSpawn[randomIndex] = temp;
        }

        // Spawn từ từ với delay
        foreach (EnemyType enemyType in enemiesToSpawn)
        {
            SpawnEnemy(enemyType);
            yield return new WaitForSeconds(spawnDelayBetweenEnemies);
        }
    }


    public void SpawnBoss()
    {
        if (bossEnemies == null || bossEnemies.Count == 0) return;
        EnemyData bossData = bossEnemies[Random.Range(0, bossEnemies.Count)];

        Vector2 spawnPosition = GetValidSpawnPosition(true);
        if (spawnPosition == Vector2.zero) return;

        GameObject bossObject = ObjectPooler.Instance.SpawnFromPool("Boss", spawnPosition, Quaternion.identity);
        if (bossObject == null) return;

        EnemyController bossController = bossObject.GetComponent<EnemyController>();
        if (bossController != null)
        {
            bossController.Initialize(bossData);

            int currentWave = GameManager.Instance.currentWave;
            int bossWaveCount = currentWave / 10;

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

            DestroyCallback callback = bossObject.GetComponent<DestroyCallback>();
            if (callback == null) callback = bossObject.AddComponent<DestroyCallback>();
            callback.OnDestroyed = () => activeEnemies.Remove(bossObject);
        }
    }



    private void SpawnEnemy(EnemyType type)
    {
        if (activeEnemies.Count >= maxEnemiesAtOnce)
            return;

        EnemyData enemyData = null;

        switch (type)
        {
            case EnemyType.Melee:
                if (meleeEnemies == null || meleeEnemies.Count == 0) return;
                enemyData = meleeEnemies[Random.Range(0, meleeEnemies.Count)];
                break;
            case EnemyType.Fast:
                if (fastEnemies == null || fastEnemies.Count == 0) return;
                enemyData = fastEnemies[Random.Range(0, fastEnemies.Count)];
                break;
            case EnemyType.Ranged:
                if (rangedEnemies == null || rangedEnemies.Count == 0) return;
                enemyData = rangedEnemies[Random.Range(0, rangedEnemies.Count)];
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

            // Nhân bản EnemyData và scale máu + damage
            EnemyData scaledData = GetScaledEnemyData(enemyData, currentWave, enemyHealthScaling, enemyDamageScaling);
            scaledData.maxHealth *= Mathf.Pow(enemyHealthScaling, currentWave - 1);
            scaledData.damage *= Mathf.Pow(enemyDamageScaling, currentWave - 1);

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
        float checkRadius = isBoss ? wallCheckRadius * 2f : wallCheckRadius;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 candidatePosition = GetRandomSpawnPosition();

            // Kiểm tra va chạm với tường
            if (!IsPositionBlocked(candidatePosition, checkRadius))
            {
                return candidatePosition;
            }
        }

        // Nếu không tìm được vị trí hợp lệ, thử spawn gần player hơn
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 candidatePosition = GetRandomSpawnPositionNearPlayer();

            if (!IsPositionBlocked(candidatePosition, checkRadius))
            {
                return candidatePosition;
            }
        }

        Debug.LogWarning("Could not find valid spawn position after all attempts!");
        return Vector2.zero; // Trả về Vector2.zero nếu không tìm được vị trí hợp lệ
    }

    private Vector2 GetRandomSpawnPosition()
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 candidatePos = (Vector2)playerTransform.position + direction * spawnRadius;

            // Raycast từ player ra vị trí spawn: nếu không trúng Wall -> hợp lệ
            RaycastHit2D hit = Physics2D.Raycast(playerTransform.position, direction, spawnRadius, wallLayerMask);
            if (hit.collider == null)
            {
                return candidatePos;
            }
        }

        return Vector2.zero;
    }


    private Vector2 GetRandomSpawnPositionNearPlayer()
    {
        // Spawn gần player hơn nếu không tìm được vị trí ở khoảng cách xa
        float reducedRadius = spawnRadius * 0.7f;
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return (Vector2)playerTransform.position + direction * reducedRadius;
    }

    private bool IsPositionBlocked(Vector2 position, float checkRadius)
    {
        // Kiểm tra va chạm với các object có tag "Wall"
        Collider2D[] wallColliders = Physics2D.OverlapCircleAll(position, checkRadius, wallLayerMask);

        foreach (Collider2D collider in wallColliders)
        {
            if (collider.CompareTag("Wall"))
            {
                return true; // Vị trí bị chặn bởi tường
            }
        }

        return false; // Vị trí hợp lệ
    }

    // Replace the following line in the OnDrawGizmosSelected method:
    // Gizmos.DrawWireCircle(playerTransform.position, spawnRadius);

    // With the following code to manually draw a wire circle using Gizmos.DrawLine:
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

    // Update the OnDrawGizmosSelected method to use the new DrawWireCircle method:
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            // Draw spawn radius circle
            Gizmos.color = Color.yellow;
            DrawWireCircle(playerTransform.position, spawnRadius);

            // Draw wall check radius circle
            Gizmos.color = Color.red;
            DrawWireCircle(playerTransform.position, wallCheckRadius);
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
