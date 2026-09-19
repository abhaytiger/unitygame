using UnityEngine;

/// <summary>
/// Mobile input handler for gyro and touch controls
/// Provides calibration and sensitivity adjustments
/// Attach to an InputManager object in the scene
/// </summary>
public class MobileInputHandler : MonoBehaviour
{
    [Header("Gyro Settings")]
    [Tooltip("Enable gyroscope input")]
    public bool enableGyro = true;
    
    [Tooltip("Gyro sensitivity multiplier")]
    public float gyroSensitivity = 1.5f;
    
    [Tooltip("Invert X axis")]
    public bool invertX = false;
    
    [Tooltip("Invert Y axis")]
    public bool invertY = false;
    
    [Header("Calibration")]
    [Tooltip("Manual calibration offset")]
    public Vector2 calibrationOffset = Vector2.zero;
    
    [Tooltip("Auto-calibrate on start")]
    public bool autoCalibrateOnStart = true;
    
    [Header("Touch Controls")]
    [Tooltip("Enable touch joystick overlay")]
    public bool enableTouchJoystick = true;
    
    [Tooltip("Joystick size")]
    public float joystickSize = 100f;
    
    [Tooltip("Joystick position (bottom-left corner)")]
    public Vector2 joystickPosition = new Vector2(150f, 150f);
    
    [Header("Debug")]
    [Tooltip("Show debug info on screen")]
    public bool showDebugInfo = false;
    
    private bool gyroAvailable = false;
    private Vector3 currentGyroInput;
    private Vector2 touchInput;
    private bool isTouching = false;
    private Vector2 touchStartPosition;
    
    // Touch joystick UI
    private Texture2D joystickTexture;
    private Texture2D knobTexture;
    private Rect joystickRect;
    private Rect knobRect;
    private bool joystickInitialized = false;
    
    void Start()
    {
        InitializeGyro();
        InitializeTouchJoystick();
    }
    
    void InitializeGyro()
    {
        if (!enableGyro)
        {
            Debug.Log("Gyro disabled by settings");
            return;
        }
        
        #if UNITY_ANDROID || UNITY_IOS
        try
        {
            Input.gyro.enabled = true;
            gyroAvailable = true;
            Debug.Log("Gyroscope initialized successfully");
            
            if (autoCalibrateOnStart)
            {
                CalibrateGyro();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Gyroscope not available: {e.Message}");
            gyroAvailable = false;
        }
        #else
        Debug.Log("Gyroscope only available on mobile devices - using keyboard fallback");
        gyroAvailable = false;
        #endif
    }
    
    void InitializeTouchJoystick()
    {
        if (!enableTouchJoystick)
            return;
        
        // Create joystick textures programmatically
        CreateJoystickTextures();
        
        // Set up joystick rectangles
        joystickRect = new Rect(
            joystickPosition.x,
            Screen.height - joystickPosition.y - joystickSize,
            joystickSize,
            joystickSize
        );
        
        knobRect = new Rect(
            joystickPosition.x + joystickSize / 2 - 25,
            Screen.height - joystickPosition.y - joystickSize / 2 - 25,
            50,
            50
        );
        
        joystickInitialized = true;
    }
    
    void CreateJoystickTextures()
    {
        // Create semi-transparent circular joystick background
        joystickTexture = new Texture2D((int)joystickSize, (int)joystickSize);
        Color[] pixels = new Color[joystickTexture.width * joystickTexture.height];
        
        float centerX = joystickTexture.width / 2f;
        float centerY = joystickTexture.height / 2f;
        float radius = joystickSize / 2f;
        
        for (int y = 0; y < joystickTexture.height; y++)
        {
            for (int x = 0; x < joystickTexture.width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist <= radius)
                {
                    float alpha = 1f - (dist / radius) * 0.5f;
                    pixels[y * joystickTexture.width + x] = new Color(0.3f, 0.3f, 0.3f, alpha * 0.5f);
                }
                else
                {
                    pixels[y * joystickTexture.width + x] = Color.clear;
                }
            }
        }
        
        joystickTexture.SetPixels(pixels);
        joystickTexture.Apply();
        
        // Create knob texture
        knobTexture = new Texture2D(50, 50);
        pixels = new Color[knobTexture.width * knobTexture.height];
        
        centerX = knobTexture.width / 2f;
        centerY = knobTexture.height / 2f;
        radius = 25f;
        
        for (int y = 0; y < knobTexture.height; y++)
        {
            for (int x = 0; x < knobTexture.width; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (dist <= radius)
                {
                    float alpha = 1f - (dist / radius) * 0.3f;
                    pixels[y * knobTexture.width + x] = new Color(0.6f, 0.6f, 0.8f, alpha);
                }
                else
                {
                    pixels[y * knobTexture.width + x] = Color.clear;
                }
            }
        }
        
        knobTexture.SetPixels(pixels);
        knobTexture.Apply();
    }
    
    void CalibrateGyro()
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (gyroAvailable && Input.gyro.enabled)
        {
            // Get current gravity vector as calibration point
            Vector3 gravity = Input.gyro.gravity;
            calibrationOffset.x = gravity.x;
            calibrationOffset.y = gravity.y;
            Debug.Log($"Gyro calibrated: offset = {calibrationOffset}");
        }
        #endif
    }
    
    void Update()
    {
        ReadGyroInput();
        ReadTouchInput();
        
        // Calibration shortcut
        if (Input.GetKeyDown(KeyCode.C))
        {
            CalibrateGyro();
        }
    }
    
    void ReadGyroInput()
    {
        currentGyroInput = Vector3.zero;
        
        #if UNITY_ANDROID || UNITY_IOS
        if (gyroAvailable && Input.gyro.enabled)
        {
            // Get gravity vector (points down)
            Vector3 gravity = Input.gyro.gravity;
            
            // Apply calibration offset
            gravity.x -= calibrationOffset.x;
            gravity.y -= calibrationOffset.y;
            
            // Apply inversion
            if (invertX) gravity.x = -gravity.x;
            if (invertY) gravity.y = -gravity.y;
            
            // Convert to input direction
            // For landscape left orientation (home button on right)
            currentGyroInput.x = gravity.y * gyroSensitivity;
            currentGyroInput.z = -gravity.x * gyroSensitivity;
            
            // Clamp input
            currentGyroInput = Vector3.ClampMagnitude(currentGyroInput, 1f);
        }
        #endif
    }
    
    void ReadTouchInput()
    {
        touchInput = Vector2.zero;
        isTouching = false;
        
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                // Check if touch is in joystick area
                if (IsTouchInJoystick(touch.position))
                {
                    isTouching = true;
                    
                    if (touch.phase == TouchPhase.Began)
                    {
                        touchStartPosition = touch.position;
                    }
                    else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        Vector2 delta = touch.position - touchStartPosition;
                        delta = Vector2.ClampMagnitude(delta, joystickSize / 2f);
                        
                        touchInput = delta / (joystickSize / 2f);
                        
                        // Update knob position
                        UpdateKnobPosition(delta);
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        ResetJoystick();
                    }
                    
                    break; // Only process first touch in joystick area
                }
            }
        }
        else
        {
            ResetJoystick();
        }
    }
    
    bool IsTouchInJoystick(Vector2 touchPos)
    {
        Vector2 joystickCenter = new Vector2(
            joystickPosition.x + joystickSize / 2f,
            Screen.height - joystickPosition.y - joystickSize / 2f
        );
        
        float dist = Vector2.Distance(touchPos, joystickCenter);
        return dist <= joystickSize / 2f;
    }
    
    void UpdateKnobPosition(Vector2 delta)
    {
        knobRect.x = joystickPosition.x + joystickSize / 2 - 25 + delta.x;
        knobRect.y = Screen.height - joystickPosition.y - joystickSize / 2 - 25 - delta.y;
    }
    
    void ResetJoystick()
    {
        touchInput = Vector2.zero;
        knobRect.x = joystickPosition.x + joystickSize / 2 - 25;
        knobRect.y = Screen.height - joystickPosition.y - joystickSize / 2 - 25;
    }
    
    void OnGUI()
    {
        if (showDebugInfo)
        {
            DrawDebugInfo();
        }
        
        if (enableTouchJoystick && joystickInitialized)
        {
            // Draw joystick
            GUI.DrawTexture(joystickRect, joystickTexture);
            GUI.DrawTexture(knobRect, knobTexture);
        }
    }
    
    void DrawDebugInfo()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label($"Gyro Available: {gyroAvailable}", style);
        GUILayout.Label($"Gyro Input: {currentGyroInput:F3}", style);
        GUILayout.Label($"Touch Input: {touchInput:F3}", style);
        GUILayout.Label($"FPS: {1f / Time.deltaTime:F1}", style);
        GUILayout.EndArea();
    }
    
    public Vector3 GetGyroInput()
    {
        return currentGyroInput;
    }
    
    public Vector2 GetTouchInput()
    {
        return touchInput;
    }
    
    public bool IsGyroAvailable()
    {
        return gyroAvailable;
    }
    
    public void Recalibrate()
    {
        CalibrateGyro();
    }
    
    void OnDestroy()
    {
        #if UNITY_ANDROID || UNITY_IOS
        if (Input.gyro.enabled)
        {
            Input.gyro.enabled = false;
        }
        #endif
        
        if (joystickTexture != null)
            Destroy(joystickTexture);
        
        if (knobTexture != null)
            Destroy(knobTexture);
    }
}
