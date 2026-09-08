using UnityEngine;

public class SoundManager : MonoBehaviour
{
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

    [Header("Voice Lines")]
    [SerializeField] private AudioClip ultiVoiceClip;

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

    void Start()
    {
        PlayAmbience();
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
        if (playerHurtClip == null) playerHurtClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "player_hurt.wav");
        if (bossDeathClip == null) bossDeathClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(sfxPath + "boss_death.wav");
        if (dungeonAmbienceClip == null) dungeonAmbienceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath + "dungeon_ambience.wav");

        // Tu dong nap file giong noi tu thu muc Assets/Audio/Voice/ hoac Assets/Resources/Voice/
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.mp3");
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.wav");
        if (ultiVoiceClip == null) ultiVoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(voicePath + "ulti_voice.ogg");
#endif
    }

    public void PlayAmbience()
    {
        if (dungeonAmbienceClip != null && musicSource != null)
        {
            musicSource.clip = dungeonAmbienceClip;
            musicSource.Play();
        }
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
