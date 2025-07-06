using UnityEngine;
using System.Runtime.InteropServices;

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
    [SerializeField] private float rotationSpeed = 0.2f; // Deutlich langsamer für WoW-Feel
    [SerializeField] private float minVerticalAngle = -40f;
    [SerializeField] private float maxVerticalAngle = 80f;
    [SerializeField] private bool invertY = false; // Option für invertierte Y-Achse

    [Header("Smoothing")]
    [SerializeField] private bool useRotationSmoothing = false; // NEU: Optional machen
    [SerializeField] private float rotationSmoothTime = 0.08f; // Nur wenn aktiviert

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayers;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("Mouse Sensitivity")]
    [SerializeField] private float mouseSensitivityX = 0.2f; // Reduziert für präzisere Kontrolle
    [SerializeField] private float mouseSensitivityY = 0.2f;

    // Current state
    private float currentDistance;
    private float targetDistance;
    private float rotationX = 0f;
    private float rotationY = 0f;
    private float targetRotationX = 0f; // NEU: Target für smoothing
    private float targetRotationY = 0f;

    // Smoothing variables
    private float distanceVelocity;
    private float rotationXVelocity; // NEU
    private float rotationYVelocity; // NEU

    // Input state
    private bool leftMouseHeld;
    private bool rightMouseHeld;
    private bool middleMouseHeld;
    private bool wasRotating = false; // NEU: Track ob wir gerade rotieren

    // NEU: Mouse Position Locking für WoW-Style Verhalten
    private Vector3 lockedMousePosition; // Screen position wo Rechtsklick gedrückt wurde
    private bool isMouseLocked = false;
    private Vector3 mouseStartPosition; // Wo die Maus war als wir anfingen zu rotieren
    private Vector3 mouseDelta; // Accumulated mouse movement

    // NEU: Dead zone für präzisere Kontrolle
    [SerializeField] private bool useDeadZone = false;
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

        // Set initial rotation from current transform
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;
        targetRotationX = rotationX;
        targetRotationY = rotationY;

        // Set collision layers (everything except Player)
        collisionLayers = ~(1 << LayerMask.NameToLayer("Player"));

        // Ensure cursor is visible and free at start
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        // Check mouse button states
        bool leftPressed = Input.GetMouseButtonDown(0);
        bool rightPressed = Input.GetMouseButtonDown(1);
        bool middlePressed = Input.GetMouseButtonDown(2);

        bool leftReleased = Input.GetMouseButtonUp(0);
        bool rightReleased = Input.GetMouseButtonUp(1);
        bool middleReleased = Input.GetMouseButtonUp(2);

        leftMouseHeld = Input.GetMouseButton(0);
        rightMouseHeld = Input.GetMouseButton(1);
        middleMouseHeld = Input.GetMouseButton(2);

        // Mouse Lock Logic - WoW Style
        bool shouldStartRotating = (leftPressed || rightPressed || middlePressed) && !isMouseLocked;
        bool shouldStopRotating = (leftReleased || rightReleased || middleReleased) &&
                                   !(leftMouseHeld || rightMouseHeld || middleMouseHeld);

        if (shouldStartRotating)
        {
            StartMouseLock();
        }
        else if (shouldStopRotating)
        {
            StopMouseLock();
        }
    }

    void StartMouseLock()
    {
        // Merke aktuelle Mausposition als Startpunkt
        mouseStartPosition = Input.mousePosition;
        lockedMousePosition = mouseStartPosition;
        isMouseLocked = true;
        mouseDelta = Vector3.zero;

        // Verstecke Cursor - WoW Style
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked; // Locked mode für echtes WoW-Gefühl

        Debug.Log($"Mouse locked - rotation mode started");
    }

    void StopMouseLock()
    {
        isMouseLocked = false;

        // Zeige Cursor wieder und entsperre
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log($"Mouse unlocked - rotation mode ended");
    }

    void HandleRotation()
    {
        // Camera rotation nur wenn eine Maustaste gedrückt ist
        if (leftMouseHeld || rightMouseHeld || middleMouseHeld)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // Apply dead zone nur wenn aktiviert
            if (useDeadZone)
            {
                if (Mathf.Abs(mouseX) < mouseDeadZone) mouseX = 0f;
                if (Mathf.Abs(mouseY) < mouseDeadZone) mouseY = 0f;
            }

            // Apply sensitivity **AND** rotation speed
            mouseX *= mouseSensitivityX * rotationSpeed;
            mouseY *= mouseSensitivityY * rotationSpeed;

            // Horizontal rotation
            targetRotationY += mouseX;

            // Vertical rotation with clamping
            float yInput = invertY ? mouseY : -mouseY;
            targetRotationX += yInput;
            targetRotationX = Mathf.Clamp(targetRotationX, minVerticalAngle, maxVerticalAngle);
        }

        // Direkte oder smooth rotation
        if (useRotationSmoothing)
        {
            rotationX = Mathf.SmoothDampAngle(rotationX, targetRotationX, ref rotationXVelocity, rotationSmoothTime);
            rotationY = Mathf.SmoothDampAngle(rotationY, targetRotationY, ref rotationYVelocity, rotationSmoothTime);
        }
        else
        {
            rotationX = targetRotationX;
            rotationY = targetRotationY;
        }
    }

    void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            targetDistance -= scrollInput * zoomSpeed;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);
    }

    void HandleCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        Vector3 targetPoint = target.position + Vector3.up * heightOffset;
        Vector3 desiredPosition = targetPoint - rotation * Vector3.forward * currentDistance;
        Vector3 rightOffset = rotation * Vector3.right * shoulderOffset;
        desiredPosition += rightOffset;

        transform.position = desiredPosition;
        transform.rotation = rotation;
    }

    void HandleCollision()
    {
        if (!target) return;

        Vector3 targetPoint = target.position + Vector3.up * heightOffset;
        Vector3 directionToCamera = (transform.position - targetPoint).normalized;
        float distanceToTarget = Vector3.Distance(transform.position, targetPoint);

        RaycastHit hit;
        if (Physics.SphereCast(targetPoint, collisionRadius, directionToCamera, out hit, distanceToTarget, collisionLayers))
        {
            float hitDistance = Vector3.Distance(targetPoint, hit.point) - collisionRadius;
            hitDistance = Mathf.Max(minDistance, hitDistance);

            Vector3 adjustedPosition = targetPoint + directionToCamera * hitDistance;
            transform.position = adjustedPosition;

            currentDistance = hitDistance;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

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

    public void SetMouseSensitivity(float x, float y)
    {
        mouseSensitivityX = Mathf.Clamp(x, 0.1f, 3f);
        mouseSensitivityY = Mathf.Clamp(y, 0.1f, 3f);
    }

    // Debug info für die aktuellen Einstellungen
    public void PrintCameraDebugInfo()
    {
        Debug.Log($"Camera State - Locked: {isMouseLocked}, Distance: {currentDistance:F1}, Rotation: X={rotationX:F1} Y={rotationY:F1}");
    }

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
