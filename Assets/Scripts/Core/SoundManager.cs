using UnityEngine;

public class SoundManager : MonoBehaviour
{
        public float MusicVolume
    {
        get => PlayerPrefs.GetFloat("Audio_MusicVolume", 0.8f);
        set
        {
            PlayerPrefs.SetFloat("Audio_MusicVolume", value);
            PlayerPrefs.Save();
            if (musicSource != null) musicSource.volume = value * 0.45f;
        }
    }

    public float SFXVolume
    {
        get => PlayerPrefs.GetFloat("Audio_SFXVolume", 0.85f);
        set
        {
            PlayerPrefs.SetFloat("Audio_SFXVolume", value);
            PlayerPrefs.Save();
            if (sfxSource != null) sfxSource.volume = value;
            if (voiceSource != null) voiceSource.volume = value * 1.35f;
        }
    }

    public static SoundManager Instance { get; private set; }

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
    [SerializeField] private AudioClip dungeonAmbienceClip;
    [SerializeField] private AudioClip coastalAmbienceClip;

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

        // Tao 3 AudioSources: 1 cho SFX, 1 cho Voice, 1 cho Music
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0.28f;
        musicSource.playOnAwake = false;

        AutoLoadClips();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        PlaySceneAmbience(scene.name);
    }

    void Start()
    {
        PlaySceneAmbience(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void PlaySceneAmbience(string sceneName)
    {
        if (musicSource == null) return;
        AutoLoadClips();

        AudioClip targetClip = (sceneName == "Map2_ValleyRuin") ? coastalAmbienceClip : dungeonAmbienceClip;
        if (targetClip == null) targetClip = dungeonAmbienceClip;

        if (targetClip != null)
        {
            if (musicSource.clip != targetClip || !musicSource.isPlaying)
            {
                musicSource.clip = targetClip;
                musicSource.volume = (sceneName == "Map2_ValleyRuin") ? 0.38f : 0.28f;
                musicSource.Play();
                Debug.Log($"<color=cyan>[AUDIO]</color> Phat nhac nen: {targetClip.name} cho {sceneName}!");
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
        if (dungeonAmbienceClip == null) dungeonAmbienceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath + "dungeon_ambience.wav");
        if (coastalAmbienceClip == null) coastalAmbienceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath + "coastal_forest_ambience.wav");

        // Tu dong nap file giong noi tu thu muc Assets/Audio/Voice/ hoac Assets/Resources/Voice/
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.mp3");
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.wav");
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.ogg");
#endif
    }

    public void PlayAmbience()
    {
        PlaySceneAmbience(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchJitter = 0.08f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayVoice(AudioClip clip, float volume = 1.35f)
    {
        if (clip == null || voiceSource == null) return;
        voiceSource.pitch = 1f;
        voiceSource.PlayOneShot(clip, volume);
    }

    public void PlayUltiVoice()
    {
        if (ultiVoiceClip == null)
        {
            ultiVoiceClip = Resources.Load<AudioClip>("Voice/ulti_voice");
#if UNITY_EDITOR
            if (ultiVoiceClip == null)
            {
                ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/ulti_voice.mp3");
                if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/ulti_voice.wav");
            }
#endif
        }

        if (ultiVoiceClip != null)
        {
            Debug.Log("<color=green>[VOICE]</color> Đang phát giọng lồng tiếng Ulti của nhân vật!");
            PlayVoice(ultiVoiceClip, 1.4f);
        }
        else
        {
            Debug.LogWarning("[VOICE] Chưa load được ulti_voice clip, phát tiếng portal spawn thay thế.");
            PlayPortalSpawn();
        }
    }

    public void PlaySwordSlash()
    {
        PlaySFX(swordSlashClip, 0.85f, 0.12f);
    }

    public void PlayHitImpact()
    {
        PlaySFX(hitImpactClip, 1.0f, 0.1f);
    }

    public void PlayEnemyShoot()
    {
        PlaySFX(enemyShootClip, 0.75f, 0.1f);
    }

    public void PlayShardPickup()
    {
        if (shardPickupClip == null || sfxSource == null) return;

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
        sfxSource.PlayOneShot(shardPickupClip, 0.9f);
    }

    public void PlayPotionDrink()
    {
        PlaySFX(potionDrinkClip, 1.0f, 0.05f);
    }

    public void PlayChestOpen()
    {
        PlaySFX(chestOpenClip, 1.0f, 0.04f);
    }

    public void PlayEyeFlareGlint()
    {
        if (eyeGlintClip == null)
        {
#if UNITY_EDITOR
            eyeGlintClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/eye_glint.wav");
#endif
        }

        if (eyeGlintClip != null)
        {
            PlaySFX(eyeGlintClip, 1.35f, 0.02f);
        }
        else
        {
            PlayPortalSpawn();
        }
    }

    public void PlayHeavenlyAscension()
    {
        if (heavenlyAscensionClip == null)
        {
#if UNITY_EDITOR
            heavenlyAscensionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/heavenly_ascension.wav");
#endif
        }

        if (heavenlyAscensionClip != null)
        {
            PlaySFX(heavenlyAscensionClip, 1.15f, 0.0f);
        }
    }

    public void PlayPortalSpawn()
    {
        PlaySFX(portalSpawnClip, 1.1f, 0.02f);
    }

    public void PlayPlayerHurt()
    {
        PlaySFX(playerHurtClip, 1.0f, 0.08f);
    }

    public void PlayBossDeath()
    {
        PlaySFX(bossDeathClip, 1.25f, 0.05f);
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
