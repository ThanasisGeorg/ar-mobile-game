using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
  public GameObject enemyPrefab;
  public Transform spawnPoint;

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
    GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

    GameObject goal = GameObject.Find("Target");

    enemy.GetComponent<EnemyMovement>().target = goal.transform;
  }
}