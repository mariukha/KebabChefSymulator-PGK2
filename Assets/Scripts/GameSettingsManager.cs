/// \file GameSettingsManager.cs
/// \brief Plik zawierający klasę GameSettingsManager odpowiedzialną za zarządzanie ustawieniami gry.
/// \details Obsługuje ustawienia graficzne (rozdzielczość, tryb okna, VSync, limit FPS, jakość),
/// ustawienia dźwięku (głośność główna, muzyka, efekty) oraz ustawienia sterowania (czułość myszy).
/// Wszystkie ustawienia są zapisywane w PlayerPrefs.

using UnityEngine;

/// <summary>
/// Menedżer ustawień gry implementujący wzorzec Singleton.
/// Zarządza wszystkimi konfigurowalnymi ustawieniami gry: grafiką, dźwiękiem i sterowaniem.
/// Ustawienia są automatycznie ładowane z PlayerPrefs przy inicjalizacji
/// i zapisywane po każdej zmianie.
/// </summary>
/// <remarks>
/// Klasa zapewnia zestaw predefiniowanych rozdzielczości i limitów FPS.
/// Obsługuje przełączanie między trybem pełnoekranowym a okienkowym,
/// cykliczne przełączanie ustawień oraz bezpośrednie ustawianie wartości suwakowych.
/// </remarks>
public class GameSettingsManager : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona menedżera ustawień, dostępna globalnie.
    /// </summary>
    public static GameSettingsManager Instance { get; private set; }

    /// <summary>
    /// Struktura przechowująca predefiniowany preset rozdzielczości ekranu.
    /// </summary>
    private struct ResolutionPreset
    {
        /// <summary>
        /// Szerokość rozdzielczości w pikselach.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Wysokość rozdzielczości w pikselach.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// Inicjalizuje nowy preset rozdzielczości z podaną szerokością i wysokością.
        /// </summary>
        /// <param name="width">Szerokość rozdzielczości w pikselach.</param>
        /// <param name="height">Wysokość rozdzielczości w pikselach.</param>
        public ResolutionPreset(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// Tablica predefiniowanych presetów rozdzielczości dostępnych w ustawieniach gry.
    /// Obejmuje rozdzielczości: 1280x720 (HD), 1600x900 (HD+), 1920x1080 (Full HD), 2560x1440 (QHD).
    /// </summary>
    private static readonly ResolutionPreset[] ResolutionPresets =
    {
        new ResolutionPreset(1280, 720),
        new ResolutionPreset(1600, 900),
        new ResolutionPreset(1920, 1080),
        new ResolutionPreset(2560, 1440)
    };

    /// <summary>
    /// Tablica dostępnych limitów klatek na sekundę (FPS).
    /// Wartość 0 oznacza brak limitu. Dostępne opcje: brak limitu, 30, 60, 120, 144 FPS.
    /// </summary>
    private static readonly int[] FpsLimits = { 0, 30, 60, 120, 144 };

    /// <summary>
    /// Klucz PlayerPrefs do zapisu indeksu wybranej rozdzielczości.
    /// </summary>
    private const string ResolutionKey = "Settings.ResolutionIndex";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu indeksu trybu okna (pełny ekran / okno).
    /// </summary>
    private const string WindowModeKey = "Settings.WindowModeIndex";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu stanu synchronizacji pionowej (VSync).
    /// </summary>
    private const string VSyncKey = "Settings.VSync";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu indeksu limitu FPS.
    /// </summary>
    private const string FpsLimitKey = "Settings.FpsLimitIndex";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu indeksu poziomu jakości grafiki.
    /// </summary>
    private const string QualityKey = "Settings.QualityIndex";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu poziomu głośności głównej.
    /// </summary>
    private const string MasterVolumeKey = "MasterVolume";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu poziomu głośności muzyki.
    /// </summary>
    private const string MusicVolumeKey = "MusicVolume";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu poziomu głośności efektów dźwiękowych.
    /// </summary>
    private const string SfxVolumeKey = "SFXVolume";

    /// <summary>
    /// Klucz PlayerPrefs do zapisu wartości czułości myszy.
    /// </summary>
    private const string MouseSensitivityKey = "MouseSensitivity";

    /// <summary>
    /// Bieżący indeks wybranej rozdzielczości w tablicy <see cref="ResolutionPresets"/>.
    /// </summary>
    private int resolutionIndex;

    /// <summary>
    /// Bieżący indeks trybu okna (0 = pełny ekran, 1 = okno).
    /// </summary>
    private int windowModeIndex;

    /// <summary>
    /// Flaga określająca, czy synchronizacja pionowa (VSync) jest włączona.
    /// </summary>
    private bool vSyncEnabled;

    /// <summary>
    /// Bieżący indeks limitu FPS w tablicy <see cref="FpsLimits"/>.
    /// </summary>
    private int fpsLimitIndex;

    /// <summary>
    /// Bieżący indeks poziomu jakości grafiki w ustawieniach Unity Quality.
    /// </summary>
    private int qualityIndex;

    /// <summary>
    /// Poziom głośności głównej w zakresie [0, 1].
    /// </summary>
    private float masterVolume;

    /// <summary>
    /// Poziom głośności muzyki w zakresie [0, 1].
    /// </summary>
    private float musicVolume;

    /// <summary>
    /// Poziom głośności efektów dźwiękowych w zakresie [0, 1].
    /// </summary>
    private float sfxVolume;

    /// <summary>
    /// Wartość czułości myszy w zakresie [0.5, 5.0].
    /// </summary>
    private float mouseSensitivity;

    /// <summary>
    /// Liczba dostępnych presetów rozdzielczości.
    /// </summary>
    public int ResolutionCount => ResolutionPresets.Length;

    /// <summary>
    /// Liczba dostępnych opcji limitu FPS.
    /// </summary>
    public int FpsLimitCount => FpsLimits.Length;

    /// <summary>
    /// Liczba dostępnych poziomów jakości grafiki.
    /// </summary>
    public int QualityCount => Mathf.Max(1, QualitySettings.names.Length);

    /// <summary>
    /// Liczba dostępnych trybów okna (pełny ekran i okno).
    /// </summary>
    public int WindowModeCount => 2;

    /// <summary>
    /// Bieżący poziom głośności głównej.
    /// </summary>
    /// <value>Wartość w zakresie [0, 1].</value>
    public float MasterVolume => masterVolume;

    /// <summary>
    /// Bieżący poziom głośności muzyki.
    /// </summary>
    /// <value>Wartość w zakresie [0, 1].</value>
    public float MusicVolume => musicVolume;

    /// <summary>
    /// Bieżący poziom głośności efektów dźwiękowych.
    /// </summary>
    /// <value>Wartość w zakresie [0, 1].</value>
    public float SFXVolume => sfxVolume;

    /// <summary>
    /// Bieżąca czułość myszy.
    /// </summary>
    /// <value>Wartość w zakresie [0.5, 5.0].</value>
    public float MouseSensitivity => mouseSensitivity;

    /// <summary>
    /// Określa, czy synchronizacja pionowa (VSync) jest włączona.
    /// </summary>
    /// <value><c>true</c> jeśli VSync jest włączony; w przeciwnym razie <c>false</c>.</value>
    public bool VSyncEnabled => vSyncEnabled;

    /// <summary>
    /// Etykieta tekstowa aktualnej rozdzielczości do wyświetlenia w interfejsie (np. "1920 x 1080").
    /// </summary>
    public string ResolutionLabel => ResolutionPresets[resolutionIndex].Width + " x " + ResolutionPresets[resolutionIndex].Height;

    /// <summary>
    /// Etykieta tekstowa aktualnego trybu okna do wyświetlenia w interfejsie.
    /// </summary>
    /// <value>"Pelny ekran" lub "Okno".</value>
    public string WindowModeLabel => windowModeIndex == 0 ? "Pelny ekran" : "Okno";

    /// <summary>
    /// Etykieta tekstowa stanu VSync do wyświetlenia w interfejsie.
    /// </summary>
    /// <value>"Wlaczone" lub "Wylaczone".</value>
    public string VSyncLabel => vSyncEnabled ? "Wlaczone" : "Wylaczone";

    /// <summary>
    /// Etykieta tekstowa aktualnego limitu FPS do wyświetlenia w interfejsie.
    /// </summary>
    /// <value>"Bez limitu" lub liczba FPS (np. "60 FPS").</value>
    public string FpsLimitLabel => FpsLimits[fpsLimitIndex] <= 0 ? "Bez limitu" : FpsLimits[fpsLimitIndex] + " FPS";

    /// <summary>
    /// Etykieta tekstowa aktualnego poziomu jakości grafiki do wyświetlenia w interfejsie.
    /// </summary>
    /// <value>Nazwa poziomu jakości z ustawień Unity lub "Domyslna" jako wartość domyślna.</value>
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

    /// <summary>
    /// Zapewnia istnienie instancji menedżera ustawień.
    /// Jeśli instancja nie istnieje, wyszukuje obiekt "GameManager" na scenie
    /// lub tworzy nowy i dodaje do niego komponent <see cref="GameSettingsManager"/>.
    /// </summary>
    /// <returns>Istniejąca lub nowo utworzona instancja <see cref="GameSettingsManager"/>.</returns>
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

    /// <summary>
    /// Inicjalizacja Singletona w metodzie Awake.
    /// Jeśli instancja już istnieje, niszczy duplikat.
    /// W przeciwnym razie ładuje ustawienia z PlayerPrefs i stosuje je.
    /// </summary>
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

    /// <summary>
    /// Ładuje wszystkie ustawienia z PlayerPrefs.
    /// Dla każdego ustawienia stosuje wartości domyślne, jeśli zapis nie istnieje,
    /// i ogranicza wartości do prawidłowych zakresów.
    /// </summary>
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

    /// <summary>
    /// Zapisuje wszystkie bieżące ustawienia do PlayerPrefs i wymusza ich trwały zapis na dysk.
    /// </summary>
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

    /// <summary>
    /// Stosuje wszystkie kategorie ustawień: wyświetlanie, dźwięk i sterowanie.
    /// </summary>
    public void ApplyAll()
    {
        ApplyDisplaySettings();
        ApplyAudioSettings();
        ApplyControlSettings();
    }

    /// <summary>
    /// Stosuje ustawienia wyświetlania: jakość grafiki, VSync, limit FPS, rozdzielczość i tryb okna.
    /// Gdy VSync jest włączony, limit FPS jest ignorowany (ustawiony na -1).
    /// </summary>
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

    /// <summary>
    /// Stosuje ustawienia dźwięku, przekazując wartości głośności do <see cref="AudioManager"/>.
    /// Metoda nie wykonuje niczego, jeśli AudioManager nie jest dostępny.
    /// </summary>
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

    /// <summary>
    /// Stosuje ustawienia sterowania, aktualizując czułość myszy we wszystkich aktywnych kontrolerach gracza.
    /// Wyszukuje wszystkie instancje <see cref="SimplePlayerController"/> na scenie.
    /// </summary>
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

    /// <summary>
    /// Cyklicznie przełącza rozdzielczość ekranu w zadanym kierunku.
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    /// <param name="direction">Kierunek przełączania: 1 — następna, -1 — poprzednia.</param>
    public void CycleResolution(int direction)
    {
        resolutionIndex = CycleIndex(resolutionIndex, ResolutionPresets.Length, direction);
        Save();
        ApplyDisplaySettings();
    }

    /// <summary>
    /// Cyklicznie przełącza tryb okna (pełny ekran / okno) w zadanym kierunku.
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    /// <param name="direction">Kierunek przełączania: 1 — następny, -1 — poprzedni.</param>
    public void CycleWindowMode(int direction)
    {
        windowModeIndex = CycleIndex(windowModeIndex, WindowModeCount, direction);
        Save();
        ApplyDisplaySettings();
    }

    /// <summary>
    /// Przełącza tryb okna jako skrót klawiszowy (toggle).
    /// Zmienia pełny ekran na okno i odwrotnie, po czym zapisuje i stosuje ustawienia.
    /// </summary>
    public void ToggleWindowModeShortcut()
    {
        windowModeIndex = windowModeIndex == 0 ? 1 : 0;
        Save();
        ApplyDisplaySettings();
    }

    /// <summary>
    /// Przełącza stan synchronizacji pionowej (VSync).
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    public void ToggleVSync()
    {
        vSyncEnabled = !vSyncEnabled;
        Save();
        ApplyDisplaySettings();
    }

    /// <summary>
    /// Cyklicznie przełącza limit FPS w zadanym kierunku.
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    /// <param name="direction">Kierunek przełączania: 1 — następny, -1 — poprzedni.</param>
    public void CycleFpsLimit(int direction)
    {
        fpsLimitIndex = CycleIndex(fpsLimitIndex, FpsLimits.Length, direction);
        Save();
        ApplyDisplaySettings();
    }

    /// <summary>
    /// Cyklicznie przełącza poziom jakości grafiki w zadanym kierunku.
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    /// <param name="direction">Kierunek przełączania: 1 — następny, -1 — poprzedni.</param>
    public void CycleQuality(int direction)
    {
        qualityIndex = CycleIndex(qualityIndex, QualityCount, direction);
        Save();
        ApplyDisplaySettings();
    }

    /// <summary>
    /// Ustawia poziom głośności głównej.
    /// Wartość jest ograniczana do zakresu [0, 1].
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    /// <param name="value">Nowy poziom głośności głównej.</param>
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        Save();
        ApplyAudioSettings();
    }

    /// <summary>
    /// Ustawia poziom głośności muzyki.
    /// Wartość jest ograniczana do zakresu [0, 1].
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    /// <param name="value">Nowy poziom głośności muzyki.</param>
    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        Save();
        ApplyAudioSettings();
    }

    /// <summary>
    /// Ustawia poziom głośności efektów dźwiękowych.
    /// Wartość jest ograniczana do zakresu [0, 1].
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je.
    /// </summary>
    /// <param name="value">Nowy poziom głośności efektów dźwiękowych.</param>
    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        Save();
        ApplyAudioSettings();
    }

    /// <summary>
    /// Ustawia czułość myszy.
    /// Wartość jest ograniczana do zakresu [0.5, 5.0].
    /// Po zmianie automatycznie zapisuje ustawienia i stosuje je do wszystkich kontrolerów gracza.
    /// </summary>
    /// <param name="value">Nowa wartość czułości myszy.</param>
    public void SetMouseSensitivity(float value)
    {
        mouseSensitivity = Mathf.Clamp(value, 0.5f, 5f);
        Save();
        ApplyControlSettings();
    }

    /// <summary>
    /// Cyklicznie przełącza indeks w podanym zakresie z zawijaniem (wrap-around).
    /// Umożliwia nawigację w obu kierunkach, np. po osiągnięciu ostatniej opcji wraca do pierwszej.
    /// </summary>
    /// <param name="value">Bieżący indeks.</param>
    /// <param name="count">Łączna liczba dostępnych opcji.</param>
    /// <param name="direction">Kierunek przesunięcia (1 = następny, -1 = poprzedni).</param>
    /// <returns>Nowy indeks po przesunięciu z zawijaniem.</returns>
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

    /// <summary>
    /// Wyznacza domyślny indeks rozdzielczości najlepiej dopasowany do aktualnej rozdzielczości monitora.
    /// Porównuje dostępne presety z rozdzielczością ekranu i wybiera najbliższą (minimalna odległość Manhattan).
    /// Domyślnie zwraca indeks 2 (1920x1080), jeśli nie udało się ustalić rozdzielczości monitora.
    /// </summary>
    /// <returns>Indeks najlepiej dopasowanego presetu rozdzielczości.</returns>
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

    /// <summary>
    /// Wywoływana przy niszczeniu obiektu. Czyści referencję Singletona,
    /// jeśli niszczony obiekt jest aktualną instancją.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
