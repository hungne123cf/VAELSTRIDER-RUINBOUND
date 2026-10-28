using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveSpawner : MonoBehaviour
{
    [Header("Enemy Configuration")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnRadius = 4.5f;
    [SerializeField] private float timeBetweenWaves = 3.5f;

    [Header("Wave Settings")]
    [SerializeField] private int totalWaves = 3;
    [SerializeField] private bool roomTriggeredMode = true;

    private int currentWave = 0;
    private int aliveEnemies = 0;
    private readonly HashSet<GameObject> currentWaveEnemies = new HashSet<GameObject>();
    private bool waveInProgress = false;
    private bool isSpawningWave = false;
    private bool allWavesCleared = false;
    private System.Action currentRoomClearedCallback;

    // Events
    public static event System.Action<int, int, string> OnWaveStarted;
    public static event System.Action<string> OnWaveMessage;
    public static event System.Action OnAllWavesCleared;
    public static void TriggerAllWavesCleared() => OnAllWavesCleared?.Invoke();

    void Awake()
    {
        totalWaves = 10;

        if (enemyPrefab == null)
        {
            var bot = FindAnyObjectByType<BotWheelAI>();
            if (bot != null)
            {
                enemyPrefab = bot.gameObject;
                bot.gameObject.SetActive(false);
            }
        }
    }

    void OnEnable()
    {
        BotWheelAI.OnEnemyDied += HandleEnemyDied;
        BloodMonsterAI.OnEnemyDied += HandleBloodMonsterDied;
        ValleyBeastAI.OnEnemyDied += HandleValleyBeastDied;
        ValleySpitterAI.OnEnemyDied += HandleValleySpitterDied;
        ValleyGolemBossAI.OnEnemyDied += HandleValleyBossDied;
    }

    void OnDisable()
    {
        BotWheelAI.OnEnemyDied -= HandleEnemyDied;
        BloodMonsterAI.OnEnemyDied -= HandleBloodMonsterDied;
        ValleyBeastAI.OnEnemyDied -= HandleValleyBeastDied;
        ValleySpitterAI.OnEnemyDied -= HandleValleySpitterDied;
        ValleyGolemBossAI.OnEnemyDied -= HandleValleyBossDied;
    }

    private void HandleValleyBeastDied(ValleyBeastAI e) { HandleAnyEnemyDied(e != null ? e.gameObject : null); }
    private void HandleValleySpitterDied(ValleySpitterAI e) { HandleAnyEnemyDied(e != null ? e.gameObject : null); }
    private void HandleValleyBossDied(ValleyGolemBossAI e) { HandleAnyEnemyDied(e != null ? e.gameObject : null); }

    void Start()
    {
        // Neu co DungeonRoomManager thi khong tu dong khoi dong Wave ngay tai cho ma cho nguoi choi buoc vao phong
        var roomMgr = FindAnyObjectByType<DungeonRoomManager>();
        if (roomMgr != null)
        {
            roomTriggeredMode = true;
            Debug.Log("<color=cyan>[WAVE SPAWNER]</color> Kích hoạt chế độ điều phối theo phòng DungeonRoom!");
        }
        else
        {
            StartCoroutine(StartFirstWaveRoutine());
        }
    }

    private IEnumerator StartFirstWaveRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        StartNextWave();
    }

    public void SpawnRoomWave(int waveNumber, int totalWaveCount, string waveTitle, List<Vector3> spawnPositions, bool isBoss, System.Action onCleared)
    {
        currentWave = waveNumber;
        totalWaves = totalWaveCount;
        currentRoomClearedCallback = onCleared;
        currentWaveEnemies.Clear();
        aliveEnemies = spawnPositions.Count;
        waveInProgress = true;
        isSpawningWave = true;

        OnWaveStarted?.Invoke(currentWave, totalWaves, waveTitle);
        StartCoroutine(SpawnRoomEnemiesRoutine(spawnPositions, isBoss));
    }

    public void SpawnRoomEnemies(List<Vector3> spawnPositions, bool isBoss, System.Action onCleared)
    {
        string wavePrefix = LocalizationManager.Get("HUD_WAVE");
        currentWave++;
        string title = isBoss 
            ? LocalizationManager.T($"CẢNH BÁO: TRÙM TITAN WHEEL! ({wavePrefix} {currentWave} / {totalWaves})", $"BOSS INCOMING: TITAN WHEEL! ({wavePrefix} {currentWave} / {totalWaves})") 
            : $"{wavePrefix} {currentWave} / {totalWaves}";

        SpawnRoomWave(currentWave, totalWaves, title, spawnPositions, isBoss, onCleared);
    }

    private void IgnorePlayerCollision(GameObject newEnemy)
    {
        if (newEnemy == null) return;
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            var pCols = player.GetComponentsInChildren<Collider2D>();
            var eCols = newEnemy.GetComponentsInChildren<Collider2D>();
            foreach (var pCol in pCols)
            {
                foreach (var eCol in eCols)
                {
                    if (pCol != null && eCol != null)
                        Physics2D.IgnoreCollision(pCol, eCol, true);
                }
            }
        }
    }

    private IEnumerator SpawnRoomEnemiesRoutine(List<Vector3> spawnPositions, bool isBoss)
    {
        for (int i = 0; i < spawnPositions.Count; i++)
        {
            Vector3 spawnPos = spawnPositions[i];
            if (enemyPrefab != null)
            {
                GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                newEnemy.SetActive(true);
                currentWaveEnemies.Add(newEnemy);

                IgnorePlayerCollision(newEnemy);

                if (isBoss)
                {
                    var ai = newEnemy.GetComponent<BotWheelAI>();
                    if (ai != null)
                    {
                        ai.SetupBoss(1.85f, 3, 2);
                    }
                }
            }

            yield return new WaitForSeconds(0.25f);
        }

        aliveEnemies = currentWaveEnemies.Count;
        isSpawningWave = false;

        // Nếu tất cả quái đã bị hạ gục ngay trong lúc đang spawn
        if (aliveEnemies <= 0 && waveInProgress)
        {
            CompleteCurrentWave();
        }
    }

    private void HandleBloodMonsterDied(BloodMonsterAI enemy)
    {
        HandleAnyEnemyDied(enemy != null ? enemy.gameObject : null);
    }

    private void HandleEnemyDied(BotWheelAI enemy)
    {
        HandleAnyEnemyDied(enemy != null ? enemy.gameObject : null);
    }

    private void HandleAnyEnemyDied(GameObject enemyObj = null)
    {
        if (enemyObj != null)
        {
            // BẢO VỆ CHẶT CHẼ: Nếu quái vật chết KHÔNG thuộc đợt/phòng hiện tại (vd quái sót từ phòng cũ), bỏ qua không trừ điểm!
            if (currentWaveEnemies.Count > 0 && !currentWaveEnemies.Contains(enemyObj))
            {
                Debug.Log($"<color=yellow>[SPAWNER]</color> Bỏ qua quái chết ngoại lai (không thuộc phòng hiện tại): {enemyObj.name}");
                return;
            }
            currentWaveEnemies.Remove(enemyObj);
            aliveEnemies = currentWaveEnemies.Count;
        }
        else
        {
            aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
        }

        Debug.Log($"<color=green>[SPAWNER]</color> Quái vật bị hạ gục! Còn lại trong phòng: {aliveEnemies} (Đang spawn: {isSpawningWave})");

        if (!isSpawningWave && aliveEnemies <= 0 && waveInProgress)
        {
            CompleteCurrentWave();
        }
    }

    private void CompleteCurrentWave()
    {
        currentWaveEnemies.Clear();
        aliveEnemies = 0;
        waveInProgress = false;

        if (currentRoomClearedCallback != null)
        {
            var cb = currentRoomClearedCallback;
            currentRoomClearedCallback = null;
            cb.Invoke();
        }
        else
        {
            StartCoroutine(WaveCompletedRoutine());
        }
    }

    private IEnumerator WaveCompletedRoutine()
    {
        if (currentWave < totalWaves)
        {
            OnWaveMessage?.Invoke(LocalizationManager.T($"HOÀN THÀNH ĐỢT {currentWave} / {totalWaves}!\n+8 Mảnh Linh Hồn. Chuẩn bị cho đợt tiếp theo...", $"WAVE {currentWave} / {totalWaves} CLEARED!\n+8 Shards. Prepare for next wave..."));
            AethelShard.SpawnShards(transform.position, 8);
            yield return new WaitForSeconds(timeBetweenWaves);
            StartNextWave();
        }
        else
        {
            allWavesCleared = true;
            Debug.Log("<color=yellow>[VICTORY]</color> ALL WAVES CLEARED!");
            OnWaveMessage?.Invoke(LocalizationManager.T("CHIẾN THẮNG!\nTÀN TÍCH ĐÃ ĐƯỢC THANH TẨY!", "VICTORY!\nRUINS PURIFIED!"));
            OnAllWavesCleared?.Invoke();

            AethelShard.SpawnShards(transform.position, 25);
            StartCoroutine(SpawnVictoryPortalRoutine());
        }
    }

    private IEnumerator SpawnVictoryPortalRoutine()
    {
        while (ShadowDoppelgangerBoss.ActiveBoss != null && !ShadowDoppelgangerBoss.ActiveBoss.IsDead)
        {
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1.8f);

        Vector3 portalPos = transform.position;
        var existingPortal = Object.FindAnyObjectByType<RuinPortal>(FindObjectsInactive.Include);
        if (existingPortal != null)
        {
            existingPortal.gameObject.SetActive(true);
            existingPortal.ActivatePortal();
            portalPos = existingPortal.transform.position;
        }
        else
        {
            RuinPortal.SpawnPortal(portalPos);
        }

        // Xuất hiện Thương Nhân Tàn Tích (Wandering Merchant) bên cạnh Cổng Không Gian
        WanderingMerchant.SpawnMerchant(portalPos + new Vector3(-3.0f, -0.4f, 0f));
    }

    private string GetMap2WaveTitle(int wave)
    {
        switch (wave)
        {
            case 1: return LocalizationManager.T("HUYẾT THÚ XUẤT HIỆN!", "BLOOD MONSTERS ARRIVE!");
            case 2: return LocalizationManager.T("BẦY HUYẾT THÚ TĂNG CƯỜNG!", "REINFORCED BLOOD PACK!");
            case 3: return LocalizationManager.T("QUÁI GAI ĐỘC THỨC TỈNH!", "VALLEY THORN SPITTERS AWAKEN!");
            case 4: return LocalizationManager.T("HOÀNG THÚ CẬN CHIẾN XUẤT KÍCH!", "VALLEY BEASTS CHARGE!");
            case 5: return LocalizationManager.T("ĐỢT QUÁI HỖN HỢP BÙNG NỔ!", "MIXED SWARM STRIKES!");
            case 6: return LocalizationManager.T("MƯA GAI ĐỘC & BẦY HUYẾT THÚ!", "THORN VOLLEY & BLOOD HORDE!");
            case 7: return LocalizationManager.T("BẦY HOÀNG THÚ CUỒNG LOẠN!", "FRENZIED VALLEY BEAST PACK!");
            case 8: return LocalizationManager.T("BINH ĐOÀN THUNG LŨNG HUNG TỢN!", "ENRAGED VALLEY LEGION!");
            case 9: return LocalizationManager.T("ĐỢT TẬP KÍCH CUỐI TRƯỚC HANG TRÙM!", "FINAL AMBUSH BEFORE THE LAIR!");
            default: return LocalizationManager.T("QUÁI VẬT TẤN CÔNG!", "MONSTERS ATTACK!");
        }
    }

    private void StartNextWave()
    {
        currentWave++;
        waveInProgress = true;
        isSpawningWave = true;

        bool isMap2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Map2") ||
                      UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Valley");

        string wavePrefix = LocalizationManager.Get("HUD_WAVE");
        string title = $"{wavePrefix} {currentWave} / {totalWaves}";
        if (currentWave == totalWaves)
        {
            title = isMap2 
                ? LocalizationManager.T($"CẢNH BÁO: ĐẠI TRÙM GOR'MOK! ({wavePrefix} {currentWave} / {totalWaves})", $"BOSS INCOMING: GOR'MOK! ({wavePrefix} {currentWave} / {totalWaves})") 
                : LocalizationManager.T($"CẢNH BÁO: TRÙM TITAN WHEEL! ({wavePrefix} {currentWave} / {totalWaves})", $"BOSS INCOMING: TITAN WHEEL! ({wavePrefix} {currentWave} / {totalWaves})");
        }
        else if (isMap2)
        {
            title = $"{wavePrefix} {currentWave} / {totalWaves}: " + GetMap2WaveTitle(currentWave);
        }

        OnWaveStarted?.Invoke(currentWave, totalWaves, title);
        StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private IEnumerator SpawnWaveRoutine(int wave)
    {
        bool isMap2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Map2") ||
                      UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Valley");

        bool isBossWave = (wave == totalWaves);
        currentWaveEnemies.Clear();

        if (isMap2)
        {
            if (isBossWave)
            {
                aliveEnemies = 1;
                Vector3 spawnPos = GetSpawnPosition(0);
                var bm = BloodMonsterAI.Spawn(spawnPos, isBoss: true, index: 1);
                if (bm != null) currentWaveEnemies.Add(bm.gameObject);
            }
            else
            {
                int bloodCount = Mathf.Clamp(2 + (wave / 2), 3, 7);
                int spitterCount = (wave >= 3) ? (wave >= 6 ? 2 : 1) : 0;
                int beastCount = (wave >= 4) ? (wave >= 7 ? 2 : 1) : 0;
                int totalEnemies = bloodCount + spitterCount + beastCount;
                aliveEnemies = totalEnemies;

                int spawnIdx = 0;
                // 1. Spawn Blood Monsters
                for (int i = 0; i < bloodCount; i++)
                {
                    Vector3 p = GetSpawnPosition(spawnIdx++);
                    var bm = BloodMonsterAI.Spawn(p, isBoss: false, index: i + 1);
                    if (bm != null) currentWaveEnemies.Add(bm.gameObject);
                    yield return new WaitForSeconds(0.25f);
                }

                // 2. Spawn Valley Spitters
                for (int i = 0; i < spitterCount; i++)
                {
                    Vector3 p = GetSpawnPosition(spawnIdx++);
                    var sp = ValleySpitterAI.Spawn(p, i + 1);
                    if (sp != null) currentWaveEnemies.Add(sp.gameObject);
                    yield return new WaitForSeconds(0.25f);
                }

                // 3. Spawn Valley Beasts
                for (int i = 0; i < beastCount; i++)
                {
                    Vector3 p = GetSpawnPosition(spawnIdx++);
                    var vb = ValleyBeastAI.Spawn(p, i + 1);
                    if (vb != null) currentWaveEnemies.Add(vb.gameObject);
                    yield return new WaitForSeconds(0.25f);
                }
            }
        }
        else
        {
            // Map 1 hoặc Map khác
            int countToSpawn = isBossWave ? 1 : Mathf.Clamp(2 + wave, 3, 9);
            aliveEnemies = countToSpawn;

            for (int i = 0; i < countToSpawn; i++)
            {
                Vector3 spawnPos = GetSpawnPosition(i);
                if (enemyPrefab != null)
                {
                    GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                    newEnemy.SetActive(true);
                    currentWaveEnemies.Add(newEnemy);

                    IgnorePlayerCollision(newEnemy);

                    if (isBossWave)
                    {
                        var ai = newEnemy.GetComponent<BotWheelAI>();
                        if (ai != null) ai.SetupBoss(1.85f, 3, 2);
                    }
                }
                yield return new WaitForSeconds(0.25f);
            }
        }

        aliveEnemies = currentWaveEnemies.Count;
        isSpawningWave = false;

        if (aliveEnemies <= 0 && waveInProgress)
        {
            CompleteCurrentWave();
        }
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return spawnPoints[index % spawnPoints.Length].position;
        }

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(3f, spawnRadius);
        return transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);
    }
}

