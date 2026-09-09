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
    private bool waveInProgress = false;
    private bool allWavesCleared = false;
    private System.Action currentRoomClearedCallback;

    // Events
    public static event System.Action<int, int, string> OnWaveStarted;
    public static event System.Action<string> OnWaveMessage;
    public static event System.Action OnAllWavesCleared;

    void Awake()
    {
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
    }

    void OnDisable()
    {
        BotWheelAI.OnEnemyDied -= HandleEnemyDied;
        BloodMonsterAI.OnEnemyDied -= HandleBloodMonsterDied;
    }

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

    public void SpawnRoomEnemies(List<Vector3> spawnPositions, bool isBoss, System.Action onCleared)
    {
        currentRoomClearedCallback = onCleared;
        aliveEnemies = spawnPositions.Count;
        waveInProgress = true;

        currentWave++;
        string title = isBoss ? "BOSS INCOMING: TITAN WHEEL!" : $"WAVE {currentWave} / {totalWaves}";
        OnWaveStarted?.Invoke(currentWave, totalWaves, title);

        StartCoroutine(SpawnRoomEnemiesRoutine(spawnPositions, isBoss));
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

                // Bo qua va cham vat ly day nhau giua nguoi choi va quai
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

                if (isBoss)
                {
                    var ai = newEnemy.GetComponent<BotWheelAI>();
                    if (ai != null)
                    {
                        ai.SetupBoss(1.85f, 3, 2);
                    }
                }
            }

            yield return new WaitForSeconds(0.35f);
        }
    }

    private void HandleBloodMonsterDied(BloodMonsterAI enemy)
    {
        HandleAnyEnemyDied();
    }

    private void HandleEnemyDied(BotWheelAI enemy)
    {
        HandleAnyEnemyDied();
    }

    private void HandleAnyEnemyDied()
    {
        aliveEnemies--;
        Debug.Log($"<color=green>[SPAWNER]</color> Quái vật bị hạ gục! Còn lại trong phòng: {aliveEnemies}");

        if (aliveEnemies <= 0 && waveInProgress)
        {
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
    }

    private IEnumerator WaveCompletedRoutine()
    {
        if (currentWave < totalWaves)
        {
            OnWaveMessage?.Invoke($"WAVE {currentWave} CLEARED!\nPrepare for next wave...");
            yield return new WaitForSeconds(timeBetweenWaves);
            StartNextWave();
        }
        else
        {
            allWavesCleared = true;
            Debug.Log("<color=yellow>[VICTORY]</color> ALL WAVES CLEARED!");
            OnWaveMessage?.Invoke("VICTORY!\nRUINS PURIFIED!");
            OnAllWavesCleared?.Invoke();

            AethelShard.SpawnShards(transform.position, 15);
            StartCoroutine(SpawnVictoryPortalRoutine());
        }
    }

    private IEnumerator SpawnVictoryPortalRoutine()
    {
        yield return new WaitForSeconds(1.8f);

        var existingPortal = Object.FindAnyObjectByType<RuinPortal>(FindObjectsInactive.Include);
        if (existingPortal != null)
        {
            existingPortal.gameObject.SetActive(true);
            existingPortal.ActivatePortal();
        }
        else
        {
            RuinPortal.SpawnPortal(transform.position);
        }
    }

    private void StartNextWave()
    {
        currentWave++;
        waveInProgress = true;

        string title = $"WAVE {currentWave} / {totalWaves}";
        if (currentWave == totalWaves)
        {
            title = "BOSS INCOMING: TITAN WHEEL!";
        }

        OnWaveStarted?.Invoke(currentWave, totalWaves, title);
        StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private IEnumerator SpawnWaveRoutine(int wave)
    {
        int countToSpawn = 0;
        bool isBossWave = (wave == totalWaves);

        if (wave == 1) countToSpawn = 2;
        else if (wave == 2) countToSpawn = 4;
        else if (isBossWave) countToSpawn = 1;

        aliveEnemies = countToSpawn;

        for (int i = 0; i < countToSpawn; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i);
            
            if (enemyPrefab != null)
            {
                GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                newEnemy.SetActive(true);

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

                if (isBossWave)
                {
                    var ai = newEnemy.GetComponent<BotWheelAI>();
                    if (ai != null)
                    {
                        ai.SetupBoss(1.85f, 3, 2);
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);
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
