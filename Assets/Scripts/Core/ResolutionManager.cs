using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ResolutionManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    private float currentRefreshRate;
    private int currentResolutionIndex = 0;

    IEnumerator Start()
    {
        yield return null;
        SetupResolutions();

        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    void SetupResolutions()
    {
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();

        resolutionDropdown.ClearOptions();
        currentRefreshRate = Mathf.Round((float)Screen.currentResolution.refreshRateRatio.value);

        for (int i = 0; i < resolutions.Length; i++)
        {
            float resRefreshRate = Mathf.Round((float)resolutions[i].refreshRateRatio.value);

            if (Mathf.Approximately(resRefreshRate, currentRefreshRate))
            {
                float aspectRatio = (float)resolutions[i].width / (float)resolutions[i].height;

                if (Mathf.Approximately(aspectRatio, 16f / 9f) || Mathf.Approximately(aspectRatio, 16f / 10f))
                {
                    filteredResolutions.Add(resolutions[i]);
                }
            }
        }


        // Tạo danh sách options cho dropdown
        List<string> options = new List<string>();
        for (int i = 0; i < filteredResolutions.Count; i++)
        {
            string resolutionOption = filteredResolutions[i].width + "x" +
                                    filteredResolutions[i].height + " " +
                                    Mathf.RoundToInt((float)filteredResolutions[i].refreshRateRatio.value) + "Hz";
            options.Add(resolutionOption);

            // Tìm resolution hiện tại dựa trên PlayerPrefs hoặc Screen hiện tại
            Resolution currentRes = GetCurrentResolution();
            if (filteredResolutions[i].width == currentRes.width &&
                filteredResolutions[i].height == currentRes.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    Resolution GetCurrentResolution()
    {
        // Ưu tiên lấy từ PlayerPrefs nếu có
        if (PlayerPrefs.HasKey("ResolutionWidth") && PlayerPrefs.HasKey("ResolutionHeight"))
        {
            Resolution savedRes = new Resolution();
            savedRes.width = PlayerPrefs.GetInt("ResolutionWidth");
            savedRes.height = PlayerPrefs.GetInt("ResolutionHeight");
            savedRes.refreshRateRatio = Screen.currentResolution.refreshRateRatio;  
            return savedRes;
        }

        // Nếu không có trong PlayerPrefs thì lấy từ Screen hiện tại
        return Screen.currentResolution;
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex >= 0 && resolutionIndex < filteredResolutions.Count)
        {
            Resolution resolution = filteredResolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);

            currentResolutionIndex = resolutionIndex;
            SaveSettings();
        }
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        SaveSettings();
    }

    void SaveSettings()
    {
        // Lưu cả index và thông tin chi tiết của resolution
        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);

        if (currentResolutionIndex < filteredResolutions.Count)
        {
            PlayerPrefs.SetInt("ResolutionWidth", filteredResolutions[currentResolutionIndex].width);
            PlayerPrefs.SetInt("ResolutionHeight", filteredResolutions[currentResolutionIndex].height);
        }

        PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        // Ensure filteredResolutions is initialized
        if (filteredResolutions == null || filteredResolutions.Count == 0)
        {
            Debug.LogWarning("Filtered resolutions not initialized yet. Skipping LoadSettings.");
            return;
        }

        // Load resolution settings
        if (PlayerPrefs.HasKey("ResolutionIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("ResolutionIndex");
            if (savedIndex >= 0 && savedIndex < filteredResolutions.Count)
            {
                SetResolution(savedIndex);
                if (resolutionDropdown != null)
                {
                    resolutionDropdown.value = savedIndex;
                    resolutionDropdown.RefreshShownValue();
                }
            }
        }

        // Load fullscreen settings
        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            bool isFullscreen = PlayerPrefs.GetInt("Fullscreen") == 1;
            Screen.fullScreen = isFullscreen;
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = isFullscreen;
        }
    }

    public void ApplyCurrentSettings()
    {
        // Only apply settings if everything is properly initialized
        if (filteredResolutions != null && filteredResolutions.Count > 0)
        {
            LoadSettings();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            // Add a small delay to ensure initialization is complete
            StartCoroutine(DelayedApplySettings());
        }
    }

    IEnumerator DelayedApplySettings()
    {
        yield return new WaitForEndOfFrame();
        ApplyCurrentSettings();
    }

}
