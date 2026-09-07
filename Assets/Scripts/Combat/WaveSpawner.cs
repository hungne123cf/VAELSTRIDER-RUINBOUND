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

    private int currentWave = 0;
    private int aliveEnemies = 0;
    private bool waveInProgress = false;
    private bool allWavesCleared = false;

    // Events
    public static event System.Action<int, int, string> OnWaveStarted;
    public static event System.Action<string> OnWaveMessage;
    public static event System.Action OnAllWavesCleared;

    void Awake()
    {
        // Auto-find template enemy in Scene if enemyPrefab not manually assigned
        if (enemyPrefab == null)
        {
            var bot = FindAnyObjectByType<BotWheelAI>();
            if (bot != null)
            {
                enemyPrefab = bot.gameObject;
                // Temporarily deactivate template in scene so spawner manages spawns cleanly
                bot.gameObject.SetActive(false);
            }
        }
    }

    void OnEnable()
    {
        BotWheelAI.OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        BotWheelAI.OnEnemyDied -= HandleEnemyDied;
    }

    void Start()
    {
        StartCoroutine(StartFirstWaveRoutine());
    }

    private IEnumerator StartFirstWaveRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        StartNextWave();
    }

    private void HandleEnemyDied(BotWheelAI enemy)
    {
        aliveEnemies--;
        Debug.Log($"<color=green>[SPAWNER]</color> Enemy defeated! Remaining in wave: {aliveEnemies}");

        if (aliveEnemies <= 0 && waveInProgress)
        {
            waveInProgress = false;
            StartCoroutine(WaveCompletedRoutine());
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

            // Reward victory with a burst of Aethel Shards
            AethelShard.SpawnShards(transform.position, 15);
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
        else if (isBossWave) countToSpawn = 1; // 1 Giant Titan Mini-Boss

        aliveEnemies = countToSpawn;

        for (int i = 0; i < countToSpawn; i++)
        {
            Vector3 spawnPos = GetSpawnPosition(i);
            
            if (enemyPrefab != null)
            {
                GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                newEnemy.SetActive(true);

                // If Boss Wave -> buff into Titan Boss
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
