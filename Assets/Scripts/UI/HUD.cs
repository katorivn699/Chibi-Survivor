using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUD : MonoBehaviour
{
    [Header("Health")]
    public Slider healthForegroundSlider; // Thanh máu đỏ (trên)
    public Slider healthBackgroundSlider; // Thanh hiệu ứng (dưới)

    [Header("Health Speed (Relative % per second)")]
    [Range(0f, 1f)] public float healSpeedPercent = 0.15f;    // 15% mỗi giây
    [Range(0f, 1f)] public float damageSpeedPercent = 0.1f;   // 10% mỗi giây

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
    private EnemyController currentBoss;
    private CanvasGroup bossCanvasGroup;
    private RectTransform bossRect;
    public float gameOverFadeDuration = 0.5f;
    public float bossFadeDuration = 0.5f;
    public float bossSlideOffset = 80f;

    private float currentHealth;
    private float targetHealth;
    private bool isHealing = false;

    [Header("Boss Health")]
    public GameObject bossHealthGroup;
    public Slider bossHealthSlider;
    public TextMeshProUGUI bossNameText;

    private void Start()
    {
        // Register events
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

        // Init boss health bar
        bossCanvasGroup = bossHealthGroup.GetComponent<CanvasGroup>();
        bossRect = bossHealthGroup.GetComponent<RectTransform>();
        bossHealthGroup.SetActive(false);
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
        float maxHealth = healthForegroundSlider.maxValue;

        if (isHealing)
        {
            float healSpeedActual = healSpeedPercent * maxHealth;

            healthForegroundSlider.value = Mathf.MoveTowards(
                healthForegroundSlider.value,
                targetHealth,
                healSpeedActual * Time.deltaTime
            );

            if (Mathf.Approximately(healthForegroundSlider.value, targetHealth))
            {
                backgroundFillImage.color = defaultColor;
                isHealing = false;
            }
        }
        else
        {
            float damageSpeedActual = damageSpeedPercent * maxHealth;

            healthBackgroundSlider.value = Mathf.MoveTowards(
                healthBackgroundSlider.value,
                targetHealth,
                damageSpeedActual * Time.deltaTime
            );
        }

        if (currentBoss != null && !currentBoss.isDead)
        {
            bossHealthSlider.value = currentBoss.currentHealth;
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

    public void ShowBossHealth(EnemyController boss)
    {
        currentBoss = boss;
        bossHealthGroup.SetActive(true);
        bossHealthSlider.maxValue = boss.enemyData.maxHealth;
        bossHealthSlider.value = boss.currentHealth;
        bossNameText.text = boss.enemyData.enemyName;

        StartCoroutine(FadeSlideInBossBar());
    }

    private IEnumerator FadeSlideInBossBar()
    {
        bossHealthGroup.SetActive(true);
        bossCanvasGroup.alpha = 0f;

        Vector2 targetPos = bossRect.anchoredPosition;
        Vector2 startPos = targetPos + new Vector2(0f, bossSlideOffset);
        bossRect.anchoredPosition = startPos;

        float timer = 0f;
        while (timer < bossFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / bossFadeDuration);

            // Slide
            bossRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            // Fade
            bossCanvasGroup.alpha = t;

            yield return null;
        }

        bossRect.anchoredPosition = targetPos;
        bossCanvasGroup.alpha = 1f;
    }


    public void HideBossHealth()
    {
        StartCoroutine(FadeOutBossBar());
        currentBoss = null;
    }

    private IEnumerator FadeOutBossBar()
    {
        float timer = 0f;
        float startAlpha = bossCanvasGroup.alpha;

        while (timer < bossFadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / bossFadeDuration;
            bossCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        bossCanvasGroup.alpha = 0f;
        bossHealthGroup.SetActive(false);
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
