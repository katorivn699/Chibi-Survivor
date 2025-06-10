using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public int baseEnemyCount = 10;
    public float enemyCountMultiplier = 1.5f;
    
    [Header("References")]
    public EnemySpawner enemySpawner;
    
    private int currentWave = 0;
    
    private void Start()
    {
        // Đăng ký sự kiện
        EventManager.Instance.OnWaveChanged += OnWaveChanged;
    }
    
    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnWaveChanged -= OnWaveChanged;
        }
    }
    
    private void OnWaveChanged(int waveNumber)
    {
        currentWave = waveNumber;
    }
    
    public void StartWave(int waveNumber)
    {
        // Tính toán số lượng kẻ địch
        int enemyCount = Mathf.RoundToInt(baseEnemyCount * Mathf.Pow(enemyCountMultiplier, waveNumber - 1));
        Debug.Log("waveNumber in StartWave():" + waveNumber);

        // Spawn kẻ địch
        enemySpawner.SpawnWave(waveNumber, enemyCount);
    }
    
    public bool IsWaveComplete()
    {
        // Kiểm tra nếu không còn kẻ địch
        return GameObject.FindGameObjectsWithTag("Enemy").Length == 0;
    }
    
    private void Update()
    {
        // Kiểm tra nếu wave đã hoàn thành
        if (IsWaveComplete() && currentWave > 0 && !GameManager.Instance.isGameOver && !GameManager.Instance.isShopOpen)
        {
            // Mở shop sau khi hoàn thành wave
            GameManager.Instance.OpenShop();
        }
    }
}
