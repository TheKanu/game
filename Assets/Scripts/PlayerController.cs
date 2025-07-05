using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float backwardSpeed = 4.5f;
    [SerializeField] private float strafeSpeed = 6.5f;
    [SerializeField] private float rotationSpeed = 540f; // Degrees per second
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -19.62f; // Double normal gravity for WoW feel
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    // Components
    private CharacterController controller;

    // Movement state
    private Vector3 velocity;
    private bool isGrounded;
    private bool wasGrounded;
    private float jumpMomentum;
    private bool isAutorunning = false;
    private float autorunToggleCooldown = 0f;

    // Input state
    private float horizontal;
    private float vertical;
    private bool jumpPressed;
    private bool leftMouseHeld;
    private bool rightMouseHeld;
    private bool bothMouseHeld;

    // For smooth rotation
    private Quaternion targetRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (!cameraController)
        {
            cameraController = Camera.main.GetComponent<CameraController>();
            if (cameraController)
            {
                cameraController.SetTarget(transform);
            }
        }

        targetRotation = transform.rotation;

        // Set ground mask to everything except Player layer
        groundMask = ~(1 << LayerMask.NameToLayer("Player"));

        // Lock cursor for better mouse look
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        HandleInput();
        HandleGroundCheck();
        HandleMovement();
        HandleRotation();
        HandleJump();
        HandleGravity();

        // Apply final movement
        controller.Move(velocity * Time.deltaTime);

        // Update autorun cooldown
        if (autorunToggleCooldown > 0)
            autorunToggleCooldown -= Time.deltaTime;
    }

    void HandleInput()
    {
        // Get movement input
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        // Get mouse button states
        leftMouseHeld = Input.GetMouseButton(0);
        rightMouseHeld = Input.GetMouseButton(1);
        bothMouseHeld = leftMouseHeld && rightMouseHeld;

        // Jump input
        jumpPressed = Input.GetButtonDown("Jump");

        // Autorun toggle
        if (Input.GetKeyDown(KeyCode.Numlock) && autorunToggleCooldown <= 0)
        {
            isAutorunning = !isAutorunning;
            autorunToggleCooldown = 0.2f; // Prevent rapid toggling
        }

        // Cancel autorun on any movement input
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            isAutorunning = false;
        }
    }

    void HandleGroundCheck()
    {
        wasGrounded = isGrounded;

        // Sphere cast for ground detection
        Vector3 spherePosition = transform.position + Vector3.down * 0.1f;
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundMask);

        // Landing detection
        if (!wasGrounded && isGrounded)
        {
            // Reset jump momentum on landing
            jumpMomentum = 0f;
        }
    }

    void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        // Both mouse buttons = forward movement
        if (bothMouseHeld)
        {
            moveDirection += transform.forward;
        }
        // Autorun
        else if (isAutorunning)
        {
            moveDirection += transform.forward;
        }
        // WASD Movement
        else
        {
            // Get camera relative directions
            Vector3 cameraForward = cameraController.transform.forward;
            Vector3 cameraRight = cameraController.transform.right;

            // Flatten directions
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Calculate move direction
            if (rightMouseHeld)
            {
                // Right mouse held: character moves relative to its own rotation
                moveDirection = transform.forward * vertical + transform.right * horizontal;
            }
            else
            {
                // No right mouse: character moves relative to camera
                moveDirection = cameraForward * vertical + cameraRight * horizontal;
            }
        }

        // Normalize diagonal movement
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // Apply different speeds based on movement direction
        float currentSpeed = walkSpeed;
        if (Vector3.Dot(moveDirection, transform.forward) < -0.5f)
        {
            // Moving backwards
            currentSpeed = backwardSpeed;
        }
        else if (Mathf.Abs(Vector3.Dot(moveDirection, transform.right)) > 0.7f)
        {
            // Strafing
            currentSpeed = strafeSpeed;
        }

        // Apply movement with jump momentum
        Vector3 horizontalVelocity = moveDirection * currentSpeed;

        // Preserve some momentum while jumping (WoW-style)
        if (!isGrounded && jumpMomentum > 0)
        {
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, moveDirection * currentSpeed * 1.1f, jumpMomentum);
        }

        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
    }

    void HandleRotation()
    {
        // Only rotate character with right mouse button
        if (rightMouseHeld && !bothMouseHeld)
        {
            float mouseX = Input.GetAxis("Mouse X");

            if (Mathf.Abs(mouseX) > 0.01f)
            {
                // Rotate character with camera
                float rotationAmount = mouseX * rotationSpeed * Time.deltaTime;
                transform.Rotate(0, rotationAmount, 0);
            }
        }

        // Face movement direction when moving without right mouse
        if (!rightMouseHeld && !bothMouseHeld && (horizontal != 0 || vertical != 0))
        {
            Vector3 moveDirection = new Vector3(velocity.x, 0, velocity.z);
            if (moveDirection != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    Time.deltaTime * 10f); // Smooth rotation
            }
        }
    }

    void HandleJump()
    {
        if (jumpPressed && isGrounded)
        {
            // Calculate jump velocity
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Store jump momentum for air control
            jumpMomentum = 1f;
        }

        // Reduce jump momentum over time
        if (!isGrounded && jumpMomentum > 0)
        {
            jumpMomentum -= Time.deltaTime * 2f; // Decay rate
            jumpMomentum = Mathf.Clamp01(jumpMomentum);
        }
    }

    void HandleGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            // Small downward force to keep grounded
            velocity.y = -2f;
        }
        else
        {
            // Apply gravity
            velocity.y += gravity * Time.deltaTime;

            // Terminal velocity
            velocity.y = Mathf.Max(velocity.y, -53f);
        }
    }

    // For debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 spherePosition = transform.position + Vector3.down * 0.1f;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}