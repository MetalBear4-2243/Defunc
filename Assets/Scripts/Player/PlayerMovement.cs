using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Basic 2D platformer movement controller.
/// Controls:
/// - Move Left/Right: A / D or Left / Right Arrow keys
/// - Jump: Spacebar
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal movement speed in units/sec.")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("Instant vertical impulse applied when jumping.")]
    [SerializeField] private float jumpForce = 13f;

    [Header("Ground Detection")]
    [Tooltip("Transform marking the bottom of the player's feet. If empty, the bottom of the Collider2D is used automatically.")]
    [SerializeField] private Transform groundCheck;

    [Tooltip("Radius of the ground overlap sphere.")]
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Tooltip("Which layers count as solid ground.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Tuning & Feel")]
    [Tooltip("Default gravity scale applied when grounded or rising at normal speed.")]
    [SerializeField] private float baseGravityScale = 3f;

    [Tooltip("Multiplier applied to gravity when falling, creating snappier falls.")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Tooltip("Multiplier applied to gravity when the jump button is released early (variable jump height).")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Tooltip("Grace period (seconds) allowing a jump immediately after walking off a ledge.")]
    [SerializeField] private float coyoteTime = 0.12f;

    [Tooltip("Grace period (seconds) remembering a jump input pressed just before touching ground.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    // Components & State
    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private float horizontalInput;
    private bool jumpRequested;
    private bool jumpHeld;
    private bool isFacingRight = true;

    // Timers
    private float coyoteCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Ensure player doesn't tip over and gravity matches tuned base
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = baseGravityScale;

        // If groundLayer was left unset (Nothing), default to "Default" layer so it works out of the box
        if (groundLayer.value == 0)
        {
            groundLayer = LayerMask.GetMask("Default");
        }
    }

    private void Update()
    {
        GatherInput();

        // Manage Coyote Time
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // Manage Jump Buffer
        if (jumpRequested)
        {
            jumpBufferCounter = jumpBufferTime;
            jumpRequested = false;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Trigger Jump if buffer and coyote time are active
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            ExecuteJump();
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        FlipSprite();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
        ApplyBetterJumpPhysics();
    }

    private void GatherInput()
    {
        horizontalInput = 0f;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontalInput -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontalInput += 1f;

            if (keyboard.spaceKey.wasPressedThisFrame)
                jumpRequested = true;

            jumpHeld = keyboard.spaceKey.isPressed;
            return;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontalInput -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontalInput += 1f;

        if (Input.GetKeyDown(KeyCode.Space))
            jumpRequested = true;

        jumpHeld = Input.GetKey(KeyCode.Space);
#endif
    }

    private void CheckGrounded()
    {
        Vector2 checkPos = GetGroundCheckPosition();

        // Check if feet overlap any collider in the designated groundLayer (excluding player's own collider)
        Collider2D hit = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        // Grounded if we hit something that is not our own collider
        isGrounded = hit != null && hit != col;
    }

    private Vector2 GetGroundCheckPosition()
    {
        if (groundCheck != null)
        {
            return groundCheck.position;
        }

        // Automatic fallback: bottom center of the player's Collider2D
        if (col != null)
        {
            return new Vector2(col.bounds.center.x, col.bounds.min.y);
        }

        return transform.position;
    }

    private void ApplyMovement()
    {
        float targetVx = horizontalInput * moveSpeed;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(targetVx, rb.linearVelocity.y);
#else
        rb.velocity = new Vector2(targetVx, rb.velocity.y);
#endif
    }

    private void ExecuteJump()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
#else
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
#endif
    }

    private void ApplyBetterJumpPhysics()
    {
#if UNITY_6000_0_OR_NEWER
        float currentVy = rb.linearVelocity.y;
#else
        float currentVy = rb.velocity.y;
#endif

        if (currentVy < -0.01f)
        {
            // Falling: increase gravity so descent is punchy
            rb.gravityScale = baseGravityScale * fallMultiplier;
        }
        else if (currentVy > 0.01f && !jumpHeld)
        {
            // Rising but player released jump key: shorten jump height
            rb.gravityScale = baseGravityScale * lowJumpMultiplier;
        }
        else
        {
            // Normal upward trajectory or grounded
            rb.gravityScale = baseGravityScale;
        }
    }

    private void FlipSprite()
    {
        if (horizontalInput > 0.01f && !isFacingRight)
        {
            isFacingRight = true;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (horizontalInput < -0.01f && isFacingRight)
        {
            isFacingRight = false;
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkPos = GetGroundCheckPosition();
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
