using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 0.125f;

    private Vector3 offset;

    void Start()
    {
        offset = transform.position - target.position;
        offset.z = transform.position.z; // Keep the camera's original z position
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.x = Mathf.Round(desiredPosition.x);
        desiredPosition.y = Mathf.Round(desiredPosition.y);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}