using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUD : MonoBehaviour
{
    [Header("Health")]
    public Slider healthForegroundSlider; // Thanh máu đỏ (trên)
    public Slider healthBackgroundSlider; // Thanh hiệu ứng (dưới)
    public float healSpeed = 15f; // Tốc độ hồi máu
    public float damageSpeed = 10f; // Tốc độ mất máu

    [Header("Money")]
    public TextMeshProUGUI moneyText;

    [Header("Wave")]
    public TextMeshProUGUI waveText;

    [Header("Colors")]
    public Image backgroundFillImage;
    public Color defaultColor = Color.white;
    public Color healColor = Color.green;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalWaveText;
    public TextMeshProUGUI highestWaveText;

    private CanvasGroup gameOverCanvasGroup;
    public float gameOverFadeDuration = 1f;

    private float currentHealth;
    private float targetHealth;
    private bool isHealing = false;

    private void Start()
    {
        // Đăng ký sự kiện
        EventManager.Instance.OnPlayerHealthChanged += UpdateHealth;
        EventManager.Instance.OnMoneyChanged += UpdateMoney;
        EventManager.Instance.OnWaveChanged += UpdateWave;
        EventManager.Instance.OnGameOver += ShowGameOver;

        gameOverPanel.SetActive(false);

        // Init sliders
        PlayerStats playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        float max = playerStats.maxHealth;
        gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
        gameOverPanel.SetActive(false);
        gameOverCanvasGroup.alpha = 0f;
        healthForegroundSlider.maxValue = max;
        healthBackgroundSlider.maxValue = max;
        currentHealth = max;
        targetHealth = max;
        healthForegroundSlider.value = max;
        healthBackgroundSlider.value = max;
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnPlayerHealthChanged -= UpdateHealth;
            EventManager.Instance.OnMoneyChanged -= UpdateMoney;
            EventManager.Instance.OnWaveChanged -= UpdateWave;
            EventManager.Instance.OnGameOver -= ShowGameOver;
        }
    }

    private void UpdateHealth(float newHealth)
    {
        PlayerStats playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();

        healthForegroundSlider.maxValue = playerStats.maxHealth;
        healthBackgroundSlider.maxValue = playerStats.maxHealth;

        targetHealth = newHealth;

        if (newHealth < currentHealth)
        {
            // Mất máu: foreground giảm ngay, background trôi sau
            healthForegroundSlider.value = newHealth;

            // Ngừng hồi máu nếu đang hồi, reset màu
            isHealing = false;
            backgroundFillImage.color = defaultColor;
        }
        else if (newHealth > currentHealth)
        {
            // Hồi máu: background đầy ngay, đổi màu xanh, foreground tăng dần
            healthBackgroundSlider.value = newHealth;
            backgroundFillImage.color = healColor;
            isHealing = true;
        }

        currentHealth = newHealth;
    }


    void Update()
    {
        if (isHealing)
        {
            // foreground tăng dần
            healthForegroundSlider.value = Mathf.MoveTowards(
                healthForegroundSlider.value,
                targetHealth,
                healSpeed * Time.deltaTime
            );

            if (Mathf.Approximately(healthForegroundSlider.value, targetHealth))
            {
                backgroundFillImage.color = defaultColor;
                isHealing = false;
            }
        }
        else
        {
            // background giảm dần theo sau khi mất máu
            healthBackgroundSlider.value = Mathf.MoveTowards(
                healthBackgroundSlider.value,
                targetHealth,
                damageSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateMoney(int money)
    {
        moneyText.text = money.ToString();
    }

    private void UpdateWave(int wave)
    {
        waveText.text = $"Wave: {wave}";
    }

    private void ShowGameOver()
    {
        int currentWave = GameManager.Instance.currentWave;
        int highestWave = PlayerPrefs.GetInt("HighestWave", 0);

        finalWaveText.text = $"Wave: {currentWave}";
        highestWaveText.text = $"Highest: {highestWave}";

        StartCoroutine(FadeInGameOver());
    }

    private IEnumerator FadeInGameOver()
    {
        gameOverPanel.SetActive(true);

        float timer = 0f;

        while (timer < gameOverFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = timer / gameOverFadeDuration;
            gameOverCanvasGroup.alpha = Mathf.Clamp01(alpha);
            yield return null;
        }

        gameOverCanvasGroup.alpha = 1f;

        // Pause game AFTER animation finishes
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        GameManager.Instance.RestartGame();
    }

    public void ReturnToMainMenu()
    {
        GameManager.Instance.LoadMainMenu();
    }
}
