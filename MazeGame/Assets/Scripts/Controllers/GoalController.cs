using UnityEngine;

namespace GyroMaze.Controllers
{
    /// <summary>
    /// Goal controller with animated effects, particle systems,
    /// and win detection for level completion.
    /// </summary>
    public class GoalController : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.1f;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float floatHeight = 0.3f;
        
        [Header("Particle Effects")]
        [SerializeField] private ParticleSystem winParticles;
        [SerializeField] private ParticleSystem idleParticles;
        
        [Header("Audio")]
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private AudioClip ambientSound;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;
        
        [Header("Light Effects")]
        [SerializeField] private Light goalLight;
        [SerializeField] private float lightPulseIntensity = 0.5f;
        
        // Components
        private AudioSource audioSource;
        private Renderer goalRenderer;
        private Vector3 originalPosition;
        private bool isCollected = false;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            goalRenderer = GetComponent<Renderer>();
            
            // Setup light if not assigned
            if (goalLight == null)
            {
                goalLight = GetComponent<Light>();
                if (goalLight == null)
                {
                    goalLight = gameObject.AddComponent<Light>();
                    goalLight.type = LightType.Point;
                    goalLight.range = 8f;
                    goalLight.intensity = 2f;
                }
            }
        }
        
        private void Start()
        {
            originalPosition = transform.position;
            
            // Ensure collider is set as trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
                sphereCol.isTrigger = true;
            }
            
            // Play ambient sound if available
            if (ambientSound != null && audioSource != null)
            {
                audioSource.clip = ambientSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void SubscribeToEvents()
        {
            GameManager.OnLevelChanged += ResetGoal;
            GameManager.OnLevelComplete += OnLevelComplete;
        }
        
        private void UnsubscribeFromEvents()
        {
            GameManager.OnLevelChanged -= ResetGoal;
            GameManager.OnLevelComplete -= OnLevelComplete;
        }
        
        private void Update()
        {
            if (isCollected) return;
            
            AnimateGoal();
            UpdateLightEffect();
        }
        
        /// <summary>
        /// Animate goal with pulse, rotation, and floating effects
        /// </summary>
        private void AnimateGoal()
        {
            // Pulse scale
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = Vector3.one * (1f + pulse);
            
            // Rotate
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // Float up and down
            float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = originalPosition + Vector3.up * floatOffset;
        }
        
        /// <summary>
        /// Update light intensity with pulsing effect
        /// </summary>
        private void UpdateLightEffect()
        {
            if (goalLight != null)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed * 1.5f) * lightPulseIntensity;
                goalLight.intensity = 1.5f + pulse;
            }
        }
        
        /// <summary>
        /// Detect when player ball enters goal
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;
            
            if (other.CompareTag("Player") || other.GetComponent<BallController>() != null)
            {
                CollectGoal();
            }
        }
        
        /// <summary>
        /// Handle goal collection - trigger level complete
        /// </summary>
        private void CollectGoal()
        {
            isCollected = true;
            
            Debug.Log("[GoalController] Goal collected!");
            
            // Play collection sound
            if (collectSound != null && audioSource != null)
            {
                audioSource.Stop(); // Stop ambient
                audioSource.PlayOneShot(collectSound, volume);
            }
            
            // Play win particles
            if (winParticles != null)
            {
                winParticles.Play();
            }
            
            // Stop idle particles
            if (idleParticles != null && idleParticles.isPlaying)
            {
                idleParticles.Stop();
            }
            
            // Notify game manager
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gm.CompleteLevel();
            }
        }
        
        /// <summary>
        /// Reset goal state for new level
        /// </summary>
        private void ResetGoal(int level)
        {
            isCollected = false;
            transform.position = originalPosition;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            
            // Restart ambient sound
            if (ambientSound != null && audioSource != null)
            {
                audioSource.clip = ambientSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            // Reset particles
            if (winParticles != null && winParticles.isPlaying)
            {
                winParticles.Stop();
            }
            
            if (idleParticles != null)
            {
                idleParticles.Play();
            }
        }
        
        /// <summary>
        /// Handle level complete event
        /// </summary>
        private void OnLevelComplete()
        {
            // Optional: Additional effects on level complete
            if (winParticles != null && !winParticles.isPlaying)
            {
                winParticles.Play();
            }
        }
        
        /// <summary>
        /// Set goal appearance based on theme
        /// </summary>
        public void SetGoalMaterial(Material material, Color lightColor)
        {
            if (goalRenderer != null && material != null)
            {
                goalRenderer.material = material;
            }
            
            if (goalLight != null)
            {
                goalLight.color = lightColor;
            }
        }
        
        #region Editor Helpers
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            if (goalLight != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, Vector3.up * 2f);
            }
        }
        
        [ContextMenu("Test Collection")]
        private void TestCollectGoal()
        {
            CollectGoal();
        }
        
        [ContextMenu("Reset Goal")]
        private void TestResetGoal()
        {
            ResetGoal(1);
        }
        #endif
        
        #endregion
    }
}
