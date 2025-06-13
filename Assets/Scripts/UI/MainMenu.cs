using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject characterSelectPanel;
    
    [Header("Character Selection")]
    public List<PlayerData> availableCharacters;
    public Transform characterContainer;
    public GameObject characterButtonPrefab;
    
    [Header("Character Preview")]
    public Image characterPreviewImage;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI characterDescriptionText;
    public TextMeshProUGUI characterStatsText;


    private int selectedCharacterIndex = 0;
    
    private void Start()
    {
        CircleSceneTransition.EnsureInstanceExists();

        // Hiển thị panel chính
        ShowMainPanel();
        
        // Khởi tạo các nút chọn nhân vật
        InitializeCharacterButtons();

        AudioController.Instance.PlayBGM("MainMenu");
    }
    
    private void InitializeCharacterButtons()
    {
        // Xóa các nút cũ (nếu có)
        foreach (Transform child in characterContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Tạo nút cho mỗi nhân vật
        for (int i = 0; i < availableCharacters.Count; i++)
        {
            PlayerData character = availableCharacters[i];
            
            // Tạo nút
            GameObject buttonObj = Instantiate(characterButtonPrefab, characterContainer);
            Button button = buttonObj.GetComponent<Button>();
            
            // Cập nhật hình ảnh và tên
            Image buttonImage = buttonObj.GetComponentInChildren<Image>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonImage != null && character.playerSprite != null)
            {
                buttonImage.sprite = character.playerSprite;
            }
            
            if (buttonText != null)
            {
                buttonText.text = character.playerName;
            }
            
            // Thêm sự kiện click
            int index = i; // Lưu index để sử dụng trong lambda
            button.onClick.AddListener(() => PreviewCharacter(index));
        }
        
        // Hiển thị nhân vật đầu tiên
        if (availableCharacters.Count > 0)
        {
            PreviewCharacter(0);
        }
    }
    
    private void PreviewCharacter(int index)
    {
        if (index < 0 || index >= availableCharacters.Count) return;
        
        selectedCharacterIndex = index;
        PlayerData character = availableCharacters[index];
        
        // Cập nhật hình ảnh
        if (characterPreviewImage != null && character.playerSprite != null)
        {
            characterPreviewImage.sprite = character.playerSprite;
        }
        
        // Cập nhật tên
        if (characterNameText != null)
        {
            characterNameText.text = character.playerName;
        }
        
        // Cập nhật mô tả
        if (characterDescriptionText != null)
        {
            characterDescriptionText.text = character.description;
        }
        
        // Cập nhật stats
        if (characterStatsText != null)
        {
            characterStatsText.text = $"Health: {character.maxHealth}\nSpeed: {character.moveSpeed}\nDamage: x{character.damageMultiplier}\nAttack Speed: x{character.attackSpeedMultiplier}";
        }
    }
    
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        characterSelectPanel.SetActive(false);
    }

    public void ShowSettingsPanel()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        characterSelectPanel.SetActive(false);
    }
    
    public void ShowCharacterSelectPanel()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
        characterSelectPanel.SetActive(true);
    }
    
    public void StartGame()
    {
        // Chuyển đến màn hình chọn nhân vật
        ShowCharacterSelectPanel();
    }
    
    public void SelectCharacter()
    {
        Debug.Log("Selected character: " + availableCharacters[selectedCharacterIndex].playerName);
        // Lưu nhân vật đã chọn
        PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
        PlayerPrefs.Save();

        // Chuyển đến scene gameplay
        //SceneManager.LoadScene("Gameplay");
        CircleSceneTransition.Instance.TransitionToScene("Gameplay");
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
