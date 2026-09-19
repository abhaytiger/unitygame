using UnityEngine;

/// <summary>
/// Controls the metal ball movement using device gyroscope/accelerometer
/// Attach this script to the player ball object
/// </summary>
public class BallController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed multiplier for ball movement")]
    public float moveSpeed = 15f;
    
    [Tooltip("Maximum velocity clamp to prevent tunneling")]
    public float maxVelocity = 20f;
    
    [Tooltip("Rolling friction coefficient")]
    public float friction = 0.98f;
    
    [Header("Gyro Settings")]
    [Tooltip("Sensitivity multiplier for gyro input")]
    public float gyroSensitivity = 1.5f;
    
    [Tooltip("Enable gyro controls (disable for editor testing with keyboard)")]
    public bool useGyro = true;
    
    [Header("Visual Effects")]
    [Tooltip("Material for the ball trail")]
    public Material trailMaterial;
    
    [Tooltip("Particle system for ball effects")]
    public ParticleSystem moveParticles;
    
    private Rigidbody rb;
    private Vector3 currentInput;
    private TrailRenderer trail;
    private bool gyroInitialized = false;
    
    // Keyboard fallback for testing in editor
    private Vector3 keyboardInput;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for rolling ball behavior
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // Add sphere collider if not present
        if (GetComponent<SphereCollider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = transform.localScale.x * 0.4f;
            collider.material = CreatePhysicsMaterial();
        }
        
        // Create trail renderer for visual effect
        CreateTrailEffect();
        
        // Initialize gyroscope
        if (useGyro && Application.isMobilePlatform)
        {
            InitializeGyro();
        }
    }
    
    void InitializeGyro()
    {
        Input.gyro.enabled = true;
        gyroInitialized = true;
        Debug.Log("Gyroscope initialized");
    }
    
    void CreateTrailEffect()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.5f;
        trail.startWidth = 0.15f;
        trail.endWidth = 0.05f;
        trail.minVertexDistance = 0.1f;
        trail.autodestruct = false;
        
        if (trailMaterial != null)
        {
            trail.material = trailMaterial;
        }
        else
        {
            // Create default glowing trail material
            Material glowMat = new Material(Shader.Find("Sprites/Default"));
            glowMat.color = new Color(0.3f, 0.6f, 1f, 0.8f);
            trail.material = glowMat;
        }
    }
    
    PhysicMaterial CreatePhysicsMaterial()
    {
        PhysicMaterial mat = new PhysicMaterial("BallPhysics");
        mat.bounciness = 0.1f;
        mat.dynamicFriction = 0.4f;
        mat.staticFriction = 0.4f;
        mat.frictionCombine = PhysicMaterialCombine.Average;
        mat.bounceCombine = PhysicMaterialCombine.Minimum;
        return mat;
    }
    
    void Update()
    {
        HandleInput();
        
        // Update particle effects based on velocity
        if (moveParticles != null && rb.velocity.magnitude > 0.5f)
        {
            if (!moveParticles.isPlaying)
                moveParticles.Play();
        }
        else if (moveParticles != null)
        {
            moveParticles.Stop();
        }
    }
    
    void HandleInput()
    {
        if (useGyro && gyroInitialized)
        {
            // Get gyroscope input
            // Note: Gyro gravity vector points down, so we negate it
            Vector3 gyroInput = -Input.gyro.gravity;
            
            // Adjust for device orientation
            // This assumes landscape left orientation for mobile
            currentInput = new Vector3(gyroInput.x, 0, gyroInput.y) * gyroSensitivity;
        }
        else
        {
            // Keyboard fallback for editor testing
            keyboardInput = Vector3.zero;
            keyboardInput.x = Input.GetAxis("Horizontal");
            keyboardInput.z = Input.GetAxis("Vertical");
            currentInput = keyboardInput;
        }
    }
    
    void FixedUpdate()
    {
        // Apply movement force based on input
        Vector3 force = currentInput * moveSpeed;
        
        // Apply force at center for smooth rolling
        rb.AddForce(force, ForceMode.Acceleration);
        
        // Clamp velocity to prevent physics issues
        if (rb.velocity.magnitude > maxVelocity)
        {
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxVelocity);
        }
        
        // Apply friction when no input
        if (currentInput.magnitude < 0.1f)
        {
            rb.velocity *= friction;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Play sound or effect on collision
        if (collision.relativeVelocity.magnitude > 2f)
        {
            // Could add impact particle effect here
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize input direction in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, currentInput * 2f);
    }
}
