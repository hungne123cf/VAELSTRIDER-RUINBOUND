using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Map3WaveSpawner : MonoBehaviour
{
    public static event System.Action OnAllWavesCleared;

    [Header("Wave Configuration")]
    public int totalWaves = 3;
    public int currentWave = 1;
    public int livingEnemies = 0;
    public bool isWaveActive = false;

    private bool bossSpawned = false;

    void OnEnable()
    {
        GoblinAI.OnEnemyDied += HandleEnemyDied;
        SkeletonAI.OnEnemyDied += HandleEnemyDied;
        SlimeAI.OnEnemyDied += HandleEnemyDied;
        SkeletonKingBossAI.OnBossDied += HandleBossDied;
        BoneEggCocoon.OnEggHatched += HandleEggHatched;
    }

    void OnDisable()
    {
        GoblinAI.OnEnemyDied -= HandleEnemyDied;
        SkeletonAI.OnEnemyDied -= HandleEnemyDied;
        SlimeAI.OnEnemyDied -= HandleEnemyDied;
        SkeletonKingBossAI.OnBossDied -= HandleBossDied;
        BoneEggCocoon.OnEggHatched -= HandleEggHatched;
    }

    void Start()
    {
        StartCoroutine(StartWaveRoutine(1));
    }

    private IEnumerator StartWaveRoutine(int waveIndex)
    {
        currentWave = waveIndex;
        isWaveActive = true;
        yield return new WaitForSeconds(1.5f);

        if (waveIndex == 1)
        {
            DamagePopup.CreateText(Vector3.zero, LocalizationManager.T("ĐỢT 1: BẦY YÊU TINH & QUÁI NHỚT", "WAVE 1: GOBLINS & SLIMES"), new Color(0.4f, 1f, 0.6f));
            yield return new WaitForSeconds(1.0f);

            // Spawn 4 Goblin + 3 Slime
            SpawnGoblin(new Vector3(-6f, 3f, 0f));
            SpawnGoblin(new Vector3(6f, 3f, 0f));
            SpawnGoblin(new Vector3(-5f, -3f, 0f));
            SpawnGoblin(new Vector3(5f, -3f, 0f));

            SpawnSlime(new Vector3(-3f, 4f, 0f));
            SpawnSlime(new Vector3(3f, 4f, 0f));
            SpawnSlime(new Vector3(0f, -4f, 0f));

            livingEnemies = 7;
        }
        else if (waveIndex == 2)
        {
            DamagePopup.CreateText(Vector3.zero, LocalizationManager.T("ĐỢT 2: BINH ĐOÀN BỘ XƯƠNG CỔ", "WAVE 2: ANCIENT SKELETON ARMY"), new Color(0.9f, 0.7f, 0.3f));
            yield return new WaitForSeconds(1.0f);

            // Spawn 5 Skeleton + 3 Goblin
            SpawnSkeleton(new Vector3(-7f, 2f, 0f));
            SpawnSkeleton(new Vector3(7f, 2f, 0f));
            SpawnSkeleton(new Vector3(0f, 5f, 0f));
            SpawnSkeleton(new Vector3(-4f, -4f, 0f));
            SpawnSkeleton(new Vector3(4f, -4f, 0f));

            SpawnGoblin(new Vector3(-2f, 3f, 0f));
            SpawnGoblin(new Vector3(2f, 3f, 0f));
            SpawnGoblin(new Vector3(0f, -3f, 0f));

            livingEnemies = 8;
        }
        else if (waveIndex == 3)
        {
            // DON SACH TOAN BO QUAI TAP CON SOT LAI TRUOC KHI VAO TRAN BOSS!
            ClearAllNonBossEnemies();

            DamagePopup.CreateText(Vector3.zero, LocalizationManager.T("CẢNH BÁO: PHONG ẤN CHÚA TỂ NHỆN CỔ ĐẠI!", "WARNING: ANCIENT SPIDER LORD!"), new Color(0.4f, 0.9f, 1f));
            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.45f, 0.6f);

            yield return new WaitForSeconds(1.5f);

            // Sinh Trung Ken Phong An o giua san dau
            SpawnBoneEggCocoon(new Vector3(0f, 3.5f, 0f));
            isWaveActive = true;
            livingEnemies = 1;
        }
    }

    private void ClearAllNonBossEnemies()
    {
        var goblins = Object.FindObjectsByType<GoblinAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var g in goblins) if (g != null) Destroy(g.gameObject);

        var skeletons = Object.FindObjectsByType<SkeletonAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var s in skeletons) if (s != null) Destroy(s.gameObject);

        var slimes = Object.FindObjectsByType<SlimeAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var sl in slimes) if (sl != null) Destroy(sl.gameObject);
    }

    private void HandleEggHatched(Vector3 pos)
    {
        // Quet don lan nua dam bao chi co minh en Boss Nhen tren san dau
        ClearAllNonBossEnemies();

        DamagePopup.CreateText(pos + Vector3.up * 2.5f, "CHUA TE NHEN THUC TINH!", Color.red);
        SpawnSkeletonKing(pos);
        bossSpawned = true;
        livingEnemies = 1;
    }

    private void HandleEnemyDied(object enemy)
    {
        livingEnemies--;
        if (livingEnemies <= 0 && isWaveActive && !bossSpawned)
        {
            isWaveActive = false;
            if (currentWave < totalWaves)
            {
                StartCoroutine(NextWaveDelayRoutine());
            }
        }
    }

    private IEnumerator NextWaveDelayRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        StartCoroutine(StartWaveRoutine(currentWave + 1));
    }

    private void HandleBossDied(SkeletonKingBossAI boss)
    {
        isWaveActive = false;
        livingEnemies = 0;
        OnAllWavesCleared?.Invoke();
        WaveSpawner.TriggerAllWavesCleared();
    }

    private void SpawnBoneEggCocoon(Vector3 pos)
    {
        GameObject egg = new GameObject("BoneEgg_Cocoon");
        egg.transform.position = pos;
        egg.layer = 6;
        egg.tag = "Enemy";

        var sr = egg.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
        sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Obstacle_0.png");
#endif
        sr.sortingOrder = 1500;

        var col = egg.AddComponent<CircleCollider2D>();
        col.radius = 2.0f;
        col.offset = Vector2.zero;
        col.isTrigger = false;

        egg.AddComponent<AutoDepthSort>();
        egg.AddComponent<BoneEggCocoon>();
    }

    private void SpawnGoblin(Vector3 pos)
    {
        GameObject g = new GameObject("Goblin");
        g.transform.position = pos;
        g.layer = 6;
        g.tag = "Enemy";

        var sr = g.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
        sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Monsters/Goblin/Variant0/Goblin_Walk_0.png");
#endif
        sr.sortingOrder = 1000;

        var anim = g.AddComponent<Animator>();
#if UNITY_EDITOR
        anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Goblin/Goblin.controller");
#endif
        var col = g.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.8f, 1.3f);
        col.offset = new Vector2(0f, 0.65f);
        col.isTrigger = false;

        g.AddComponent<AutoDepthSort>();
        g.AddComponent<GoblinAI>();
    }

    private void SpawnSkeleton(Vector3 pos)
    {
        GameObject s = new GameObject("Skeleton");
        s.transform.position = pos;
        s.layer = 6;
        s.tag = "Enemy";

        var sr = s.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
        sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Monsters/Skeleton/Variant0/Skeleton_Walk_0.png");
#endif
        sr.sortingOrder = 1000;

        var anim = s.AddComponent<Animator>();
#if UNITY_EDITOR
        anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Skeleton/Skeleton.controller");
#endif
        var col = s.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.9f, 2.0f);
        col.offset = new Vector2(0f, 1.0f);
        col.isTrigger = false;

        s.AddComponent<AutoDepthSort>();
        s.AddComponent<SkeletonAI>();
    }

    private void SpawnSlime(Vector3 pos)
    {
        GameObject sl = new GameObject("Slime");
        sl.transform.position = pos;
        sl.layer = 6;
        sl.tag = "Enemy";

        var sr = sl.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
        sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Monsters/Slime/Variant0/Slime_Walk_0.png");
#endif
        sr.sortingOrder = 1000;

        var anim = sl.AddComponent<Animator>();
#if UNITY_EDITOR
        anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Slime/Slime.controller");
#endif
        var col = sl.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.8f, 0.8f);
        col.offset = new Vector2(0f, 0.4f);
        col.isTrigger = false;

        sl.AddComponent<AutoDepthSort>();
        sl.AddComponent<SlimeAI>();
    }

    private void SpawnSkeletonKing(Vector3 pos)
    {
        GameObject bk = new GameObject("SkeletonKing_Boss");
        bk.transform.position = pos;
        bk.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        bk.layer = 6;
        bk.tag = "Enemy";

        var sr = bk.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
        sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Walk_0.png");
#endif
        sr.sortingOrder = 2500;

        var anim = bk.AddComponent<Animator>();
#if UNITY_EDITOR
        anim.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/SkeletonKing/SkeletonKing.controller");
#endif
        var col = bk.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(2.5f, 4.0f);
        col.offset = new Vector2(0f, 2.0f);
        col.direction = CapsuleDirection2D.Vertical;
        col.isTrigger = false;

        bk.AddComponent<AutoDepthSort>();
        bk.AddComponent<SkeletonKingBossAI>();
    }
}