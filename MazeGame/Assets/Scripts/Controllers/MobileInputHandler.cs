using UnityEngine;

namespace GyroMaze.Controllers
{
    /// <summary>
    /// Mobile input handler with gyroscope calibration, touch joystick fallback,
    /// and haptic feedback for professional mobile experience.
    /// </summary>
    public class MobileInputHandler : MonoBehaviour
    {
        [Header("Gyroscope Settings")]
        [SerializeField] private bool enableGyro = true;
        [SerializeField] private float gyroSensitivity = 1.5f;
        [SerializeField] private Vector3 gyroCalibrationOffset = Vector3.zero;
        
        [Header("Touch Joystick Settings")]
        [SerializeField] private bool enableTouchJoystick = true;
        [SerializeField] private float joystickRadius = 100f;
        [SerializeField] private Vector2 joystickPosition = new Vector2(150, 150);
        [SerializeField] private Color joystickColor = new Color(1f, 1f, 1f, 0.3f);
        [SerializeField] private Color joystickHandleColor = new Color(1f, 1f, 1f, 0.6f);
        
        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private HapticIntensity hapticIntensity = HapticIntensity.Medium;
        
        [Header("UI References")]
        [SerializeField] private RectTransform calibrationButton;
        [SerializeField] private UnityEngine.UI.Text statusText;
        
        // State
        private bool gyroAvailable = false;
        private Quaternion gyroBaseRotation;
        private Vector2 touchStartPos;
        private Vector2 currentTouchDelta;
        private bool isTouching = false;
        private int activeTouchId = -1;
        
        // Properties
        public Vector3 CurrentInput { get; private set; } = Vector3.zero;
        public bool IsGyroEnabled => gyroAvailable && enableGyro;
        public bool IsTouchActive => isTouching;
        
        public enum HapticIntensity
        {
            Light,
            Medium,
            Heavy
        }
        
        private void Start()
        {
            InitializeGyroscope();
            
            if (enableTouchJoystick)
            {
                Debug.Log("[MobileInput] Touch joystick enabled");
            }
            
            // Subscribe to calibration button if exists
            if (calibrationButton != null)
            {
                UnityEngine.UI.Button btn = calibrationButton.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(CalibrateGyro);
                }
            }
        }
        
        private void Update()
        {
            ProcessInput();
            UpdateUI();
        }
        
        /// <summary>
        /// Initialize gyroscope if available on device
        /// </summary>
        private void InitializeGyroscope()
        {
            if (enableGyro && SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                Input.gyro.updateInterval = 1f / 60f; // 60Hz update rate
                gyroAvailable = true;
                
                Debug.Log("[MobileInput] Gyroscope initialized successfully");
                
                // Auto-calibrate after short delay
                Invoke(nameof(CalibrateGyro), 0.5f);
            }
            else
            {
                gyroAvailable = false;
                enableGyro = false;
                Debug.LogWarning("[MobileInput] Gyroscope not available on this device");
            }
        }
        
        /// <summary>
        /// Calibrate gyroscope to current device orientation
        /// </summary>
        public void CalibrateGyro()
        {
            if (!gyroAvailable) return;
            
            gyroBaseRotation = Input.gyro.attitude;
            gyroBaseRotation = new Quaternion(gyroBaseRotation.x, gyroBaseRotation.y, -gyroBaseRotation.z, -gyroBaseRotation.w);
            
            // Apply offset
            gyroBaseRotation *= Quaternion.Euler(gyroCalibrationOffset);
            
            Debug.Log("[MobileInput] Gyroscope calibrated");
            
            // Haptic feedback on calibration
            TriggerHaptic(HapticIntensity.Light);
            
            UpdateStatusText("Gyro Calibrated");
        }
        
        /// <summary>
        /// Process all input sources (gyro, touch, keyboard)
        /// </summary>
        private void ProcessInput()
        {
            CurrentInput = Vector3.zero;
            
            // Priority: Gyro > Touch > Keyboard
            if (gyroAvailable && enableGyro)
            {
                ProcessGyroInput();
            }
            else if (enableTouchJoystick)
            {
                ProcessTouchInput();
            }
            else
            {
                ProcessKeyboardInput();
            }
        }
        
        /// <summary>
        /// Process gyroscope input
        /// </summary>
        private void ProcessGyroInput()
        {
            Quaternion gyroRot = Input.gyro.attitude;
            gyroRot = new Quaternion(gyroRot.x, gyroRot.y, -gyroRot.z, -gyroRot.w);
            
            // Calculate rotation from base
            Quaternion deltaRot = gyroRot * Quaternion.Inverse(gyroBaseRotation);
            
            // Extract tilt angles
            Vector3 euler = deltaRot.eulerAngles;
            
            // Normalize to -180 to 180 range
            float tiltX = euler.x > 180 ? euler.x - 360 : euler.x;
            float tiltZ = euler.z > 180 ? euler.z - 360 : euler.z;
            
            // Apply sensitivity and clamp
            float inputX = Mathf.Clamp(tiltX * gyroSensitivity / 45f, -1f, 1f);
            float inputZ = Mathf.Clamp(tiltZ * gyroSensitivity / 45f, -1f, 1f);
            
            CurrentInput = new Vector3(inputX, 0, inputZ);
        }
        
        /// <summary>
        /// Process touch joystick input
        /// </summary>
        private void ProcessTouchInput()
        {
            if (Input.touchCount == 0)
            {
                isTouching = false;
                activeTouchId = -1;
                currentTouchDelta = Vector2.zero;
                return;
            }
            
            // Find or start new touch
            if (activeTouchId == -1)
            {
                // Look for touch in joystick area
                foreach (Touch touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        Vector2 touchPos = touch.position;
                        float distanceToJoystick = Vector2.Distance(touchPos, joystickPosition);
                        
                        if (distanceToJoystick <= joystickRadius * 1.5f)
                        {
                            activeTouchId = touch.fingerId;
                            touchStartPos = touchPos;
                            isTouching = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                // Track existing touch
                foreach (Touch touch in Input.touches)
                {
                    if (touch.fingerId == activeTouchId)
                    {
                        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            activeTouchId = -1;
                            isTouching = false;
                            currentTouchDelta = Vector2.zero;
                        }
                        else
                        {
                            Vector2 delta = touch.position - touchStartPos;
                            
                            // Clamp to joystick radius
                            if (delta.magnitude > joystickRadius)
                            {
                                delta = delta.normalized * joystickRadius;
                            }
                            
                            currentTouchDelta = delta;
                            
                            // Convert to input vector (-1 to 1)
                            CurrentInput = new Vector3(
                                delta.x / joystickRadius,
                                0,
                                delta.y / joystickRadius
                            );
                        }
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Fallback keyboard input for editor testing
        /// </summary>
        private void ProcessKeyboardInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            CurrentInput = new Vector3(horizontal, 0, vertical);
        }
        
        /// <summary>
        /// Trigger haptic feedback
        /// </summary>
        public void TriggerHaptic(HapticIntensity intensity)
        {
            if (!enableHaptics) return;
            
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            
            // For more advanced haptics, use native plugins
            // This is a basic implementation
            #endif
            
            Debug.Log($"[MobileInput] Haptic feedback: {intensity}");
        }
        
        /// <summary>
        /// Update UI elements
        /// </summary>
        private void UpdateUI()
        {
            if (statusText != null)
            {
                string gyroStatus = gyroAvailable ? "Gyro: ON" : "Gyro: OFF";
                string mode = enableGyro ? "Tilt" : "Touch";
                statusText.text = $"{gyroStatus} | Mode: {mode}";
            }
        }
        
        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
                Invoke(nameof(ClearStatusText), 2f);
            }
        }
        
        private void ClearStatusText()
        {
            if (statusText != null)
            {
                UpdateUI();
            }
        }
        
        /// <summary>
        /// Get normalized input vector for ball movement
        /// </summary>
        public Vector3 GetNormalizedInput()
        {
            return CurrentInput.normalized * Mathf.Min(CurrentInput.magnitude, 1f);
        }
        
        /// <summary>
        /// Toggle between gyro and touch mode
        /// </summary>
        public void ToggleInputMode()
        {
            if (gyroAvailable)
            {
                enableGyro = !enableGyro;
                Debug.Log($"[MobileInput] Input mode: {(enableGyro ? "Gyro" : "Touch")}");
                UpdateStatusText(enableGyro ? "Gyro Mode" : "Touch Mode");
                TriggerHaptic(HapticIntensity.Light);
            }
        }
        
        #region Editor Helpers
        
        #if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            
            // Draw debug info
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"Gyro Available: {gyroAvailable}");
            GUILayout.Label($"Gyro Enabled: {enableGyro}");
            GUILayout.Label($"Input: {CurrentInput}");
            GUILayout.Label($"Touch Active: {isTouching}");
            GUILayout.EndArea();
            
            // Draw virtual joystick preview
            if (enableTouchJoystick)
            {
                Handles.color = joystickColor;
                Handles.DrawSolidDisc(joystickPosition, Vector3.forward, joystickRadius);
                
                if (isTouching)
                {
                    Handles.color = joystickHandleColor;
                    Handles.DrawSolidDisc(joystickPosition + currentTouchDelta, Vector3.forward, 30f);
                }
            }
        }
        
        [ContextMenu("Calibrate Gyro")]
        private void EditorCalibrateGyro()
        {
            CalibrateGyro();
        }
        
        [ContextMenu("Toggle Input Mode")]
        private void EditorToggleMode()
        {
            ToggleInputMode();
        }
        #endif
        
        #endregion
    }
}
