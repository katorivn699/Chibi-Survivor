using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    // Events
    public event Action OnGameStarted;
    public event Action<int> OnWaveChanged;
    public event Action OnGameOver;
    public event Action<float> OnPlayerHealthChanged;
    public event Action<int> OnMoneyChanged;
    public event Action OnBossSpawned;
    public event Action OnShopOpened;
    public event Action OnGamePaused;
    public event Action OnGameResumed;

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

    // Methods to trigger events
    public void GameStarted() => OnGameStarted?.Invoke();
    public void WaveChanged(int waveNumber) => OnWaveChanged?.Invoke(waveNumber);
    public void GameOver() => OnGameOver?.Invoke();
    public void PlayerHealthChanged(float health) => OnPlayerHealthChanged?.Invoke(health);
    public void MoneyChanged(int money) => OnMoneyChanged?.Invoke(money);
    public void BossSpawned() => OnBossSpawned?.Invoke();
    public void ShopOpened() => OnShopOpened?.Invoke();
    public void GamePaused() => OnGamePaused?.Invoke();
    public void GameResumed() => OnGameResumed?.Invoke();
}
