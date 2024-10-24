using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int initialEnemyCount = 5;
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private int maxEnemies = 20;

    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private DungeonGenerator dungeonGenerator;

    public void InitializeWithDungeonGenerator(DungeonGenerator generator)
    {
        dungeonGenerator = generator;
    }


    public void Initialize(DungeonGenerator generator)
    {
        dungeonGenerator = generator;
    }
    private float lastSpawnTime;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetDungeonGenerator(DungeonGenerator generator)
    {
        dungeonGenerator = generator;
    }
    private void Start()
    {
        if (dungeonGenerator == null)
        {
            Debug.LogError("EnemyManager: DungeonGenerator not found in the scene!");
            return;
        }

        lastSpawnTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - lastSpawnTime >= spawnInterval)
        {
            TrySpawnEnemy();
            lastSpawnTime = Time.time;
        }
    }

    public void InitializeEnemies()
    {
        ClearAllEnemies();
        SpawnInitialEnemies();
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < initialEnemyCount; i++)
        {
            TrySpawnEnemy();
        }
    }


    private Vector2 GetRandomSpawnPosition()
    {
        List<Vector2> floorTiles = dungeonGenerator.GetFloorTilePositions();
        floorTiles = floorTiles.Where(pos => !IsPositionOccupied(pos)).ToList();

        if (floorTiles.Count > 0)
        {
            int randomIndex = Random.Range(0, floorTiles.Count);
            return floorTiles[randomIndex];
        }

        return Vector2.negativeInfinity;
    }

    private bool IsPositionOccupied(Vector2 position)
    {
        return activeEnemies.Any(enemy => Vector2.Distance(enemy.transform.position, position) < 0.5f);
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        activeEnemies.Remove(enemy);
        Destroy(enemy.gameObject);
    }


    public void ClearAllEnemies()
    {
        foreach (EnemyController enemy in activeEnemies)
        {
            Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
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

    public bool AreEnemiesRemaining()
    {
        return activeEnemies.Count > 0;
    }
    private List<Vector2> GetFloorTilePositions()
    {
        return dungeonGenerator != null ? dungeonGenerator.GetFloorTilePositions() : new List<Vector2>();
    }
    public void SpawnEnemiesForFloor(int floorNumber)
    {
        ClearAllEnemies(); // Ensure no old enemies remain

        int numberOfEnemies = initialEnemyCount + (floorNumber - 1) * 2;
        numberOfEnemies = Mathf.Min(numberOfEnemies, maxEnemies);

        for (int i = 0; i < numberOfEnemies; i++)
        {
            TrySpawnEnemy();
        }
    }

    private void TrySpawnEnemy()
    {
        if (activeEnemies.Count >= maxEnemies)
        {
            return;
        }

        Vector2 spawnPosition = GetRandomSpawnPosition();
        if (spawnPosition != Vector2.negativeInfinity)
        {
            SpawnEnemy(spawnPosition);
        }
    }

    private void SpawnEnemy(Vector2 position)
    {
        GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.identity);
        EnemyController enemyController = enemyObject.GetComponent<EnemyController>();

        if (enemyController != null)
        {
            activeEnemies.Add(enemyController);
            Debug.Log($"Spawned enemy at {position}");
        }
    }

}