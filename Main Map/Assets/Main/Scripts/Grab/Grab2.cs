using UnityEngine;

public class PlayerObjectGrabber : MonoBehaviour
{
    public Camera playerCamera;
    public Transform holdPosition;
    public float moveSpeed = 10f;
    public float grabRange = 3f;
    public float grabRadius = 1.5f;

    private GameObject grabbedObject;
    private Rigidbody grabbedRb;
    private bool isBlocked = false; // 벽에 막혀있는지 여부
    private string originalTag;  // 원래 태그 저장
    private Collider grabbedCollider;
    private Collider playerCollider;

    void Start()
    {
        // 플레이어의 Collider 가져오기 (자동 탐색)
        playerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryGrabObject();
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseObject();
        }

        if (grabbedObject && !isBlocked) // 벽에 막히지 않았을 때만 이동
        {
            MoveObjectWithPlayer();
        }
    }

    void TryGrabObject()
    {
        Vector3 sphereCenter = playerCamera.transform.position + playerCamera.transform.forward * grabRange / 2;
        Collider[] colliders = Physics.OverlapSphere(sphereCenter, grabRadius);

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Grabbable"))
            {
                grabbedObject = collider.gameObject;
                grabbedRb = grabbedObject.GetComponent<Rigidbody>();

                if (grabbedRb)
                {
                    grabbedRb.useGravity = false;
                    grabbedRb.freezeRotation = true;
                    grabbedRb.isKinematic = true;
                }

                // **태그 변경 (기존 태그 저장)**
                originalTag = grabbedObject.tag;
                grabbedObject.tag = "Untagged";  // 임시 태그로 변경

                return; // 가장 가까운 오브젝트 하나만 잡기
            }
        }
    }
    void MoveObjectWithPlayer()
    {
        if (grabbedObject && holdPosition)
        {
            grabbedObject.transform.position = Vector3.Lerp(
                grabbedObject.transform.position,
                holdPosition.position,
                Time.deltaTime * moveSpeed
            );
        }
    }

    void ReleaseObject()
    {
        if (grabbedRb)
        {
            grabbedRb.useGravity = true;
            grabbedRb.freezeRotation = false;
            grabbedRb.isKinematic = false;
        }

        // **태그 복원**
        if (grabbedObject)
        {
            grabbedObject.tag = originalTag;
        }

        grabbedObject = null;
        grabbedRb = null;
        isBlocked = false; // 초기화
    }

    // 벽에 닿으면 이동 차단
    /*public void SetBlocked(bool blocked)
    {
        isBlocked = blocked;
    }*/
}
