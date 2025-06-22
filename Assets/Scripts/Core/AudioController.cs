using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    public bool loop = false;
    public AudioMixerGroup mixerGroup;
    [HideInInspector]
    public AudioSource source;
}

public class AudioController : MonoBehaviour
{
    public static AudioController Instance;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmMixerGroup;
    public AudioMixerGroup sfxMixerGroup;

    [Header("Background Music")]
    public Sound[] bgmSounds;

    [Header("Sound Effects")]
    public Sound[] sfxSounds;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("UI References")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    private Dictionary<string, Sound> bgmDictionary = new Dictionary<string, Sound>();
    private Dictionary<string, Sound> sfxDictionary = new Dictionary<string, Sound>();
    private AudioSource currentBGM;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetupSliders();
        SceneManager.sceneLoaded += OnSceneLoaded;
        LoadVolumeSettings();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupSliders();
    }

    void SetupSliders()
    {
        if (masterVolumeSlider == null)
            masterVolumeSlider = GameObject.Find("MVSlider")?.GetComponent<Slider>();

        if (bgmVolumeSlider == null)
            bgmVolumeSlider = GameObject.Find("BGMSlider")?.GetComponent<Slider>();

        if (sfxVolumeSlider == null)
            sfxVolumeSlider = GameObject.Find("SFXSlider")?.GetComponent<Slider>();

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            masterVolumeSlider.value = masterVolume;
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
            bgmVolumeSlider.value = bgmVolume;
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
            sfxVolumeSlider.value = sfxVolume;
        }
    }

    void InitializeAudio()
    {
        Debug.Log("Initializing AudioController");

        // Khởi tạo BGM
        foreach (Sound sound in bgmSounds)
        {
            Debug.Log($"Adding BGM: {sound.name}");
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.outputAudioMixerGroup = sound.mixerGroup ?? bgmMixerGroup;
            bgmDictionary.Add(sound.name, sound);
        }

        // Khởi tạo SFX
        foreach (Sound sound in sfxSounds)
        {
            Debug.Log($"Adding SFX: {sound.name}, Loop: {sound.loop}, Clip: {sound.clip?.name}");
            if (!sound.loop)
            {
                sound.source = gameObject.AddComponent<AudioSource>();
                sound.source.clip = sound.clip;
                sound.source.volume = sound.volume;
                sound.source.pitch = sound.pitch;
                sound.source.loop = sound.loop;
                sound.source.outputAudioMixerGroup = sound.mixerGroup ?? sfxMixerGroup;
            }
            sfxDictionary.Add(sound.name, sound); // Thêm tất cả SFX vào sfxDictionary
        }

        Debug.Log($"Initialized {bgmDictionary.Count} BGM sounds, {sfxDictionary.Count} SFX sounds");
    }

    // Lấy thông tin Sound cho SFX
    public Sound GetSFXSound(string name)
    {
        Debug.Log($"Getting SFX sound: {name}");
        if (sfxDictionary.ContainsKey(name))
        {
            Debug.Log($"Found SFX sound: {name}, clip: {sfxDictionary[name].clip?.name}");
            return sfxDictionary[name];
        }
        Debug.LogWarning($"SFX sound: {name} not found!");
        return null;
    }

    // BGM Methods
    public void PlayBGM(string name, bool fadeIn = false, float fadeTime = 1f)
    {
        if (bgmDictionary.ContainsKey(name))
        {
            Sound sound = bgmDictionary[name];
            if (currentBGM != null && currentBGM.isPlaying)
            {
                if (fadeIn)
                {
                    StartCoroutine(CrossfadeBGM(currentBGM, sound.source, fadeTime));
                }
                else
                {
                    currentBGM.Stop();
                    sound.source.Play();
                }
            }
            else
            {
                if (fadeIn)
                {
                    StartCoroutine(FadeInBGM(sound.source, fadeTime));
                }
                else
                {
                    sound.source.Play();
                }
            }
            currentBGM = sound.source;
        }
        else
        {
            Debug.LogWarning("BGM sound: " + name + " not found!");
        }
    }

    public void StopBGM(bool fadeOut = false, float fadeTime = 1f)
    {
        if (currentBGM != null && currentBGM.isPlaying)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutBGM(currentBGM, fadeTime));
            }
            else
            {
                currentBGM.Stop();
            }
        }
    }

    public void PauseBGM()
    {
        if (currentBGM != null && currentBGM.isPlaying)
        {
            currentBGM.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (currentBGM != null && !currentBGM.isPlaying)
        {
            currentBGM.UnPause();
        }
    }

    // SFX Methods
    public void PlaySFX(string name, float volumeScale = 1f)
    {
        if (sfxDictionary.ContainsKey(name))
        {
            Sound sound = sfxDictionary[name];
            if (sound.source != null)
            {
                sound.source.PlayOneShot(sound.clip, sound.volume * volumeScale);
            }
            else
            {
                Debug.LogWarning($"No AudioSource assigned for SFX: {name}, playing at point");
                AudioSource.PlayClipAtPoint(sound.clip, Vector3.zero, sound.volume * volumeScale);
            }
        }
        else
        {
            Debug.LogWarning("SFX sound: " + name + " not found!");
        }
    }

    public void PlaySFXAtPoint(string name, Vector3 position, float volumeScale = 1f)
    {
        if (sfxDictionary.ContainsKey(name))
        {
            Sound sound = sfxDictionary[name];
            AudioSource.PlayClipAtPoint(sound.clip, position, sound.volume * volumeScale);
        }
        else
        {
            Debug.LogWarning($"SFX sound: {name} not found!");
        }
    }

    // Volume Control Methods
    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        if (volume <= 0f)
        {
            audioMixer.SetFloat("MasterVolume", -80f);
        }
        else
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20f);
        }
        SaveVolumeSettings();
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        if (volume <= 0f)
        {
            audioMixer.SetFloat("BGMVolume", -80f);
        }
        else
        {
            audioMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20f);
        }
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (volume <= 0f)
        {
            audioMixer.SetFloat("SFXVolume", -80f);
        }
        else
        {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20f);
        }
        SaveVolumeSettings();
    }

    // Fade Effects
    IEnumerator FadeInBGM(AudioSource audioSource, float fadeTime)
    {
        audioSource.volume = 0f;
        audioSource.Play();
        while (audioSource.volume < bgmVolume)
        {
            audioSource.volume += Time.deltaTime / fadeTime;
            yield return null;
        }
        audioSource.volume = bgmVolume;
    }

    IEnumerator FadeOutBGM(AudioSource audioSource, float fadeTime)
    {
        float startVolume = audioSource.volume;
        while (audioSource.volume > 0f)
        {
            audioSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    IEnumerator CrossfadeBGM(AudioSource fadeOut, AudioSource fadeIn, float fadeTime)
    {
        float startVolume = fadeOut.volume;
        fadeIn.volume = 0f;
        fadeIn.Play();
        float elapsedTime = 0f;
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeTime;
            fadeOut.volume -= startVolume * Time.deltaTime / fadeTime;
            fadeIn.volume += Time.deltaTime / fadeTime;
            yield return null;
        }
        fadeOut.Stop();
        fadeOut.volume = startVolume;
    }

    // Save Volume Settings
    void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    void LoadVolumeSettings()
    {
        if (PlayerPrefs.HasKey("MasterVolume"))
        {
            SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume"));
        }
        if (PlayerPrefs.HasKey("BGMVolume"))
        {
            SetBGMVolume(PlayerPrefs.GetFloat("BGMVolume"));
        }
        if (PlayerPrefs.HasKey("SFXVolume"))
        {
            SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume"));
        }
    }
}