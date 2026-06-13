using UnityEngine;

public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    private struct ResolutionPreset
    {
        public readonly int Width;
        public readonly int Height;

        public ResolutionPreset(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    private static readonly ResolutionPreset[] ResolutionPresets =
    {
        new ResolutionPreset(1280, 720),
        new ResolutionPreset(1600, 900),
        new ResolutionPreset(1920, 1080),
        new ResolutionPreset(2560, 1440)
    };

    private static readonly int[] FpsLimits = { 0, 30, 60, 120, 144 };

    private const string ResolutionKey = "Settings.ResolutionIndex";
    private const string WindowModeKey = "Settings.WindowModeIndex";
    private const string VSyncKey = "Settings.VSync";
    private const string FpsLimitKey = "Settings.FpsLimitIndex";
    private const string QualityKey = "Settings.QualityIndex";
    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SFXVolume";
    private const string MouseSensitivityKey = "MouseSensitivity";

    private int resolutionIndex;
    private int windowModeIndex;
    private bool vSyncEnabled;
    private int fpsLimitIndex;
    private int qualityIndex;
    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private float mouseSensitivity;

    public int ResolutionCount => ResolutionPresets.Length;
    public int FpsLimitCount => FpsLimits.Length;
    public int QualityCount => Mathf.Max(1, QualitySettings.names.Length);
    public int WindowModeCount => 2;

    public float MasterVolume => masterVolume;
    public float MusicVolume => musicVolume;
    public float SFXVolume => sfxVolume;
    public float MouseSensitivity => mouseSensitivity;
    public bool VSyncEnabled => vSyncEnabled;

    public string ResolutionLabel => ResolutionPresets[resolutionIndex].Width + " x " + ResolutionPresets[resolutionIndex].Height;
    public string WindowModeLabel => windowModeIndex == 0 ? "Pelny ekran" : "Okno";
    public string VSyncLabel => vSyncEnabled ? "Wlaczone" : "Wylaczone";
    public string FpsLimitLabel => FpsLimits[fpsLimitIndex] <= 0 ? "Bez limitu" : FpsLimits[fpsLimitIndex] + " FPS";
    public string QualityLabel
    {
        get
        {
            string[] names = QualitySettings.names;
            if (names == null || names.Length == 0)
            {
                return "Domyslna";
            }

            return names[Mathf.Clamp(qualityIndex, 0, names.Length - 1)];
        }
    }

    public static GameSettingsManager EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject managerObject = GameObject.Find("GameManager");
        if (managerObject == null)
        {
            managerObject = new GameObject("GameManager");
        }

        return managerObject.AddComponent<GameSettingsManager>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
        ApplyAll();
    }

    public void Load()
    {
        int defaultResolution = GetDefaultResolutionIndex();
        resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(ResolutionKey, defaultResolution), 0, ResolutionPresets.Length - 1);
        windowModeIndex = Mathf.Clamp(PlayerPrefs.GetInt(WindowModeKey, Screen.fullScreen ? 0 : 1), 0, WindowModeCount - 1);
        vSyncEnabled = PlayerPrefs.GetInt(VSyncKey, 0) == 1;
        fpsLimitIndex = Mathf.Clamp(PlayerPrefs.GetInt(FpsLimitKey, 2), 0, FpsLimits.Length - 1);
        qualityIndex = Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, QualityCount - 1);
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.3f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 0.7f));
        mouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, 2f), 0.5f, 5f);
    }

    public void Save()
    {
        PlayerPrefs.SetInt(ResolutionKey, resolutionIndex);
        PlayerPrefs.SetInt(WindowModeKey, windowModeIndex);
        PlayerPrefs.SetInt(VSyncKey, vSyncEnabled ? 1 : 0);
        PlayerPrefs.SetInt(FpsLimitKey, fpsLimitIndex);
        PlayerPrefs.SetInt(QualityKey, qualityIndex);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetFloat(MouseSensitivityKey, mouseSensitivity);
        PlayerPrefs.Save();
    }

    public void ApplyAll()
    {
        ApplyDisplaySettings();
        ApplyAudioSettings();
        ApplyControlSettings();
    }

    public void ApplyDisplaySettings()
    {
        qualityIndex = Mathf.Clamp(qualityIndex, 0, QualityCount - 1);
        QualitySettings.SetQualityLevel(qualityIndex, true);
        QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        Application.targetFrameRate = vSyncEnabled ? -1 : FpsLimits[fpsLimitIndex];

        ResolutionPreset preset = ResolutionPresets[resolutionIndex];
        FullScreenMode mode = windowModeIndex == 0 ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.SetResolution(preset.Width, preset.Height, mode);
    }

    public void ApplyAudioSettings()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.SetMasterVolume(masterVolume);
        AudioManager.Instance.SetMusicVolume(musicVolume);
        AudioManager.Instance.SetSFXVolume(sfxVolume);
    }

    public void ApplyControlSettings()
    {
        SimplePlayerController[] controllers = FindObjectsByType<SimplePlayerController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (SimplePlayerController controller in controllers)
        {
            controller.sensitivity = mouseSensitivity;
        }
    }

    public void CycleResolution(int direction)
    {
        resolutionIndex = CycleIndex(resolutionIndex, ResolutionPresets.Length, direction);
        Save();
        ApplyDisplaySettings();
    }

    public void CycleWindowMode(int direction)
    {
        windowModeIndex = CycleIndex(windowModeIndex, WindowModeCount, direction);
        Save();
        ApplyDisplaySettings();
    }

    public void ToggleWindowModeShortcut()
    {
        windowModeIndex = windowModeIndex == 0 ? 1 : 0;
        Save();
        ApplyDisplaySettings();
    }

    public void ToggleVSync()
    {
        vSyncEnabled = !vSyncEnabled;
        Save();
        ApplyDisplaySettings();
    }

    public void CycleFpsLimit(int direction)
    {
        fpsLimitIndex = CycleIndex(fpsLimitIndex, FpsLimits.Length, direction);
        Save();
        ApplyDisplaySettings();
    }

    public void CycleQuality(int direction)
    {
        qualityIndex = CycleIndex(qualityIndex, QualityCount, direction);
        Save();
        ApplyDisplaySettings();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        Save();
        ApplyAudioSettings();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        Save();
        ApplyAudioSettings();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        Save();
        ApplyAudioSettings();
    }

    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 0.5f, 5f);
        Save();
        ApplyControlSettings();
    }

    private int CycleIndex(int value, int count, int direction)
    {
        if (count <= 0)
        {
            return 0;
        }

        int next = value + direction;
        while (next < 0)
        {
            next += count;
        }

        return next % count;
    }

    private int GetDefaultResolutionIndex()
    {
        int targetWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920;
        int targetHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080;
        int bestIndex = 2;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < ResolutionPresets.Length; i++)
        {
            int distance = Mathf.Abs(ResolutionPresets[i].Width - targetWidth)
                + Mathf.Abs(ResolutionPresets[i].Height - targetHeight);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
