using UnityEngine;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button mainMenuButton;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            Debug.Log("Restart button bound to GameManager.RestartGame");
        }
        else
        {
            Debug.LogWarning("RestartButton is not assigned!", this);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(() => GameManager.Instance.LoadMainMenu());
            Debug.Log("MainMenu button bound to GameManager.LoadMainMenu");
        }
        else
        {
            Debug.LogWarning("MainMenuButton is not assigned!", this);
        }


        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameOver += ShowGameOverPanel;
            EventManager.Instance.OnGameRestarted += HideGameOverPanel;
        }
    }

    private void OnDestroy()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGameOver -= ShowGameOverPanel;
            EventManager.Instance.OnGameRestarted -= HideGameOverPanel;
        }
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    private void HideGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }
}
