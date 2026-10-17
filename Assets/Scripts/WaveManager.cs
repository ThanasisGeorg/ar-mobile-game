using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
  public static WaveManager Instance;
  public static readonly List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>();

  public GameObject enemyPrefab;
  public Transform spawnPoint;
  public Transform target;

  [HideInInspector] public Vector3[] waypoints;

  public int totalWaves = 10;
  public int enemiesPerWave = 5;
  public int extraEnemiesPerWave = 1;
  public float spawnInterval = 1f;
  public float firstWaveDelay = 8f;
  public float timeBetweenWaves = 5f;

  [Tooltip("Extra enemy health per wave (0.2 = +20% per wave).")]
  public float healthGrowthPerWave = 0.2f;

  [Tooltip("Extra enemy speed per wave (0.03 = +3% per wave).")]
  public float speedGrowthPerWave = 0.03f;

  private int waveIndex = 0;
  private bool started;
  private bool finishedSpawning;

  public int CurrentWave => waveIndex;
  public int TotalWaves => totalWaves;
  public int UpcomingWave => Mathf.Min(waveIndex + 1, totalWaves);

  // Seconds until the next wave starts, or a negative value while a wave is spawning.
  public float NextWaveIn { get; private set; } = -1f;
  public int EnemiesSpawned { get; private set; }

  public event Action<int> WaveStarted;

  private void Awake()
  {
    Instance = this;
  }

  private void OnDestroy()
  {
    if (Instance == this)
      Instance = null;
  }

  public void Begin()
  {
    if (started)
      return;

    started = true;
    StartCoroutine(SpawnWaves());
  }

  void Update()
  {
    if (!finishedSpawning || ActiveEnemies.Count > 0)
      return;

    GameManager gm = GameManager.Instance;

    if (gm != null && gm.State == GameState.Playing)
    {
      gm.Victory();
    }
  }

  IEnumerator SpawnWaves()
  {
    for (int wave = 1; wave <= totalWaves; wave++)
    {
      float countdown = wave == 1 ? firstWaveDelay : timeBetweenWaves;

      while (countdown > 0f)
      {
        NextWaveIn = countdown;
        countdown -= Time.deltaTime;
        yield return null;
      }

      NextWaveIn = -1f;
      waveIndex = wave;

      Debug.Log("Wave " + waveIndex);
      WaveStarted?.Invoke(waveIndex);

      int count = enemiesPerWave + extraEnemiesPerWave * (wave - 1);

      for (int i = 0; i < count; i++)
      {
        SpawnEnemy(wave);
        yield return new WaitForSeconds(spawnInterval);
      }
    }

    finishedSpawning = true;
  }

  void SpawnEnemy(int wave)
  {
    if (enemyPrefab == null || spawnPoint == null)
    {
      Debug.LogError("WaveManager is missing an enemy prefab or spawn point.");
      return;
    }

    if (target == null)
    {
      GameObject goal = GameObject.Find("Target");

      if (goal != null)
      {
        target = goal.transform;
      }
    }

    if (target == null && (waypoints == null || waypoints.Length == 0))
    {
      Debug.LogError("WaveManager could not find a target for enemies.");
      return;
    }

    GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    EnemiesSpawned++;

    EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();

    if (enemyMovement != null)
    {
      enemyMovement.target = target;
      enemyMovement.waypoints = waypoints;
      enemyMovement.speed *= 1f + speedGrowthPerWave * (wave - 1);
    }
    else
    {
      Debug.LogError("Enemy prefab is missing the EnemyMovement component.");
    }

    EnemyHealth health = enemy.GetComponent<EnemyHealth>();

    if (health != null)
    {
      health.Init(Mathf.RoundToInt(health.maxHealth * (1f + healthGrowthPerWave * (wave - 1))));
    }
  }
}
