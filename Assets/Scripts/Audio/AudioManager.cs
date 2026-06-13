using UnityEngine;

/// <summary>
/// \file AudioManager.cs
/// \brief Centralny menedżer dźwięku dla gry Kebab Chef Simulator.
/// \details Wszystkie efekty dźwiękowe są generowane proceduralnie (nie wymaga zewnętrznych zasobów audio).
/// Klasa zapewnia jednorazowe odtwarzanie efektów dźwiękowych (SFX) oraz zapętloną muzykę
/// w tle i dźwięki otoczenia. Implementuje wzorzec Singleton, dzięki czemu jest
/// dostępna globalnie z każdego miejsca w kodzie gry.
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// Statyczna instancja Singletona menedżera dźwięku.
    /// Umożliwia globalny dostęp do systemu audio z dowolnego miejsca w grze.
    /// </summary>
    public static AudioManager Instance { get; private set; }

    /// <summary>
    /// Źródło dźwięku używane do odtwarzania jednorazowych efektów dźwiękowych (SFX).
    /// </summary>
    private AudioSource sfxSource;

    /// <summary>
    /// Źródło dźwięku używane do odtwarzania zapętlonej muzyki w tle.
    /// </summary>
    private AudioSource musicSource;

    /// <summary>
    /// Źródło dźwięku używane do odtwarzania zapętlonych dźwięków otoczenia (np. skwierczenie grilla).
    /// </summary>
    private AudioSource ambientSource;

    /// <summary>
    /// Klip audio dla efektu krojenia noża.
    /// </summary>
    private AudioClip clipChop;

    /// <summary>
    /// Klip audio dla efektu podnoszenia przedmiotu.
    /// </summary>
    private AudioClip clipPickup;

    /// <summary>
    /// Klip audio dla efektu upuszczenia przedmiotu.
    /// </summary>
    private AudioClip clipDrop;

    /// <summary>
    /// Klip audio dla efektu otrzymania pieniędzy.
    /// </summary>
    private AudioClip clipMoney;

    /// <summary>
    /// Klip audio dla efektu niepowodzenia (błąd zamówienia).
    /// </summary>
    private AudioClip clipFail;

    /// <summary>
    /// Klip audio dla efektu nowego zamówienia.
    /// </summary>
    private AudioClip clipNewOrder;

    /// <summary>
    /// Klip audio dla efektu kliknięcia przycisku w interfejsie.
    /// </summary>
    private AudioClip clipButtonClick;

    /// <summary>
    /// Klip audio dla efektu gotowości (potrawa gotowa).
    /// </summary>
    private AudioClip clipReady;

    /// <summary>
    /// Klip audio dla efektu zawijania kebaba w lawasz.
    /// </summary>
    private AudioClip clipWrap;

    /// <summary>
    /// Klip audio dla efektu zakupu ulepszenia.
    /// </summary>
    private AudioClip clipUpgrade;

    /// <summary>
    /// Klip audio dla zapętlonego efektu skwierczenia grilla.
    /// </summary>
    private AudioClip clipGrillLoop;

    /// <summary>
    /// Klip audio dla zapętlonej muzyki w tle.
    /// </summary>
    private AudioClip clipMusicLoop;

    /// <summary>
    /// Główna głośność, wpływająca na wszystkie źródła dźwięku. Zakres: 0.0 - 1.0.
    /// </summary>
    private float masterVolume = 1f;

    /// <summary>
    /// Głośność efektów dźwiękowych (SFX). Zakres: 0.0 - 1.0.
    /// </summary>
    private float sfxVolume = 0.7f;

    /// <summary>
    /// Głośność muzyki w tle. Zakres: 0.0 - 1.0.
    /// </summary>
    private float musicVolume = 0.3f;

    /// <summary>
    /// Częstotliwość próbkowania używana do generowania proceduralnych klipów audio (w Hz).
    /// </summary>
    private const int SampleRate = 44100;

    /// <summary>
    /// Inicjalizuje Singleton menedżera dźwięku.
    /// Jeśli instancja już istnieje, niszczy duplikat obiektu.
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Konfiguruje źródła dźwięku, generuje wszystkie klipy audio proceduralnie,
    /// stosuje ustawienia audio z menedżera ustawień gry i rozpoczyna odtwarzanie muzyki.
    /// </summary>
    private void Start()
    {

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;

        GenerateAllClips();
        if (GameSettingsManager.Instance != null)
        {
            GameSettingsManager.Instance.ApplyAudioSettings();
        }
        PlayMusic();
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy krojenia noża.
    /// Używany przy krojeniu składników na desce do krojenia.
    /// </summary>
    public void PlayChopSound()
    {
        PlaySFX(clipChop, 0.46f, 0.94f, 1.08f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy podnoszenia przedmiotu.
    /// Używany gdy gracz podnosi składnik lub przedmiot ze stanowiska.
    /// </summary>
    public void PlayPickupSound()
    {
        PlaySFX(clipPickup, 0.34f, 0.96f, 1.08f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy upuszczenia przedmiotu.
    /// Używany gdy gracz odkłada składnik na stanowisko.
    /// </summary>
    public void PlayDropSound()
    {
        PlaySFX(clipDrop, 0.34f, 0.92f, 1.04f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy otrzymania pieniędzy.
    /// Używany przy pomyślnym zrealizowaniu zamówienia klienta.
    /// </summary>
    public void PlayMoneySound()
    {
        PlaySFX(clipMoney, 0.54f, 0.98f, 1.04f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy niepowodzenia.
    /// Używany gdy zamówienie wygaśnie lub zostanie źle zrealizowane.
    /// </summary>
    public void PlayFailSound()
    {
        PlaySFX(clipFail, 0.48f, 0.95f, 1.02f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy nowego zamówienia.
    /// Używany gdy pojawia się nowe zamówienie od klienta.
    /// </summary>
    public void PlayNewOrderSound()
    {
        PlaySFX(clipNewOrder, 0.42f, 0.98f, 1.04f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy kliknięcia przycisku interfejsu.
    /// Używany we wszystkich interaktywnych elementach UI.
    /// </summary>
    public void PlayButtonClick()
    {
        PlaySFX(clipButtonClick, 0.24f, 0.96f, 1.05f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy gotowości potrawy.
    /// Używany gdy składnik jest w pełni przetworzony i gotowy do użycia.
    /// </summary>
    public void PlayReadySound()
    {
        PlaySFX(clipReady, 0.38f, 0.96f, 1.06f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy zawijania kebaba.
    /// Używany gdy gracz zawija składniki w lawasz.
    /// </summary>
    public void PlayWrapSound()
    {
        PlaySFX(clipWrap, 0.42f, 0.94f, 1.04f);
    }

    /// <summary>
    /// Odtwarza efekt dźwiękowy zakupu ulepszenia.
    /// Używany gdy gracz kupuje ulepszenie w sklepie.
    /// </summary>
    public void PlayUpgradeSound()
    {
        PlaySFX(clipUpgrade, 0.52f, 0.98f, 1.02f);
    }

    /// <summary>
    /// Rozpoczyna odtwarzanie zapętlonego dźwięku skwierczenia grilla.
    /// Dźwięk jest odtwarzany tylko jeśli nie jest już aktywny.
    /// </summary>
    public void StartGrillAmbient()
    {
        if (ambientSource != null && !ambientSource.isPlaying)
        {
            ambientSource.clip = clipGrillLoop;
            ambientSource.volume = 0.12f * sfxVolume * masterVolume;
            ambientSource.Play();
        }
    }

    /// <summary>
    /// Zatrzymuje odtwarzanie zapętlonego dźwięku skwierczenia grilla.
    /// </summary>
    public void StopGrillAmbient()
    {
        if (ambientSource != null && ambientSource.isPlaying)
        {
            ambientSource.Stop();
        }
    }

    /// <summary>
    /// Ustawia główną głośność i aktualizuje wszystkie aktywne źródła dźwięku.
    /// </summary>
    /// <param name="volume">Nowa wartość głównej głośności (zostanie ograniczona do zakresu 0.0 - 1.0).</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    /// <summary>
    /// Ustawia głośność efektów dźwiękowych i aktualizuje aktywne źródła dźwięku.
    /// </summary>
    /// <param name="volume">Nowa wartość głośności SFX (zostanie ograniczona do zakresu 0.0 - 1.0).</param>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    /// <summary>
    /// Ustawia głośność muzyki w tle i aktualizuje aktywne źródła dźwięku.
    /// </summary>
    /// <param name="volume">Nowa wartość głośności muzyki (zostanie ograniczona do zakresu 0.0 - 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    /// <summary>
    /// Aktualna wartość głównej głośności (tylko do odczytu). Zakres: 0.0 - 1.0.
    /// </summary>
    public float MasterVolume => masterVolume;

    /// <summary>
    /// Aktualna wartość głośności efektów dźwiękowych (tylko do odczytu). Zakres: 0.0 - 1.0.
    /// </summary>
    public float SFXVolume => sfxVolume;

    /// <summary>
    /// Aktualna wartość głośności muzyki (tylko do odczytu). Zakres: 0.0 - 1.0.
    /// </summary>
    public float MusicVolume => musicVolume;

    /// <summary>
    /// Odtwarza jednorazowy efekt dźwiękowy z domyślnym zakresem pitch (bez zmiany).
    /// </summary>
    /// <param name="clip">Klip audio do odtworzenia.</param>
    /// <param name="volumeScale">Skala głośności efektu (mnożona przez głośność SFX i główną).</param>
    private void PlaySFX(AudioClip clip, float volumeScale)
    {
        PlaySFX(clip, volumeScale, 1f, 1f);
    }

    /// <summary>
    /// Odtwarza jednorazowy efekt dźwiękowy z losową zmianą wysokości tonu.
    /// Losowy pitch dodaje naturalne zróżnicowanie przy wielokrotnym odtwarzaniu tego samego efektu.
    /// </summary>
    /// <param name="clip">Klip audio do odtworzenia.</param>
    /// <param name="volumeScale">Skala głośności efektu (mnożona przez głośność SFX i główną).</param>
    /// <param name="minPitch">Minimalna wartość losowego pitch (wysokości tonu).</param>
    /// <param name="maxPitch">Maksymalna wartość losowego pitch (wysokości tonu).</param>
    private void PlaySFX(AudioClip clip, float volumeScale, float minPitch, float maxPitch)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        float previousPitch = sfxSource.pitch;
        sfxSource.pitch = Random.Range(minPitch, maxPitch);
        sfxSource.PlayOneShot(clip, volumeScale * sfxVolume * masterVolume);
        sfxSource.pitch = previousPitch;
    }

    /// <summary>
    /// Rozpoczyna odtwarzanie zapętlonej muzyki w tle.
    /// Głośność jest obliczana jako iloczyn głośności muzyki i głównej głośności.
    /// </summary>
    private void PlayMusic()
    {
        if (musicSource == null || clipMusicLoop == null)
        {
            return;
        }

        musicSource.clip = clipMusicLoop;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.Play();
    }

    /// <summary>
    /// Aktualizuje głośność wszystkich aktualnie odtwarzanych źródeł dźwięku
    /// (muzyki i dźwięków otoczenia) na podstawie bieżących wartości głośności.
    /// </summary>
    private void ApplyVolumes()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.volume = musicVolume * masterVolume;
        }

        if (ambientSource != null && ambientSource.isPlaying)
        {
            ambientSource.volume = 0.12f * sfxVolume * masterVolume;
        }
    }

    /// <summary>
    /// Generuje proceduralnie wszystkie klipy audio używane w grze.
    /// Próbuje załadować muzykę z zasobów (Resources/Audio/music), a w przypadku
    /// braku pliku używa proceduralnie wygenerowanego dźwięku otoczenia.
    /// </summary>
    private void GenerateAllClips()
    {
        clipChop = GenerateChop();
        clipPickup = GeneratePickup();
        clipDrop = GenerateDrop();
        clipMoney = GenerateMoney();
        clipFail = GenerateFail();
        clipNewOrder = GenerateNewOrder();
        clipButtonClick = GenerateClick();
        clipReady = GenerateReady();
        clipWrap = GenerateWrap();
        clipUpgrade = GenerateUpgrade();
        clipGrillLoop = GenerateGrillLoop();

        clipMusicLoop = Resources.Load<AudioClip>("Audio/music");
        if (clipMusicLoop == null)
        {
            Debug.LogWarning("Custom music 'Audio/music.mp3' not found in Resources. Falling back to procedural ambient.");
            clipMusicLoop = GenerateMusicLoop();
        }
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy krojenia noża — ostry szum z szybkim wygaszeniem.
    /// Łączy biały szum z krótkim kliknięciem sinusoidalnym imitującym uderzenie ostrza.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu krojenia.</returns>
    private AudioClip GenerateChop()
    {
        int samples = (int)(SampleRate * 0.12f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Exp(-t * 25f);
            float noise = (Random.value * 2f - 1f);
            float click = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Exp(-t * 40f);
            data[i] = (noise * 0.6f + click * 0.4f) * envelope;
        }

        return CreateClip("SFX_Chop", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy podnoszenia — jasny, narastający ton.
    /// Częstotliwość rośnie od 400 Hz do 900 Hz, tworząc wrażenie podnoszenia.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu podnoszenia.</returns>
    private AudioClip GeneratePickup()
    {
        int samples = (int)(SampleRate * 0.15f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(400f, 900f, t * t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
        }

        return CreateClip("SFX_Pickup", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy upuszczenia — opadający ton.
    /// Częstotliwość spada od 700 Hz do 250 Hz, symulując efekt spadania.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu upuszczenia.</returns>
    private AudioClip GenerateDrop()
    {
        int samples = (int)(SampleRate * 0.18f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(700f, 250f, t);
            float envelope = 1f - t;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.45f;
        }

        return CreateClip("SFX_Drop", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy pieniędzy — jasny podwójny dzwonek
    /// w stylu kasy fiskalnej. Składa się z kilku harmonicznych tonów z opóźnionym uderzeniem.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu pieniędzy.</returns>
    private AudioClip GenerateMoney()
    {
        int samples = (int)(SampleRate * 0.4f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float ding1 = Mathf.Sin(2f * Mathf.PI * 1047f * t) * Mathf.Exp(-t * 6f);
            float ding2 = Mathf.Sin(2f * Mathf.PI * 1319f * t) * Mathf.Exp(-t * 5f);
            float ding3 = Mathf.Sin(2f * Mathf.PI * 1568f * t) * Mathf.Exp(-t * 7f) * 0.4f;

            float t2 = Mathf.Max(0f, t - 0.15f);
            float hit2 = Mathf.Sin(2f * Mathf.PI * 1397f * t2) * Mathf.Exp(-t2 * 8f) * 0.6f;
            data[i] = (ding1 * 0.35f + ding2 * 0.35f + ding3 + hit2) * 0.5f;
        }

        return CreateClip("SFX_Money", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy niepowodzenia — opadający dysonansowy ton.
    /// Dwie bliskie częstotliwości tworzą efekt dudnienia, sygnalizujący błąd.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu niepowodzenia.</returns>
    private AudioClip GenerateFail()
    {
        int samples = (int)(SampleRate * 0.5f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq1 = Mathf.Lerp(440f, 220f, t);
            float freq2 = Mathf.Lerp(415f, 185f, t);
            float envelope = (1f - t) * (1f - t);
            float wave1 = Mathf.Sin(2f * Mathf.PI * freq1 * t);
            float wave2 = Mathf.Sin(2f * Mathf.PI * freq2 * t);
            data[i] = (wave1 * 0.5f + wave2 * 0.5f) * envelope * 0.5f;
        }

        return CreateClip("SFX_Fail", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy nowego zamówienia — jasny dzwonek
    /// z alikwotami i lekkim połyskiem, imitujący dzwonek w restauracji.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu nowego zamówienia.</returns>
    private AudioClip GenerateNewOrder()
    {
        int samples = (int)(SampleRate * 0.35f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float bell = Mathf.Sin(2f * Mathf.PI * 1047f * t);
            float overtone = Mathf.Sin(2f * Mathf.PI * 2094f * t) * 0.3f;
            float shimmer = Mathf.Sin(2f * Mathf.PI * 3136f * t) * 0.1f;
            float envelope = Mathf.Exp(-t * 5f);
            data[i] = (bell + overtone + shimmer) * envelope * 0.45f;
        }

        return CreateClip("SFX_NewOrder", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy kliknięcia przycisku — bardzo krótki, suchy dźwięk.
    /// Szybkie wygaszenie eksponencjalne tworzy wrażenie mechanicznego kliknięcia.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu kliknięcia.</returns>
    private AudioClip GenerateClick()
    {
        int samples = (int)(SampleRate * 0.04f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float envelope = Mathf.Exp(-t * 60f);
            data[i] = Mathf.Sin(2f * Mathf.PI * 800f * t) * envelope * 0.4f;
        }

        return CreateClip("SFX_Click", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy gotowości — miękki metaliczny stuk
    /// z ciepłym tonem potwierdzenia. Łączy szum impulsowy z tonami harmonicznymi.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu gotowości.</returns>
    private AudioClip GenerateReady()
    {
        int samples = (int)(SampleRate * 0.28f);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float time = (float)i / SampleRate;
            float t = (float)i / samples;
            float tapEnvelope = Mathf.Exp(-t * 32f);
            float toneEnvelope = Mathf.Exp(-t * 7f);
            float tap = (Random.value * 2f - 1f) * tapEnvelope * 0.18f;
            float tone = Mathf.Sin(2f * Mathf.PI * 784f * time) * toneEnvelope * 0.32f;
            float overtone = Mathf.Sin(2f * Mathf.PI * 1175f * time) * toneEnvelope * 0.12f;
            data[i] = tap + tone + overtone;
        }

        return CreateClip("SFX_Ready", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy zawijania — szelest papieru/lawasza
    /// z kontrolowanym uderzeniem. Używa filtrowanego szumu i akcentów niskoczęstotliwościowych.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu zawijania.</returns>
    private AudioClip GenerateWrap()
    {
        int samples = (int)(SampleRate * 0.36f);
        float[] data = new float[samples];
        float filteredNoise = 0f;
        for (int i = 0; i < samples; i++)
        {
            float time = (float)i / SampleRate;
            float t = (float)i / samples;
            float noise = Random.value * 2f - 1f;
            filteredNoise = filteredNoise * 0.82f + noise * 0.18f;

            float rustleEnvelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float lowThump = Mathf.Sin(2f * Mathf.PI * 120f * time) * Mathf.Exp(-t * 15f) * 0.22f;
            float foldAccent = Mathf.Sin(2f * Mathf.PI * 360f * time) * Mathf.Exp(-Mathf.Abs(t - 0.45f) * 16f) * 0.10f;
            data[i] = filteredNoise * rustleEnvelope * 0.28f + lowThump + foldAccent;
        }

        return CreateClip("SFX_Wrap", data);
    }

    /// <summary>
    /// Generuje proceduralny klip dźwiękowy ulepszenia — kompaktowe arpeggio premium,
    /// nie głośna fanfara arcade'owa. Cztery nuty grane kolejno z lekkim połyskiem.
    /// </summary>
    /// <returns>Wygenerowany klip audio efektu ulepszenia.</returns>
    private AudioClip GenerateUpgrade()
    {
        int samples = (int)(SampleRate * 0.52f);
        float[] data = new float[samples];
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        for (int i = 0; i < samples; i++)
        {
            float time = (float)i / SampleRate;
            float t = (float)i / samples;
            float value = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float start = n * 0.075f;
                float local = time - start;
                if (local < 0f)
                {
                    continue;
                }

                float envelope = Mathf.Exp(-local * 7.5f);
                value += Mathf.Sin(2f * Mathf.PI * notes[n] * local) * envelope * 0.18f;
                value += Mathf.Sin(2f * Mathf.PI * notes[n] * 2f * local) * envelope * 0.045f;
            }

            float shimmer = Mathf.Sin(2f * Mathf.PI * 1568f * time) * Mathf.Exp(-t * 5f) * 0.045f;
            data[i] = (value + shimmer) * (1f - Mathf.Clamp01(t - 0.86f) / 0.14f);
        }

        return CreateClip("SFX_Upgrade", data);
    }

    /// <summary>
    /// Generuje proceduralny klip zapętlonego skwierczenia grilla — filtrowany szum
    /// z losowymi trzaskami i powolną modulacją amplitudy. Trwa 4 sekundy.
    /// </summary>
    /// <returns>Wygenerowany klip audio zapętlonego efektu grilla.</returns>
    private AudioClip GenerateGrillLoop()
    {
        int samples = SampleRate * 4;
        float[] data = new float[samples];
        float phase = 0f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float noise = Random.value * 2f - 1f;

            phase = phase * 0.92f + noise * 0.08f;

            float crackle = (Random.value < 0.003f) ? (Random.value * 0.4f) : 0f;

            float mod = 1f + Mathf.Sin(t * 1.5f) * 0.15f;
            data[i] = (phase * 0.35f + crackle) * mod;
        }

        return CreateClip("SFX_GrillLoop", data);
    }

    /// <summary>
    /// Generuje proceduralny klip zapętlonej muzyki w tle — ciepły dron otoczenia
    /// z łagodną progresją akordów. Zawiera bas, akord trójdźwiękowy, pad szumowy
    /// oraz płynne wejścia i wyjścia (fade in/out). Trwa 16 sekund.
    /// </summary>
    /// <returns>Wygenerowany klip audio zapętlonej muzyki otoczenia.</returns>
    private AudioClip GenerateMusicLoop()
    {
        int samples = SampleRate * 16;
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / SampleRate;
            float loopT = t / 16f;

            float bass = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.15f;

            float chordPhase = loopT * Mathf.PI * 2f;
            float note1Freq = 262f + Mathf.Sin(chordPhase) * 30f;
            float note2Freq = 330f + Mathf.Sin(chordPhase * 0.5f) * 20f;
            float note3Freq = 392f + Mathf.Sin(chordPhase * 0.75f) * 15f;

            float chord = Mathf.Sin(2f * Mathf.PI * note1Freq * t) * 0.06f
                        + Mathf.Sin(2f * Mathf.PI * note2Freq * t) * 0.05f
                        + Mathf.Sin(2f * Mathf.PI * note3Freq * t) * 0.04f;

            float breath = 0.7f + Mathf.Sin(t * 0.4f) * 0.3f;

            float pad = (Mathf.PerlinNoise(t * 2f, 0.5f) - 0.5f) * 0.04f;

            data[i] = (bass + chord + pad) * breath;

            float fadeZone = 0.5f;
            if (t > 16f - fadeZone)
            {
                float fadeT = (t - (16f - fadeZone)) / fadeZone;
                data[i] *= (1f - fadeT);
            }
            else if (t < fadeZone)
            {
                data[i] *= (t / fadeZone);
            }
        }

        return CreateClip("Music_Ambient", data);
    }

    /// <summary>
    /// Tworzy klip audio Unity z podanej tablicy próbek.
    /// </summary>
    /// <param name="clipName">Nazwa klipu audio do identyfikacji.</param>
    /// <param name="data">Tablica próbek audio (wartości float od -1.0 do 1.0).</param>
    /// <returns>Utworzony klip audio Unity z jednym kanałem i zadaną częstotliwością próbkowania.</returns>
    private AudioClip CreateClip(string clipName, float[] data)
    {
        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// Czyści referencję Singletona przy niszczeniu obiektu,
    /// zapobiegając odwoływaniu się do zniszczonej instancji.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
