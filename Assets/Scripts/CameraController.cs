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
    [SerializeField] private float rotationSpeed = 2.5f; // Reduziert von 5f für WoW-Feel
    [SerializeField] private float minVerticalAngle = -40f;
    [SerializeField] private float maxVerticalAngle = 80f;
    [SerializeField] private bool invertY = false; // Option für invertierte Y-Achse

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.1f;
    [SerializeField] private float rotationSmoothTime = 0.08f; // NEU: Smoothing für Rotation
    [SerializeField] private float followSmoothTime = 0.15f; // NEU: Separates Follow-Smoothing

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("Mouse Sensitivity")]
    [SerializeField] private float mouseSensitivityX = 1f; // NEU: Separate Sensitivität
    [SerializeField] private float mouseSensitivityY = 1f;

    // Current state
    private float currentDistance;
    private float targetDistance;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float targetRotationX = 0f; // NEU: Target für smoothing
    private float targetRotationY = 0f;

    // Smoothing variables
    private Vector3 currentPosition;
    private Vector3 positionVelocity;
    private float distanceVelocity;
    private float rotationXVelocity; // NEU
    private float rotationYVelocity; // NEU

    // Input state
    private bool leftMouseHeld;
    private bool rightMouseHeld;
    private bool middleMouseHeld;

    // NEU: Dead zone für präzisere Kontrolle
    private float mouseDeadZone = 0.001f;

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
        targetRotationX = rotationX;
        targetRotationY = rotationY;

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

            // Apply dead zone
            if (Mathf.Abs(mouseX) < mouseDeadZone) mouseX = 0f;
            if (Mathf.Abs(mouseY) < mouseDeadZone) mouseY = 0f;

            // Apply sensitivity
            mouseX *= mouseSensitivityX;
            mouseY *= mouseSensitivityY;

            // Horizontal rotation
            targetRotationY += mouseX * rotationSpeed;

            // Vertical rotation with clamping
            float yInput = invertY ? mouseY : -mouseY;
            targetRotationX += yInput * rotationSpeed;
            targetRotationX = Mathf.Clamp(targetRotationX, minVerticalAngle, maxVerticalAngle);
        }

        // Smooth rotation interpolation
        rotationX = Mathf.SmoothDampAngle(rotationX, targetRotationX, ref rotationXVelocity, rotationSmoothTime);
        rotationY = Mathf.SmoothDampAngle(rotationY, targetRotationY, ref rotationYVelocity, rotationSmoothTime);
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

        // Different smoothing based on camera mode
        float smoothTime = (leftMouseHeld || rightMouseHeld) ? positionSmoothTime : followSmoothTime;

        // Smooth camera movement
        currentPosition = Vector3.SmoothDamp(currentPosition, desiredPosition, ref positionVelocity, smoothTime);

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
        targetRotationX = x;
        targetRotationY = y;
    }

    public void SetDistance(float newDistance)
    {
        targetDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
    }

    // NEU: Methode zum Anpassen der Sensitivität zur Laufzeit
    public void SetMouseSensitivity(float x, float y)
    {
        mouseSensitivityX = Mathf.Clamp(x, 0.1f, 3f);
        mouseSensitivityY = Mathf.Clamp(y, 0.1f, 3f);
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