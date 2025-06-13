using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Pause Panel")]
    public GameObject pausePanel;
    
    [Header("Settings")]
    public Slider musicVolumeSlider;
    public AudioSource musicSource;
    
    private void Start()
    {
        // Ẩn pause panel
        pausePanel.SetActive(false);
        
        // Đăng ký sự kiện
        EventManager.Instance.OnGamePaused += ShowPauseMenu;
        EventManager.Instance.OnGameResumed += HidePauseMenu;
        
        // Cập nhật slider
        if (musicSource != null)
        {
            musicVolumeSlider.value = musicSource.volume;
        }
    }
    
    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnGamePaused -= ShowPauseMenu;
            EventManager.Instance.OnGameResumed -= HidePauseMenu;
        }
    }
    
    private void Update()
    {
        // Kiểm tra nếu nhấn Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    public void TogglePause()
    {
        if (GameManager.Instance.isGamePaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }
    
    public void PauseGame()
    {
        GameManager.Instance.PauseGame();
    }
    
    public void ResumeGame()
    {
        GameManager.Instance.ResumeGame();
    }
    
    private void ShowPauseMenu()
    {
        pausePanel.SetActive(true);
    }
    
    private void HidePauseMenu()
    {
        pausePanel.SetActive(false);
    }
    
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
        
        // Lưu cài đặt
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }
    
    public void ReturnToMainMenu()
    {
        GameManager.Instance.LoadMainMenu();
    }
}
