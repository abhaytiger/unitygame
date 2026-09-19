using UnityEngine;
using System.Collections.Generic;

namespace GyroMaze.Generators
{
    /// <summary>
    /// Professional procedural maze generator using recursive backtracking algorithm.
    /// Supports 100 levels with progressive difficulty and 10 unique visual themes.
    /// </summary>
    public class MazeGenerator : MonoBehaviour
    {
        [Header("Maze Configuration")]
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private float wallHeight = 1.5f;
        [SerializeField] private float wallThickness = 0.2f;
        
        [Header("Theme Materials")]
        [SerializeField] private List<ThemeData> themeDataList;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject goalPrefab;
        
        [Header("Lighting")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private LightSettings[] lightSettings;
        
        // Current maze state
        private int[,] mazeGrid;
        private int currentMazeSize;
        private Vector3 startPos;
        private Vector3 goalPos;
        
        // Generated objects for cleanup
        private List<GameObject> generatedObjects = new List<GameObject>();
        
        // Current theme index
        private int currentThemeIndex = 0;
        
        public int CurrentMazeSize => currentMazeSize;
        public Vector3 StartPosition => startPos;
        public Vector3 GoalPosition => goalPos;
        
        private void Awake()
        {
            InitializeThemes();
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            GameManager.OnThemeChanged += HandleThemeChange;
        }
        
        private void UnsubscribeFromEvents()
        {
            GameManager.OnThemeChanged -= HandleThemeChange;
        }
        
        private void InitializeThemes()
        {
            if (themeDataList == null || themeDataList.Count == 0)
            {
                CreateDefaultThemes();
            }
        }
        
        /// <summary>
        /// Generate a new maze with specified level and difficulty
        /// </summary>
        public void GenerateMaze(int level, int difficulty)
        {
            CleanupPreviousMaze();
            
            currentMazeSize = difficulty;
            mazeGrid = new int[currentMazeSize, currentMazeSize];
            
            // Initialize grid (1 = wall, 0 = path)
            for (int x = 0; x < currentMazeSize; x++)
            {
                for (int z = 0; z < currentMazeSize; z++)
                {
                    mazeGrid[x, z] = 1;
                }
            }
            
            // Generate maze using recursive backtracking
            GenerateRecursiveBacktrack();
            
            // Add loops for higher difficulty levels
            if (level > 20)
            {
                AddLoops(Mathf.Min(5, level / 10));
            }
            
            // Build physical maze
            BuildMazeGeometry();
            
            // Set start and goal positions
            startPos = new Vector3(0, 0.5f, 0);
            goalPos = new Vector3((currentMazeSize - 1) * cellSize, 0.5f, (currentMazeSize - 1) * cellSize);
            
            SpawnGoal();
            ApplyCurrentTheme();
            
            Debug.Log($"[MazeGenerator] Generated level {level} - Size: {currentMazeSize}x{currentMazeSize}");
        }
        
        /// <summary>
        /// Recursive backtracking maze generation algorithm
        /// </summary>
        private void GenerateRecursiveBacktrack()
        {
            Stack<Vector2Int> stack = new Stack<Vector2Int>();
            Vector2Int current = new Vector2Int(0, 0);
            
            mazeGrid[0, 0] = 0;
            stack.Push(current);
            
            while (stack.Count > 0)
            {
                current = stack.Peek();
                
                List<Vector2Int> neighbors = GetUnvisitedNeighbors(current);
                
                if (neighbors.Count > 0)
                {
                    Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                    
                    // Remove wall between current and next
                    RemoveWall(current, next);
                    
                    mazeGrid[next.x, next.y] = 0;
                    stack.Push(next);
                }
                else
                {
                    stack.Pop();
                }
            }
        }
        
        /// <summary>
        /// Get unvisited neighbor cells
        /// </summary>
        private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            int step = 2;
            
            // Check all four directions
            if (cell.y - step >= 0 && mazeGrid[cell.x, cell.y - step] == 1)
                neighbors.Add(new Vector2Int(cell.x, cell.y - step));
            
            if (cell.y + step < currentMazeSize && mazeGrid[cell.x, cell.y + step] == 1)
                neighbors.Add(new Vector2Int(cell.x, cell.y + step));
            
            if (cell.x - step >= 0 && mazeGrid[cell.x - step, cell.y] == 1)
                neighbors.Add(new Vector2Int(cell.x - step, cell.y));
            
            if (cell.x + step < currentMazeSize && mazeGrid[cell.x + step, cell.y] == 1)
                neighbors.Add(new Vector2Int(cell.x + step, cell.y));
            
            return neighbors;
        }
        
        /// <summary>
        /// Remove wall between two cells
        /// </summary>
        private void RemoveWall(Vector2Int a, Vector2Int b)
        {
            int midX = (a.x + b.x) / 2;
            int midY = (a.y + b.y) / 2;
            mazeGrid[midX, midY] = 0;
        }
        
        /// <summary>
        /// Add random loops to make maze more complex
        /// </summary>
        private void AddLoops(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int x = Random.Range(1, currentMazeSize - 1);
                int z = Random.Range(1, currentMazeSize - 1);
                
                if (mazeGrid[x, z] == 1)
                {
                    mazeGrid[x, z] = 0;
                }
            }
        }
        
        /// <summary>
        /// Build 3D geometry for the maze
        /// </summary>
        private void BuildMazeGeometry()
        {
            // Create floor
            CreateFloor();
            
            // Create walls
            for (int x = 0; x < currentMazeSize; x++)
            {
                for (int z = 0; z < currentMazeSize; z++)
                {
                    if (mazeGrid[x, z] == 1)
                    {
                        CreateWall(x, z);
                    }
                }
            }
            
            // Create outer boundary
            CreateBoundary();
        }
        
        private void CreateFloor()
        {
            float size = currentMazeSize * cellSize;
            
            GameObject floor;
            if (floorPrefab != null)
            {
                floor = Instantiate(floorPrefab, new Vector3(size / 2 - cellSize / 2, 0, size / 2 - cellSize / 2), Quaternion.identity);
                floor.transform.localScale = new Vector3(size, 1, size);
            }
            else
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.transform.position = new Vector3(size / 2 - cellSize / 2, 0, size / 2 - cellSize / 2);
                floor.transform.localScale = new Vector3(size / 10, 1, size / 10);
            }
            
            floor.name = "MazeFloor";
            generatedObjects.Add(floor);
        }
        
        private void CreateWall(int x, int z)
        {
            Vector3 position = new Vector3(x * cellSize, wallHeight / 2, z * cellSize);
            
            GameObject wall;
            if (wallPrefab != null)
            {
                wall = Instantiate(wallPrefab, position, Quaternion.identity);
                wall.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
            }
            else
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = position;
                wall.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
                
                // Remove collider from visual walls if needed
                Collider col = wall.GetComponent<Collider>();
                if (col != null)
                {
                    col.isTrigger = false;
                }
            }
            
            wall.name = $"Wall_{x}_{z}";
            generatedObjects.Add(wall);
        }
        
        private void CreateBoundary()
        {
            float size = currentMazeSize * cellSize;
            float halfSize = size / 2;
            
            // Create four boundary walls
            CreateBoundaryWall(new Vector3(-cellSize / 2, wallHeight / 2, halfSize - cellSize / 2), new Vector3(1, wallHeight, size + cellSize * 2));
            CreateBoundaryWall(new Vector3(size + cellSize / 2, wallHeight / 2, halfSize - cellSize / 2), new Vector3(1, wallHeight, size + cellSize * 2));
            CreateBoundaryWall(new Vector3(halfSize - cellSize / 2, wallHeight / 2, -cellSize / 2), new Vector3(size + cellSize * 2, wallHeight, 1));
            CreateBoundaryWall(new Vector3(halfSize - cellSize / 2, wallHeight / 2, size + cellSize / 2), new Vector3(size + cellSize * 2, wallHeight, 1));
        }
        
        private void CreateBoundaryWall(Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.name = "BoundaryWall";
            generatedObjects.Add(wall);
        }
        
        private void SpawnGoal()
        {
            if (goalPrefab != null)
            {
                GameObject goal = Instantiate(goalPrefab, goalPos, Quaternion.identity);
                generatedObjects.Add(goal);
            }
            else
            {
                GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                goal.transform.position = goalPos;
                goal.transform.localScale = Vector3.one * 0.8f;
                goal.name = "Goal";
                generatedObjects.Add(goal);
                
                // Add goal collider
                SphereCollider goalCollider = goal.GetComponent<SphereCollider>();
                if (goalCollider != null)
                {
                    goalCollider.isTrigger = true;
                }
            }
        }
        
        /// <summary>
        /// Clean up previously generated maze objects
        /// </summary>
        private void CleanupPreviousMaze()
        {
            foreach (GameObject obj in generatedObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            generatedObjects.Clear();
        }
        
        /// <summary>
        /// Handle theme change event from GameManager
        /// </summary>
        private void HandleThemeChange(int themeIndex)
        {
            currentThemeIndex = themeIndex;
            ApplyCurrentTheme();
        }
        
        /// <summary>
        /// Apply visual theme to maze elements
        /// </summary>
        private void ApplyCurrentTheme()
        {
            if (themeDataList == null || themeDataList.Count == 0 || currentThemeIndex >= themeDataList.Count)
                return;
            
            ThemeData theme = themeDataList[currentThemeIndex];
            
            // Apply materials to walls
            foreach (GameObject obj in generatedObjects)
            {
                if (obj == null) continue;
                
                if (obj.name.StartsWith("Wall") || obj.name.StartsWith("Boundary"))
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null && theme.wallMaterial != null)
                    {
                        renderer.material = theme.wallMaterial;
                    }
                    
                    // Add emissive effect for certain themes
                    if (theme.emissiveColor != Color.clear)
                    {
                        SetEmissiveMaterial(renderer, theme.emissiveColor, theme.emissiveIntensity);
                    }
                }
                else if (obj.name == "MazeFloor")
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null && theme.floorMaterial != null)
                    {
                        renderer.material = theme.floorMaterial;
                    }
                }
                else if (obj.name == "Goal")
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null && theme.goalMaterial != null)
                    {
                        renderer.material = theme.goalMaterial;
                    }
                    
                    // Update goal light
                    Light goalLight = obj.GetComponent<Light>();
                    if (goalLight != null)
                    {
                        goalLight.color = theme.goalLightColor;
                    }
                }
            }
            
            // Apply lighting settings
            ApplyLightingSettings(theme);
        }
        
        private void SetEmissiveMaterial(Renderer renderer, Color color, float intensity)
        {
            if (renderer == null || renderer.material == null) return;
            
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", color * intensity);
        }
        
        private void ApplyLightingSettings(ThemeData theme)
        {
            if (directionalLight != null)
            {
                directionalLight.color = theme.lightColor;
                directionalLight.intensity = theme.lightIntensity;
            }
            
            RenderSettings.fog = theme.enableFog;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;
        }
        
        /// <summary>
        /// Create default theme data if none assigned
        /// </summary>
        private void CreateDefaultThemes()
        {
            themeDataList = new List<ThemeData>();
            
            // Theme 1: Classic Steel (Levels 1-10)
            themeDataList.Add(new ThemeData
            {
                name = "Classic Steel",
                wallMaterial = CreateMetallicMaterial(Color.gray, 0.8f),
                floorMaterial = CreateMetallicMaterial(new Color(0.2f, 0.2f, 0.25f), 0.6f),
                goalMaterial = CreateGlowingMaterial(Color.green),
                goalLightColor = Color.green,
                lightColor = new Color(1f, 0.95f, 0.9f),
                lightIntensity = 1.2f,
                enableFog = false,
                fogColor = Color.gray,
                fogDensity = 0.02f,
                emissiveColor = Color.clear,
                emissiveIntensity = 0f
            });
            
            // Theme 2: Neon City (Levels 11-20)
            themeDataList.Add(new ThemeData
            {
                name = "Neon City",
                wallMaterial = CreateDarkMaterial(new Color(0.1f, 0.05f, 0.15f)),
                floorMaterial = CreateDarkMaterial(new Color(0.05f, 0.05f, 0.1f)),
                goalMaterial = CreateGlowingMaterial(new Color(0f, 1f, 1f)),
                goalLightColor = new Color(0f, 1f, 1f),
                lightColor = new Color(0.8f, 0.6f, 1f),
                lightIntensity = 0.8f,
                enableFog = true,
                fogColor = new Color(0.1f, 0.05f, 0.15f),
                fogDensity = 0.03f,
                emissiveColor = new Color(0f, 1f, 1f),
                emissiveIntensity = 0.5f
            });
            
            // Theme 3: Ancient Stone (Levels 21-30)
            themeDataList.Add(new ThemeData
            {
                name = "Ancient Stone",
                wallMaterial = CreateStoneMaterial(new Color(0.6f, 0.5f, 0.4f)),
                floorMaterial = CreateStoneMaterial(new Color(0.5f, 0.45f, 0.4f)),
                goalMaterial = CreateGlowingMaterial(new Color(1f, 0.8f, 0.2f)),
                goalLightColor = new Color(1f, 0.8f, 0.2f),
                lightColor = new Color(1f, 0.9f, 0.7f),
                lightIntensity = 1f,
                enableFog = true,
                fogColor = new Color(0.7f, 0.65f, 0.6f),
                fogDensity = 0.015f,
                emissiveColor = Color.clear,
                emissiveIntensity = 0f
            });
            
            // Theme 4: Ice Palace (Levels 31-40)
            themeDataList.Add(new ThemeData
            {
                name = "Ice Palace",
                wallMaterial = CreateIcyMaterial(new Color(0.8f, 0.9f, 1f)),
                floorMaterial = CreateIcyMaterial(new Color(0.7f, 0.85f, 0.95f)),
                goalMaterial = CreateGlowingMaterial(new Color(0.5f, 0.8f, 1f)),
                goalLightColor = new Color(0.5f, 0.8f, 1f),
                lightColor = new Color(0.9f, 0.95f, 1f),
                lightIntensity = 1.3f,
                enableFog = true,
                fogColor = new Color(0.8f, 0.9f, 1f),
                fogDensity = 0.02f,
                emissiveColor = new Color(0.5f, 0.8f, 1f),
                emissiveIntensity = 0.3f
            });
            
            // Theme 5: Lava Factory (Levels 41-50)
            themeDataList.Add(new ThemeData
            {
                name = "Lava Factory",
                wallMaterial = CreateDarkMaterial(new Color(0.2f, 0.1f, 0.1f)),
                floorMaterial = CreateDarkMaterial(new Color(0.15f, 0.1f, 0.1f)),
                goalMaterial = CreateGlowingMaterial(new Color(1f, 0.5f, 0f)),
                goalLightColor = new Color(1f, 0.5f, 0f),
                lightColor = new Color(1f, 0.7f, 0.5f),
                lightIntensity = 1f,
                enableFog = true,
                fogColor = new Color(0.3f, 0.15f, 0.1f),
                fogDensity = 0.025f,
                emissiveColor = new Color(1f, 0.3f, 0f),
                emissiveIntensity = 0.6f
            });
            
            // Theme 6: Crystal Cavern (Levels 51-60)
            themeDataList.Add(new ThemeData
            {
                name = "Crystal Cavern",
                wallMaterial = CreateCrystalMaterial(new Color(0.6f, 0.3f, 0.8f)),
                floorMaterial = CreateCrystalMaterial(new Color(0.5f, 0.25f, 0.7f)),
                goalMaterial = CreateGlowingMaterial(new Color(0.8f, 0.4f, 1f)),
                goalLightColor = new Color(0.8f, 0.4f, 1f),
                lightColor = new Color(0.9f, 0.7f, 1f),
                lightIntensity = 0.9f,
                enableFog = true,
                fogColor = new Color(0.3f, 0.15f, 0.4f),
                fogDensity = 0.03f,
                emissiveColor = new Color(0.7f, 0.3f, 0.9f),
                emissiveIntensity = 0.4f
            });
            
            // Theme 7: Forest Temple (Levels 61-70)
            themeDataList.Add(new ThemeData
            {
                name = "Forest Temple",
                wallMaterial = CreateWoodMaterial(new Color(0.4f, 0.3f, 0.2f)),
                floorMaterial = CreateWoodMaterial(new Color(0.35f, 0.28f, 0.18f)),
                goalMaterial = CreateGlowingMaterial(new Color(0.3f, 1f, 0.4f)),
                goalLightColor = new Color(0.3f, 1f, 0.4f),
                lightColor = new Color(0.9f, 1f, 0.85f),
                lightIntensity = 1.1f,
                enableFog = true,
                fogColor = new Color(0.6f, 0.75f, 0.6f),
                fogDensity = 0.015f,
                emissiveColor = Color.clear,
                emissiveIntensity = 0f
            });
            
            // Theme 8: Space Station (Levels 71-80)
            themeDataList.Add(new ThemeData
            {
                name = "Space Station",
                wallMaterial = CreateMetallicMaterial(new Color(0.7f, 0.75f, 0.8f), 0.9f),
                floorMaterial = CreateMetallicMaterial(new Color(0.5f, 0.55f, 0.6f), 0.7f),
                goalMaterial = CreateGlowingMaterial(new Color(0f, 0.8f, 1f)),
                goalLightColor = new Color(0f, 0.8f, 1f),
                lightColor = new Color(0.95f, 0.95f, 1f),
                lightIntensity = 1.4f,
                enableFog = false,
                fogColor = Color.black,
                fogDensity = 0f,
                emissiveColor = new Color(0f, 0.6f, 0.8f),
                emissiveIntensity = 0.4f
            });
            
            // Theme 9: Shadow Realm (Levels 81-90)
            themeDataList.Add(new ThemeData
            {
                name = "Shadow Realm",
                wallMaterial = CreateDarkMaterial(new Color(0.1f, 0.1f, 0.15f)),
                floorMaterial = CreateDarkMaterial(new Color(0.08f, 0.08f, 0.12f)),
                goalMaterial = CreateGlowingMaterial(new Color(0.6f, 0.2f, 0.8f)),
                goalLightColor = new Color(0.6f, 0.2f, 0.8f),
                lightColor = new Color(0.7f, 0.5f, 0.8f),
                lightIntensity = 0.6f,
                enableFog = true,
                fogColor = new Color(0.15f, 0.1f, 0.2f),
                fogDensity = 0.04f,
                emissiveColor = new Color(0.5f, 0.2f, 0.7f),
                emissiveIntensity = 0.5f
            });
            
            // Theme 10: Divine Paradise (Levels 91-100)
            themeDataList.Add(new ThemeData
            {
                name = "Divine Paradise",
                wallMaterial = CreateGoldenMaterial(new Color(1f, 0.9f, 0.6f)),
                floorMaterial = CreateGoldenMaterial(new Color(0.95f, 0.85f, 0.55f)),
                goalMaterial = CreateGlowingMaterial(new Color(1f, 1f, 0.8f)),
                goalLightColor = new Color(1f, 1f, 0.9f),
                lightColor = new Color(1f, 0.98f, 0.9f),
                lightIntensity = 1.5f,
                enableFog = true,
                fogColor = new Color(1f, 0.95f, 0.85f),
                fogDensity = 0.01f,
                emissiveColor = new Color(1f, 0.9f, 0.6f),
                emissiveIntensity = 0.6f
            });
        }
        
        #region Material Helpers
        
        private Material CreateMetallicMaterial(Color baseColor, float metallic)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", 0.7f);
            return mat;
        }
        
        private Material CreateDarkMaterial(Color baseColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0.3f);
            mat.SetFloat("_Smoothness", 0.4f);
            return mat;
        }
        
        private Material CreateStoneMaterial(Color baseColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.3f);
            return mat;
        }
        
        private Material CreateIcyMaterial(Color baseColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0.2f);
            mat.SetFloat("_Smoothness", 0.9f);
            return mat;
        }
        
        private Material CreateCrystalMaterial(Color baseColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0.4f);
            mat.SetFloat("_Smoothness", 0.85f);
            return mat;
        }
        
        private Material CreateWoodMaterial(Color baseColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.25f);
            return mat;
        }
        
        private Material CreateGoldenMaterial(Color baseColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = baseColor;
            mat.SetFloat("_Metallic", 0.9f);
            mat.SetFloat("_Smoothness", 0.85f);
            return mat;
        }
        
        private Material CreateGlowingMaterial(Color glowColor)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.white;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * 1.5f);
            mat.SetFloat("_Metallic", 0.5f);
            mat.SetFloat("_Smoothness", 0.9f);
            return mat;
        }
        
        #endregion
        
        #region Data Classes
        
        [System.Serializable]
        public class ThemeData
        {
            public string name;
            public Material wallMaterial;
            public Material floorMaterial;
            public Material goalMaterial;
            public Color goalLightColor;
            public Color lightColor;
            public float lightIntensity = 1f;
            public bool enableFog;
            public Color fogColor;
            public float fogDensity;
            public Color emissiveColor;
            public float emissiveIntensity;
        }
        
        [System.Serializable]
        public class LightSettings
        {
            public Color color;
            public float intensity;
            public float shadowStrength;
        }
        
        #endregion
    }
}
