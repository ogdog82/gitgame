using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Game state
    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentGameState { get; private set; }

    // Player data
    public PlayerController Player { get; private set; }
    public int CurrentFloor { get; private set; }
    public int Gold { get; private set; }

    // Managers
    public TurnManager TurnManager { get; private set; }
    public DungeonGenerator DungeonGenerator { get; private set; }
    public EnemyManager EnemyManager { get; private set; }

    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnGoldChanged;
    public event Action<int> OnFloorChanged;

    public Camera mainCamera;
    public float minCameraSize = 5f;
    public float maxCameraSize = 30f;
    public EnemyManager enemyManager;


    private List<EnemyController> enemies = new List<EnemyController>();


    public void UpdateCameraSize(float revealedPercentage)
    {
        float minSize = 5f;
        float maxSize = 15f;
        Camera.main.orthographicSize = Mathf.Lerp(minSize, maxSize, revealedPercentage);
    }

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
            return;
        }

        // Try to get the DungeonGenerator from this GameObject
        DungeonGenerator = GetComponent<DungeonGenerator>();

        // If not found on this GameObject, try to find it in the scene
        if (DungeonGenerator == null)
        {
            DungeonGenerator = FindObjectOfType<DungeonGenerator>();
        }

        // If still not found, log an error
        if (DungeonGenerator == null)
        {
            Debug.LogError("DungeonGenerator component not found in the scene!");
        }

        // Repeat similar process for EnemyManager and TurnManager
        enemyManager = GetComponent<EnemyManager>() ?? FindObjectOfType<EnemyManager>();
        TurnManager = GetComponent<TurnManager>() ?? FindObjectOfType<TurnManager>();

        if (enemyManager == null)
        {
            Debug.LogError("EnemyManager component not found in the scene!");
        }

        if (TurnManager == null)
        {
            Debug.LogError("TurnManager component not found in the scene!");
        }

        if (DungeonGenerator != null && enemyManager != null)
        {
            DungeonGenerator.SetEnemyManager(enemyManager);
            enemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
            Debug.Log("DungeonGenerator and EnemyManager initialized successfully.");
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        // Consider removing this line if it's redundant
        // enemyManager = FindObjectOfType<EnemyManager>();

        StartNewGame();
    }
    public void RemoveEnemy(EnemyController enemy)
    {
        enemies.Remove(enemy);
    }
    public void StartNewGame()
    {
        Player = FindObjectOfType<PlayerController>();
        enemyManager = FindObjectOfType<EnemyManager>();

        if (Player == null || enemyManager == null)
        {
            Debug.LogError("PlayerController or EnemyManager not found in the scene!");
            return;
        }

        enemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
        DungeonGenerator.GenerateNewDungeon();
        Player.ResetPosition();
        enemyManager.SpawnEnemiesForFloor(CurrentFloor);

        List<IActable> combatants = new List<IActable> { Player };

        Debug.Log($"Combatants count: {combatants.Count}");

        TurnManager.StartCombat(combatants);

        ChangeGameState(GameState.Playing);
    }
    public GameObject damageNumberPrefab;

    private List<IActable> GetCombatants()
    {
        List<IActable> combatants = new List<IActable> { Player };
        return combatants;
    }

    public void ShowDamageNumber(Vector3 position, int damage)
    {
        if (damageNumberPrefab != null)
        {
            GameObject damageObj = Instantiate(damageNumberPrefab, position, Quaternion.identity);
            DamageNumber damageNumber = damageObj.GetComponent<DamageNumber>();
            if (damageNumber != null)
            {
                damageNumber.SetDamage(damage);
            }
        }
        else
        {
            Debug.LogError("Damage Number Prefab is not assigned in GameManager!");
        }
    }

    public void ChangeGameState(GameState newState)
    {
        if (CurrentGameState != newState)
        {
            CurrentGameState = newState;
            OnGameStateChanged?.Invoke(CurrentGameState);

            switch (CurrentGameState)
            {
                case GameState.MainMenu:
                    Time.timeScale = 0;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1;
                    break;
                case GameState.Paused:
                    Time.timeScale = 0;
                    break;
                case GameState.GameOver:
                    Time.timeScale = 0;
                    HandleGameOver();
                    break;
            }
        }
    }

    public void PauseGame()
    {
        if (CurrentGameState == GameState.Playing)
        {
            ChangeGameState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            ChangeGameState(GameState.Playing);
        }
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public bool SpendGold(int amount)
    {
        if (Gold >= amount)
        {
            Gold -= amount;
            OnGoldChanged?.Invoke(Gold);
            return true;
        }
        return false;
    }

    public void GenerateNewFloor()
    {
        CurrentFloor++;
        OnFloorChanged?.Invoke(CurrentFloor);
        DungeonGenerator.GenerateNewDungeon();
        Player.ResetPosition();
    }

    private void HandleGameOver()
    {
        Debug.Log("Game Over!");
        // Implement game over logic here
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    private void OnApplicationQuit()
    {
        // Save game data if needed
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Save game data when the application is paused (e.g., phone call)
        }
    }
}