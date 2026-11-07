using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Map3WaveSpawner : MonoBehaviour
{
    public static event System.Action OnAllWavesCleared;

    [Header("Wave Configuration")]
    public int totalWaves = 10;
    public int currentWave = 1;
    public int livingEnemies = 0;
    public bool isWaveActive = false;

    private bool bossSpawned = false;

    void Awake()
    {
        totalWaves = 10;
    }

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
        if (FindAnyObjectByType<DungeonRoomManager>() != null)
        {
            isWaveActive = false;
            enabled = false;
            return;
        }
        totalWaves = 10;
        StartCoroutine(StartWaveRoutine(1));
    }

    private IEnumerator StartWaveRoutine(int waveIndex)
    {
        currentWave = waveIndex;
        isWaveActive = true;
        yield return new WaitForSeconds(1.2f);

        var waveUI = FindAnyObjectByType<WaveUI>();
        string wavePrefix = LocalizationManager.Get("HUD_WAVE");

        if (waveIndex == 1)
        {
            string t = $"{wavePrefix} 1 / 10: " + LocalizationManager.T("BẦY YÊU TINH & QUÁI NHỚT", "GOBLINS & SLIMES");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Quét sạch bầy quái nhỏ khai màn!", "Purge the opening swarm!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            SpawnGoblin(new Vector3(-6f, 3f, 0f));
            SpawnGoblin(new Vector3(6f, 3f, 0f));
            SpawnGoblin(new Vector3(-5f, -3f, 0f));
            SpawnGoblin(new Vector3(5f, -3f, 0f));
            SpawnSlime(new Vector3(-3f, 4f, 0f));
            SpawnSlime(new Vector3(3f, 4f, 0f));

            livingEnemies = 6;
        }
        else if (waveIndex == 2)
        {
            string t = $"{wavePrefix} 2 / 10: " + LocalizationManager.T("YÊU TINH TẬP KÍCH & ĐẠI BẦY NHỚT", "GOBLIN AMBUSH & SLIME PACK");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Cẩn thận đòn áp sát nhanh của yêu tinh!", "Beware of fast goblin leaps!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            SpawnGoblin(new Vector3(-6f, 2f, 0f));
            SpawnGoblin(new Vector3(6f, 2f, 0f));
            SpawnGoblin(new Vector3(-4f, -3f, 0f));
            SpawnGoblin(new Vector3(4f, -3f, 0f));
            SpawnSlime(new Vector3(-3f, 4f, 0f));
            SpawnSlime(new Vector3(3f, 4f, 0f));
            SpawnSlime(new Vector3(-1f, -4f, 0f));
            SpawnSlime(new Vector3(1f, -4f, 0f));

            livingEnemies = 8;
        }
        else if (waveIndex == 3)
        {
            string t = $"{wavePrefix} 3 / 10: " + LocalizationManager.T("BINH ĐOÀN BỘ XƯƠNG THỨC TỈNH", "SKELETON ARMY AWAKENS");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Bộ xương cổ đại mang khiên giáp thức giấc!", "Ancient armored skeletons arise!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            SpawnSkeleton(new Vector3(-6f, 3f, 0f));
            SpawnSkeleton(new Vector3(6f, 3f, 0f));
            SpawnSkeleton(new Vector3(-4f, -3f, 0f));
            SpawnSkeleton(new Vector3(4f, -3f, 0f));
            SpawnSlime(new Vector3(-2f, 4f, 0f));
            SpawnSlime(new Vector3(2f, 4f, 0f));
            SpawnSlime(new Vector3(-2f, -4f, 0f));
            SpawnSlime(new Vector3(2f, -4f, 0f));

            livingEnemies = 8;
        }
        else if (waveIndex == 4)
        {
            string t = $"{wavePrefix} 4 / 10: " + LocalizationManager.T("LIÊN MINH XƯƠNG & YÊU TINH", "SKELETON & GOBLIN ALLIANCE");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Sự phối hợp giữa kiếm xương và đòn chém yêu tinh!", "Combined bone swords and goblin slashes!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            SpawnSkeleton(new Vector3(-7f, 2f, 0f));
            SpawnSkeleton(new Vector3(7f, 2f, 0f));
            SpawnSkeleton(new Vector3(0f, 5f, 0f));
            SpawnSkeleton(new Vector3(-4f, -4f, 0f));
            SpawnSkeleton(new Vector3(4f, -4f, 0f));
            SpawnGoblin(new Vector3(-3f, 3f, 0f));
            SpawnGoblin(new Vector3(3f, 3f, 0f));
            SpawnGoblin(new Vector3(-2f, -2f, 0f));
            SpawnGoblin(new Vector3(2f, -2f, 0f));

            livingEnemies = 9;
        }
        else if (waveIndex == 5)
        {
            string t = $"{wavePrefix} 5 / 10: " + LocalizationManager.T("TIỂU ĐỘI BỘ XƯƠNG CỔ ĐẠI", "ANCIENT SKELETON BRIGADE");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Binh đoàn xương tràn ngập chiến trường!", "Skeleton legion swarms the field!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            SpawnSkeleton(new Vector3(-6f, 3f, 0f));
            SpawnSkeleton(new Vector3(6f, 3f, 0f));
            SpawnSkeleton(new Vector3(-3f, 4f, 0f));
            SpawnSkeleton(new Vector3(3f, 4f, 0f));
            SpawnSkeleton(new Vector3(-5f, -3f, 0f));
            SpawnSkeleton(new Vector3(5f, -3f, 0f));
            SpawnSlime(new Vector3(-2f, 2f, 0f));
            SpawnSlime(new Vector3(2f, 2f, 0f));
            SpawnSlime(new Vector3(-2f, -2f, 0f));
            SpawnSlime(new Vector3(2f, -2f, 0f));

            livingEnemies = 10;
        }
        else if (waveIndex == 6)
        {
            string t = $"{wavePrefix} 6 / 10: " + LocalizationManager.T("BẦY YÊU TINH CUỒNG NỘ", "FRENZIED GOBLIN HORDE");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Bầy yêu tinh tăng cường số lượng áp đảo!", "Goblins assault with overwhelming speed!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            SpawnGoblin(new Vector3(-6f, 3f, 0f));
            SpawnGoblin(new Vector3(6f, 3f, 0f));
            SpawnGoblin(new Vector3(-4f, 4f, 0f));
            SpawnGoblin(new Vector3(4f, 4f, 0f));
            SpawnGoblin(new Vector3(-5f, -3f, 0f));
            SpawnGoblin(new Vector3(5f, -3f, 0f));
            SpawnSkeleton(new Vector3(-2f, 2f, 0f));
            SpawnSkeleton(new Vector3(2f, 2f, 0f));
            SpawnSkeleton(new Vector3(-2f, -3f, 0f));
            SpawnSkeleton(new Vector3(2f, -3f, 0f));

            livingEnemies = 10;
        }
        else if (waveIndex == 7)
        {
            string t = $"{wavePrefix} 7 / 10: " + LocalizationManager.T("ĐỢT TẤN CÔNG ĐA CHỦNG LOÀI", "TRI-SPECIES ONSLAUGHT");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Sự hợp kích của toàn bộ sinh vật bóng tối!", "Full synergy assault of shadow creatures!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            SpawnSkeleton(new Vector3(-7f, 3f, 0f));
            SpawnSkeleton(new Vector3(7f, 3f, 0f));
            SpawnSkeleton(new Vector3(0f, 5f, 0f));
            SpawnSkeleton(new Vector3(-4f, -4f, 0f));
            SpawnSkeleton(new Vector3(4f, -4f, 0f));
            SpawnGoblin(new Vector3(-3f, 2f, 0f));
            SpawnGoblin(new Vector3(3f, 2f, 0f));
            SpawnGoblin(new Vector3(-2f, -2f, 0f));
            SpawnGoblin(new Vector3(2f, -2f, 0f));
            SpawnSlime(new Vector3(0f, 2f, 0f));
            SpawnSlime(new Vector3(0f, -3f, 0f));

            livingEnemies = 11;
        }
        else if (waveIndex == 8)
        {
            string t = $"{wavePrefix} 8 / 10: " + LocalizationManager.T("BINH ĐOÀN TỬ THẦN TẬP KÍCH", "DEATHWATCH LEGION ADVANCES");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Quái vật đông đảo và hung tợn tột độ!", "Extreme swarm density! Stand your ground!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            for (int i = 0; i < 6; i++)
            {
                float ang = (i / 6f) * 360f * Mathf.Deg2Rad;
                SpawnSkeleton(new Vector3(Mathf.Cos(ang) * 6f, Mathf.Sin(ang) * 4f, 0f));
            }
            for (int i = 0; i < 6; i++)
            {
                float ang = ((i + 0.5f) / 6f) * 360f * Mathf.Deg2Rad;
                SpawnGoblin(new Vector3(Mathf.Cos(ang) * 4.5f, Mathf.Sin(ang) * 3f, 0f));
            }

            livingEnemies = 12;
        }
        else if (waveIndex == 9)
        {
            string t = $"{wavePrefix} 9 / 10: " + LocalizationManager.T("ĐỢT TẤN CÔNG CUỐI CÙNG TRƯỚC ĐẠI TRÙM!", "FINAL WAVE BEFORE ANCIENT BOSS!");
            if (waveUI != null) waveUI.ShowBanner(t, LocalizationManager.T("Càn quét đợt sóng quái cuối cùng để tích lũy sức mạnh!", "Purge the final wave to reach peak power!"), 2.5f);
            yield return new WaitForSeconds(0.8f);

            for (int i = 0; i < 7; i++)
            {
                float ang = (i / 7f) * 360f * Mathf.Deg2Rad;
                SpawnSkeleton(new Vector3(Mathf.Cos(ang) * 6.5f, Mathf.Sin(ang) * 4.5f, 0f));
            }
            for (int i = 0; i < 5; i++)
            {
                float ang = (i / 5f) * 360f * Mathf.Deg2Rad;
                SpawnGoblin(new Vector3(Mathf.Cos(ang) * 4.5f, Mathf.Sin(ang) * 3f, 0f));
            }
            SpawnSlime(new Vector3(-2f, 0f, 0f));
            SpawnSlime(new Vector3(2f, 0f, 0f));

            livingEnemies = 14;
        }
        else if (waveIndex == 10)
        {
            // DON SACH TOAN BO QUAI TAP CON SOT LAI TRUOC KHI VAO TRAN BOSS!
            ClearAllNonBossEnemies();

            string bossTitle = LocalizationManager.T("CẢNH BÁO: CHÚA TỂ NHỆN CỔ ĐẠI! (ĐỢT 10 / 10)", "BOSS INCOMING: ANCIENT SPIDER LORD! (WAVE 10 / 10)");
            if (waveUI != null) waveUI.ShowBanner(bossTitle, LocalizationManager.T("PHONG ẤN TRỨNG KÉN ĐÃ XUẤT HIỆN!", "THE COCOON SEAL HAS APPEARED!"), 3.0f);

            var cam = Object.FindAnyObjectByType<CameraFollow>();
            if (cam != null) cam.Shake(0.5f, 0.7f);

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

        var minispiders = Object.FindObjectsByType<MiniSpiderMinion>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var ms in minispiders) if (ms != null) Destroy(ms.gameObject);

        var eggs = Object.FindObjectsByType<BoneEggObstacle>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var e in eggs) if (e != null) Destroy(e.gameObject);
    }

    private void HandleEggHatched(Vector3 pos)
    {
        if (!enabled) return;

        // Quet don lan nua dam bao chi co minh en Boss Nhen tren san dau
        ClearAllNonBossEnemies();

        DamagePopup.CreateText(pos + Vector3.up * 2.5f, LocalizationManager.T("CHÚA TỂ NHỆN THỨC TỈNH!", "SPIDER LORD AWAKENS!"), Color.red);
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
        // Thuong dot quai
        string clearMsg = LocalizationManager.T($"HOÀN THÀNH ĐỢT {currentWave} / {totalWaves}!", $"WAVE {currentWave} / {totalWaves} CLEARED!");

        var waveUI = FindAnyObjectByType<WaveUI>();
        if (waveUI != null)
        {
            waveUI.ShowBanner(clearMsg, LocalizationManager.T("+10 Mảnh Linh Hồn. Chuẩn bị đợt tiếp theo...", "+10 Shards. Prepare for next wave..."), 2.2f);
        }

        AethelShard.SpawnShards(Vector3.zero, 10);
        WorldItemPickup.RollDrop(Vector3.zero + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.8f, 0.8f), 0), false);
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(StartWaveRoutine(currentWave + 1));
    }

    private void HandleBossDied(SkeletonKingBossAI boss)
    {
        isWaveActive = false;
        livingEnemies = 0;

        if (CorruptedCycleManager.IsCorruptedActive && CorruptedCycleManager.CurrentRealm == CalamityRealm.VoidNightmare)
        {
            StartCoroutine(WaitForDoppelgangerRoutine());
        }
        else
        {
            OnAllWavesCleared?.Invoke();
            WaveSpawner.TriggerAllWavesCleared();
        }
    }

    private IEnumerator WaitForDoppelgangerRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        while (ShadowDoppelgangerBoss.ActiveBoss != null && !ShadowDoppelgangerBoss.ActiveBoss.IsDead)
        {
            yield return new WaitForSeconds(0.5f);
        }
        yield return new WaitForSeconds(1.0f);
        OnAllWavesCleared?.Invoke();
        WaveSpawner.TriggerAllWavesCleared();
    }

    public static GameObject SpawnBoneEggCocoonStatic(Vector3 pos)
    {
        GameObject egg = new GameObject("BoneEgg_Cocoon");
        egg.transform.position = pos;
        egg.layer = 6;
        egg.tag = "Enemy";

        var sr = egg.AddComponent<SpriteRenderer>();
        var spr = Resources.Load<Sprite>("Enemies/Map3/SkeletonKing_Obstacle_0");
#if UNITY_EDITOR
        if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Obstacle_0.png");
#endif
        if (spr == null) spr = MagmaCalamityHazardManager.GetSoftCircleSprite();
        sr.sprite = spr;
        sr.sortingOrder = 1500;

        egg.AddComponent<AutoDepthSort>();
        egg.AddComponent<BoneEggCocoon>();
        return egg;
    }

    private void SpawnBoneEggCocoon(Vector3 pos) => SpawnBoneEggCocoonStatic(pos);

    public static GameObject SpawnGoblinStatic(Vector3 pos)
    {
        GameObject g = new GameObject("Goblin");
        g.transform.position = pos;
        g.layer = 6;
        g.tag = "Enemy";

        var sr = g.AddComponent<SpriteRenderer>();
        var spr = Resources.Load<Sprite>("Enemies/Map3/Goblin_Walk_0");
#if UNITY_EDITOR
        if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Monsters/Goblin/Variant0/Goblin_Walk_0.png");
#endif
        sr.sprite = spr;
        sr.sortingOrder = 1000;

        var anim = g.AddComponent<Animator>();
        var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/Goblin");
#if UNITY_EDITOR
        if (ctrl == null) ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Goblin/Goblin.controller");
#endif
        anim.runtimeAnimatorController = ctrl;

        g.AddComponent<AutoDepthSort>();
        g.AddComponent<GoblinAI>();
        EnemyHealthHelper.OnEnemySpawned(g);
        return g;
    }

    private void SpawnGoblin(Vector3 pos) => SpawnGoblinStatic(pos);

    public static GameObject SpawnSkeletonStatic(Vector3 pos)
    {
        GameObject s = new GameObject("Skeleton");
        s.transform.position = pos;
        s.layer = 6;
        s.tag = "Enemy";

        var sr = s.AddComponent<SpriteRenderer>();
        var spr = Resources.Load<Sprite>("Enemies/Map3/Skeleton_Walk_0");
#if UNITY_EDITOR
        if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Monsters/Skeleton/Variant0/Skeleton_Walk_0.png");
#endif
        sr.sprite = spr;
        sr.sortingOrder = 1000;

        var anim = s.AddComponent<Animator>();
        var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/Skeleton");
#if UNITY_EDITOR
        if (ctrl == null) ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Skeleton/Skeleton.controller");
#endif
        anim.runtimeAnimatorController = ctrl;

        s.AddComponent<AutoDepthSort>();
        s.AddComponent<SkeletonAI>();
        EnemyHealthHelper.OnEnemySpawned(s);
        return s;
    }

    private void SpawnSkeleton(Vector3 pos) => SpawnSkeletonStatic(pos);

    public static GameObject SpawnSlimeStatic(Vector3 pos)
    {
        GameObject sl = new GameObject("Slime");
        sl.transform.position = pos;
        sl.layer = 6;
        sl.tag = "Enemy";

        var sr = sl.AddComponent<SpriteRenderer>();
        var spr = Resources.Load<Sprite>("Enemies/Map3/Slime_Walk_0");
#if UNITY_EDITOR
        if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Monsters/Slime/Variant0/Slime_Walk_0.png");
#endif
        sr.sprite = spr;
        sr.sortingOrder = 1000;

        var anim = sl.AddComponent<Animator>();
        var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/Slime");
#if UNITY_EDITOR
        if (ctrl == null) ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Slime/Slime.controller");
#endif
        anim.runtimeAnimatorController = ctrl;

        sl.AddComponent<AutoDepthSort>();
        sl.AddComponent<SlimeAI>();
        EnemyHealthHelper.OnEnemySpawned(sl);
        return sl;
    }

    private void SpawnSlime(Vector3 pos) => SpawnSlimeStatic(pos);

    public static SkeletonKingBossAI SpawnSkeletonKingStatic(Vector3 pos)
    {
        GameObject bk = new GameObject("SkeletonKing_Boss");
        bk.transform.position = pos;
        bk.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        bk.layer = 6;
        bk.tag = "Enemy";

        var sr = bk.AddComponent<SpriteRenderer>();
        var spr = Resources.Load<Sprite>("Enemies/Map3/SkeletonKing_Walk_0");
#if UNITY_EDITOR
        if (spr == null) spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ART/Bosses/Skeleton King/SkeletonKing_Walk_0.png");
#endif
        sr.sprite = spr;
        sr.sortingOrder = 2500;

        var anim = bk.AddComponent<Animator>();
        var ctrl = Resources.Load<RuntimeAnimatorController>("Controllers/SkeletonKing");
#if UNITY_EDITOR
        if (ctrl == null) ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/SkeletonKing/SkeletonKing.controller");
#endif
        anim.runtimeAnimatorController = ctrl;

        bk.AddComponent<AutoDepthSort>();
        var boss = bk.AddComponent<SkeletonKingBossAI>();
        EnemyHealthHelper.OnEnemySpawned(bk);
        return boss;
    }

    private void SpawnSkeletonKing(Vector3 pos) => SpawnSkeletonKingStatic(pos);
}