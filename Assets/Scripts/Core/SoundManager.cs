// ========================================================
// VAELSTRIDER: RUINBOUND - Sound Manager
// ========================================================
using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    public const string PREF_BGM_VOL = "Game_BGM_Volume";
    public const string PREF_SFX_VOL = "Game_SFX_Volume";

    public float MusicVolume
    {
        get => PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.75f);
        set
        {
            PlayerPrefs.SetFloat(PREF_BGM_VOL, value);
            PlayerPrefs.Save();
            if (musicSource != null) musicSource.volume = value * 0.45f;
        }
    }

    public float SFXVolume
    {
        get => PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.85f);
        set
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOL, value);
            PlayerPrefs.Save();
            if (sfxSource != null) sfxSource.volume = value;
            if (voiceSource != null) voiceSource.volume = value * 1.35f;
        }
    }

    public void SetBGMVolume(float val) => MusicVolume = val;
    public float GetBGMVolume() => MusicVolume;
    public void SetSFXVolume(float val) => SFXVolume = val;
    public float GetSFXVolume() => SFXVolume;
    public void SetMusicVolume(float val) => MusicVolume = val;
    public float GetMusicVolume() => MusicVolume;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip swordSlashClip;
    [SerializeField] private AudioClip hitImpactClip;
    [SerializeField] private AudioClip enemyShootClip;
    [SerializeField] private AudioClip shardPickupClip;
    [SerializeField] private AudioClip potionDrinkClip;
    [SerializeField] private AudioClip chestOpenClip;
    [SerializeField] private AudioClip portalSpawnClip;
    [SerializeField] private AudioClip playerHurtClip;
    [SerializeField] private AudioClip bossDeathClip;
    [SerializeField] private AudioClip bossWarningClip;
    [SerializeField] private AudioClip dungeonAmbienceClip;
    [SerializeField] private AudioClip coastalAmbienceClip;
    [SerializeField] private AudioClip runePillarHumClip;

    [Header("Voice Lines")]
    [SerializeField] private AudioClip ultiVoiceClip;
    [SerializeField] private AudioClip eyeGlintClip;
    [SerializeField] private AudioClip heavenlyAscensionClip;

    private AudioSource sfxSource;
    private AudioSource voiceSource;
    private AudioSource musicSource;

    private float lastShardTime = 0f;
    private int consecutiveShards = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Tao 3 AudioSources rieng biet: SFX, Voice, Music
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        AutoLoadClips();
        ApplyVolumes();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneAmbience(scene.name);
    }

    void Start()
    {
        PlaySceneAmbience(SceneManager.GetActiveScene().name);
    }

    public void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = MusicVolume * 0.45f;
        if (sfxSource != null) sfxSource.volume = SFXVolume;
        if (voiceSource != null) voiceSource.volume = SFXVolume * 1.35f;
    }

    public void PlaySceneAmbience(string sceneName)
    {
        AutoLoadClips();
        if (musicSource == null) return;

        AudioClip targetClip = null;
        float vol = MusicVolume * 0.45f;

        if (sceneName == "Map2_ValleyRuin")
        {
            targetClip = coastalAmbienceClip != null ? coastalAmbienceClip : dungeonAmbienceClip;
            vol = MusicVolume * 0.55f;
        }
        else if (sceneName == "MainMenuScene" || sceneName == "LoginScene")
        {
            targetClip = dungeonAmbienceClip;
            vol = MusicVolume * 0.35f;
        }
        else
        {
            targetClip = dungeonAmbienceClip;
            vol = MusicVolume * 0.42f;
        }

        if (targetClip != null)
        {
            if (musicSource.clip != targetClip || !musicSource.isPlaying)
            {
                musicSource.clip = targetClip;
                musicSource.volume = vol;
                musicSource.Play();
                Debug.Log($"<color=cyan>[AUDIO]</color> Đang phát nhạc nền môi trường: {targetClip.name} cho scene {sceneName}!");
            }
        }
    }

    private void AutoLoadClips()
    {
        if (ultiVoiceClip == null)
        {
            ultiVoiceClip = Resources.Load<AudioClip>("Voice/ulti_voice");
        }

#if UNITY_EDITOR
        string sfxPath = "Assets/Audio/SFX/";
        string musicPath = "Assets/Audio/Music/";
        string voicePath = "Assets/Audio/Voice/";

        if (swordSlashClip == null) swordSlashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "sword_slash.wav");
        if (hitImpactClip == null) hitImpactClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "hit_impact.wav");
        if (enemyShootClip == null) enemyShootClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "enemy_shoot.wav");
        if (shardPickupClip == null) shardPickupClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "shard_pickup.wav");
        if (potionDrinkClip == null) potionDrinkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "potion_drink.wav");
        if (chestOpenClip == null) chestOpenClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "chest_open.wav");
        if (portalSpawnClip == null) portalSpawnClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "portal_spawn.wav");
        if (eyeGlintClip == null) eyeGlintClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "eye_glint.wav");
        if (playerHurtClip == null) playerHurtClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "player_hurt.wav");
        if (bossDeathClip == null) bossDeathClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "boss_death.wav");
        if (bossWarningClip == null) bossWarningClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "boss_warning.wav");
        if (heavenlyAscensionClip == null) heavenlyAscensionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "heavenly_ascension.wav");
        if (runePillarHumClip == null) runePillarHumClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "rune_pillar_hum.wav");

        if (dungeonAmbienceClip == null) dungeonAmbienceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath + "dungeon_ambience.wav");
        if (coastalAmbienceClip == null) coastalAmbienceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath + "coastal_forest_ambience.wav");

        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.mp3");
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.wav");
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.ogg");
#endif
    }

    public void PlayAmbience()
    {
        PlaySceneAmbience(SceneManager.GetActiveScene().name);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.volume = MusicVolume * 0.45f;
        musicSource.Play();
    }

    public void StopBGM()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchJitter = 0.08f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfxSource.PlayOneShot(clip, SFXVolume * volume);
    }

    public void PlayVoice(AudioClip clip, float volume = 1.35f)
    {
        if (clip == null || voiceSource == null) return;
        voiceSource.pitch = 1f;
        voiceSource.PlayOneShot(clip, SFXVolume * volume);
    }

    public void PlayUltiVoice()
    {
        AutoLoadClips();
        if (ultiVoiceClip != null)
        {
            Debug.Log("<color=green>[VOICE]</color> Đang phát giọng lồng tiếng Tuyệt Kỹ của nhân vật!");
            PlayVoice(ultiVoiceClip, 1.4f);
        }
        else
        {
            PlayPortalSpawn();
        }
    }

    public void PlaySwordSlash()
    {
        AutoLoadClips();
        if (swordSlashClip != null) PlaySFX(swordSlashClip, 0.85f, 0.12f);
        else PlaySynthFallback(380f, 0.07f);
    }

    public void PlayHitImpact()
    {
        AutoLoadClips();
        if (hitImpactClip != null) PlaySFX(hitImpactClip, 1.0f, 0.1f);
        else PlaySynthFallback(250f, 0.09f);
    }

    public void PlayEnemyShoot()
    {
        AutoLoadClips();
        if (enemyShootClip != null) PlaySFX(enemyShootClip, 0.75f, 0.1f);
        else PlaySynthFallback(480f, 0.08f);
    }

    public void PlayShardPickup()
    {
        AutoLoadClips();
        if (shardPickupClip != null && sfxSource != null)
        {
            if (Time.time - lastShardTime < 0.25f)
            {
                consecutiveShards = Mathf.Min(consecutiveShards + 1, 8);
            }
            else
            {
                consecutiveShards = 0;
            }
            lastShardTime = Time.time;

            float pitchBonus = consecutiveShards * 0.05f;
            sfxSource.pitch = 1f + pitchBonus + Random.Range(-0.02f, 0.02f);
            sfxSource.PlayOneShot(shardPickupClip, SFXVolume * 0.9f);
        }
        else
        {
            PlaySynthFallback(1350f, 0.12f);
        }
    }

    public void PlayPotionDrink()
    {
        AutoLoadClips();
        if (potionDrinkClip != null) PlaySFX(potionDrinkClip, 1.0f, 0.05f);
        else PlaySynthFallback(750f, 0.15f);
    }

    public void PlayChestOpen()
    {
        AutoLoadClips();
        if (chestOpenClip != null) PlaySFX(chestOpenClip, 1.0f, 0.04f);
        else PlaySynthFallback(700f, 0.12f);
    }

    public void PlayEyeFlareGlint()
    {
        AutoLoadClips();
        if (eyeGlintClip != null) PlaySFX(eyeGlintClip, 1.35f, 0.02f);
        else PlayPortalSpawn();
    }

    public void PlayHeavenlyAscension()
    {
        AutoLoadClips();
        if (heavenlyAscensionClip != null) PlaySFX(heavenlyAscensionClip, 1.15f, 0.0f);
        else PlaySynthFallback(1200f, 0.5f);
    }

    public void PlayPortalSpawn()
    {
        AutoLoadClips();
        if (portalSpawnClip != null) PlaySFX(portalSpawnClip, 1.1f, 0.02f);
        else PlaySynthFallback(500f, 0.3f);
    }

    public void PlayPlayerHurt()
    {
        AutoLoadClips();
        if (playerHurtClip != null) PlaySFX(playerHurtClip, 1.0f, 0.08f);
        else PlaySynthFallback(200f, 0.12f);
    }

    public void PlayBossDeath()
    {
        AutoLoadClips();
        if (bossDeathClip != null) PlaySFX(bossDeathClip, 1.25f, 0.05f);
        else PlaySynthFallback(140f, 0.45f);
    }

    public void PlayBossWarning()
    {
        AutoLoadClips();
        if (bossWarningClip != null) PlaySFX(bossWarningClip, 1.35f, 0.01f);
        else PlayEyeFlareGlint();
    }

    public void PlayMenuHover()
    {
        AutoLoadClips();
        if (shardPickupClip != null) PlaySFX(shardPickupClip, 0.45f, 0.05f);
        else PlaySynthFallback(700f, 0.03f);
    }

    public void PlayButtonClick()
    {
        AutoLoadClips();
        if (shardPickupClip != null) PlaySFX(shardPickupClip, 0.65f, 0.05f);
        else PlaySynthFallback(880f, 0.05f);
    }

    public void PlayMenuOpen()
    {
        AutoLoadClips();
        if (chestOpenClip != null) PlaySFX(chestOpenClip, 0.8f, 0.05f);
        else PlaySynthFallback(520f, 0.08f);
    }

    public void PlayMenuClose()
    {
        AutoLoadClips();
        if (chestOpenClip != null) PlaySFX(chestOpenClip, 0.6f, -0.1f);
        else PlaySynthFallback(380f, 0.08f);
    }

    public void PlayItemPickup() => PlayShardPickup();
    public void PlayUpgradeSuccess() => PlayHeavenlyAscension();
    public void PlayUpgradeFail() => PlayPlayerHurt();
    public void PlayChestBreak() => PlayChestOpen();
    public void PlayShrineInteract() => PlayHeavenlyAscension();
    public void PlayPortalEnter() => PlayPortalSpawn();
    public void PlayBarrierBreak() => PlayChestOpen();
    public void PlayVictory() => PlayHeavenlyAscension();
    public void PlayGameOver() => PlayBossDeath();
    public void PlayDash() => PlaySwordSlash();
    public void PlayAttack() => PlaySwordSlash();
    public void PlayUltiSFX() => PlayHeavenlyAscension();
    public void PlayBossRoar() => PlayBossWarning();
    public void PlayBossSlam() => PlayHitImpact();
    public void PlayBossSkill() => PlayBossWarning();

    private void PlaySynthFallback(float frequency, float duration)
    {
        if (sfxSource == null || !sfxSource.enabled) return;
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Clamp01(1.0f - (t / duration));
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("SynthBeep", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        sfxSource.PlayOneShot(clip, SFXVolume);
    }

    public static SoundManager EnsureInstance()
    {
        if (Instance == null)
        {
            var existing = Object.FindAnyObjectByType<SoundManager>();
            if (existing != null)
            {
                Instance = existing;
            }
            else
            {
                GameObject soundObj = new GameObject("SoundManager");
                Instance = soundObj.AddComponent<SoundManager>();
            }
        }
        return Instance;
    }
}
