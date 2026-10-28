// ========================================================
// VAELSTRIDER: RUINBOUND - Sound Manager (Dark Fantasy Overhaul)
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
            ApplyVolumes();
        }
    }

    public void SetBGMVolume(float val) => MusicVolume = val;
    public float GetBGMVolume() => MusicVolume;
    public void SetSFXVolume(float val) => SFXVolume = val;
    public float GetSFXVolume() => SFXVolume;
    public void SetMusicVolume(float val) => MusicVolume = val;
    public float GetMusicVolume() => MusicVolume;

    [Header("Audio Clips - Combat")]
    [SerializeField] private AudioClip swordSlashClip;
    [SerializeField] private AudioClip swordSlashHeavyClip;
    [SerializeField] private AudioClip katanaSheatheClip;
    [SerializeField] private AudioClip hitImpactClip;
    [SerializeField] private AudioClip hitCritClip;
    [SerializeField] private AudioClip titanSlamClip;
    [SerializeField] private AudioClip dashWindClip;
    [SerializeField] private AudioClip thunderStrikeClip;

    [Header("Audio Clips - Environment & SFX")]
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
    [SerializeField] private AudioClip uiStoneClickClip;

    [Header("Voice Lines")]
    [SerializeField] private AudioClip ultiVoiceClip;
    [SerializeField] private AudioClip eyeGlintClip;
    [SerializeField] private AudioClip heavenlyAscensionClip;

    // Multi-voice SFX Pool (10 Channels) preventing audio clipping and cutoffs
    private const int SFX_POOL_SIZE = 10;
    private AudioSource[] sfxPool;
    private int sfxPoolIndex = 0;

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
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        try
        {
            // Khoi tao Pool 10 AudioSources cho SFX de combo da tang khong bao gio bi nghen am
            sfxPool = new AudioSource[SFX_POOL_SIZE];
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                sfxPool[i] = src;
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
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SoundManager] Warning khoi tao Audio Device (FMOD): {ex.Message}");
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
        if (voiceSource != null) voiceSource.volume = SFXVolume * 1.35f;
        if (sfxPool != null)
        {
            for (int i = 0; i < sfxPool.Length; i++)
            {
                if (sfxPool[i] != null) sfxPool[i].volume = SFXVolume;
            }
        }
    }

    public void PlaySceneAmbience(string sceneName)
    {
        AutoLoadClips();
        if (musicSource == null) return;

        AudioClip targetClip = null;
        float vol = MusicVolume * 0.45f;
        float pitch = 1.0f;

        // Nếu đang trong Chu Kỳ Tha Hóa / Ác Mộng (Calamity Mode)
        if (CorruptedCycleManager.IsCorruptedActive)
        {
            // Thay thế hoàn toàn tiếng đàn ghita lạc quẻ bằng âm hưởng rền rĩ u tối của Cõi Hư Không
            targetClip = (runePillarHumClip != null) ? runePillarHumClip : dungeonAmbienceClip;
            vol = MusicVolume * 0.50f;
            pitch = 0.75f;
        }
        else if (sceneName == "Map2_ValleyRuin")
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
                musicSource.pitch = pitch;
                musicSource.loop = true;
                musicSource.Play();
                Debug.Log($"<color=cyan>[AUDIO]</color> Đang phát nhạc nền môi trường: {targetClip.name} (Pitch: {pitch}) cho scene {sceneName}!");
            }
        }
    }

    private void AutoLoadClips()
    {
        // 1. Tải từ Resources (chuẩn mọi platform cả Editor và Build)
        if (swordSlashClip == null) swordSlashClip = Resources.Load<AudioClip>("Audio/SFX/sword_slash");
        if (swordSlashHeavyClip == null) swordSlashHeavyClip = Resources.Load<AudioClip>("Audio/SFX/sword_slash_heavy");
        if (katanaSheatheClip == null) katanaSheatheClip = Resources.Load<AudioClip>("Audio/SFX/katana_sheathe");
        if (hitImpactClip == null) hitImpactClip = Resources.Load<AudioClip>("Audio/SFX/hit_impact");
        if (hitCritClip == null) hitCritClip = Resources.Load<AudioClip>("Audio/SFX/hit_crit");
        if (titanSlamClip == null) titanSlamClip = Resources.Load<AudioClip>("Audio/SFX/titan_slam");
        if (dashWindClip == null) dashWindClip = Resources.Load<AudioClip>("Audio/SFX/dash_wind");
        if (thunderStrikeClip == null) thunderStrikeClip = Resources.Load<AudioClip>("Audio/SFX/thunder_strike");
        if (uiStoneClickClip == null) uiStoneClickClip = Resources.Load<AudioClip>("Audio/SFX/ui_stone_click");
        if (heavenlyAscensionClip == null) heavenlyAscensionClip = Resources.Load<AudioClip>("Audio/SFX/heavenly_ascension");

        if (ultiVoiceClip == null) ultiVoiceClip = Resources.Load<AudioClip>("Voice/ulti_voice");

#if UNITY_EDITOR
        string sfxPath = "Assets/Audio/SFX/";
        string musicPath = "Assets/Audio/Music/";
        string voicePath = "Assets/Audio/Voice/";

        if (swordSlashClip == null) swordSlashClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "sword_slash.wav");
        if (swordSlashHeavyClip == null) swordSlashHeavyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "sword_slash_heavy.wav");
        if (katanaSheatheClip == null) katanaSheatheClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "katana_sheathe.wav");
        if (hitImpactClip == null) hitImpactClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "hit_impact.wav");
        if (hitCritClip == null) hitCritClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "hit_crit.wav");
        if (titanSlamClip == null) titanSlamClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "titan_slam.wav");
        if (dashWindClip == null) dashWindClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "dash_wind.wav");
        if (thunderStrikeClip == null) thunderStrikeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "thunder_strike.wav");
        if (uiStoneClickClip == null) uiStoneClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "ui_stone_click.wav");

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
        musicSource.clip = clip;
        musicSource.volume = MusicVolume * 0.45f;
        musicSource.Play();
    }

    public void StopBGM()
    {
        if (musicSource != null) musicSource.Stop();
    }

    /// <summary>
    /// Phát hiệu ứng âm thanh với Voice Pooling và Pitch Randomization chuẩn Action RPG
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchJitter = 0.08f)
    {
        if (clip == null || sfxPool == null || sfxPool.Length == 0) return;

        AudioSource src = sfxPool[sfxPoolIndex];
        sfxPoolIndex = (sfxPoolIndex + 1) % sfxPool.Length;

        if (src != null)
        {
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(clip, SFXVolume * volume);
        }
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

    /// <summary>
    /// Âm thanh vung kiếm biến thiên theo nhịp Combo (Combo Step 0, 1: chém nhanh; 2+: chém nặng)
    /// </summary>
    public void PlaySwordSlash(int comboStep = 0)
    {
        AutoLoadClips();
        if (comboStep >= 2 && swordSlashHeavyClip != null)
        {
            PlaySFX(swordSlashHeavyClip, 1.05f, 0.06f);
        }
        else if (swordSlashClip != null)
        {
            float pitchOffset = (comboStep == 1) ? 0.08f : -0.04f;
            PlaySFX(swordSlashClip, 0.95f, 0.12f);
        }
        else
        {
            PlaySynthFallback(380f, 0.07f);
        }
    }

    public void PlaySwordSlash() => PlaySwordSlash(0);
    public void PlaySwordWhoosh() => PlaySwordSlash(0);
    public void PlayAttackSwipe() => PlaySwordSlash(0);

    private float lastHitImpactTime = 0f;

    /// <summary>
    /// Âm thanh chém trúng đích đa tầng: Thịt, Giáp nặng, hoặc Bạo Kích nổ giòn giã
    /// </summary>
    public void PlayHitImpact(bool isCrit = false, bool isHeavy = false)
    {
        // Chống nghẽn Audio Thread khi chém quét trúng 10-20 quái cùng 1 frame
        if (!isCrit && !isHeavy && Time.unscaledTime - lastHitImpactTime < 0.04f) return;
        lastHitImpactTime = Time.unscaledTime;

        AutoLoadClips();
        if (isCrit && hitCritClip != null)
        {
            PlaySFX(hitCritClip, 1.35f, 0.03f); // Bạo kích uy lực chấn động
        }
        else if (isHeavy && titanSlamClip != null)
        {
            PlaySFX(titanSlamClip, 1.15f, 0.05f); // Đòn chém nặng / nện búa
        }
        else if (hitImpactClip != null)
        {
            PlaySFX(hitImpactClip, 1.05f, 0.10f); // Chém trúng thịt bình thường
        }
        else
        {
            PlaySynthFallback(250f, 0.09f);
        }
    }

    public void PlayHitImpact() => PlayHitImpact(false, false);
    public void PlayCritImpact() => PlayHitImpact(true, false);

    /// <summary>
    /// Tiếng tra kiếm Katana "Ching!" của Ronin
    /// </summary>
    public void PlayKatanaSheathe()
    {
        AutoLoadClips();
        if (katanaSheatheClip != null) PlaySFX(katanaSheatheClip, 1.15f, 0.02f);
    }

    /// <summary>
    /// Tiếng dậm đất / búa nện rung chuyển của Titan
    /// </summary>
    public void PlayTitanSlam()
    {
        AutoLoadClips();
        if (titanSlamClip != null) PlaySFX(titanSlamClip, 1.25f, 0.04f);
        else PlayHitImpact();
    }

    /// <summary>
    /// Tiếng lướt xé gió tốc độ cao
    /// </summary>
    public void PlayDash()
    {
        AutoLoadClips();
        if (dashWindClip != null) PlaySFX(dashWindClip, 0.95f, 0.12f);
        else PlaySwordSlash();
    }

    /// <summary>
    /// Tiếng giật sét lôi đình lan truyền
    /// </summary>
    public void PlayChainLightning()
    {
        AutoLoadClips();
        if (thunderStrikeClip != null) PlaySFX(thunderStrikeClip, 1.1f, 0.05f);
        else PlaySwordSlash();
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
        if (shardPickupClip != null)
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
            PlaySFX(shardPickupClip, 0.9f, 0.04f);
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
        if (uiStoneClickClip != null) PlaySFX(uiStoneClickClip, 0.45f, 0.05f);
        else if (shardPickupClip != null) PlaySFX(shardPickupClip, 0.45f, 0.05f);
    }

    public void PlayButtonClick()
    {
        AutoLoadClips();
        if (uiStoneClickClip != null) PlaySFX(uiStoneClickClip, 0.85f, 0.06f);
        else if (shardPickupClip != null) PlaySFX(shardPickupClip, 0.65f, 0.05f);
    }

    public void PlayMenuOpen()
    {
        AutoLoadClips();
        if (chestOpenClip != null) PlaySFX(chestOpenClip, 0.8f, 0.05f);
    }

    public void PlayMenuClose()
    {
        AutoLoadClips();
        if (chestOpenClip != null) PlaySFX(chestOpenClip, 0.6f, -0.1f);
    }

    public void PlayItemPickup() => PlayShardPickup();
    public void PlayGemSocket() => PlayShardPickup();
    public void PlayCoin() => PlayShardPickup();
    public void PlayButtonBack() => PlayMenuClose();
    public void PlayUpgradeSuccess() => PlayHeavenlyAscension();
    public void PlayUpgradeFail() => PlayPlayerHurt();
    public void PlayError() => PlayUpgradeFail();
    public void PlayChestBreak() => PlayChestOpen();
    public void PlayShrineInteract() => PlayHeavenlyAscension();
    public void PlayPortalEnter() => PlayPortalSpawn();
    public void PlayBarrierBreak() => PlayChestOpen();
    public void PlayVictory() => PlayHeavenlyAscension();
    public void PlayGameOver() => PlayBossDeath();
    public void PlayAttack() => PlaySwordSlash();
    public void PlayUltiSFX() => PlayHeavenlyAscension();
    public void PlayBossRoar() => PlayBossWarning();
    public void PlayBossSlam() => PlayHitImpact(false, true);
    public void PlayBossSkill() => PlayBossWarning();
    public void PlayParrySuccess() => PlayHitImpact(true, true);

    private void PlaySynthFallback(float frequency, float duration)
    {
        if (sfxPool == null || sfxPool.Length == 0) return;
        AudioSource src = sfxPool[sfxPoolIndex];
        if (src == null || !src.enabled) return;

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
        src.PlayOneShot(clip, SFXVolume);
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
