using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main game manager that handles level progression, UI, and game state
/// Attach to a GameManager object in the scene
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int currentLevel = 1;
    public int maxLevels = 100;
    public float levelTimeLimit = 120f; // Time limit per level in seconds
    
    [Header("References")]
    public MazeGenerator mazeGenerator;
    public BallController playerBall;
    public Transform goalObject;
    
    [Header("UI References")]
    public GameObject levelCompletePanel;
    public GameObject gameOverPanel;
    public UnityEngine.UI.Text levelText;
    public UnityEngine.UI.Text timeText;
    public UnityEngine.UI.Text themeText;
    
    [Header("Visual Effects")]
    public ParticleSystem levelCompleteParticles;
    public AudioSource levelCompleteSound;
    public AudioSource backgroundMusic;
    
    private float currentTime;
    private bool isLevelActive = false;
    private bool isTransitioning = false;
    
    void Start()
    {
        InitializeGame();
    }
    
    void Update()
    {
        if (isLevelActive && !isTransitioning)
        {
            UpdateTimer();
            CheckGoalReached();
        }
        
        // Skip level shortcut for testing
        if (Input.GetKeyDown(KeyCode.N) && !isTransitioning)
        {
            NextLevel();
        }
        
        // Restart level shortcut
        if (Input.GetKeyDown(KeyCode.R) && !isTransitioning)
        {
            RestartLevel();
        }
    }
    
    void InitializeGame()
    {
        Time.timeScale = 1f;
        isLevelActive = true;
        isTransitioning = false;
        
        // Load saved progress or start from level 1
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        
        if (mazeGenerator == null)
        {
            mazeGenerator = FindObjectOfType<MazeGenerator>();
        }
        
        GenerateCurrentLevel();
        UpdateUI();
    }
    
    void GenerateCurrentLevel()
    {
        if (mazeGenerator != null)
        {
            mazeGenerator.currentLevel = currentLevel;
            mazeGenerator.GenerateLevel(currentLevel);
            
            // Spawn player at start position
            SpawnPlayer();
            
            // Get goal reference
            goalObject = mazeGenerator.GetGoalTransform();
        }
    }
    
    void SpawnPlayer()
    {
        if (playerBall == null)
        {
            // Create player if not exists
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            player.name = "Player";
            player.transform.position = mazeGenerator.GetPlayerSpawnPosition();
            player.transform.localScale = Vector3.one * 0.8f;
            
            playerBall = player.AddComponent<BallController>();
            
            // Add camera follow
            CameraFollow camFollow = player.AddComponent<CameraFollow>();
        }
        else
        {
            playerBall.transform.position = mazeGenerator.GetPlayerSpawnPosition();
            playerBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
            playerBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
    
    void UpdateTimer()
    {
        currentTime -= Time.deltaTime;
        
        if (currentTime <= 0)
        {
            TimeUp();
        }
        
        UpdateUI();
    }
    
    void CheckGoalReached()
    {
        if (goalObject == null || playerBall == null)
            return;
        
        float distance = Vector3.Distance(playerBall.transform.position, goalObject.position);
        
        if (distance < 1f)
        {
            LevelComplete();
        }
    }
    
    void LevelComplete()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        isLevelActive = false;
        
        // Play effects
        if (levelCompleteParticles != null)
            levelCompleteParticles.Play();
        
        if (levelCompleteSound != null)
            levelCompleteSound.Play();
        
        Debug.Log($"Level {currentLevel} Complete!");
        
        // Save progress
        if (currentLevel < maxLevels)
        {
            PlayerPrefs.SetInt("CurrentLevel", currentLevel + 1);
        }
        else
        {
            PlayerPrefs.SetInt("CurrentLevel", 1); // Reset after completing all levels
        }
        PlayerPrefs.Save();
        
        // Show completion UI
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        // Wait then go to next level
        Invoke(nameof(NextLevel), 2f);
    }
    
    void NextLevel()
    {
        if (currentLevel >= maxLevels)
        {
            // Game completed!
            GameCompleted();
            return;
        }
        
        currentLevel++;
        LoadLevel();
    }
    
    void RestartLevel()
    {
        LoadLevel();
    }
    
    void LoadLevel()
    {
        isTransitioning = false;
        isLevelActive = true;
        currentTime = levelTimeLimit;
        
        // Hide UI panels
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Regenerate maze
        GenerateCurrentLevel();
        UpdateUI();
    }
    
    void TimeUp()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        isLevelActive = false;
        
        Debug.Log($"Time's up on Level {currentLevel}!");
        
        // Show game over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Restart level after delay
        Invoke(nameof(RestartLevel), 2f);
    }
    
    void GameCompleted()
    {
        Debug.Log("Congratulations! All 100 levels completed!");
        
        // Show special completion screen
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        // Reset progress
        PlayerPrefs.DeleteKey("CurrentLevel");
    }
    
    void UpdateUI()
    {
        if (levelText != null)
        {
            levelText.text = $"Level {currentLevel}/{maxLevels}";
        }
        
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timeText.text = $"{minutes:00}:{seconds:00}";
        }
        
        if (themeText != null && mazeGenerator != null)
        {
            ThemeData theme = mazeGenerator.GetCurrentTheme();
            if (theme != null)
            {
                themeText.text = theme.name;
            }
        }
    }
    
    public void OnLevelCompleteButton()
    {
        NextLevel();
    }
    
    public void OnRestartButton()
    {
        RestartLevel();
    }
    
    public void OnMainMenuButton()
    {
        SceneManager.LoadScene(0); // Load main menu scene
    }
    
    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
    
    void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            PlayerPrefs.Save();
        }
    }
}
