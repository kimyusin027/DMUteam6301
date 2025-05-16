using UnityEngine;

public class ObjectCollisionHandler : MonoBehaviour
{
    private PlayerObjectGrabber grabber;

    public void SetGrabber(PlayerObjectGrabber grabber)
    {
        this.grabber = grabber;
    }

   /* void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor")) // 벽과 닿으면 이동 차단
        {
            grabber.SetBlocked(true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor")) // 벽에서 떨어지면 이동 가능
        {
            grabber.SetBlocked(false);
        }
    }*/
}
