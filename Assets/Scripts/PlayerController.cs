using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
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

    [Header("Air Control")]
    [SerializeField] private float airControl = 0.65f; // Wie stark man in der Luft steuern kann (0-1)
    [SerializeField] private float airAcceleration = 18f; // Beschleunigung in der Luft
    [SerializeField] private float maxAirSpeed = 7f; // Maximale Geschwindigkeit in der Luft

    [Header("Camera Reference")]
    [SerializeField] private CameraController cameraController;

    // Components
    private CharacterController controller;

    // Movement state
    private Vector3 velocity;
    private Vector3 horizontalVelocity; // Separate horizontale Geschwindigkeit
    private bool isGrounded;
    private bool wasGrounded;
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
    }

    void Update()
    {
        HandleInput();
        HandleGroundCheck();
        HandleMovement();
        HandleRotation();
        HandleJump();
        HandleGravity();

        // Combine horizontal and vertical velocity
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;

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
            // Smooth landing - don't abruptly stop horizontal movement
            // Just let ground movement take over naturally
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
                // WoW-Style: Mit rechter Maustaste bewegt sich der Character relativ zur Kamera
                moveDirection = cameraForward * vertical + cameraRight * horizontal;
            }
            else
            {
                // Ohne rechte Maus: Character bewegt sich auch relativ zur Kamera
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

        // Ground movement vs Air control
        if (isGrounded)
        {
            // Direct control on ground
            horizontalVelocity = moveDirection * currentSpeed;
        }
        else
        {
            // Air control - kann Bewegung beeinflussen aber nicht sofort stoppen
            if (moveDirection.magnitude > 0.1f)
            {
                Vector3 targetVelocity = moveDirection * currentSpeed * 0.8f; // 80% der Bodengeschwindigkeit

                // Direkte Luftbewegung für bessere Kontrolle
                Vector3 velocityChange = targetVelocity - horizontalVelocity;
                velocityChange *= airControl;

                horizontalVelocity += velocityChange * airAcceleration * Time.deltaTime;

                // Begrenze maximale Luftgeschwindigkeit
                if (horizontalVelocity.magnitude > maxAirSpeed)
                {
                    horizontalVelocity = horizontalVelocity.normalized * maxAirSpeed;
                }
            }
            // Kein Input = behalte momentum (keine Bremsung in der Luft)
        }
    }

    void HandleRotation()
    {
        // WoW-Style: NUR Rechte Maustaste = Character schaut in Kamera-Richtung
        // WICHTIG: leftMouseHeld darf NICHT den Character drehen!
        if (rightMouseHeld && !leftMouseHeld)
        {
            // Character rotiert zur Kamera-Richtung
            Vector3 cameraForward = cameraController.transform.forward;
            cameraForward.y = 0; // Nur horizontale Rotation

            if (cameraForward != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(cameraForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    Time.deltaTime * 15f); // Schnelle Rotation zur Kamera
            }
        }
        // Beide Maustasten = auch Character-Rotation zur Kamera (für Vorwärtslaufen)
        else if (bothMouseHeld)
        {
            Vector3 cameraForward = cameraController.transform.forward;
            cameraForward.y = 0;

            if (cameraForward != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(cameraForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                    Time.deltaTime * 15f);
            }
        }
        // Keyboard Movement ohne Maus: Character dreht sich in Bewegungsrichtung
        else if (!leftMouseHeld && !rightMouseHeld && !bothMouseHeld &&
                 (horizontal != 0 || vertical != 0) && isGrounded)
        {
            Vector3 moveDirection = new Vector3(horizontalVelocity.x, 0, horizontalVelocity.z);
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

            // Bei Bewegungsinput beim Springen: Leichter Geschwindigkeitsboost
            if (horizontal != 0 || vertical != 0)
            {
                // Füge einen kleinen Boost in Bewegungsrichtung hinzu
                Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;
                Vector3 moveBoost = Vector3.zero;

                if (rightMouseHeld)
                {
                    moveBoost = transform.forward * vertical + transform.right * horizontal;
                }
                else
                {
                    Vector3 camForward = cameraController.transform.forward;
                    Vector3 camRight = cameraController.transform.right;
                    camForward.y = 0;
                    camRight.y = 0;
                    camForward.Normalize();
                    camRight.Normalize();
                    moveBoost = camForward * vertical + camRight * horizontal;
                }

                // Füge 30% der Walk-Speed als Boost hinzu
                horizontalVelocity += moveBoost.normalized * walkSpeed * 0.3f;
            }
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

        // Zeige aktuelle Geschwindigkeit
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, horizontalVelocity);
        }
    }
}