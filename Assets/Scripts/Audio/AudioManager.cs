using UnityEngine;

/// <summary>
/// Centralized audio manager for Kebab Chef Simulator.
/// All sounds are generated procedurally (no external audio assets needed).
/// Provides SFX one-shots and looping background music/ambient.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private AudioSource ambientSource;

    private AudioClip clipChop;
    private AudioClip clipPickup;
    private AudioClip clipDrop;
    private AudioClip clipMoney;
    private AudioClip clipFail;
    private AudioClip clipNewOrder;
    private AudioClip clipButtonClick;
    private AudioClip clipReady;
    private AudioClip clipWrap;
    private AudioClip clipUpgrade;
    private AudioClip clipGrillLoop;
    private AudioClip clipMusicLoop;

    private float masterVolume = 1f;
    private float sfxVolume = 0.7f;
    private float musicVolume = 0.3f;

    private const int SampleRate = 44100;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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

    public void PlayChopSound()
    {
        PlaySFX(clipChop, 0.46f, 0.94f, 1.08f);
    }

    public void PlayPickupSound()
    {
        PlaySFX(clipPickup, 0.34f, 0.96f, 1.08f);
    }

    public void PlayDropSound()
    {
        PlaySFX(clipDrop, 0.34f, 0.92f, 1.04f);
    }

    public void PlayMoneySound()
    {
        PlaySFX(clipMoney, 0.54f, 0.98f, 1.04f);
    }

    public void PlayFailSound()
    {
        PlaySFX(clipFail, 0.48f, 0.95f, 1.02f);
    }

    public void PlayNewOrderSound()
    {
        PlaySFX(clipNewOrder, 0.42f, 0.98f, 1.04f);
    }

    public void PlayButtonClick()
    {
        PlaySFX(clipButtonClick, 0.24f, 0.96f, 1.05f);
    }

    public void PlayReadySound()
    {
        PlaySFX(clipReady, 0.38f, 0.96f, 1.06f);
    }

    public void PlayWrapSound()
    {
        PlaySFX(clipWrap, 0.42f, 0.94f, 1.04f);
    }

    public void PlayUpgradeSound()
    {
        PlaySFX(clipUpgrade, 0.52f, 0.98f, 1.02f);
    }

    public void StartGrillAmbient()
    {
        if (ambientSource != null && !ambientSource.isPlaying)
        {
            ambientSource.clip = clipGrillLoop;
            ambientSource.volume = 0.12f * sfxVolume * masterVolume;
            ambientSource.Play();
        }
    }

    public void StopGrillAmbient()
    {
        if (ambientSource != null && ambientSource.isPlaying)
        {
            ambientSource.Stop();
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public float MasterVolume => masterVolume;
    public float SFXVolume => sfxVolume;
    public float MusicVolume => musicVolume;

    private void PlaySFX(AudioClip clip, float volumeScale)
    {
        PlaySFX(clip, volumeScale, 1f, 1f);
    }

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

    /// <summary>Knife chop — sharp noise burst with fast decay</summary>
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

    /// <summary>Pickup — bright rising tone</summary>
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

    /// <summary>Drop — falling tone</summary>
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

    /// <summary>Money — bright double ding (cash register feel)</summary>
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

    /// <summary>Fail — descending dissonant tone</summary>
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

    /// <summary>New order bell — bright single ding</summary>
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

    /// <summary>Button click — very short tick</summary>
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

    /// <summary>Ready cue - soft metal tap with warm confirmation tone.</summary>
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

    /// <summary>Wrap cue - paper/lavash rustle with a controlled handoff thump.</summary>
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

    /// <summary>Upgrade cue - compact premium arpeggio, not a loud arcade fanfare.</summary>
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

    /// <summary>Grill sizzle — filtered noise loop</summary>
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

    /// <summary>Background music — warm ambient drone with gentle chord progression</summary>
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

    private AudioClip CreateClip(string clipName, float[] data)
    {
        AudioClip clip = AudioClip.Create(clipName, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
