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

    public AudioClip footstepSound;
    private AudioSource audioSource;

    public AudioClip landingSfx;            // 착지 소리용 사운드 클립
    private AudioSource landingAudioSource; // 착지 소리 재생용 오디오 소스
    private bool wasGrounded;
    private bool hasJumped = false; // 점프했는지 상태 저장
    private float airTime = 0f;
    public float airTimeThreshold = 1f; // 2초 이상 공중에 있으면 착지음 재생

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = footstepSound;
        audioSource.loop = false;  // 한 번만 재생
        audioSource.playOnAwake = false;

        // 착지 소리용 오디오 소스 생성
        landingAudioSource = gameObject.AddComponent<AudioSource>();
        landingAudioSource.loop = false;
        landingAudioSource.playOnAwake = false;
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            // 착지 조건: 
            // 1) 점프 후 착지
            // 2) 2초 이상 공중에 있다가 착지
            if ((hasJumped || airTime >= airTimeThreshold) && !wasGrounded)
            {
                landingAudioSource.PlayOneShot(landingSfx);
                hasJumped = false;
                airTime = 0f;
            }
        }
        else
        {
            // 공중에 떠있으면 시간 누적
            airTime += Time.deltaTime;
        }

        wasGrounded = isGrounded;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            hasJumped = true;
            airTime = 0f; // 점프할 땐 공중시간 초기화
        }

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

        // 소리 재생 또는 정지 처리
        if (isGrounded && move.magnitude > 0.1f)
        {
            audioSource.pitch = isRunning ? 1.2f : 1.0f;

            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop(); // 움직임 멈추면 즉시 소리 중단
            }
        }
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
