using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public int baseEnemyCount = 10;
    public float enemyCountMultiplier = 1.5f;

    [Header("References")]
    public EnemySpawner enemySpawner;

    private int currentWave = 0;
    private bool isWaveCompleted = false; // Theo dõi trạng thái wave hoàn thành

    private void Start()
    {
        EventManager.Instance.OnWaveChanged += OnWaveChanged;
        Debug.Log("WaveManager Start: Registered OnWaveChanged");
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnWaveChanged -= OnWaveChanged;
            Debug.Log("WaveManager OnDestroy: Unregistered OnWaveChanged");
        }
    }

    private void OnWaveChanged(int waveNumber)
    {
        currentWave = waveNumber;
        isWaveCompleted = false; // Reset trạng thái khi wave thay đổi
        Debug.Log($"WaveManager: Wave changed to {waveNumber}");
    }

    public void StartWave(int waveNumber)
    {
        int enemyCount = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(enemyCountMultiplier, waveNumber - 1));
        Debug.Log($"WaveManager: Starting wave {waveNumber} with {enemyCount} enemies");
        enemySpawner.SpawnWave(waveNumber, enemyCount);
    }

    public bool IsWaveComplete()
    {
        bool isComplete = GameObject.FindGameObjectsWithTag("Enemy").Length == 0;
        Debug.Log($"WaveManager: IsWaveComplete = {isComplete}");
        return isComplete;
    }

    private void Update()
    {
        if (IsWaveComplete() && currentWave > 0 && !GameManager.Instance.isGameOver && !GameManager.Instance.isShopOpen && !isWaveCompleted)
        {
            Debug.Log($"WaveManager: Wave {currentWave} completed");
            isWaveCompleted = true; // Đánh dấu wave đã hoàn thành
            GameManager.Instance.HandleWaveCompletion(currentWave);
        }
    }
}