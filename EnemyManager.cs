using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EnemyManager : MonoBehaviour
{
    private DungeonGenerator dungeonGenerator;
    private List<EnemyController> activeEnemies = new List<EnemyController>();

    public void InitializeWithDungeonGenerator(DungeonGenerator generator)
    {
        dungeonGenerator = generator;
    }

    public void InitializeEnemies()
    {
        activeEnemies = FindObjectsOfType<EnemyController>().ToList();
    }

    public EnemyController GetNearestEnemy(Vector2 position)
    {
        if (activeEnemies.Count == 0)
        {
            return null;
        }

        return activeEnemies
            .OrderBy(enemy => Vector2.Distance(enemy.transform.position, position))
            .FirstOrDefault();
    }

    public void SpawnEnemiesForFloor(int floor)
    {
        ClearExistingEnemies();
        int enemyCount = Mathf.Clamp(floor * 2, 5, 20); // Example logic for enemy count based on floor

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 spawnPosition = GetRandomSpawnPosition();
            SpawnEnemy(spawnPosition);
        }
    }

    private void ClearExistingEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    private Vector2 GetRandomSpawnPosition()
    {
        int width = dungeonGenerator.Width;
        int height = dungeonGenerator.Height;

        while (true)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            if (dungeonGenerator.IsWalkableTile(x, y))
            {
                return new Vector2(x, y);
            }
        }
    }

    private void SpawnEnemy(Vector2 position)
    {
        GameObject enemyPrefab = Resources.Load<GameObject>("EnemyPrefab"); // Assuming you have an enemy prefab in Resources folder
        if (enemyPrefab != null)
        {
            GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.identity);
            EnemyController enemyController = enemyObject.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                activeEnemies.Add(enemyController);
            }
        }
        else
        {
            Debug.LogError("EnemyPrefab not found in Resources folder!");
        }
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
}