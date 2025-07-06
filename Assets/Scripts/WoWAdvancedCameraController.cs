using UnityEngine;
using System;

public class WoWAdvancedCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float targetHeightOffset = 1.65f; // Character eye level
    [SerializeField] private float shoulderOffset = 0.45f; // Over-shoulder offset

    [Header("Distance Settings")]
    [SerializeField] private float defaultDistance = 5f;
    [SerializeField] private float minDistance = 2.5f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float zoomDamping = 0.1f;
    [SerializeField] private float maxZoomRate = 5f; // yards/sec

    [Header("Rotation Settings")]
    [SerializeField] private float mouseSensitivityX = 1.2f;
    [SerializeField] private float mouseSensitivityY = 1.2f;
    [SerializeField] private float keyboardRotationSpeed = 90f; // degrees/sec
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 40f;
    [SerializeField] private float maxRotationSpeed = 120f; // degrees/sec
    [SerializeField] private bool invertY = false;

    [Header("Spring-Damper Settings")]
    [SerializeField] private float springStiffness = 500f; // k value
    [SerializeField] private float dampingCoefficient = 20f; // c value
    [SerializeField] private float positionLerpSpeed = 0.1f; // Alpha for position tracking

    [Header("Deadzone & Filtering")]
    [SerializeField] private bool useSoftDeadzone = true;
    [SerializeField] private float deadzoneThreshold = 0.001f;
    [SerializeField] private AnimationCurve deadzoneCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Look-Ahead Settings")]
    [SerializeField] private bool useLookAhead = true;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 3f;

    [Header("Collision Settings")]
    [SerializeField] private float collisionRadius = 0.3f;
    [SerializeField] private float collisionBuffer = 0.3f;
    [SerializeField] private LayerMask collisionLayers = -1;
    [SerializeField] private float collisionRecoverySpeed = 5f;
    [SerializeField] private bool dynamicZoomOverride = true;

    // State variables
    private float currentYaw;
    private float currentPitch;
    private float targetYaw;
    private float targetPitch;
    private float yawVelocity;
    private float pitchVelocity;

    private float currentDistance;
    private float targetDistance;
    private float distanceVelocity;
    private float collisionDistance;

    private Vector3 currentTargetPosition;
    private Vector3 lookAheadOffset;

    // Input accumulation
    private float mouseInputX;
    private float mouseInputY;
    private float accumulatedMouseX;
    private float accumulatedMouseY;

    void Start()
    {
        if (!target)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.transform;
        }

        // Initialize camera state
        Vector3 angles = transform.eulerAngles;
        currentYaw = targetYaw = angles.y;
        currentPitch = targetPitch = NormalizeAngle(angles.x);
        currentDistance = targetDistance = defaultDistance;
        collisionDistance = currentDistance;

        if (target)
        {
            currentTargetPosition = target.position;
        }

        // Setup collision layers
        collisionLayers = ~(1 << LayerMask.NameToLayer("Player"));
    }

    void Update()
    {
        if (!target) return;

        GatherInput();
        ApplyDeadzone();
        UpdateRotation();
        UpdateZoom();
        UpdateLookAhead();
    }

    void LateUpdate()
    {
        if (!target) return;

        UpdateTargetTracking();
        UpdateCameraTransform();
        HandleCollision();
    }

    void GatherInput()
    {
        // Reset input
        mouseInputX = 0f;
        mouseInputY = 0f;

        // Mouse input (RMB, LMB, or MMB held)
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            mouseInputX = Input.GetAxis("Mouse X");
            mouseInputY = Input.GetAxis("Mouse Y");
        }

        // Keyboard rotation
        if (Input.GetKey(KeyCode.LeftArrow))
            mouseInputX -= keyboardRotationSpeed * Time.deltaTime / mouseSensitivityX;
        if (Input.GetKey(KeyCode.RightArrow))
            mouseInputX += keyboardRotationSpeed * Time.deltaTime / mouseSensitivityX;
        if (Input.GetKey(KeyCode.PageUp))
            mouseInputY += keyboardRotationSpeed * Time.deltaTime / mouseSensitivityY;
        if (Input.GetKey(KeyCode.PageDown))
            mouseInputY -= keyboardRotationSpeed * Time.deltaTime / mouseSensitivityY;

        // Keyboard zoom
        if (Input.GetKey(KeyCode.KeypadPlus))
            targetDistance -= zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.KeypadMinus))
            targetDistance += zoomSpeed * Time.deltaTime;

        // Mouse wheel zoom
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            targetDistance -= scrollInput * zoomSpeed;
        }

        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }

    void ApplyDeadzone()
    {
        if (!useSoftDeadzone) return;

        // Apply soft deadzone with curve
        float inputMagnitude = Mathf.Sqrt(mouseInputX * mouseInputX + mouseInputY * mouseInputY);

        if (inputMagnitude < deadzoneThreshold)
        {
            mouseInputX = 0f;
            mouseInputY = 0f;
        }
        else
        {
            float normalizedMagnitude = (inputMagnitude - deadzoneThreshold) / (1f - deadzoneThreshold);
            float curveValue = deadzoneCurve.Evaluate(normalizedMagnitude);

            mouseInputX = (mouseInputX / inputMagnitude) * curveValue * inputMagnitude;
            mouseInputY = (mouseInputY / inputMagnitude) * curveValue * inputMagnitude;
        }
    }

    void UpdateRotation()
    {
        // Accumulate input with sensitivity
        accumulatedMouseX += mouseInputX * mouseSensitivityX;
        accumulatedMouseY += mouseInputY * mouseSensitivityY;

        // Update target rotation
        targetYaw += accumulatedMouseX;
        float pitchInput = invertY ? accumulatedMouseY : -accumulatedMouseY;
        targetPitch = Mathf.Clamp(targetPitch + pitchInput, minPitch, maxPitch);

        // Spring-damper physics for smooth rotation
        float dt = Time.deltaTime;

        // Yaw spring-damper
        float yawError = targetYaw - currentYaw;
        float yawSpringForce = -springStiffness * yawError;
        float yawDampingForce = -dampingCoefficient * yawVelocity;
        float yawAcceleration = (yawSpringForce + yawDampingForce);

        yawVelocity += yawAcceleration * dt;
        yawVelocity = Mathf.Clamp(yawVelocity, -maxRotationSpeed, maxRotationSpeed);
        currentYaw += yawVelocity * dt;

        // Pitch spring-damper
        float pitchError = targetPitch - currentPitch;
        float pitchSpringForce = -springStiffness * pitchError;
        float pitchDampingForce = -dampingCoefficient * pitchVelocity;
        float pitchAcceleration = (pitchSpringForce + pitchDampingForce);

        pitchVelocity += pitchAcceleration * dt;
        pitchVelocity = Mathf.Clamp(pitchVelocity, -maxRotationSpeed, maxRotationSpeed);
        currentPitch += pitchVelocity * dt;

        // Clear accumulated input
        accumulatedMouseX = 0f;
        accumulatedMouseY = 0f;
    }

    void UpdateZoom()
    {
        // Spring-damper for zoom
        float dt = Time.deltaTime;
        float distanceError = targetDistance - currentDistance;
        float springForce = -springStiffness * distanceError;
        float dampingForce = -dampingCoefficient * distanceVelocity;
        float acceleration = (springForce + dampingForce);

        distanceVelocity += acceleration * dt;
        distanceVelocity = Mathf.Clamp(distanceVelocity, -maxZoomRate, maxZoomRate);
        currentDistance += distanceVelocity * dt;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    void UpdateLookAhead()
    {
        if (!useLookAhead || !target) return;

        // Calculate look-ahead based on character velocity
        CharacterController controller = target.GetComponent<CharacterController>();
        if (controller != null && controller.velocity.magnitude > 0.1f)
        {
            Vector3 velocity = controller.velocity;
            velocity.y = 0; // Only horizontal look-ahead
            Vector3 targetLookAhead = velocity.normalized * lookAheadDistance;

            lookAheadOffset = Vector3.Lerp(lookAheadOffset, targetLookAhead,
                Time.deltaTime * lookAheadSpeed);
        }
        else
        {
            lookAheadOffset = Vector3.Lerp(lookAheadOffset, Vector3.zero,
                Time.deltaTime * lookAheadSpeed);
        }
    }

    void UpdateTargetTracking()
    {
        if (!target) return;

        // Linear interpolation for smooth following
        Vector3 targetPos = target.position + lookAheadOffset;
        currentTargetPosition = Vector3.Lerp(currentTargetPosition, targetPos, positionLerpSpeed);
    }

    void UpdateCameraTransform()
    {
        // Calculate camera position
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 targetPoint = currentTargetPosition + Vector3.up * targetHeightOffset;

        // Use collision distance if smaller than current distance
        float effectiveDistance = dynamicZoomOverride ?
            Mathf.Min(currentDistance, collisionDistance) : currentDistance;

        // Calculate offset
        Vector3 offset = rotation * Vector3.back * effectiveDistance;
        Vector3 rightOffset = rotation * Vector3.right * shoulderOffset;

        // Final position
        transform.position = targetPoint + offset + rightOffset;
        transform.rotation = rotation;
    }

    void HandleCollision()
    {
        if (!target) return;

        Vector3 targetPoint = currentTargetPosition + Vector3.up * targetHeightOffset;
        Vector3 desiredPosition = transform.position;
        Vector3 direction = (desiredPosition - targetPoint).normalized;
        float distance = Vector3.Distance(desiredPosition, targetPoint);

        // Multi-point collision check
        RaycastHit hit;
        float nearestHit = distance;

        // Center ray
        if (Physics.SphereCast(targetPoint, collisionRadius, direction, out hit, distance, collisionLayers))
        {
            nearestHit = Mathf.Min(nearestHit, hit.distance - collisionBuffer);
        }

        // Additional rays for better coverage
        Vector3[] additionalDirections = {
            Quaternion.Euler(0, 5, 0) * direction,
            Quaternion.Euler(0, -5, 0) * direction,
            Quaternion.Euler(5, 0, 0) * direction,
            Quaternion.Euler(-5, 0, 0) * direction
        };

        foreach (Vector3 dir in additionalDirections)
        {
            if (Physics.Raycast(targetPoint, dir, out hit, distance, collisionLayers))
            {
                nearestHit = Mathf.Min(nearestHit, hit.distance - collisionBuffer);
            }
        }

        nearestHit = Mathf.Max(nearestHit, minDistance);

        // Smooth collision distance
        if (nearestHit < distance)
        {
            collisionDistance = Mathf.Lerp(collisionDistance, nearestHit,
                Time.deltaTime * collisionRecoverySpeed * 2f);
        }
        else
        {
            collisionDistance = Mathf.Lerp(collisionDistance, distance,
                Time.deltaTime * collisionRecoverySpeed);
        }
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target)
        {
            currentTargetPosition = target.position;
        }
    }

    public void SetSensitivity(float x, float y)
    {
        mouseSensitivityX = Mathf.Clamp(x, 0.1f, 5f);
        mouseSensitivityY = Mathf.Clamp(y, 0.1f, 5f);
    }

    public void ResetCamera()
    {
        if (target)
        {
            currentYaw = targetYaw = target.eulerAngles.y;
            currentPitch = targetPitch = 0f;
            currentDistance = targetDistance = defaultDistance;
            yawVelocity = pitchVelocity = distanceVelocity = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!target) return;

        // Target position
        Gizmos.color = Color.yellow;
        Vector3 targetPoint = currentTargetPosition + Vector3.up * targetHeightOffset;
        Gizmos.DrawWireSphere(targetPoint, 0.2f);

        // Camera ray
        Gizmos.color = Color.red;
        Gizmos.DrawLine(targetPoint, transform.position);

        // Collision sphere
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);

        // Look-ahead offset
        if (useLookAhead && lookAheadOffset.magnitude > 0.1f)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(currentTargetPosition, currentTargetPosition + lookAheadOffset);
            Gizmos.DrawWireSphere(currentTargetPosition + lookAheadOffset, 0.3f);
        }
    }
}