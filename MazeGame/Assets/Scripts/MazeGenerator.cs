using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages maze generation with increasing difficulty across 100 levels
/// Changes theme every 10 levels
/// Attach to a GameManager object in the scene
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Header("Level Settings")]
    public int currentLevel = 1;
    public int maxLevels = 100;
    
    [Header("Maze Dimensions")]
    [Range(5, 50)]
    public int mazeWidth = 10;
    [Range(5, 50)]
    public int mazeHeight = 10;
    
    [Header("Difficulty Progression")]
    [Tooltip("Base complexity for level 1")]
    public float baseComplexity = 0.3f;
    [Tooltip("Complexity increase per level")]
    public float complexityPerLevel = 0.008f;
    [Tooltip("Maximum complexity cap")]
    public float maxComplexity = 0.9f;
    
    [Header("Theme Settings")]
    public ThemeData[] themes;
    private ThemeData currentTheme;
    
    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject goalPrefab;
    public GameObject startPlatformPrefab;
    
    [Header("References")]
    public Transform mazeParent;
    public Transform playerSpawnPoint;
    public Transform goalObject;
    
    private Cell[,] mazeGrid;
    private List<Vector3> walls = new List<Vector3>();
    private Vector3Int startPos;
    private Vector3Int goalPos;
    
    void Start()
    {
        InitializeThemes();
        GenerateLevel(currentLevel);
    }
    
    void InitializeThemes()
    {
        if (themes == null || themes.Length == 0)
        {
            // Create default themes if none assigned
            themes = CreateDefaultThemes();
        }
    }
    
    ThemeData[] CreateDefaultThemes()
    {
        ThemeData[] defaultThemes = new ThemeData[10];
        
        // Theme 1: Classic Steel (Levels 1-10)
        defaultThemes[0] = new ThemeData
        {
            name = "Classic Steel",
            wallColor = new Color(0.4f, 0.4f, 0.45f),
            floorColor = new Color(0.2f, 0.2f, 0.25f),
            goalColor = new Color(0.2f, 0.8f, 0.3f),
            ambientLight = new Color(0.6f, 0.6f, 0.65f),
            fogColor = new Color(0.3f, 0.3f, 0.35f),
            wallMaterialType = MaterialType.Metal,
            floorMaterialType = MaterialType.Concrete
        };
        
        // Theme 2: Neon City (Levels 11-20)
        defaultThemes[1] = new ThemeData
        {
            name = "Neon City",
            wallColor = new Color(0.1f, 0.1f, 0.2f),
            floorColor = new Color(0.05f, 0.05f, 0.1f),
            goalColor = new Color(0.0f, 1f, 1f),
            ambientLight = new Color(0.2f, 0.1f, 0.3f),
            fogColor = new Color(0.1f, 0.05f, 0.15f),
            wallMaterialType = MaterialType.Glowing,
            floorMaterialType = MaterialType.Grid
        };
        
        // Theme 3: Ancient Stone (Levels 21-30)
        defaultThemes[2] = new ThemeData
        {
            name = "Ancient Stone",
            wallColor = new Color(0.5f, 0.45f, 0.4f),
            floorColor = new Color(0.35f, 0.3f, 0.25f),
            goalColor = new Color(1f, 0.8f, 0.2f),
            ambientLight = new Color(0.7f, 0.65f, 0.55f),
            fogColor = new Color(0.4f, 0.35f, 0.3f),
            wallMaterialType = MaterialType.Stone,
            floorMaterialType = MaterialType.Stone
        };
        
        // Theme 4: Ice Palace (Levels 31-40)
        defaultThemes[3] = new ThemeData
        {
            name = "Ice Palace",
            wallColor = new Color(0.7f, 0.85f, 0.95f),
            floorColor = new Color(0.5f, 0.7f, 0.85f),
            goalColor = new Color(0.3f, 0.6f, 1f),
            ambientLight = new Color(0.8f, 0.9f, 1f),
            fogColor = new Color(0.7f, 0.85f, 0.95f),
            wallMaterialType = MaterialType.Ice,
            floorMaterialType = MaterialType.Ice
        };
        
        // Theme 5: Lava Factory (Levels 41-50)
        defaultThemes[4] = new ThemeData
        {
            name = "Lava Factory",
            wallColor = new Color(0.3f, 0.25f, 0.2f),
            floorColor = new Color(0.2f, 0.15f, 0.1f),
            goalColor = new Color(1f, 0.3f, 0.1f),
            ambientLight = new Color(0.8f, 0.4f, 0.2f),
            fogColor = new Color(0.3f, 0.15f, 0.1f),
            wallMaterialType = MaterialType.Metal,
            floorMaterialType = MaterialType.Grates
        };
        
        // Theme 6: Crystal Cavern (Levels 51-60)
        defaultThemes[5] = new ThemeData
        {
            name = "Crystal Cavern",
            wallColor = new Color(0.6f, 0.3f, 0.7f),
            floorColor = new Color(0.4f, 0.2f, 0.5f),
            goalColor = new Color(0.9f, 0.5f, 1f),
            ambientLight = new Color(0.5f, 0.3f, 0.6f),
            fogColor = new Color(0.3f, 0.15f, 0.4f),
            wallMaterialType = MaterialType.Crystal,
            floorMaterialType = MaterialType.Crystal
        };
        
        // Theme 7: Forest Temple (Levels 61-70)
        defaultThemes[6] = new ThemeData
        {
            name = "Forest Temple",
            wallColor = new Color(0.3f, 0.5f, 0.3f),
            floorColor = new Color(0.2f, 0.35f, 0.2f),
            goalColor = new Color(0.8f, 0.9f, 0.3f),
            ambientLight = new Color(0.5f, 0.7f, 0.4f),
            fogColor = new Color(0.3f, 0.45f, 0.3f),
            wallMaterialType = MaterialType.Wood,
            floorMaterialType = MaterialType.Grass
        };
        
        // Theme 8: Space Station (Levels 71-80)
        defaultThemes[7] = new ThemeData
        {
            name = "Space Station",
            wallColor = new Color(0.6f, 0.65f, 0.7f),
            floorColor = new Color(0.3f, 0.35f, 0.4f),
            goalColor = new Color(0.2f, 1f, 0.8f),
            ambientLight = new Color(0.5f, 0.6f, 0.7f),
            fogColor = new Color(0.1f, 0.1f, 0.15f),
            wallMaterialType = MaterialType.Technology,
            floorMaterialType = MaterialType.Technology
        };
        
        // Theme 9: Shadow Realm (Levels 81-90)
        defaultThemes[8] = new ThemeData
        {
            name = "Shadow Realm",
            wallColor = new Color(0.15f, 0.1f, 0.2f),
            floorColor = new Color(0.08f, 0.05f, 0.12f),
            goalColor = new Color(0.7f, 0.2f, 0.8f),
            ambientLight = new Color(0.2f, 0.15f, 0.25f),
            fogColor = new Color(0.1f, 0.08f, 0.15f),
            wallMaterialType = MaterialType.Shadow,
            floorMaterialType = MaterialType.Shadow
        };
        
        // Theme 10: Divine Paradise (Levels 91-100)
        defaultThemes[9] = new ThemeData
        {
            name = "Divine Paradise",
            wallColor = new Color(0.95f, 0.9f, 0.8f),
            floorColor = new Color(0.85f, 0.8f, 0.7f),
            goalColor = new Color(1f, 0.95f, 0.6f),
            ambientLight = new Color(1f, 0.95f, 0.85f),
            fogColor = new Color(0.9f, 0.85f, 0.75f),
            wallMaterialType = MaterialType.Marble,
            floorMaterialType = MaterialType.Marble
        };
        
        return defaultThemes;
    }
    
    public void GenerateLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, maxLevels);
        
        // Clear existing maze
        ClearMaze();
        
        // Determine theme based on level (every 10 levels)
        int themeIndex = ((currentLevel - 1) / 10) % themes.Length;
        currentTheme = themes[themeIndex];
        
        // Calculate maze size based on level
        CalculateMazeDimensions(currentLevel);
        
        // Calculate complexity based on level
        float complexity = Mathf.Min(baseComplexity + (currentLevel * complexityPerLevel), maxComplexity);
        
        // Generate maze using recursive backtracking with complexity adjustment
        GenerateRecursiveBacktrack(complexity);
        
        // Build 3D maze from grid data
        BuildMaze3D();
        
        // Apply theme
        ApplyTheme(currentTheme);
        
        Debug.Log($"Generated Level {currentLevel} with theme: {currentTheme.name}, Complexity: {complexity:F2}");
    }
    
    void CalculateMazeDimensions(int level)
    {
        // Base size increases with level
        int baseSize = 8;
        int sizeIncrease = level / 5;
        int maxSize = 25 + (level / 10) * 5;
        
        mazeWidth = Mathf.Min(baseSize + sizeIncrease * 2, maxSize);
        mazeHeight = Mathf.Min(baseSize + sizeIncrease * 2, maxSize);
        
        // Ensure odd dimensions for proper maze generation
        if (mazeWidth % 2 == 0) mazeWidth++;
        if (mazeHeight % 2 == 0) mazeHeight++;
    }
    
    void GenerateRecursiveBacktrack(float complexity)
    {
        mazeGrid = new Cell[mazeWidth, mazeHeight];
        
        // Initialize all cells as walls
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                mazeGrid[x, y] = new Cell
                {
                    isWall = true,
                    visited = false
                };
            }
        }
        
        // Start from position (1, 1)
        Vector2Int start = new Vector2Int(1, 1);
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        
        mazeGrid[start.x, start.y].isWall = false;
        mazeGrid[start.x, start.y].visited = true;
        stack.Push(start);
        
        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            
            // Get unvisited neighbors
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current.x, current.y);
            
            if (neighbors.Count > 0)
            {
                // Choose random neighbor
                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                
                // Remove wall between current and next
                int wallX = (current.x + next.x) / 2;
                int wallY = (current.y + next.y) / 2;
                mazeGrid[wallX, wallY].isWall = false;
                mazeGrid[wallX, wallY].visited = true;
                
                // Mark next cell as passage
                mazeGrid[next.x, next.y].isWall = false;
                mazeGrid[next.x, next.y].visited = true;
                
                stack.Push(next);
            }
            else
            {
                stack.Pop();
            }
        }
        
        // Add additional passages based on complexity
        AddExtraPassages(complexity);
        
        // Set start and goal positions
        startPos = new Vector3Int(1, 0, 1);
        goalPos = new Vector3Int(mazeWidth - 2, 0, mazeHeight - 2);
        
        // Ensure goal is accessible
        mazeGrid[goalPos.x, goalPos.z].isWall = false;
    }
    
    List<Vector2Int> GetUnvisitedNeighbors(int x, int y)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        
        // Check cells 2 steps away (to maintain wall thickness)
        int[][] directions = new int[][]
        {
            new int[] { 0, -2 }, // Up
            new int[] { 0, 2 },  // Down
            new int[] { -2, 0 }, // Left
            new int[] { 2, 0 }   // Right
        };
        
        foreach (int[] dir in directions)
        {
            int nx = x + dir[0];
            int ny = y + dir[1];
            
            if (nx >= 0 && nx < mazeWidth && ny >= 0 && ny < mazeHeight)
            {
                if (!mazeGrid[nx, ny].visited)
                {
                    neighbors.Add(new Vector2Int(nx, ny));
                }
            }
        }
        
        return neighbors;
    }
    
    void AddExtraPassages(float complexity)
    {
        int extraPassages = Mathf.FloorToInt(complexity * (mazeWidth * mazeHeight) * 0.02f);
        
        for (int i = 0; i < extraPassages; i++)
        {
            int x = Random.Range(1, mazeWidth - 1);
            int z = Random.Range(1, mazeHeight - 1);
            
            // Don't remove start or goal
            if ((x == startPos.x && z == startPos.z) || (x == goalPos.x && z == goalPos.z))
                continue;
            
            // Randomly remove some walls to create loops and alternative paths
            if (mazeGrid[x, z].isWall)
            {
                mazeGrid[x, z].isWall = false;
            }
        }
    }
    
    void BuildMaze3D()
    {
        if (mazeParent == null)
        {
            GameObject parent = new GameObject("Maze");
            mazeParent = parent.transform;
        }
        
        float cellSize = 2f;
        float wallHeight = 1.5f;
        
        // Create floor
        CreateFloor(cellSize);
        
        // Create walls
        for (int x = 0; x < mazeWidth; x++)
        {
            for (int z = 0; z < mazeHeight; z++)
            {
                if (mazeGrid[x, z].isWall)
                {
                    Vector3 pos = new Vector3(
                        (x - mazeWidth / 2f) * cellSize,
                        0,
                        (z - mazeHeight / 2f) * cellSize
                    );
                    
                    CreateWall(pos, cellSize, wallHeight);
                }
            }
        }
        
        // Create start platform
        CreateStartPlatform();
        
        // Create goal
        CreateGoal();
    }
    
    void CreateFloor(float cellSize)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.parent = mazeParent;
        
        float floorWidth = mazeWidth * cellSize;
        float floorHeight = mazeHeight * cellSize;
        
        floor.transform.position = new Vector3(0, -0.01f, 0);
        floor.transform.localScale = new Vector3(floorWidth / 10f, 1f, floorHeight / 10f);
    }
    
    void CreateWall(Vector3 position, float width, float height)
    {
        if (wallPrefab != null)
        {
            GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity, mazeParent);
            wall.transform.localScale = new Vector3(width, height, width);
        }
        else
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.parent = mazeParent;
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(width, height, width);
        }
    }
    
    void CreateStartPlatform()
    {
        Vector3 spawnPos = new Vector3(
            (startPos.x - mazeWidth / 2f) * 2f,
            0.5f,
            (startPos.z - mazeHeight / 2f) * 2f
        );
        
        if (playerSpawnPoint == null)
        {
            GameObject spawnObj = new GameObject("PlayerSpawn");
            playerSpawnPoint = spawnObj.transform;
        }
        
        playerSpawnPoint.position = spawnPos;
    }
    
    void CreateGoal()
    {
        Vector3 goalPos3D = new Vector3(
            (goalPos.x - mazeWidth / 2f) * 2f,
            0.5f,
            (goalPos.z - mazeHeight / 2f) * 2f
        );
        
        if (goalObject == null)
        {
            if (goalPrefab != null)
            {
                goalObject = Instantiate(goalPrefab, goalPos3D, Quaternion.identity, mazeParent).transform;
            }
            else
            {
                GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                goal.name = "Goal";
                goal.transform.parent = mazeParent;
                goal.transform.position = goalPos3D;
                goal.transform.localScale = Vector3.one * 0.8f;
                goalObject = goal.transform;
            }
        }
        else
        {
            goalObject.position = goalPos3D;
        }
    }
    
    void ApplyTheme(ThemeData theme)
    {
        // Apply lighting
        RenderSettings.ambientLight = theme.ambientLight;
        RenderSettings.fogColor = theme.fogColor;
        RenderSettings.fog = true;
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        
        // Apply materials to maze objects
        foreach (Transform child in mazeParent)
        {
            if (child.name == "Floor")
            {
                ApplyMaterialToObject(child.gameObject, theme.floorColor, theme.floorMaterialType);
            }
            else if (child.name == "Wall")
            {
                ApplyMaterialToObject(child.gameObject, theme.wallColor, theme.wallMaterialType);
            }
            else if (child.name == "Goal")
            {
                ApplyMaterialToObject(child.gameObject, theme.goalColor, MaterialType.Glowing);
            }
        }
    }
    
    void ApplyMaterialToObject(GameObject obj, Color color, MaterialType materialType)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            
            switch (materialType)
            {
                case MaterialType.Metal:
                    mat.SetFloat("_Metallic", 0.8f);
                    mat.SetFloat("_Smoothness", 0.6f);
                    break;
                case MaterialType.Glowing:
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                    mat.SetColor("_EmissionColor", color * 2f);
                    break;
                case MaterialType.Ice:
                    mat.SetFloat("_Metallic", 0.3f);
                    mat.SetFloat("_Smoothness", 0.9f);
                    mat.color = new Color(color.r, color.g, color.b, 0.7f);
                    break;
                case MaterialType.Crystal:
                    mat.SetFloat("_Metallic", 0.5f);
                    mat.SetFloat("_Smoothness", 0.8f);
                    break;
                case MaterialType.Stone:
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Smoothness", 0.3f);
                    break;
                case MaterialType.Wood:
                    mat.SetFloat("_Metallic", 0f);
                    mat.SetFloat("_Smoothness", 0.4f);
                    break;
                case MaterialType.Technology:
                    mat.SetFloat("_Metallic", 0.7f);
                    mat.SetFloat("_Smoothness", 0.7f);
                    break;
                default:
                    mat.SetFloat("_Metallic", 0.5f);
                    mat.SetFloat("_Smoothness", 0.5f);
                    break;
            }
            
            renderer.material = mat;
        }
    }
    
    void ClearMaze()
    {
        if (mazeParent != null)
        {
            #if UNITY_EDITOR
            if (Application.isEditor)
            {
                DestroyImmediate(mazeParent.gameObject);
            }
            else
            #endif
            {
                Destroy(mazeParent.gameObject);
            }
        }
        
        mazeParent = null;
        walls.Clear();
    }
    
    public int GetThemeIndex()
    {
        return ((currentLevel - 1) / 10) % themes.Length;
    }
    
    public ThemeData GetCurrentTheme()
    {
        return currentTheme;
    }
    
    public Vector3 GetPlayerSpawnPosition()
    {
        if (playerSpawnPoint != null)
            return playerSpawnPoint.position;
        return Vector3.zero;
    }
    
    public Transform GetGoalTransform()
    {
        return goalObject;
    }
    
    // Cell structure for maze generation
    struct Cell
    {
        public bool isWall;
        public bool visited;
    }
}

[System.Serializable]
public class ThemeData
{
    public string name;
    public Color wallColor;
    public Color floorColor;
    public Color goalColor;
    public Color ambientLight;
    public Color fogColor;
    public MaterialType wallMaterialType;
    public MaterialType floorMaterialType;
}

public enum MaterialType
{
    Metal,
    Glowing,
    Stone,
    Ice,
    Crystal,
    Wood,
    Grass,
    Technology,
    Shadow,
    Marble,
    Concrete,
    Grid,
    Grates
}
