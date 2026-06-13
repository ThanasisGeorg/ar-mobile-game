using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
  public GameObject enemyPrefab;
  public Transform spawnPoint;
  public Transform target;

  public int enemiesPerWave = 5;
  public float spawnInterval = 1f;
  public float timeBetweenWaves = 5f;

  private int waveIndex = 0;

  void Start()
  {
    StartCoroutine(SpawnWaves());
  }

  IEnumerator SpawnWaves()
  {
    while (true)
    {
      waveIndex++;

      Debug.Log("Wave " + waveIndex);

      for (int i = 0; i < enemiesPerWave; i++)
      {
        SpawnEnemy();
        yield return new WaitForSeconds(spawnInterval);
      }

      yield return new WaitForSeconds(timeBetweenWaves);

      enemiesPerWave++;
    }
  }

  void SpawnEnemy()
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

    if (target == null)
    {
      Debug.LogError("WaveManager could not find a target for enemies.");
      return;
    }

    GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();

    if (enemyMovement != null)
    {
      enemyMovement.target = target;
    }
    else
    {
      Debug.LogError("Enemy prefab is missing the EnemyMovement component.");
    }
  }
}