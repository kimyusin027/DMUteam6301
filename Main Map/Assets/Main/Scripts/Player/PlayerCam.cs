/*using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public Transform target; // 플레이어
    public float sensor = 2f; // 마우스 감도
    public float maxCam = 90f; // 카메라 상하 최대 각도

    public Vector3 offset = new Vector3(0, 1.6f, 0);

    float verticalRotation = 0f;
    float horizontalRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // 마우스 고정
    }

    void Update()
    {
        RotateCam();
    }

    void RotateCam()
    {
        // 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * sensor;
        float mouseY = Input.GetAxis("Mouse Y") * sensor;

        // 좌우
        horizontalRotation += mouseX;
        target.rotation = Quaternion.Euler(0, horizontalRotation, 0);

        // 카메라(상하 회전)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxCam, maxCam);
        transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);

        target.rotation = Quaternion.Euler(0, -horizontalRotation, 0);

        // 카메라 위치를 플레이어와 동기화
        transform.position = target.position + target.TransformDirection(offset);
    }
}*/
/*using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerCam : MonoBehaviour
{
    public Transform target;  // 플레이어
    public Vector3 offset;    // 카메라 오프셋

    public float mouseSensitivity = 2f; // 마우스 감도
    private float xRotation = 0f;       // 상하 회전 값
    private float yRotation = 0f;       // 좌우 회전 값

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서 고정
    }

    void Update()
    {
        LookAround();
    }

    void LookAround()
    {
        // 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 좌우(Y축) 회전
        yRotation += mouseX;

        // 상하(X축) 회전 (90도 제한)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 카메라 위치를 플레이어 기준으로 업데이트
        transform.position = target.position + offset;
        transform.rotation = target.rotation;
    }
}*/
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    public Transform player;  // 플레이어를 따라갈 대상
    public Vector3 offset;
    public float mouseSensor = 2f;

    float xRotation = 0f;
    float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensor;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensor;

        // 상하 회전 (카메라만 회전)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 좌우 회전 (카메라 + 플레이어 함께 회전)
        yRotation += mouseX;

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        transform.position = player.position + offset;

        // 플레이어도 카메라의 Y축 회전을 따라가도록 설정
        player.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
