using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Distance")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance = 0f; // 0 for first person
    [SerializeField] private float maxDistance = 15f; // ~15 yards
    [SerializeField] private float firstPersonThreshold = 0.5f; // When to switch to FP mode

    [Header("Camera Pivot")]
    [SerializeField] private float pivotHeight = 1.65f; // Chest/head height offset
    [SerializeField] private float shoulderOffset = 0f; // Removed offset to center player

    [Header("Rotation Limits")]
    [SerializeField] private float minPitch = -40f; // Looking down
    [SerializeField] private float maxPitch = 40f; // Looking up

    [Header("Field of View")]
    [SerializeField] private float thirdPersonFOV = 70f; // Default WoW FOV
    [SerializeField] private float firstPersonFOV = 50f; // Tighter FP view
    [SerializeField] private float minFOV = 60f; // Console command limits
    [SerializeField] private float maxFOV = 100f;

    [Header("Smoothing Settings")]
    [SerializeField] private bool useSmoothRotation = true;
    [SerializeField] private float rotationDamping = 0.1f; // Critical damping
    [SerializeField] private float zoomDamping = 0.15f;

    [Header("Input Settings")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float keyboardRotationSpeed = 120f; // Degrees per second
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float keyboardZoomSpeed = 3f;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float collisionOffset = 0.1f; // Small buffer from walls
    [SerializeField] private LayerMask collisionLayers = -1;

    // Camera state
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float targetYaw = 0f;
    private float targetPitch = 0f;
    private float currentDistance;
    private float targetDistance;

    // Smoothing velocities
    private float yawVelocity;
    private float pitchVelocity;
    private float distanceVelocity;

    // Camera states
    private bool isFirstPerson = false;
    private Camera cam;

    // Input states
    private bool rightMouseHeld;
    private bool leftMouseHeld;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Find player if not assigned
        if (!target)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }

        // Initialize camera state
        currentDistance = defaultDistance;
        targetDistance = defaultDistance;

        // Get initial rotation from current transform
        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;
        if (currentPitch > 180) currentPitch -= 360; // Normalize to -180 to 180

        targetYaw = currentYaw;
        targetPitch = currentPitch;

        // Set initial FOV
        cam.fieldOfView = thirdPersonFOV;

        // Setup collision layers (everything except Player layer)
        if (collisionLayers == -1)
            collisionLayers = ~(1 << LayerMask.NameToLayer("Player"));
    }

    void Update()
    {
        HandleInput();
    }

    void LateUpdate()
    {
        if (!target) return;

        HandleRotation();
        HandleZoom();
        HandleCameraMode();
        UpdateCameraPosition();
    }

    void HandleInput()
    {
        // Mouse buttons
        rightMouseHeld = Input.GetMouseButton(1);
        leftMouseHeld = Input.GetMouseButton(0);

        // Keyboard rotation
        if (!rightMouseHeld)
        {
            // Arrow keys for camera rotation when not using mouse
            float horizontalKeys = 0f;
            float verticalKeys = 0f;

            if (Input.GetKey(KeyCode.LeftArrow)) horizontalKeys = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontalKeys = 1f;
            if (Input.GetKey(KeyCode.PageUp)) verticalKeys = 1f;
            if (Input.GetKey(KeyCode.PageDown)) verticalKeys = -1f;

            if (horizontalKeys != 0 || verticalKeys != 0)
            {
                targetYaw += horizontalKeys * keyboardRotationSpeed * Time.deltaTime;
                targetPitch -= verticalKeys * keyboardRotationSpeed * Time.deltaTime;
                targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            }
        }

        // Keyboard zoom (+ and - keys)
        if (Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Plus))
        {
            targetDistance -= keyboardZoomSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Minus))
        {
            targetDistance += keyboardZoomSpeed * Time.deltaTime;
        }
    }

    void HandleRotation()
    {
        // Mouse rotation (right mouse button)
        if (rightMouseHeld)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            targetYaw += mouseX;
            targetPitch -= mouseY;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        // Apply smoothing
        if (useSmoothRotation)
        {
            // Critical damping for smooth deceleration
            currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationDamping);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, rotationDamping);
        }
        else
        {
            currentYaw = targetYaw;
            currentPitch = targetPitch;
        }
    }

    void HandleZoom()
    {
        // Mouse wheel zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            targetDistance -= scrollInput * zoomSpeed;
        }

        // Clamp target distance
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        // Smooth zoom
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomDamping);
    }

    void HandleCameraMode()
    {
        // Check if we should be in first person
        bool wasFirstPerson = isFirstPerson;
        isFirstPerson = currentDistance <= firstPersonThreshold;

        // Smooth FOV transition
        float targetFOV = isFirstPerson ? firstPersonFOV : thirdPersonFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 5f);

        // Hide player model in first person (optional)
        if (isFirstPerson != wasFirstPerson && target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = !isFirstPerson;
            }
        }
    }

    void UpdateCameraPosition()
    {
        // Calculate pivot point (chest/head height)
        Vector3 pivotPoint = target.position + Vector3.up * pivotHeight;

        // Calculate desired camera position
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 desiredPosition;

        if (isFirstPerson)
        {
            // First person: camera at pivot point
            desiredPosition = pivotPoint;
        }
        else
        {
            // Third person: offset from pivot
            Vector3 offset = rotation * Vector3.back * currentDistance;
            desiredPosition = pivotPoint + offset;
        }

        // Collision detection
        float collisionDistance = HandleCollision(pivotPoint, desiredPosition, currentDistance);

        // Apply final position
        if (!isFirstPerson && collisionDistance < currentDistance)
        {
            // Recalculate position with collision-adjusted distance
            Vector3 offset = rotation * Vector3.back * collisionDistance;
            transform.position = pivotPoint + offset;
        }
        else
        {
            transform.position = desiredPosition;
        }

        // Apply rotation
        transform.rotation = rotation;
    }

    float HandleCollision(Vector3 pivot, Vector3 desiredPos, float desiredDistance)
    {
        if (isFirstPerson) return 0f;

        Vector3 direction = (desiredPos - pivot).normalized;
        float distance = Vector3.Distance(pivot, desiredPos);

        // Raycast for obstacles
        RaycastHit hit;
        if (Physics.SphereCast(pivot, collisionRadius, direction, out hit, distance + collisionOffset, collisionLayers))
        {
            // Calculate clear distance
            float clearDistance = hit.distance - collisionOffset;
            clearDistance = Mathf.Max(minDistance, clearDistance);

            // Return the clear distance directly for immediate response
            return clearDistance;
        }

        // No collision, return desired distance
        return desiredDistance;
    }

    // Public methods for runtime adjustment
    public void SetFOV(float fov)
    {
        thirdPersonFOV = Mathf.Clamp(fov, minFOV, maxFOV);
        if (!isFirstPerson) cam.fieldOfView = thirdPersonFOV;
    }

    public void SetSmoothingStyle(float damping)
    {
        rotationDamping = Mathf.Clamp(damping, 0.05f, 0.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Console command simulation
    public void ConsoleCommand(string command, string value)
    {
        switch (command.ToLower())
        {
            case "camerafov":
                if (float.TryParse(value, out float fov))
                    SetFOV(fov);
                break;
            case "camerasmoothstyle":
                if (float.TryParse(value, out float style))
                    SetSmoothingStyle(style * 0.1f); // Convert to damping value
                break;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!target) return;

        // Draw pivot point
        Gizmos.color = Color.yellow;
        Vector3 pivot = target.position + Vector3.up * pivotHeight;
        Gizmos.DrawWireSphere(pivot, 0.1f);

        // Draw camera collision sphere
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);

        // Draw line from pivot to camera
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pivot, transform.position);
    }
}