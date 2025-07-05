using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Camera Settings")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float heightOffset = 1.65f; // Character eye level
    [SerializeField] private float shoulderOffset = 0.5f; // Slight over-shoulder offset
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float minVerticalAngle = -40f;
    [SerializeField] private float maxVerticalAngle = 80f;
    
    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.1f;
    [SerializeField] private float rotationSmoothTime = 0.05f;
    
    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers;
    
    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float zoomSmoothTime = 0.1f;
    
    // Current state
    private float currentDistance;
    private float targetDistance;
    private float rotationX = 0f;
    private float rotationY = 0f;
    
    // Smoothing variables
    private Vector3 currentPosition;
    private Vector3 positionVelocity;
    private float distanceVelocity;
    
    // Input state
    private bool leftMouseHeld;
    private bool rightMouseHeld;
    private bool middleMouseHeld;
    
    void Start()
    {
        if (!target)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player)
            {
                target = player.transform;
            }
        }
        
        // Initialize camera position
        currentDistance = distance;
        targetDistance = distance;
        currentPosition = transform.position;
        
        // Set initial rotation from current transform
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;
        
        // Set collision layers (everything except Player)
        collisionLayers = ~(1 << LayerMask.NameToLayer("Player"));
    }
    
    void LateUpdate()
    {
        if (!target) return;
        
        HandleInput();
        HandleRotation();
        HandleZoom();
        HandleCameraPosition();
        HandleCollision();
    }
    
    void HandleInput()
    {
        leftMouseHeld = Input.GetMouseButton(0);
        rightMouseHeld = Input.GetMouseButton(1);
        middleMouseHeld = Input.GetMouseButton(2);
    }
    
    void HandleRotation()
    {
        // Camera rotation with left mouse, right mouse, or middle mouse
        if (leftMouseHeld || rightMouseHeld || middleMouseHeld)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            // Horizontal rotation
            rotationY += mouseX * rotationSpeed;
            
            // Vertical rotation with clamping
            rotationX -= mouseY * rotationSpeed;
            rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        }
    }
    
    void HandleZoom()
    {
        // Mouse wheel zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            targetDistance -= scrollInput * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
        
        // Smooth zoom
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);
    }
    
    void HandleCameraPosition()
    {
        // Calculate desired position
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        
        // Target position with height offset (character eye level)
        Vector3 targetPoint = target.position + Vector3.up * heightOffset;
        
        // Calculate camera offset
        Vector3 desiredPosition = targetPoint - rotation * Vector3.forward * currentDistance;
        
        // Add slight shoulder offset for WoW feel
        Vector3 rightOffset = rotation * Vector3.right * shoulderOffset;
        desiredPosition += rightOffset;
        
        // Smooth camera movement
        currentPosition = Vector3.SmoothDamp(currentPosition, desiredPosition, ref positionVelocity, positionSmoothTime);
        
        // Apply position and rotation
        transform.position = currentPosition;
        transform.rotation = rotation;
    }
    
    void HandleCollision()
    {
        if (!target) return;
        
        Vector3 targetPoint = target.position + Vector3.up * heightOffset;
        Vector3 directionToCamera = (transform.position - targetPoint).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
        
        // Raycast from target to camera
        RaycastHit hit;
        if (Physics.SphereCast(targetPoint, collisionRadius, directionToCamera, out hit, distanceToTarget, collisionLayers))
        {
            // Adjust camera position to avoid collision
            float hitDistance = Vector3.Distance(targetPoint, hit.point) - collisionRadius;
            hitDistance = Mathf.Max(minDistance, hitDistance);
            
            Vector3 adjustedPosition = targetPoint + directionToCamera * hitDistance;
            transform.position = adjustedPosition;
            currentPosition = adjustedPosition;
            
            // Update current distance for smooth recovery
            currentDistance = hitDistance;
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    // For smooth transitions
    public void SetRotation(float x, float y)
    {
        rotationX = x;
        rotationY = y;
    }
    
    public void SetDistance(float newDistance)
    {
        targetDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }
    
    // For debugging
    void OnDrawGizmosSelected()
    {
        if (!target) return;
        
        Gizmos.color = Color.yellow;
        Vector3 targetPoint = target.position + Vector3.up * heightOffset;
        Gizmos.DrawWireSphere(targetPoint, 0.1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(targetPoint, transform.position);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}