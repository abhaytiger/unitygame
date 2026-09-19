using UnityEngine;

namespace GyroMaze.Core
{
    /// <summary>
    /// Central game state manager handling level progression, win/lose states, and theme transitions.
    /// Singleton pattern for global access.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Game Configuration")]
        [SerializeField] private int totalLevels = 100;
        [SerializeField] private float levelTransitionDelay = 2f;
        
        [Header("References")]
        [SerializeField] private MazeGenerator mazeGenerator;
        [SerializeField] private BallController ballController;
        [SerializeField] private CameraFollow cameraFollow;
        
        // Game State
        private int currentLevel = 1;
        private bool isLevelActive = false;
        private bool isTransitioning = false;
        
        // Events
        public static System.Action<int> OnLevelChanged;
        public static System.Action<int> OnThemeChanged;
        public static System.Action OnLevelComplete;
        public static System.Action OnGameComplete;
        
        // Theme configuration (changes every 10 levels)
        public enum GameTheme
        {
            ClassicSteel,      // 1-10
            NeonCity,          // 11-20
            AncientStone,      // 21-30
            IcePalace,         // 31-40
            LavaFactory,       // 41-50
            CrystalCavern,     // 51-60
            ForestTemple,      // 61-70
            SpaceStation,      // 71-80
            ShadowRealm,       // 81-90
            DivineParadise     // 91-100
        }
        
        private GameTheme currentTheme;
        
        public int CurrentLevel => currentLevel;
        public int TotalLevels => totalLevels;
        public GameTheme CurrentTheme => currentTheme;
        public bool IsLevelActive => isLevelActive;
        
        private void Awake()
        {
            // Ensure singleton behavior
            if (FindObjectsOfType<GameManager>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
            InitializeGame();
        }
        
        private void Start()
        {
            LoadLevel(currentLevel);
        }
        
        /// <summary>
        /// Initialize game state and reset progress
        /// </summary>
        public void InitializeGame()
        {
            currentLevel = 1;
            isLevelActive = false;
            isTransitioning = false;
            UpdateTheme();
            
            Debug.Log($"[GameManager] Game initialized. Total Levels: {totalLevels}");
        }
        
        /// <summary>
        /// Load a specific level with appropriate difficulty
        /// </summary>
        public void LoadLevel(int level)
        {
            if (level < 1 || level > totalLevels)
            {
                Debug.LogError($"[GameManager] Invalid level: {level}");
                return;
            }
            
            isTransitioning = true;
            isLevelActive = false;
            
            // Update theme if crossing 10-level boundary
            UpdateTheme();
            
            // Notify systems of level change
            OnLevelChanged?.Invoke(level);
            
            // Generate maze with level-appropriate difficulty
            if (mazeGenerator != null)
            {
                int difficulty = CalculateDifficulty(level);
                mazeGenerator.GenerateMaze(level, difficulty);
            }
            
            // Reset ball position
            if (ballController != null)
            {
                ballController.ResetBall();
            }
            
            // Configure camera for new maze size
            if (cameraFollow != null && mazeGenerator != null)
            {
                cameraFollow.SetMazeSize(mazeGenerator.CurrentMazeSize);
            }
            
            StartCoroutine(LevelStartSequence());
        }
        
        /// <summary>
        /// Calculate difficulty multiplier based on level (1.0 to 3.0)
        /// </summary>
        private int CalculateDifficulty(int level)
        {
            // Linear progression from 1 to 100
            // Maze size increases, complexity increases
            int baseSize = 8;
            int maxSize = 25;
            int sizeProgression = Mathf.FloorToInt(baseSize + ((maxSize - baseSize) * (level - 1f) / (totalLevels - 1f)));
            
            return sizeProgression;
        }
        
        /// <summary>
        /// Update current theme based on level number
        /// </summary>
        private void UpdateTheme()
        {
            int themeIndex = Mathf.FloorToInt((currentLevel - 1) / 10f);
            themeIndex = Mathf.Clamp(themeIndex, 0, System.Enum.GetNames(typeof(GameTheme)).Length - 1);
            
            GameTheme newTheme = (GameTheme)themeIndex;
            
            if (newTheme != currentTheme)
            {
                currentTheme = newTheme;
                OnThemeChanged?.Invoke((int)currentTheme);
                Debug.Log($"[GameManager] Theme changed to: {currentTheme}");
            }
        }
        
        /// <summary>
        /// Called when player reaches the goal
        /// </summary>
        public void CompleteLevel()
        {
            if (!isLevelActive || isTransitioning) return;
            
            isLevelActive = false;
            isTransitioning = true;
            
            OnLevelComplete?.Invoke();
            Debug.Log($"[GameManager] Level {currentLevel} completed!");
            
            if (currentLevel >= totalLevels)
            {
                CompleteGame();
                return;
            }
            
            Invoke(nameof(LoadNextLevel), levelTransitionDelay);
        }
        
        /// <summary>
        /// Load the next level in sequence
        /// </summary>
        private void LoadNextLevel()
        {
            currentLevel++;
            LoadLevel(currentLevel);
        }
        
        /// <summary>
        /// Called when all 100 levels are completed
        /// </summary>
        private void CompleteGame()
        {
            OnGameComplete?.Invoke();
            Debug.Log("[GameManager] 🎉 GAME COMPLETE! All 100 levels finished!");
            
            // Optional: Show end screen, save achievements, etc.
            isTransitioning = false;
        }
        
        /// <summary>
        /// Restart current level (e.g., player fell off)
        /// </summary>
        public void RestartLevel()
        {
            if (isTransitioning) return;
            
            Debug.Log($"[GameManager] Restarting level {currentLevel}");
            LoadLevel(currentLevel);
        }
        
        /// <summary>
        /// Coroutine to handle level start sequence (fade in, enable controls, etc.)
        /// </summary>
        private System.Collections.IEnumerator LevelStartSequence()
        {
            // Disable ball controls during setup
            if (ballController != null)
                ballController.SetControlsEnabled(false);
            
            yield return new WaitForSeconds(0.5f);
            
            // Enable controls
            if (ballController != null)
                ballController.SetControlsEnabled(true);
            
            isLevelActive = true;
            isTransitioning = false;
            
            Debug.Log($"[GameManager] Level {currentLevel} started. Difficulty: {CalculateDifficulty(currentLevel)}");
        }
        
        #region Editor Helpers
        
        #if UNITY_EDITOR
        [ContextMenu("Test Complete Level")]
        private void TestCompleteLevel()
        {
            CompleteLevel();
        }
        
        [ContextMenu("Test Restart Level")]
        private void TestRestartLevel()
        {
            RestartLevel();
        }
        
        [ContextMenu("Go to Next Level")]
        private void GoToNextLevel()
        {
            if (currentLevel < totalLevels)
            {
                currentLevel++;
                LoadLevel(currentLevel);
            }
        }
        
        [ContextMenu("Go to Previous Level")]
        private void GoToPreviousLevel()
        {
            if (currentLevel > 1)
            {
                currentLevel--;
                LoadLevel(currentLevel);
            }
        }
        #endif
        
        #endregion
    }
}
