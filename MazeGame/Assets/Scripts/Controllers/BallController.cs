using UnityEngine;

namespace GyroMaze.Controllers
{
    /// <summary>
    /// Professional-grade ball controller with gyro input, physics-based movement,
    /// trail effects, and mobile-optimized controls.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [Header("Physics Settings")]
        [SerializeField] private float moveSpeed = 15f;
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private PhysicsMaterial ballPhysicsMaterial;
        
        [Header("Gyro Settings")]
        [SerializeField] private bool useGyro = true;
        [SerializeField] private float gyroSensitivity = 1.5f;
        [SerializeField] private Vector3 gyroOffset = Vector3.zero;
        [SerializeField] private bool autoCalibrateOnStart = true;
        
        [Header("Visual Effects")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem moveParticles;
        [SerializeField] private Light ballLight;
        [SerializeField] private Material metallicMaterial;
        
        [Header("Audio")]
        [SerializeField] private AudioClip rollSound;
        [SerializeField] private AudioClip bumpSound;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;
        
        // Components
        private Rigidbody rb;
        private AudioSource audioSource;
        private Camera mainCamera;
        
        // State
        private Vector3 currentInput = Vector3.zero;
        private bool controlsEnabled = true;
        private bool isGrounded = true;
        private Quaternion gyroBaseRotation;
        private bool gyroInitialized = false;
        
        // Properties
        public bool ControlsEnabled 
        { 
            get => controlsEnabled; 
            set => controlsEnabled = value;
        }
        
        public Vector3 Velocity => rb.velocity;
        public bool IsGrounded => isGrounded;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();
            
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            ConfigureRigidbody();
            InitializeGyro();
        }
        
        private void Start()
        {
            mainCamera = Camera.main;
            
            if (autoCalibrateOnStart && useGyro && SystemInfo.supportsGyroscope)
            {
                CalibrateGyro();
            }
            
            SetupVisualEffects();
        }
        
        /// <summary>
        /// Configure rigidbody for smooth ball movement
        /// </summary>
        private void ConfigureRigidbody()
        {
            rb.mass = 1f;
            rb.drag = 0.1f;
            rb.angularDrag = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            
            if (ballPhysicsMaterial != null)
            {
                GetComponent<Collider>().material = ballPhysicsMaterial;
            }
        }
        
        /// <summary>
        /// Initialize gyroscope if available
        /// </summary>
        private void InitializeGyro()
        {
            if (useGyro && SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
                Input.gyro.updateInterval = 1f / 60f; // 60Hz
                gyroInitialized = true;
                Debug.Log("[BallController] Gyroscope initialized");
            }
            else
            {
                useGyro = false;
                Debug.Log("[BallController] Gyroscope not available, using keyboard/touch fallback");
            }
        }
        
        /// <summary>
        /// Calibrate gyroscope to current device orientation
        /// </summary>
        public void CalibrateGyro()
        {
            if (!gyroInitialized) return;
            
            // Store current rotation as base reference
            gyroBaseRotation = Quaternion.Euler(gyroOffset);
            Debug.Log("[BallController] Gyroscope calibrated");
        }
        
        /// <summary>
        /// Setup visual effects based on current theme
        /// </summary>
        private void SetupVisualEffects()
        {
            if (trailRenderer == null)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                ConfigureTrailRenderer();
            }
            
            if (ballLight == null)
            {
                ballLight = gameObject.AddComponent<Light>();
                ballLight.type = LightType.Point;
                ballLight.range = 5f;
                ballLight.intensity = 1f;
            }
        }
        
        private void ConfigureTrailRenderer()
        {
            trailRenderer.time = 0.5f;
            trailRenderer.startWidth = 0.3f;
            trailRenderer.endWidth = 0.1f;
            trailRenderer.autodestruct = false;
            trailRenderer.minVertexDistance = 0.1f;
        }
        
        private void Update()
        {
            HandleInput();
            CheckGrounded();
            
            // Keyboard calibration for testing
            if (Input.GetKeyDown(KeyCode.C))
            {
                CalibrateGyro();
            }
        }
        
        private void FixedUpdate()
        {
            if (!controlsEnabled) return;
            
            ApplyMovement();
            ApplyRotation();
            ClampVelocity();
        }
        
        /// <summary>
        /// Process input from gyro, keyboard, or touch
        /// </summary>
        private void HandleInput()
        {
            if (!controlsEnabled)
            {
                currentInput = Vector3.zero;
                return;
            }
            
            if (useGyro && gyroInitialized)
            {
                HandleGyroInput();
            }
            else
            {
                HandleKeyboardInput();
            }
        }
        
        /// <summary>
        /// Process gyroscope input with camera-relative mapping
        /// </summary>
        private void HandleGyroInput()
        {
            // Get gyro rotation relative to base calibration
            Quaternion gyroRot = Input.gyro.attitude;
            gyroRot = new Quaternion(gyroRot.x, gyroRot.y, -gyroRot.z, -gyroRot.w) * gyroBaseRotation;
            
            // Extract tilt angles
            Vector3 euler = gyroRot.eulerAngles;
            
            // Remap to -180 to 180 range
            float tiltX = euler.x > 180 ? euler.x - 360 : euler.x;
            float tiltZ = euler.z > 180 ? euler.z - 360 : euler.z;
            
            // Normalize and apply sensitivity
            float inputX = Mathf.Clamp(tiltX * gyroSensitivity / 45f, -1f, 1f);
            float inputZ = Mathf.Clamp(tiltZ * gyroSensitivity / 45f, -1f, 1f);
            
            // Convert to camera-relative direction
            if (mainCamera != null)
            {
                Vector3 forward = mainCamera.transform.forward;
                Vector3 right = mainCamera.transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();
                
                currentInput = (forward * -inputX) + (right * inputZ);
            }
            else
            {
                currentInput = new Vector3(inputZ, 0, inputX);
            }
        }
        
        /// <summary>
        /// Fallback keyboard input for editor testing
        /// </summary>
        private void HandleKeyboardInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            if (mainCamera != null)
            {
                Vector3 forward = mainCamera.transform.forward;
                Vector3 right = mainCamera.transform.right;
                forward.y = 0;
                right.y = 0;
                forward.Normalize();
                right.Normalize();
                
                currentInput = (forward * -vertical) + (right * horizontal);
            }
            else
            {
                currentInput = new Vector3(horizontal, 0, vertical);
            }
        }
        
        /// <summary>
        /// Apply movement force to rigidbody
        /// </summary>
        private void ApplyMovement()
        {
            if (currentInput.magnitude > 0.01f)
            {
                Vector3 moveDirection = currentInput.normalized;
                rb.AddForce(moveDirection * moveSpeed, ForceMode.Acceleration);
                
                // Play move particles if enabled
                if (moveParticles != null && !moveParticles.isPlaying)
                {
                    moveParticles.Play();
                }
            }
            else if (moveParticles != null && moveParticles.isPlaying)
            {
                moveParticles.Stop();
            }
        }
        
        /// <summary>
        /// Rotate ball visually based on movement direction
        /// </summary>
        private void ApplyRotation()
        {
            if (rb.velocity.magnitude > 0.01f)
            {
                Vector3 rotationAxis = Vector3.Cross(rb.velocity.normalized, Vector3.up);
                rb.MoveRotation(rb.rotation * Quaternion.AngleAxis(rb.velocity.magnitude * rotationSpeed * Time.fixedDeltaTime, rotationAxis));
            }
        }
        
        /// <summary>
        /// Clamp velocity to maximum speed
        /// </summary>
        private void ClampVelocity()
        {
            if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }
        
        /// <summary>
        /// Check if ball is in contact with ground
        /// </summary>
        private void CheckGrounded()
        {
            RaycastHit hit;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out hit, 0.6f);
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > 2f && bumpSound != null)
            {
                audioSource.PlayOneShot(bumpSound, volume * 0.5f);
            }
        }
        
        /// <summary>
        /// Reset ball to starting position
        /// </summary>
        public void ResetBall()
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = Vector3.zero; // Will be set by MazeGenerator
            transform.rotation = Quaternion.identity;
            
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }
        
        /// <summary>
        /// Enable or disable player controls
        /// </summary>
        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;
            if (!enabled)
            {
                currentInput = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Update ball appearance based on theme
        /// </summary>
        public void UpdateBallAppearance(Material newMaterial, Color lightColor)
        {
            if (metallicMaterial != null && newMaterial != null)
            {
                metallicMaterial = newMaterial;
                GetComponent<Renderer>().material = metallicMaterial;
            }
            
            if (ballLight != null)
            {
                ballLight.color = lightColor;
            }
        }
        
        #region Editor Helpers
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (rb != null)
            {
                ConfigureRigidbody();
            }
        }
        
        [ContextMenu("Calibrate Gyro")]
        private void EditorCalibrateGyro()
        {
            CalibrateGyro();
        }
        #endif
        
        #endregion
    }
}
