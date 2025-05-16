using UnityEngine;

public class ObjectGrabber : MonoBehaviour
{
    public Camera cam;
    public float grabDistance = 5f;
    public float moveSpeed = 10f;

    private GameObject grabOb;
    private Rigidbody rigid;
    private Vector3 Vectarget;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 클릭
        {
            TryGrabObject();
        }

        if (Input.GetMouseButtonUp(0)) // 마우스 버튼에서 손을 떼면 놓기
        {
            ReleaseObject();
        }

        if (grabOb)
        {
            MoveObject();
        }
    }

    void TryGrabObject()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, grabDistance))
        {
            if (hit.collider.CompareTag("Grabbable"))  // "Grabbable" 태그가 있는 오브젝트만 잡기 가능
            {
                grabOb = hit.collider.gameObject;
                rigid = grabOb.GetComponent<Rigidbody>();

                if (rigid)
                {
                    rigid.useGravity = false;  // 중력 해제
                    rigid.freezeRotation = true;  // 회전 고정
                    rigid.isKinematic = true;  // Kinematic으로 변경 (velocity 오류 방지)
                }
            }
        }
    }

    void MoveObject()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vectarget = ray.origin + ray.direction * grabDistance;

        if (grabOb)
        {
            grabOb.transform.position = Vector3.Lerp(grabOb.transform.position, Vectarget, Time.deltaTime * moveSpeed);
        }
    }

    void ReleaseObject()
    {
        if (rigid)
        {
            rigid.useGravity = true;
            rigid.freezeRotation = false;
            rigid.isKinematic = false;  // 다시 물리 적용
        }

        grabOb = null;
        rigid = null;
    }
}
