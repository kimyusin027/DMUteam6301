using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class MainCam : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    void Update()
    {
        transform.position = target.position + offset;
    }
}