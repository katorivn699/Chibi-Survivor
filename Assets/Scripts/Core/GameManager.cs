using System.Collections.Generic;
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

    [Header("Map Settings")]
    public List<MapData> availableMaps;
    public MapData currentMap;
    private int currentMapIndex = 0;

    [Header("References")]
    public WaveManager waveManager;
    public PlayerStats playerStats;
    public EnemySpawner enemySpawner;
    private GameObject lightGateInstance;

    [Header("Light Gate Settings")]
    [Tooltip("Minimum distance from player for Light Gate spawn")]
    public float minDistanceFromPlayer = 5f;
    [Tooltip("SFX name for Light Gate sound")]
    public string lightGateSoundName = "LightGateLoop";

    private bool isNewGame = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        LoadGameProgress();
    }

    private void Start()
    {
        if (availableMaps.Count == 0)
        {
            Debug.LogError("No maps assigned to GameManager!");
        }
        Debug.Log("GameManager Start");
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

    public void LoadMap(MapData map)
    {
        currentMap = map;
        CircleSceneTransition.EnsureInstanceExists();
        CircleSceneTransition.Instance.TransitionToScene(map.sceneName, () =>
        {
            FindSceneReferences();
            enemySpawner.InitializeForMap(map);
            StartGame();
            EventManager.Instance.MapChanged(map.mapName);
            AudioController.Instance.PlayBGM(map.backgroundMusic);
        });
    }

    public void SelectMap(int mapIndex)
    {
        if (mapIndex >= 0 && mapIndex < availableMaps.Count)
        {
            currentMapIndex = mapIndex;
            LoadMap(availableMaps[mapIndex]);
        }
    }

    public void StartGame()
    {
        if (isNewGame)
        {
            currentWave = 0; 
            isGameOver = false;
            isGamePaused = false;
            isShopOpen = false;

            if (lightGateInstance != null)
            {
                Destroy(lightGateInstance);
                lightGateInstance = null;
            }

            EventManager.Instance.GameStarted();
        }
        StartNextWave();
        isNewGame = false; 
    }

    public void StartNextWave()
    {
        currentWave++;
        EventManager.Instance.WaveChanged(currentWave);

        if (currentWave % 2 == 0)
        {
            enemySpawner.SpawnBoss();
            EventManager.Instance.BossSpawned();
        }

        Debug.Log($"Starting wave {currentWave}");
        waveManager.StartWave(currentWave);
    }

    public void HandleWaveCompletion(int waveNumber)
    {
        if (waveNumber >= currentMap.maxWaves)
        {
            if (lightGateInstance == null)
            {
                SpawnLightGate();
            }
        }
        else
        {
            OpenShop();
        }
    }

    private void SpawnLightGate()
    {
        if (currentMap == null)
        {
            return;
        }

        if (currentMap.lightGatePrefab == null)
        {
            SwitchToNextMap();
            return;
        }

        if (lightGateInstance != null)
        {
            Destroy(lightGateInstance);
        }

        Vector2 spawnPosition;
        if (currentMap.lightGateSpawnPoints != null && currentMap.lightGateSpawnPoints.Count > 0)
        {
            spawnPosition = GetValidSpawnPointFromList();
        }
        else
        {
            spawnPosition = GetValidSpawnPosition();
        }

        lightGateInstance = Instantiate(currentMap.lightGatePrefab, spawnPosition, Quaternion.identity);

        Sound sound = AudioController.Instance.GetSFXSound(lightGateSoundName);
        if (sound != null)
        {
            AudioSource audioSource = lightGateInstance.AddComponent<AudioSource>();
            audioSource.clip = sound.clip;
            audioSource.volume = sound.volume;
            audioSource.pitch = sound.pitch;
            audioSource.loop = true;
            audioSource.outputAudioMixerGroup = sound.mixerGroup ?? AudioController.Instance.sfxMixerGroup;
            audioSource.spatialBlend = 1f; 
            audioSource.minDistance = 5f; 
            audioSource.maxDistance = 20f;
            audioSource.Play();
        }
        else
        {
        }
    }

    private Vector2 GetValidSpawnPointFromList()
    {
        int maxAttempts = currentMap.lightGateSpawnPoints.Count;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 spawnPoint = currentMap.lightGateSpawnPoints[Random.Range(0, currentMap.lightGateSpawnPoints.Count)];
            float distanceToPlayer = Vector2.Distance(spawnPoint, playerStats.transform.position);
            if (distanceToPlayer >= minDistanceFromPlayer && !Physics2D.OverlapCircle(spawnPoint, currentMap.lightGateSpawnCheckRadius))
            {
                return spawnPoint;
            }
        }
        return GetValidSpawnPosition();
    }

    private Vector2 GetValidSpawnPosition()
    {
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomPoint = (Vector2)playerStats.transform.position + Random.insideUnitCircle * currentMap.spawnRadius;
            float distanceToPlayer = Vector2.Distance(randomPoint, playerStats.transform.position);
            if (distanceToPlayer >= minDistanceFromPlayer && !Physics2D.OverlapCircle(randomPoint, currentMap.lightGateSpawnCheckRadius))
            {
                return randomPoint;
            }
        }
        return playerStats.transform.position + new Vector3(minDistanceFromPlayer, 0, 0);
    }

    public void OpenShop()
    {
        isShopOpen = true;
        Time.timeScale = 0;
        EventManager.Instance.ShopOpened();
    }

    public void CloseShop()
    {
        isShopOpen = false;
        Time.timeScale = 1;
        StartNextWave();
    }

    public void PauseGame()
    {
        if (isShopOpen || isGameOver) return;
        isGamePaused = true;
        Time.timeScale = 0;
        EventManager.Instance.GamePaused();
    }

    public void ResumeGame()
    {
        if (isShopOpen || isGameOver) return;
        isGamePaused = false;
        Time.timeScale = 1;
        EventManager.Instance.GameResumed();
    }

    public void GameOver()
    {
        AudioController.Instance.PlaySFX("Lose");
        isGameOver = true;
        Time.timeScale = 0;
        SaveHighestWave();
        EventManager.Instance.GameOver();
    }

    private void SaveHighestWave()
    {
        int highestWave = PlayerPrefs.GetInt($"HighestWave_{currentMap.mapName}", 0);
        if (currentWave > highestWave)
        {
            PlayerPrefs.SetInt($"HighestWave_{currentMap.mapName}", currentWave);
            PlayerPrefs.Save();
        }
    }

    public void RestartGame()
    {
        isNewGame = true; 
        Time.timeScale = 1;
        LoadMap(availableMaps[0]);
        EventManager.Instance.GameRestarted();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1;
        CircleSceneTransition.EnsureInstanceExists();
        CircleSceneTransition.Instance.TransitionToScene("MainMenu");
    }

    public void SwitchToNextMap()
    {
        Debug.Log("SwitchToNextMap called");
        if (lightGateInstance != null)
        {
            Destroy(lightGateInstance);
            lightGateInstance = null;
        }

        SaveGameProgress();
        currentMapIndex = (currentMapIndex + 1) % availableMaps.Count;
        LoadMap(availableMaps[currentMapIndex]);
    }

    private void SaveGameProgress()
    {
        PlayerPrefs.SetInt("CurrentWave", currentWave);
        PlayerPrefs.SetInt("CurrentMapIndex", currentMapIndex);
        PlayerPrefs.Save();
    }

    private void LoadGameProgress()
    {
        currentWave = PlayerPrefs.GetInt("CurrentWave", 0);
        currentMapIndex = PlayerPrefs.GetInt("CurrentMapIndex", 0);
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