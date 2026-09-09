using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Map3ArenaManager : MonoBehaviour
{
    public static Map3ArenaManager Instance { get; private set; }

    [Header("Cổng Phong Ấn (Room Barrier)")]
    [SerializeField] private RoomBarrier entranceBarrier;

    [Header("Boss & Quái Vật")]
    [SerializeField] private BloodMonsterAI bossGorMok;
    [SerializeField] private List<BloodMonsterAI> minions = new List<BloodMonsterAI>();

    [Header("Cổng Cổ Tịch (Ruin Portal)")]
    [SerializeField] private RuinPortal exitPortal;

    [Header("Vùng kích hoạt trận đấu Boss")]
    [SerializeField] private BoxCollider2D arenaTrigger;

    [Header("Trạng Thái")]
    [SerializeField] private bool fightStarted = false;
    [SerializeField] private bool fightCleared = false;

    public bool FightStarted => fightStarted;
    public bool FightCleared => fightCleared;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        BloodMonsterAI.OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        BloodMonsterAI.OnEnemyDied -= HandleEnemyDied;
    }

    void Start()
    {
        // Ẩn Cổng Kết Thúc khi chưa đánh xong Boss
        if (exitPortal != null)
        {
            var sr = exitPortal.GetComponent<SpriteRenderer>();
            var col = exitPortal.GetComponent<Collider2D>();
            if (sr != null) sr.enabled = false;
            if (col != null) col.enabled = false;
            exitPortal.transform.localScale = Vector3.zero;
        }

        // Cổng ban đầu mở để người chơi bước vào đấu trường
        if (entranceBarrier != null)
        {
            entranceBarrier.Unlock();
        }

        // Tắt WaveSpawner nếu có trong Scene để không sinh nhầm quái robot
        var spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null)
        {
            spawner.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fightStarted || fightCleared) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            StartBossFight();
        }
    }

    /// <summary>
    /// Kích hoạt khi người chơi bước qua bậc thang vào đài giác đấu
    /// </summary>
    public void StartBossFight()
    {
        if (fightStarted || fightCleared) return;
        fightStarted = true;

        Debug.Log("<color=red>[MAP 3 ARENA]</color> Người chơi đã tiến vào đấu trường! Khóa cổng phong ấn!");

        // 1. Khóa cổng ngăn chặn lối vào (Barrier)
        if (entranceBarrier != null)
        {
            entranceBarrier.Lock();
        }

        // 2. Rung lắc camera tạo cảm giác uy nghiêm
        var cam = CameraFollow.Instance ?? FindAnyObjectByType<CameraFollow>();
        if (cam != null) cam.Shake(0.4f, 0.5f);

        // 3. Âm thanh và Banner thông báo
        SoundManager.EnsureInstance()?.PlayHitImpact();
        WaveUI.EnsureUIExists()?.ShowBanner("GOR'MOK THỨC TỈNH", "Lối thoát đã bị phong ấn! Hãy đánh bại Huyết Thú Cổ Tự!");

        // 4. Bật thanh máu Boss Gor'Mok
        if (bossGorMok != null)
        {
            BossHealthBarUI.EnsureInstance()?.ShowBoss("GOR'MOK • HUYẾT THÚ CỔ TỰ", bossGorMok.totalBossPhases);
        }
    }

    private void HandleEnemyDied(BloodMonsterAI enemy)
    {
        if (enemy == null) return;

        if (minions.Contains(enemy))
        {
            minions.Remove(enemy);
        }

        // Khi Boss Gor'Mok bị tiêu diệt
        if (enemy == bossGorMok || enemy.IsBoss)
        {
            OnBossDefeated();
        }
    }

    private void OnBossDefeated()
    {
        if (fightCleared) return;
        fightCleared = true;

        Debug.Log("<color=green>[MAP 3 ARENA]</color> Boss Gor'Mok đã gục ngã! Mở khóa cổng ngăn chặn và kích hoạt Cổng Cổ Tích!");

        // 1. Mở khóa cổng ngăn chặn để người chơi tự do di chuyển
        if (entranceBarrier != null)
        {
            entranceBarrier.Unlock();
        }

        // 2. Kích hoạt Cổng Cổ Tích (Ruin Portal)
        StartCoroutine(ActivatePortalDelayed());

        // 3. Thông báo chiến thắng huy hoàng
        WaveUI.EnsureUIExists()?.ShowBanner("CHIẾN THẮNG HUYỀN THOẠI!", "Cổ Tự đã được thanh tẩy! Cổng dịch chuyển đã mở!");
    }

    private IEnumerator ActivatePortalDelayed()
    {
        yield return new WaitForSeconds(1.2f);

        if (exitPortal != null)
        {
            exitPortal.gameObject.SetActive(true);
            exitPortal.ActivatePortal();
            SoundManager.EnsureInstance()?.PlayPortalSpawn();
        }
    }

    public void SetupReferences(RoomBarrier barrier, BloodMonsterAI boss, List<BloodMonsterAI> minionList, RuinPortal portal, BoxCollider2D trigger)
    {
        entranceBarrier = barrier;
        bossGorMok = boss;
        minions = minionList;
        exitPortal = portal;
        arenaTrigger = trigger;
    }

    public static Map3ArenaManager EnsureManager()
    {
        if (Instance != null) return Instance;

        var existing = FindAnyObjectByType<Map3ArenaManager>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject go = new GameObject("Map3_Arena_Manager");
        var mgr = go.AddComponent<Map3ArenaManager>();
        Instance = mgr;

        // 1. Cổng ngăn chặn (Room Barrier)
        var barrier = FindAnyObjectByType<RoomBarrier>();
        if (barrier == null)
        {
            barrier = RoomBarrier.Create(new Vector3(1.8f, -0.5f, 0f), true, 3.2f, "Arena_Entrance_Barrier");
        }
        else
        {
            barrier.transform.position = new Vector3(1.8f, -0.5f, 0f);
            barrier.Setup(true, 3.2f);
        }

        // 2. Cổng cổ tịch (Ruin Portal)
        var portal = FindAnyObjectByType<RuinPortal>();
        if (portal != null)
        {
            portal.transform.position = new Vector3(24.0f, 10.0f, 0f);
            HeavenlyPortalFX.Attach(portal.gameObject);
        }

        // 3. Quái vật và Boss
        var allBM = FindObjectsByType<BloodMonsterAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        BloodMonsterAI boss = null;
        List<BloodMonsterAI> minionList = new List<BloodMonsterAI>();

        foreach (var bm in allBM)
        {
            if (bm != null)
            {
                if (bm.isBoss || bm.gameObject.name.Contains("Boss")) boss = bm;
                else minionList.Add(bm);
            }
        }

        // Tự động sinh Boss nếu chưa có trong scene
        if (boss == null)
        {
            boss = SpawnBossGorMok(new Vector3(18.0f, 6.0f, 0f));
        }

        // Tự động sinh 3 Minions nếu chưa có
        if (minionList.Count == 0)
        {
            minionList.Add(SpawnMinion(new Vector3(9.0f, 1.0f, 0f), 1));
            minionList.Add(SpawnMinion(new Vector3(14.0f, -2.0f, 0f), 2));
            minionList.Add(SpawnMinion(new Vector3(12.0f, 7.0f, 0f), 3));
        }

        // 4. Vùng Trigger kích hoạt trận đấu khi người chơi bước vào đấu trường
        var trig = go.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(4.5f, 6.0f);
        go.transform.position = new Vector3(5.5f, -0.5f, 0f);

        mgr.SetupReferences(barrier, boss, minionList, portal, trig);

        // 5. Tắt WaveSpawner nếu có
        var spawner = FindAnyObjectByType<WaveSpawner>();
        if (spawner != null) spawner.gameObject.SetActive(false);

        // 6. Camera Elevation Trigger cho bậc thang
        EnsureCameraElevationTrigger();

        return mgr;
    }

    public static BloodMonsterAI SpawnBossGorMok(Vector3 pos)
    {
        GameObject bossObj = new GameObject("Boss_GorMok_BloodMonster");
        bossObj.transform.position = pos;
        bossObj.transform.localScale = Vector3.one * 1.85f;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) bossObj.layer = enemyLayer;
        bossObj.tag = "Enemy";

        var sr = bossObj.AddComponent<SpriteRenderer>();
        sr.sprite = LoadBloodMonsterSprite();
        sr.sortingOrder = 3000;

        var anim = bossObj.AddComponent<Animator>();
        anim.runtimeAnimatorController = LoadBloodMonsterAnimator();

        var cap = bossObj.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.5f, 1.4f);
        cap.direction = CapsuleDirection2D.Vertical;
        cap.isTrigger = false;

        var rb = bossObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var bossAI = bossObj.AddComponent<BloodMonsterAI>();
        bossAI.SetupBoss(1.85f, 2);
        return bossAI;
    }

    public static BloodMonsterAI SpawnMinion(Vector3 pos, int index)
    {
        GameObject minionObj = new GameObject($"BloodMonster_Minion_{index}");
        minionObj.transform.position = pos;
        minionObj.transform.localScale = Vector3.one * 1.05f;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1) minionObj.layer = enemyLayer;
        minionObj.tag = "Enemy";

        var sr = minionObj.AddComponent<SpriteRenderer>();
        sr.sprite = LoadBloodMonsterSprite();
        sr.sortingOrder = 3000;

        var anim = minionObj.AddComponent<Animator>();
        anim.runtimeAnimatorController = LoadBloodMonsterAnimator();

        var cap = minionObj.AddComponent<CapsuleCollider2D>();
        cap.size = new Vector2(1.0f, 1.0f);
        cap.direction = CapsuleDirection2D.Vertical;
        cap.isTrigger = false;

        var rb = minionObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var ai = minionObj.AddComponent<BloodMonsterAI>();
        ai.isBoss = false;
        return ai;
    }

    private static void EnsureCameraElevationTrigger()
    {
        var existing = FindAnyObjectByType<CameraElevationTrigger>();
        if (existing != null) return;

        GameObject elevationObj = new GameObject("Camera_Elevation_Trigger");
        elevationObj.transform.position = new Vector3(4.0f, -2.5f, 0f);
        var col = elevationObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(4.5f, 4.0f);
        elevationObj.AddComponent<CameraElevationTrigger>();
    }

    private static RuntimeAnimatorController LoadBloodMonsterAnimator()
    {
        RuntimeAnimatorController anim = null;
#if UNITY_EDITOR
        anim = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/BloodMonster/BloodMonster.controller");
#endif
        if (anim == null) anim = Resources.Load<RuntimeAnimatorController>("Animation/BloodMonster");
        return anim;
    }

    private static Sprite LoadBloodMonsterSprite()
    {
        Sprite s = null;
#if UNITY_EDITOR
        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ART/Blood Monster_A with shadows/Blood Monster_A.png");
        if (all != null)
        {
            foreach (var a in all)
            {
                if (a is Sprite sp) { s = sp; break; }
            }
        }
#endif
        return s;
    }
}


