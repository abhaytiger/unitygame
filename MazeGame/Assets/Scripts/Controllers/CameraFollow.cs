using UnityEngine;

namespace GyroMaze.Controllers
{
    /// <summary>
    /// Professional camera controller with smooth follow, orbit controls,
    /// pinch-to-zoom, and mobile-optimized settings.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 15, -5);
        
        [Header("Follow Settings")]
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float rotationSpeed = 3f;
        [SerializeField] private bool smoothFollow = true;
        
        [Header("Zoom Settings")]
        [SerializeField] private float minZoom = 8f;
        [SerializeField] private float maxZoom = 25f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float pinchSensitivity = 0.1f;
        
        [Header("Orbit Settings")]
        [SerializeField] private bool allowOrbit = true;
        [SerializeField] private float orbitSensitivity = 2f;
        [SerializeField] private float minVerticalAngle = 10f;
        [SerializeField] private float maxVerticalAngle = 80f;
        
        [Header("Boundary Settings")]
        [SerializeField] private float boundaryPadding = 2f;
        
        // Private state
        private float currentDistance;
        private float verticalAngle = 45f;
        private float horizontalAngle = 0f;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private int mazeSize = 10;
        private Vector2 previousTouchDelta;
        private bool isPinching = false;
        
        private void Start()
        {
            if (target == null)
            {
                // Try to find the ball automatically
                GameObject ball = GameObject.FindGameObjectWithTag("Player");
                if (ball != null)
                {
                    target = ball.transform;
                }
            }
            
            currentDistance = offset.magnitude;
            CalculateInitialAngles();
        }
        
        private void LateUpdate()
        {
            if (target == null) return;
            
            HandleMobileInput();
            CalculateTargetPosition();
            ApplyBoundaries();
            
            if (smoothFollow)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }
        
        /// <summary>
        /// Handle mobile touch input for orbit and zoom
        /// </summary>
        private void HandleMobileInput()
        {
            if (!allowOrbit) return;
            
            // Pinch-to-zoom
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);
                
                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                
                float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
                float currentTouchDeltaMag = (touch0.position - touch1.position).magnitude;
                
                float deltaMagnitude = currentTouchDeltaMag - prevTouchDeltaMag;
                currentDistance -= deltaMagnitude * pinchSensitivity;
                currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom);
                
                isPinching = true;
            }
            else if (Input.touchCount == 1 && !isPinching)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Moved)
                {
                    // Orbit camera
                    horizontalAngle += touch.deltaPosition.x * orbitSensitivity * 0.1f;
                    verticalAngle -= touch.deltaPosition.y * orbitSensitivity * 0.1f;
                    verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
                }
            }
            else
            {
                isPinching = false;
            }
            
            // Mouse fallback for editor testing
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                
                horizontalAngle += mouseX * orbitSensitivity;
                verticalAngle -= mouseY * orbitSensitivity;
                verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
            }
            
            // Scroll wheel zoom for editor
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance -= scroll * zoomSpeed * 5f;
                currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom);
            }
        }
        
        /// <summary>
        /// Calculate initial camera angles based on offset
        /// </summary>
        private void CalculateInitialAngles()
        {
            Vector3 flatOffset = new Vector3(offset.x, 0, offset.z);
            if (flatOffset.magnitude > 0.01f)
            {
                horizontalAngle = Mathf.Atan2(flatOffset.x, flatOffset.z) * Mathf.Rad2Deg;
            }
            
            float verticalOffset = offset.y;
            float horizontalDistance = flatOffset.magnitude;
            if (horizontalDistance > 0.01f)
            {
                verticalAngle = 90f - Mathf.Atan2(verticalOffset, horizontalDistance) * Mathf.Rad2Deg;
                verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);
            }
        }
        
        /// <summary>
        /// Calculate target position and rotation
        /// </summary>
        private void CalculateTargetPosition()
        {
            // Calculate spherical coordinates
            float radH = horizontalAngle * Mathf.Deg2Rad;
            float radV = verticalAngle * Mathf.Deg2Rad;
            
            float x = currentDistance * Mathf.Sin(radH) * Mathf.Cos(radV);
            float y = currentDistance * Mathf.Sin(radV);
            float z = currentDistance * Mathf.Cos(radH) * Mathf.Cos(radV);
            
            targetPosition = target.position + new Vector3(x, y, -z);
            targetRotation = Quaternion.LookRotation(target.position - targetPosition);
        }
        
        /// <summary>
        /// Apply boundaries to keep camera within maze bounds
        /// </summary>
        private void ApplyBoundaries()
        {
            float halfSize = (mazeSize * 2f) / 2f + boundaryPadding;
            float minY = minZoom * Mathf.Sin(minVerticalAngle * Mathf.Deg2Rad);
            float maxY = maxZoom;
            
            Vector3 clampedPos = targetPosition;
            clampedPos.x = Mathf.Clamp(clampedPos.x, -halfSize, halfSize);
            clampedPos.z = Mathf.Clamp(clampedPos.z, -halfSize, halfSize);
            
            // Don't clamp Y to allow zoom variation
            targetPosition = clampedPos;
        }
        
        /// <summary>
        /// Set maze size for boundary calculations
        /// </summary>
        public void SetMazeSize(int size)
        {
            mazeSize = size;
            AdjustCameraForMazeSize(size);
        }
        
        /// <summary>
        /// Automatically adjust camera distance based on maze size
        /// </summary>
        private void AdjustCameraForMazeSize(int size)
        {
            // Larger mazes need camera further away
            float targetDistance = Mathf.Lerp(minZoom, maxZoom, Mathf.InverseLerp(8, 25, size));
            currentDistance = Mathf.Clamp(targetDistance, minZoom, maxZoom);
        }
        
        /// <summary>
        /// Manually set the target transform
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        /// <summary>
        /// Reset camera to default position
        /// </summary>
        public void ResetCamera()
        {
            currentDistance = offset.magnitude;
            verticalAngle = 45f;
            horizontalAngle = 0f;
            CalculateInitialAngles();
        }
        
        #region Editor Helpers
        
        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.position);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(target.position, 0.5f);
            }
        }
        #endif
        
        #endregion
    }
}
