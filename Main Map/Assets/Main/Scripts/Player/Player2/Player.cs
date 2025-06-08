using NUnit.Framework.Interfaces;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 2f;
    public float jumpForce = 9f;
    public float interactDistance = 1.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    bool isRunning = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        Move();
        Jump();


        Vector3 vertical = Vector3.up * velocity.y;

        // 중력만 velocity에 유지
        velocity.y += gravity * Time.deltaTime;

        controller.Move((vertical) * Time.deltaTime);
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 1f)
            move = move.normalized;

        if (isGrounded)
            isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? moveSpeed * 1.25f : moveSpeed;

        Vector3 horizontal = move * currentSpeed;

        controller.Move((horizontal) * Time.deltaTime);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

}
