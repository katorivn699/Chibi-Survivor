using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public int currentWave = 0;
    public bool isGamePaused = false;
    public bool isGameOver = false;
    public bool isShopOpen = false;

    [Header("References")]
    public WaveManager waveManager;
    public PlayerStats playerStats;
    public EnemySpawner enemySpawner;


    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FindSceneReferences();
        // Khởi tạo game
        StartGame();
    }

    void FindSceneReferences()
    {
        Debug.Log("Finding scene references...");
        waveManager = Object.FindFirstObjectByType<WaveManager>();
        playerStats = Object.FindFirstObjectByType<PlayerStats>();
        enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();

        if (waveManager == null) Debug.LogError("GameManager could not find WaveManager in the scene!");
        if (playerStats == null) Debug.LogError("GameManager could not find PlayerStats in the scene!");
        if (enemySpawner == null) Debug.LogError("GameManager could not find EnemySpawner in the scene!");
    }

    public void StartGame()
    {
        currentWave = 0;
        isGameOver = false;
        isGamePaused = false;
        isShopOpen = false;
        
        // Thông báo bắt đầu game
        EventManager.Instance.GameStarted();

        AudioController.Instance.PlayBGM("BGM1");

        // Bắt đầu wave đầu tiên
        StartNextWave();
    }

    public void StartNextWave()
    {
        currentWave++;
        
        // Thông báo wave mới
        EventManager.Instance.WaveChanged(currentWave);
        
        // Kiểm tra điều kiện xuất hiện boss (sau mỗi 10 wave)
        if (currentWave % 2 == 0)
        {
            enemySpawner.SpawnBoss();
            EventManager.Instance.BossSpawned();
        }
        
        // Bắt đầu spawn quái
        waveManager.StartWave(currentWave);
        
        // Mở shop sau khi hoàn thành wave
        // Shop sẽ được mở thông qua WaveManager khi wave hoàn thành
    }

    public void OpenShop()
    {
        isShopOpen = true;
        Time.timeScale = 0; // Tạm dừng game
        EventManager.Instance.ShopOpened();
    }

    public void CloseShop()
    {
        isShopOpen = false;
        Time.timeScale = 1; // Tiếp tục game
        StartNextWave();
    }

    public void PauseGame()
    {
        if (isShopOpen || isGameOver)
        {
            return;
        }
        isGamePaused = true;
        Time.timeScale = 0;
        EventManager.Instance.GamePaused();
    }

    public void ResumeGame()
    {
        if (isShopOpen || isGameOver)
        {
            return;
        }
        isGamePaused = false;
        Time.timeScale = 1;
        EventManager.Instance.GameResumed();
    }

    public void GameOver()
    {
        AudioController.Instance.PlaySFX("Lose");
        isGameOver = true;
        Time.timeScale = 0;
        
        // Lưu kết quả cao nhất
        SaveHighestWave();
        
        EventManager.Instance.GameOver();
    }

    private void SaveHighestWave()
    {
        int highestWave = PlayerPrefs.GetInt("HighestWave", 0);
        if (currentWave > highestWave)
        {
            PlayerPrefs.SetInt("HighestWave", currentWave);
            PlayerPrefs.Save();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        CircleSceneTransition.EnsureInstanceExists();

        CircleSceneTransition.Instance.TransitionToScene("Gameplay");

        FindSceneReferences();
        StartGame();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1;

        //SceneManager.LoadScene("MainMenu");
        CircleSceneTransition.EnsureInstanceExists();
        CircleSceneTransition.Instance.TransitionToScene("MainMenu");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
