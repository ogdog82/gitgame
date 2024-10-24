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
    public GameObject damageNumberPrefab;

    private List<EnemyController> enemies = new List<EnemyController>();

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
        EnemyManager = GetComponent<EnemyManager>() ?? FindObjectOfType<EnemyManager>();
        TurnManager = GetComponent<TurnManager>() ?? FindObjectOfType<TurnManager>();

        if (EnemyManager == null)
        {
            Debug.LogError("EnemyManager component not found in the scene!");
        }

        if (TurnManager == null)
        {
            Debug.LogError("TurnManager component not found in the scene!");
        }

        if (DungeonGenerator != null && EnemyManager != null)
        {
            DungeonGenerator.SetEnemyManager(EnemyManager);
            EnemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
            Debug.Log("DungeonGenerator and EnemyManager initialized successfully.");
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        StartNewGame();
    }

    public void StartNewGame()
    {
        Player = FindObjectOfType<PlayerController>();
        EnemyManager = FindObjectOfType<EnemyManager>();

        if (Player == null || EnemyManager == null)
        {
            Debug.LogError("PlayerController or EnemyManager not found in the scene!");
            return;
        }

        EnemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
        DungeonGenerator.GenerateNewDungeon();
        Player.ResetPosition();
        EnemyManager.SpawnEnemiesForFloor(CurrentFloor);

        List<IActable> combatants = new List<IActable> { Player };

        Debug.Log($"Combatants count: {combatants.Count}");

        TurnManager.StartCombat(combatants);

        ChangeGameState(GameState.Playing);
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
        Application.Quit();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quitting...");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }

    public void ShowDamageNumber(Vector3 position, int damage)
    {
        if (damageNumberPrefab != null)
        {
            GameObject damageNumberObject = Instantiate(damageNumberPrefab, position, Quaternion.identity);
            DamageNumber damageNumber = damageNumberObject.GetComponent<DamageNumber>();
            if (damageNumber != null)
            {
                damageNumber.SetDamage(damage);
            }
        }
        else
        {
            Debug.LogError("DamageNumberPrefab not assigned in GameManager!");
        }
    }

    public void UpdateCameraSize(float revealedPercentage)
    {
        float newSize = Mathf.Lerp(minCameraSize, maxCameraSize, revealedPercentage);
        mainCamera.orthographicSize = newSize;
    }

    private List<IActable> GetCombatants()
    {
        List<IActable> combatants = new List<IActable> { Player };
        combatants.AddRange(enemies);
        return combatants;
    }

    public void InitializeGame()
    {
        Player = FindObjectOfType<PlayerController>();
        EnemyManager = FindObjectOfType<EnemyManager>();
        DungeonGenerator = FindObjectOfType<DungeonGenerator>();
        TurnManager = FindObjectOfType<TurnManager>();

        if (Player == null || EnemyManager == null || DungeonGenerator == null || TurnManager == null)
        {
            Debug.LogError("One or more essential components not found in the scene!");
            return;
        }

        DungeonGenerator.SetEnemyManager(EnemyManager);
        EnemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
        TurnManager.Initialize(Player, EnemyManager);

        Debug.Log("Game initialized successfully.");
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("Gold", Gold);
        PlayerPrefs.SetInt("CurrentFloor", CurrentFloor);
        PlayerPrefs.Save();
        Debug.Log("Game saved successfully.");
    }

    public void LoadGame()
    {
        Gold = PlayerPrefs.GetInt("Gold", 0);
        CurrentFloor = PlayerPrefs.GetInt("CurrentFloor", 1);
        Debug.Log("Game loaded successfully.");
    }

    public void ResetGame()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Game reset successfully.");
    }

    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }

    public void SetResolution(int width, int height, bool fullScreen)
    {
        Screen.SetResolution(width, height, fullScreen);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void MuteAudio(bool mute)
    {
        AudioListener.pause = mute;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetGraphicsSettings(bool enableShadows, bool enablePostProcessing)
    {
        QualitySettings.shadows = enableShadows ? ShadowQuality.All : ShadowQuality.Disable;
        // Assuming you have a post-processing manager or similar
        PostProcessingManager.Instance.SetPostProcessing(enablePostProcessing);
    }

    public void SetControlScheme(ControlScheme scheme)
    {
        // Assuming you have a control manager or similar
        ControlManager.Instance.SetControlScheme(scheme);
    }

    public void SetLanguage(string languageCode)
    {
        // Assuming you have a localization manager or similar
        LocalizationManager.Instance.SetLanguage(languageCode);
    }

    public void ShowSettingsMenu()
    {
        // Assuming you have a UI manager or similar
        UIManager.Instance.ShowSettingsMenu();
    }

    public void HideSettingsMenu()
    {
        UIManager.Instance.HideSettingsMenu();
    }

    public void ShowMainMenu()
    {
        UIManager.Instance.ShowMainMenu();
    }

    public void HideMainMenu()
    {
        UIManager.Instance.HideMainMenu();
    }

    public void ShowPauseMenu()
    {
        UIManager.Instance.ShowPauseMenu();
    }

    public void HidePauseMenu()
    {
        UIManager.Instance.HidePauseMenu();
    }

    public void ShowGameOverMenu()
    {
        UIManager.Instance.ShowGameOverMenu();
    }

    public void HideGameOverMenu()
    {
        UIManager.Instance.HideGameOverMenu();
    }

    public void ShowHUD()
    {
        UIManager.Instance.ShowHUD();
    }

    public void HideHUD()
    {
        UIManager.Instance.HideHUD();
    }

    public void ShowInventory()
    {
        UIManager.Instance.ShowInventory();
    }

    public void HideInventory()
    {
        UIManager.Instance.HideInventory();
    }

    public void ShowMap()
    {
        UIManager.Instance.ShowMap();
    }

    public void HideMap()
    {
        UIManager.Instance.HideMap();
    }

    public void ShowQuestLog()
    {
        UIManager.Instance.ShowQuestLog();
    }

    public void HideQuestLog()
    {
        UIManager.Instance.HideQuestLog();
    }

    public void ShowCharacterStats()
    {
        UIManager.Instance.ShowCharacterStats();
    }

    public void HideCharacterStats()
    {
        UIManager.Instance.HideCharacterStats();
    }

    public void ShowSkillTree()
    {
        UIManager.Instance.ShowSkillTree();
    }

    public void HideSkillTree()
    {
        UIManager.Instance.HideSkillTree();
    }

    public void ShowCraftingMenu()
    {
        UIManager.Instance.ShowCraftingMenu();
    }

    public void HideCraftingMenu()
    {
        UIManager.Instance.HideCraftingMenu();
    }

    public void ShowDialogue()
    {
        UIManager.Instance.ShowDialogue();
    }

    public void HideDialogue()
    {
        UIManager.Instance.HideDialogue();
    }

    public void ShowShop()
    {
        UIManager.Instance.ShowShop();
    }

    public void HideShop()
    {
        UIManager.Instance.HideShop();
    }

    public void ShowTutorial()
    {
        UIManager.Instance.ShowTutorial();
    }

    public void HideTutorial()
    {
        UIManager.Instance.HideTutorial();
    }

    public void ShowAchievements()
    {
        UIManager.Instance.ShowAchievements();
    }

    public void HideAchievements()
    {
        UIManager.Instance.HideAchievements();
    }

    public void ShowLeaderboard()
    {
        UIManager.Instance.ShowLeaderboard();
    }

    public void HideLeaderboard()
    {
        UIManager.Instance.HideLeaderboard();
    }

    public void ShowCredits()
    {
        UIManager.Instance.ShowCredits();
    }

    public void HideCredits()
    {
        UIManager.Instance.HideCredits();
    }

    public void ShowLoadingScreen()
    {
        UIManager.Instance.ShowLoadingScreen();
    }

    public void HideLoadingScreen()
    {
        UIManager.Instance.HideLoadingScreen();
    }

    public void ShowNotification(string message)
    {
        UIManager.Instance.ShowNotification(message);
    }

    public void HideNotification()
    {
        UIManager.Instance.HideNotification();
    }

    public void ShowTooltip(string message)
    {
        UIManager.Instance.ShowTooltip(message);
    }

    public void HideTooltip()
    {
        UIManager.Instance.HideTooltip();
    }

    public void ShowContextMenu(Vector3 position, List<string> options)
    {
        UIManager.Instance.ShowContextMenu(position, options);
    }

    public void HideContextMenu()
    {
        UIManager.Instance.HideContextMenu();
    }

    public void ShowConfirmationDialog(string message, Action onConfirm, Action onCancel)
    {
        UIManager.Instance.ShowConfirmationDialog(message, onConfirm, onCancel);
    }

    public void HideConfirmationDialog()
    {
        UIManager.Instance.HideConfirmationDialog();
    }

    public void ShowInputDialog(string message, Action<string> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowInputDialog(message, onSubmit, onCancel);
    }

    public void HideInputDialog()
    {
        UIManager.Instance.HideInputDialog();
    }

    public void ShowFileDialog(string message, Action<string> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowFileDialog(message, onSubmit, onCancel);
    }

    public void HideFileDialog()
    {
        UIManager.Instance.HideFileDialog();
    }

    public void ShowColorPicker(Action<Color> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowColorPicker(onSubmit, onCancel);
    }

    public void HideColorPicker()
    {
        UIManager.Instance.HideColorPicker();
    }

    public void ShowDatePicker(Action<DateTime> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowDatePicker(onSubmit, onCancel);
    }

    public void HideDatePicker()
    {
        UIManager.Instance.HideDatePicker();
    }

    public void ShowTimePicker(Action<TimeSpan> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowTimePicker(onSubmit, onCancel);
    }

    public void HideTimePicker()
    {
        UIManager.Instance.HideTimePicker();
    }

    public void ShowSlider(string message, float minValue, float maxValue, Action<float> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowSlider(message, minValue, maxValue, onSubmit, onCancel);
    }

    public void HideSlider()
    {
        UIManager.Instance.HideSlider();
    }

    public void ShowProgressBar(string message, float progress)
    {
        UIManager.Instance.ShowProgressBar(message, progress);
    }

    public void HideProgressBar()
    {
        UIManager.Instance.HideProgressBar();
    }

    public void ShowLoadingBar(string message, float progress)
    {
        UIManager.Instance.ShowLoadingBar(message, progress);
    }

    public void HideLoadingBar()
    {
        UIManager.Instance.HideLoadingBar();
    }

    public void ShowStatusBar(string message, float progress)
    {
        UIManager.Instance.ShowStatusBar(message, progress);
    }

    public void HideStatusBar()
    {
        UIManager.Instance.HideStatusBar();
    }

    public void ShowHealthBar(float health)
    {
        UIManager.Instance.ShowHealthBar(health);
    }

    public void HideHealthBar()
    {
        UIManager.Instance.HideHealthBar();
    }

    public void ShowManaBar(float mana)
    {
        UIManager.Instance.ShowManaBar(mana);
    }

    public void HideManaBar()
    {
        UIManager.Instance.HideManaBar();
    }

    public void ShowStaminaBar(float stamina)
    {
        UIManager.Instance.ShowStaminaBar(stamina);
    }

    public void HideStaminaBar()
    {
        UIManager.Instance.HideStaminaBar();
    }

    public void ShowExperienceBar(float experience)
    {
        UIManager.Instance.ShowExperienceBar(experience);
    }

    public void HideExperienceBar()
    {
        UIManager.Instance.HideExperienceBar();
    }

    public void ShowLevelUpNotification(int level)
    {
        UIManager.Instance.ShowLevelUpNotification(level);
    }

    public void HideLevelUpNotification()
    {
        UIManager.Instance.HideLevelUpNotification();
    }

    public void ShowQuestNotification(string questName)
    {
        UIManager.Instance.ShowQuestNotification(questName);
    }

    public void HideQuestNotification()
    {
        UIManager.Instance.HideQuestNotification();
    }

    public void ShowAchievementNotification(string achievementName)
    {
        UIManager.Instance.ShowAchievementNotification(achievementName);
    }

    public void HideAchievementNotification()
    {
        UIManager.Instance.HideAchievementNotification();
    }

    public void ShowItemPickupNotification(string itemName)
    {
        UIManager.Instance.ShowItemPickupNotification(itemName);
    }

    public void HideItemPickupNotification()
    {
        UIManager.Instance.HideItemPickupNotification();
    }

    public void ShowSkillUnlockNotification(string skillName)
    {
        UIManager.Instance.ShowSkillUnlockNotification(skillName);
    }

    public void HideSkillUnlockNotification()
    {
        UIManager.Instance.HideSkillUnlockNotification();
    }

    public void ShowCraftingNotification(string itemName)
    {
        UIManager.Instance.ShowCraftingNotification(itemName);
    }

    public void HideCraftingNotification()
    {
        UIManager.Instance.HideCraftingNotification();
    }

    public void ShowDialogueOption(string optionText, Action onSelect)
    {
        UIManager.Instance.ShowDialogueOption(optionText, onSelect);
    }

    public void HideDialogueOption()
    {
        UIManager.Instance.HideDialogueOption();
    }

    public void ShowDialogueResponse(string responseText)
    {
        UIManager.Instance.ShowDialogueResponse(responseText);
    }

    public void HideDialogueResponse()
    {
        UIManager.Instance.HideDialogueResponse();
    }

    public void ShowDialogueChoices(List<string> choices, Action<int> onSelect)
    {
        UIManager.Instance.ShowDialogueChoices(choices, onSelect);
    }

    public void HideDialogueChoices()
    {
        UIManager.Instance.HideDialogueChoices();
    }

    public void ShowDialogueHistory(List<string> history)
    {
        UIManager.Instance.ShowDialogueHistory(history);
    }

    public void HideDialogueHistory()
    {
        UIManager.Instance.HideDialogueHistory();
    }

    public void ShowDialoguePortrait(Sprite portrait)
    {
        UIManager.Instance.ShowDialoguePortrait(portrait);
    }

    public void HideDialoguePortrait()
    {
        UIManager.Instance.HideDialoguePortrait();
    }

    public void ShowDialogueBackground(Sprite background)
    {
        UIManager.Instance.ShowDialogueBackground(background);
    }

    public void HideDialogueBackground()
    {
        UIManager.Instance.HideDialogueBackground();
    }

    public void ShowDialogueName(string name)
    {
        UIManager.Instance.ShowDialogueName(name);
    }

    public void HideDialogueName()
    {
        UIManager.Instance.HideDialogueName();
    }

    public void ShowDialogueText(string text)
    {
        UIManager.Instance.ShowDialogueText(text);
    }

    public void HideDialogueText()
    {
        UIManager.Instance.HideDialogueText();
    }

    public void ShowDialogueContinueButton(Action onClick)
    {
        UIManager.Instance.ShowDialogueContinueButton(onClick);
    }

    public void HideDialogueContinueButton()
    {
        UIManager.Instance.HideDialogueContinueButton();
    }

    public void ShowDialogueSkipButton(Action onusing UnityEngine;
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
    public GameObject damageNumberPrefab;

    private List<EnemyController> enemies = new List<EnemyController>();

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
        EnemyManager = GetComponent<EnemyManager>() ?? FindObjectOfType<EnemyManager>();
        TurnManager = GetComponent<TurnManager>() ?? FindObjectOfType<TurnManager>();

        if (EnemyManager == null)
        {
            Debug.LogError("EnemyManager component not found in the scene!");
        }

        if (TurnManager == null)
        {
            Debug.LogError("TurnManager component not found in the scene!");
        }

        if (DungeonGenerator != null && EnemyManager != null)
        {
            DungeonGenerator.SetEnemyManager(EnemyManager);
            EnemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
            Debug.Log("DungeonGenerator and EnemyManager initialized successfully.");
        }
    }

    private void Start()
    {
        mainCamera = Camera.main;

        StartNewGame();
    }

    public void StartNewGame()
    {
        Player = FindObjectOfType<PlayerController>();
        EnemyManager = FindObjectOfType<EnemyManager>();

        if (Player == null || EnemyManager == null)
        {
            Debug.LogError("PlayerController or EnemyManager not found in the scene!");
            return;
        }

        EnemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
        DungeonGenerator.GenerateNewDungeon();
        Player.ResetPosition();
        EnemyManager.SpawnEnemiesForFloor(CurrentFloor);

        List<IActable> combatants = new List<IActable> { Player };

        Debug.Log($"Combatants count: {combatants.Count}");

        TurnManager.StartCombat(combatants);

        ChangeGameState(GameState.Playing);
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
        Application.Quit();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Application quitting...");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
        }
    }

    public void ShowDamageNumber(Vector3 position, int damage)
    {
        if (damageNumberPrefab != null)
        {
            GameObject damageNumberObject = Instantiate(damageNumberPrefab, position, Quaternion.identity);
            DamageNumber damageNumber = damageNumberObject.GetComponent<DamageNumber>();
            if (damageNumber != null)
            {
                damageNumber.SetDamage(damage);
            }
        }
        else
        {
            Debug.LogError("DamageNumberPrefab not assigned in GameManager!");
        }
    }

    public void UpdateCameraSize(float revealedPercentage)
    {
        float newSize = Mathf.Lerp(minCameraSize, maxCameraSize, revealedPercentage);
        mainCamera.orthographicSize = newSize;
    }

    private List<IActable> GetCombatants()
    {
        List<IActable> combatants = new List<IActable> { Player };
        combatants.AddRange(enemies);
        return combatants;
    }

    public void InitializeGame()
    {
        Player = FindObjectOfType<PlayerController>();
        EnemyManager = FindObjectOfType<EnemyManager>();
        DungeonGenerator = FindObjectOfType<DungeonGenerator>();
        TurnManager = FindObjectOfType<TurnManager>();

        if (Player == null || EnemyManager == null || DungeonGenerator == null || TurnManager == null)
        {
            Debug.LogError("One or more essential components not found in the scene!");
            return;
        }

        DungeonGenerator.SetEnemyManager(EnemyManager);
        EnemyManager.InitializeWithDungeonGenerator(DungeonGenerator);
        TurnManager.Initialize(Player, EnemyManager);

        Debug.Log("Game initialized successfully.");
    }

    public void SaveGame()
    {
        PlayerPrefs.SetInt("Gold", Gold);
        PlayerPrefs.SetInt("CurrentFloor", CurrentFloor);
        PlayerPrefs.Save();
        Debug.Log("Game saved successfully.");
    }

    public void LoadGame()
    {
        Gold = PlayerPrefs.GetInt("Gold", 0);
        CurrentFloor = PlayerPrefs.GetInt("CurrentFloor", 1);
        Debug.Log("Game loaded successfully.");
    }

    public void ResetGame()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Game reset successfully.");
    }

    public void ToggleFullScreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
    }

    public void SetResolution(int width, int height, bool fullScreen)
    {
        Screen.SetResolution(width, height, fullScreen);
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    public void MuteAudio(bool mute)
    {
        AudioListener.pause = mute;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetGraphicsSettings(bool enableShadows, bool enablePostProcessing)
    {
        QualitySettings.shadows = enableShadows ? ShadowQuality.All : ShadowQuality.Disable;
        // Assuming you have a post-processing manager or similar
        PostProcessingManager.Instance.SetPostProcessing(enablePostProcessing);
    }

    public void SetControlScheme(ControlScheme scheme)
    {
        // Assuming you have a control manager or similar
        ControlManager.Instance.SetControlScheme(scheme);
    }

    public void SetLanguage(string languageCode)
    {
        // Assuming you have a localization manager or similar
        LocalizationManager.Instance.SetLanguage(languageCode);
    }

    public void ShowSettingsMenu()
    {
        // Assuming you have a UI manager or similar
        UIManager.Instance.ShowSettingsMenu();
    }

    public void HideSettingsMenu()
    {
        UIManager.Instance.HideSettingsMenu();
    }

    public void ShowMainMenu()
    {
        UIManager.Instance.ShowMainMenu();
    }

    public void HideMainMenu()
    {
        UIManager.Instance.HideMainMenu();
    }

    public void ShowPauseMenu()
    {
        UIManager.Instance.ShowPauseMenu();
    }

    public void HidePauseMenu()
    {
        UIManager.Instance.HidePauseMenu();
    }

    public void ShowGameOverMenu()
    {
        UIManager.Instance.ShowGameOverMenu();
    }

    public void HideGameOverMenu()
    {
        UIManager.Instance.HideGameOverMenu();
    }

    public void ShowHUD()
    {
        UIManager.Instance.ShowHUD();
    }

    public void HideHUD()
    {
        UIManager.Instance.HideHUD();
    }

    public void ShowInventory()
    {
        UIManager.Instance.ShowInventory();
    }

    public void HideInventory()
    {
        UIManager.Instance.HideInventory();
    }

    public void ShowMap()
    {
        UIManager.Instance.ShowMap();
    }

    public void HideMap()
    {
        UIManager.Instance.HideMap();
    }

    public void ShowQuestLog()
    {
        UIManager.Instance.ShowQuestLog();
    }

    public void HideQuestLog()
    {
        UIManager.Instance.HideQuestLog();
    }

    public void ShowCharacterStats()
    {
        UIManager.Instance.ShowCharacterStats();
    }

    public void HideCharacterStats()
    {
        UIManager.Instance.HideCharacterStats();
    }

    public void ShowSkillTree()
    {
        UIManager.Instance.ShowSkillTree();
    }

    public void HideSkillTree()
    {
        UIManager.Instance.HideSkillTree();
    }

    public void ShowCraftingMenu()
    {
        UIManager.Instance.ShowCraftingMenu();
    }

    public void HideCraftingMenu()
    {
        UIManager.Instance.HideCraftingMenu();
    }

    public void ShowDialogue()
    {
        UIManager.Instance.ShowDialogue();
    }

    public void HideDialogue()
    {
        UIManager.Instance.HideDialogue();
    }

    public void ShowShop()
    {
        UIManager.Instance.ShowShop();
    }

    public void HideShop()
    {
        UIManager.Instance.HideShop();
    }

    public void ShowTutorial()
    {
        UIManager.Instance.ShowTutorial();
    }

    public void HideTutorial()
    {
        UIManager.Instance.HideTutorial();
    }

    public void ShowAchievements()
    {
        UIManager.Instance.ShowAchievements();
    }

    public void HideAchievements()
    {
        UIManager.Instance.HideAchievements();
    }

    public void ShowLeaderboard()
    {
        UIManager.Instance.ShowLeaderboard();
    }

    public void HideLeaderboard()
    {
        UIManager.Instance.HideLeaderboard();
    }

    public void ShowCredits()
    {
        UIManager.Instance.ShowCredits();
    }

    public void HideCredits()
    {
        UIManager.Instance.HideCredits();
    }

    public void ShowLoadingScreen()
    {
        UIManager.Instance.ShowLoadingScreen();
    }

    public void HideLoadingScreen()
    {
        UIManager.Instance.HideLoadingScreen();
    }

    public void ShowNotification(string message)
    {
        UIManager.Instance.ShowNotification(message);
    }

    public void HideNotification()
    {
        UIManager.Instance.HideNotification();
    }

    public void ShowTooltip(string message)
    {
        UIManager.Instance.ShowTooltip(message);
    }

    public void HideTooltip()
    {
        UIManager.Instance.HideTooltip();
    }

    public void ShowContextMenu(Vector3 position, List<string> options)
    {
        UIManager.Instance.ShowContextMenu(position, options);
    }

    public void HideContextMenu()
    {
        UIManager.Instance.HideContextMenu();
    }

    public void ShowConfirmationDialog(string message, Action onConfirm, Action onCancel)
    {
        UIManager.Instance.ShowConfirmationDialog(message, onConfirm, onCancel);
    }

    public void HideConfirmationDialog()
    {
        UIManager.Instance.HideConfirmationDialog();
    }

    public void ShowInputDialog(string message, Action<string> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowInputDialog(message, onSubmit, onCancel);
    }

    public void HideInputDialog()
    {
        UIManager.Instance.HideInputDialog();
    }

    public void ShowFileDialog(string message, Action<string> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowFileDialog(message, onSubmit, onCancel);
    }

    public void HideFileDialog()
    {
        UIManager.Instance.HideFileDialog();
    }

    public void ShowColorPicker(Action<Color> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowColorPicker(onSubmit, onCancel);
    }

    public void HideColorPicker()
    {
        UIManager.Instance.HideColorPicker();
    }

    public void ShowDatePicker(Action<DateTime> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowDatePicker(onSubmit, onCancel);
    }

    public void HideDatePicker()
    {
        UIManager.Instance.HideDatePicker();
    }

    public void ShowTimePicker(Action<TimeSpan> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowTimePicker(onSubmit, onCancel);
    }

    public void HideTimePicker()
    {
        UIManager.Instance.HideTimePicker();
    }

    public void ShowSlider(string message, float minValue, float maxValue, Action<float> onSubmit, Action onCancel)
    {
        UIManager.Instance.ShowSlider(message, minValue, maxValue, onSubmit, onCancel);
    }

    public void HideSlider()
    {
        UIManager.Instance.HideSlider();
    }

    public void ShowProgressBar(string message, float progress)
    {
        UIManager.Instance.ShowProgressBar(message, progress);
    }

    public void HideProgressBar()
    {
        UIManager.Instance.HideProgressBar();
    }

    public void ShowLoadingBar(string message, float progress)
    {
        UIManager.Instance.ShowLoadingBar(message, progress);
    }

    public void HideLoadingBar()
    {
        UIManager.Instance.HideLoadingBar();
    }

    public void ShowStatusBar(string message, float progress)
    {
        UIManager.Instance.ShowStatusBar(message, progress);
    }

    public void HideStatusBar()
    {
        UIManager.Instance.HideStatusBar();
    }

    public void ShowHealthBar(float health)
    {
        UIManager.Instance.ShowHealthBar(health);
    }

    public void HideHealthBar()
    {
        UIManager.Instance.HideHealthBar();
    }

    public void ShowManaBar(float mana)
    {
        UIManager.Instance.ShowManaBar(mana);
    }

    public void HideManaBar()
    {
        UIManager.Instance.HideManaBar();
    }

    public void ShowStaminaBar(float stamina)
    {
        UIManager.Instance.ShowStaminaBar(stamina);
    }

    public void HideStaminaBar()
    {
        UIManager.Instance.HideStaminaBar();
    }

    public void ShowExperienceBar(float experience)
    {
        UIManager.Instance.ShowExperienceBar(experience);
    }

    public void HideExperienceBar()
    {
        UIManager.Instance.HideExperienceBar();
    }

    public void ShowLevelUpNotification(int level)
    {
        UIManager.Instance.ShowLevelUpNotification(level);
    }

    public void HideLevelUpNotification()
    {
        UIManager.Instance.HideLevelUpNotification();
    }

    public void ShowQuestNotification(string questName)
    {
        UIManager.Instance.ShowQuestNotification(questName);
    }

    public void HideQuestNotification()
    {
        UIManager.Instance.HideQuestNotification();
    }

    public void ShowAchievementNotification(string achievementName)
    {
        UIManager.Instance.ShowAchievementNotification(achievementName);
    }

    public void HideAchievementNotification()
    {
        UIManager.Instance.HideAchievementNotification();
    }

    public void ShowItemPickupNotification(string itemName)
    {
        UIManager.Instance.ShowItemPickupNotification(itemName);
    }

    public void HideItemPickupNotification()
    {
        UIManager.Instance.HideItemPickupNotification();
    }

    public void ShowSkillUnlockNotification(string skillName)
    {
        UIManager.Instance.ShowSkillUnlockNotification(skillName);
    }

    public void HideSkillUnlockNotification()
    {
        UIManager.Instance.HideSkillUnlockNotification();
    }

    public void ShowCraftingNotification(string itemName)
    {
        UIManager.Instance.ShowCraftingNotification(itemName);
    }

    public void HideCraftingNotification()
    {
        UIManager.Instance.HideCraftingNotification();
    }

    public void ShowDialogueOption(string optionText, Action onSelect)
    {
        UIManager.Instance.ShowDialogueOption(optionText, onSelect);
    }

    public void HideDialogueOption()
    {
        UIManager.Instance.HideDialogueOption();
    }

    public void ShowDialogueResponse(string responseText)
    {
        UIManager.Instance.ShowDialogueResponse(responseText);
    }

    public void HideDialogueResponse()
    {
        UIManager.Instance.HideDialogueResponse();
    }

    public void ShowDialogueChoices(List<string> choices, Action<int> onSelect)
    {
        UIManager.Instance.ShowDialogueChoices(choices, onSelect);
    }

    public void HideDialogueChoices()
    {
        UIManager.Instance.HideDialogueChoices();
    }

    public void ShowDialogueHistory(List<string> history)
    {
        UIManager.Instance.ShowDialogueHistory(history);
    }

    public void HideDialogueHistory()
    {
        UIManager.Instance.HideDialogueHistory();
    }

    public void ShowDialoguePortrait(Sprite portrait)
    {
        UIManager.Instance.ShowDialoguePortrait(portrait);
    }

    public void HideDialoguePortrait()
    {
        UIManager.Instance.HideDialoguePortrait();
    }

    public void ShowDialogueBackground(Sprite background)
    {
        UIManager.Instance.ShowDialogueBackground(background);
    }

    public void HideDialogueBackground()
    {
        UIManager.Instance.HideDialogueBackground();
    }

    public void ShowDialogueName(string name)
    {
        UIManager.Instance.ShowDialogueName(name);
    }

    public void HideDialogueName()
    {
        UIManager.Instance.HideDialogueName();
    }

    public void ShowDialogueText(string text)
    {
        UIManager.Instance.ShowDialogueText(text);
    }

    public void HideDialogueText()
    {
        UIManager.Instance.HideDialogueText();
    }

    public void ShowDialogueContinueButton(Action onClick)
    {
        UIManager.Instance.ShowDialogueContinueButton(onClick);
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
        }
    }

    private void Start()
    {
        enemyManager = FindObjectOfType<EnemyManager>();
        DungeonGenerator = FindObjectOfType<DungeonGenerator>();
        if (enemyManager == null)
        {
            Debug.LogError("EnemyManager not found in the scene!");
        }
        if (DungeonGenerator == null)
        {
            Debug.LogError("DungeonGenerator not found in the scene!");
        }
    }

    public void HideDialogueText()
    {
        UIManager.Instance.HideDialogueText();
    }

    public void HideDialogueOption()
    {
        UIManager.Instance.HideDialogueOption();
    }

    public void HideDialogueResponse()
    {
        UIManager.Instance.HideDialogueResponse();
    }

    public void HideDialogue()
    {
        UIManager.Instance.HideDialogue();
    }

    public void ShowDialogueContinueButton(Action onClick)
    {
        UIManager.Instance.ShowDialogueContinueButton(onClick);
    }

    public void HideDialogueContinueButton()
    {
        UIManager.Instance.HideDialogueContinueButton();
    }

    public void ShowDialogueSkipButton(Action onClick)
    {
        UIManager.Instance.ShowDialogueSkipButton(onClick);
    }

    public void HideDialogueSkipButton()
    {
        UIManager.Instance.HideDialogueSkipButton();
    }

    public void ShowDamageNumber(Vector3 position, int damage)
    {
        DamageNumber damageNumber = Instantiate(Resources.Load<DamageNumber>("DamageNumberPrefab"), position, Quaternion.identity);
        damageNumber.SetDamage(damage);
    }

    public void RemoveEnemy(EnemyController enemy)
    {
        if (enemyManager != null)
        {
            enemyManager.RemoveEnemy(enemy);
        }
    }

    public void ChangeGameState(GameState newState)
    {
        // Implement game state change logic here
    }

    public enum GameState
    {
        Playing,
        Paused,
        GameOver
    }
}