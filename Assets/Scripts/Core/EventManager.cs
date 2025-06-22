using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public event Action OnGameStarted;
    public event Action<int> OnWaveChanged;
    public event Action OnGameOver;
    public event Action<float> OnPlayerHealthChanged;
    public event Action<int> OnMoneyChanged;
    public event Action OnBossSpawned;
    public event Action OnShopOpened;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action<string> OnMapChanged;
    public event Action OnGameRestarted;

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
    }

    public void GameStarted() => OnGameStarted?.Invoke();
    public void WaveChanged(int waveNumber) => OnWaveChanged?.Invoke(waveNumber);
    public void GameOver() => OnGameOver?.Invoke();
    public void PlayerHealthChanged(float health) => OnPlayerHealthChanged?.Invoke(health);
    public void MoneyChanged(int money) => OnMoneyChanged?.Invoke(money);
    public void BossSpawned() => OnBossSpawned?.Invoke();
    public void ShopOpened() => OnShopOpened?.Invoke();
    public void GamePaused() => OnGamePaused?.Invoke();
    public void GameResumed() => OnGameResumed?.Invoke();
    public void MapChanged(string mapName) => OnMapChanged?.Invoke(mapName);
    public void GameRestarted() => OnGameRestarted?.Invoke();

}
