using UnityEngine;

public class Player : MonoBehaviour
{
    // 이동
    public float speed = 5f;
    public float mouseSensor = 2f;
    public float interactDistance = 1f;
    public Rigidbody rb;

    float hAxis;
    float vAxis;
    float mouseX;
    //float mouseY;

    // 점프
    bool isJump;

    // Run
    bool wDown;
    bool jDown;

    Vector3 moveVec;
    //Camera playerCamera;
    float xRotation = 0f;

    //Animator anim;
    Rigidbody rigid;



    void Start() // 초기설정
    {
        rigid = GetComponent<Rigidbody>();
        //anim = GetComponentInChildren<Animator>();
        //playerCamera = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
    }

    
    void Update() // 메인
    {
        GetInput();
        Move();
        //Turn();
        LookAround();
        Jump();
        Interact();
        //Anime();
    }


    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        jDown = Input.GetButtonDown("Jump");
        wDown = Input.GetButton("Walk");

        mouseX = Input.GetAxis("Mouse X") * mouseSensor;
    }

    void Move() // 이동
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0;
        right.y = 0;

        moveVec = (forward * vAxis + right * hAxis).normalized;
        float moveSpeed = speed * (wDown && !isJump ? 1.5f : 1f);

        Vector3 targetPos = rb.position + moveVec * moveSpeed * Time.deltaTime;

        rb.MovePosition(targetPos);
    }
    void LookAround() // 플레이어 마우스 회전
    {
        // 좌우 회전 (Y축 기준)
        //float yRotation = mouseX * mouseSensor;
        transform.Rotate(Vector3.up * mouseX);

        // 상하 회전 (X축 기준, 카메라만 회전)
        //xRotation -= mouseY;
        //xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        //playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    /*
    void Turn() // 플레이어 회전
    {
        transform.LookAt(transform.position + moveVec);
    }
    */

    void Jump() // 점프
    {
        if (jDown && !isJump)
        {
            rigid.AddForce(Vector3.up * 7.5f, ForceMode.Impulse);
            //anim.SetBool("isJump", true);
            //anim.SetTrigger("doJump");
            isJump = true;
        }
    }

    void OnCollisionEnter(Collision collision) // 점프 착지 판정
    {
        if (collision.gameObject.tag == "Floor")
        {
            //anim.SetBool("isJump", false);
            isJump = false;
        }
    }

    void FreezeRotation() // 자동회전 방지
    {
        rigid.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        FreezeRotation();
    }

    /*void Anime()
    {
        //transform.position += moveVec * speed * (wDown ? 0.1f : 0.3f) * Time.deltaTime;
        
        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);
    }*/

    void Interact()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryDoor();
        }
    }

    void TryDoor()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider.CompareTag("Door"))
            {
                // 문과 연결된 텔레포트 위치 가져오기
                Door door = hit.collider.GetComponent<Door>();
                if (door != null && door.tpTarget != null)
                {
                    transform.position = door.tpTarget.position;
                }
            }
        }
    }
}