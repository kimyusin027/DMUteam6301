using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 2f;

    public float crouchHeight = 1f;
    public float standingHeight = 1.5f;
    public float crouchSpeedMultiplier = 0.5f;

    public Camera playerCamera;
    public float zoomSpeed = 10f;
    public float minFOV = 30f;
    public float maxFOV = 90f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning = false;
    private bool isCrouching = false;

    public LayerMask ceilingMask;
    public float ceilingCheckRadius = 0.3f;
    public float ceilingCheckDistance = 1.1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        HandleCrouch();
        Move();
        Jump();
        ZoomCamera();

        // 중력 처리
        velocity.y += gravity * Time.deltaTime;
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
    }

    void HandleCrouch()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (!isCrouching)
            {
                isCrouching = true;
                controller.height = crouchHeight;
                controller.center = new Vector3(0, 0.2f, 0);
            }
        }
        else
        {
            if (isCrouching && !IsCeilingBlocked())
            {
                isCrouching = false;
                controller.height = standingHeight;
                controller.center = new Vector3(0, 0, 0);
            }
        }
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 1f)
            move.Normalize();

        if (isGrounded)
            isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? moveSpeed * 1.3f : moveSpeed;

        Vector3 horizontal = move * currentSpeed;

        controller.Move(horizontal * Time.deltaTime);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    bool IsCeilingBlocked()
    {
        Vector3 start = transform.position + Vector3.up * (controller.height / 2f);
        Vector3 end = transform.position + Vector3.up * standingHeight;
        return Physics.CheckCapsule(start, end, ceilingCheckRadius, ceilingMask);
    }

    void ZoomCamera()
    {
        if (playerCamera == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            playerCamera.fieldOfView -= scroll * zoomSpeed;
            playerCamera.fieldOfView = Mathf.Clamp(playerCamera.fieldOfView, minFOV, maxFOV);
        }
    }
}
