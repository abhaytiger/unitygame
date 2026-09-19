using UnityEngine;

/// <summary>
/// Camera follow script that smoothly tracks the player ball
/// Attach to the Main Camera
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The target to follow (player ball)")]
    public Transform target;
    
    [Tooltip("Offset from target position")]
    public Vector3 offset = new Vector3(0, 15f, -10f);
    
    [Header("Smooth Follow")]
    [Tooltip("How quickly the camera follows the target")]
    public float smoothSpeed = 5f;
    
    [Tooltip("Minimum distance from target")]
    public float minDistance = 5f;
    
    [Tooltip("Maximum distance from target")]
    public float maxDistance = 25f;
    
    [Header("Camera Angles")]
    [Range(0, 90)]
    [Tooltip("Vertical angle of the camera")]
    public float verticalAngle = 45f;
    
    [Range(0, 360)]
    [Tooltip("Horizontal rotation around target")]
    public float horizontalRotation = 0f;
    
    [Header("Zoom Settings")]
    [Tooltip("Enable pinch zoom on mobile")]
    public bool enableZoom = true;
    
    [Tooltip("Zoom speed for pinch gesture")]
    public float zoomSpeed = 2f;
    
    [Tooltip("Current camera distance")]
    public float currentDistance = 15f;
    
    [Header("Boundaries")]
    [Tooltip("Keep camera within maze bounds")]
    public bool clampToMazeBounds = true;
    
    [Tooltip("Size of the maze for clamping")]
    public float mazeSize = 30f;
    
    private Vector3 velocity = Vector3.zero;
    private float touchStartDistance = 0f;
    private bool isZooming = false;
    
    void Start()
    {
        // Find target if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                player = GameObject.Find("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }
        
        // Set initial distance
        currentDistance = offset.magnitude;
    }
    
    void LateUpdate()
    {
        if (target == null)
            return;
        
        HandleZoomInput();
        FollowTarget();
    }
    
    void FollowTarget()
    {
        // Calculate desired position based on orbit controls
        float radianX = verticalAngle * Mathf.Deg2Rad;
        float radianY = horizontalRotation * Mathf.Deg2Rad;
        
        // Convert spherical coordinates to Cartesian
        float x = currentDistance * Mathf.Sin(radianX) * Mathf.Sin(radianY);
        float y = currentDistance * Mathf.Cos(radianX);
        float z = currentDistance * Mathf.Sin(radianX) * Mathf.Cos(radianY);
        
        Vector3 desiredPosition = target.position + new Vector3(x, y, -z);
        
        // Clamp to maze bounds if enabled
        if (clampToMazeBounds)
        {
            float halfMaze = mazeSize / 2f;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, -halfMaze, halfMaze);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, -halfMaze, halfMaze);
        }
        
        // Smoothly interpolate to desired position
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, 
            1f / smoothSpeed
        );
        
        transform.position = smoothedPosition;
        
        // Always look at target
        transform.LookAt(target.position + Vector3.up * 0.5f);
    }
    
    void HandleZoomInput()
    {
        if (!enableZoom)
            return;
        
        // Mobile pinch zoom
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            
            if (touch0.phase == TouchPhase.Began && touch1.phase == TouchPhase.Began)
            {
                touchStartDistance = Vector2.Distance(touch0.position, touch1.position);
                isZooming = true;
            }
            else if (isZooming)
            {
                float currentTouchDistance = Vector2.Distance(touch0.position, touch1.position);
                float delta = currentTouchDistance - touchStartDistance;
                
                currentDistance -= delta * zoomSpeed * 0.01f;
                currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
                
                touchStartDistance = currentTouchDistance;
            }
        }
        else
        {
            isZooming = false;
        }
        
        // Mouse wheel zoom for editor testing
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    public void ResetCamera()
    {
        currentDistance = 15f;
        verticalAngle = 45f;
        horizontalRotation = 0f;
    }
    
    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, 0.5f);
        }
    }
}
