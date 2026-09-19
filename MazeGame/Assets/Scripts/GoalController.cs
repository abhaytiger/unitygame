using UnityEngine;

/// <summary>
/// Goal controller that handles player reaching the goal
/// Includes visual effects and particle systems
/// Attach to the goal object
/// </summary>
public class GoalController : MonoBehaviour
{
    [Header("Visual Effects")]
    [Tooltip("Particle system for goal glow effect")]
    public ParticleSystem glowParticles;
    
    [Tooltip("Particle system for goal completion")]
    public ParticleSystem completeParticles;
    
    [Tooltip("Light attached to goal")]
    public Light goalLight;
    
    [Header("Animation")]
    [Tooltip("Rotation speed of the goal")]
    public float rotationSpeed = 50f;
    
    [Tooltip("Bobbing amplitude")]
    public float bobAmplitude = 0.3f;
    
    [Tooltip("Bobbing frequency")]
    public float bobFrequency = 2f;
    
    [Header("Audio")]
    [Tooltip("Sound played when player reaches goal")]
    public AudioSource goalReachedSound;
    
    [Header("Colors")]
    [Tooltip("Base color of the goal")]
    public Color baseColor = Color.cyan;
    
    [Tooltip("Pulse color intensity")]
    public float pulseIntensity = 0.5f;
    
    [Tooltip("Pulse speed")]
    public float pulseSpeed = 3f;
    
    private Renderer goalRenderer;
    private Material goalMaterial;
    private Vector3 initialPosition;
    private bool isCompleted = false;
    private float pulseTime = 0f;
    
    void Start()
    {
        InitializeGoal();
    }
    
    void InitializeGoal()
    {
        initialPosition = transform.position;
        
        // Get or create renderer
        goalRenderer = GetComponent<Renderer>();
        if (goalRenderer == null)
        {
            goalRenderer = gameObject.AddComponent<Renderer>();
        }
        
        // Create glowing material if not exists
        if (goalMaterial == null)
        {
            goalMaterial = new Material(Shader.Find("Standard"));
            goalMaterial.color = baseColor;
            goalMaterial.SetFloat("_Metallic", 0.3f);
            goalMaterial.SetFloat("_Smoothness", 0.8f);
            
            if (goalRenderer != null)
            {
                goalRenderer.material = goalMaterial;
            }
        }
        
        // Add light if not exists
        if (goalLight == null)
        {
            GameObject lightObj = new GameObject("GoalLight");
            lightObj.transform.parent = transform;
            lightObj.transform.localPosition = Vector3.zero;
            
            goalLight = lightObj.AddComponent<Light>();
            goalLight.type = LightType.Point;
            goalLight.color = baseColor;
            goalLight.range = 5f;
            goalLight.intensity = 1f;
        }
        
        // Create particle system if not assigned
        if (glowParticles == null)
        {
            CreateGlowParticles();
        }
    }
    
    void CreateGlowParticles()
    {
        GameObject particleObj = new GameObject("GlowParticles");
        particleObj.transform.parent = transform;
        particleObj.transform.localPosition = Vector3.zero;
        
        glowParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = glowParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = 1.5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.2f;
        main.startColor = baseColor;
        main.emissionRate = 20f;
        
        var emission = glowParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 20f;
        
        var shape = glowParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        var renderer = glowParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Particles/Standard"));
        }
    }
    
    void Update()
    {
        if (!isCompleted)
        {
            AnimateGoal();
            PulseColor();
        }
    }
    
    void AnimateGoal()
    {
        // Rotate goal
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Bob up and down
        float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = initialPosition + Vector3.up * bobOffset;
    }
    
    void PulseColor()
    {
        if (goalMaterial != null)
        {
            pulseTime += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTime) * pulseIntensity;
            
            Color pulseColor = baseColor + new Color(pulse, pulse, pulse);
            goalMaterial.color = pulseColor;
            
            if (goalLight != null)
            {
                goalLight.color = pulseColor;
                goalLight.intensity = 1f + pulse;
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<BallController>() != null)
        {
            OnGoalReached();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<BallController>() != null)
        {
            OnGoalReached();
        }
    }
    
    void OnGoalReached()
    {
        if (isCompleted) return;
        
        isCompleted = true;
        
        Debug.Log("Goal Reached!");
        
        // Play completion particles
        if (completeParticles != null)
        {
            completeParticles.Play();
        }
        else
        {
            PlayCompletionEffect();
        }
        
        // Play sound
        if (goalReachedSound != null)
        {
            goalReachedSound.Play();
        }
        
        // Intensify glow
        if (goalLight != null)
        {
            goalLight.intensity = 3f;
        }
    }
    
    void PlayCompletionEffect()
    {
        // Create explosion of particles
        GameObject effectObj = new GameObject("CompletionEffect");
        effectObj.transform.position = transform.position;
        
        ParticleSystem particles = effectObj.AddComponent<ParticleSystem>();
        
        var main = particles.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = 2f;
        main.startSpeed = 5f;
        main.startSize = 0.3f;
        main.startColor = baseColor;
        main.emissionRate = 50f;
        
        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 100f;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.xMultiplier = 2f;
        velocityOverLifetime.yMultiplier = 2f;
        velocityOverLifetime.zMultiplier = 2f;
        
        particles.Play();
        
        // Destroy after playing
        GameObject.Destroy(effectObj, 3f);
    }
    
    public void ResetGoal()
    {
        isCompleted = false;
        pulseTime = 0f;
        
        if (goalMaterial != null)
        {
            goalMaterial.color = baseColor;
        }
        
        if (goalLight != null)
        {
            goalLight.intensity = 1f;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw trigger radius
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // Draw light range
        if (goalLight != null)
        {
            Gizmos.color = new Color(goalLight.color.r, goalLight.color.g, goalLight.color.b, 0.1f);
            Gizmos.DrawWireSphere(transform.position, goalLight.range);
        }
    }
}
