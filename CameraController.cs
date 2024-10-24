using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target;
    public DungeonGenerator dungeonGenerator;
    public float smoothSpeed = 0.125f;
    public float minZoom = 5f;
    public float maxZoom = 15f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null || dungeonGenerator == null)
        {
            Debug.LogWarning("Camera target or DungeonGenerator not set.");
            return;
        }

        // Move camera
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        // Zoom camera based on revealed percentage
        float revealedPercentage = dungeonGenerator.GetRevealedPercentage();
        float newSize = Mathf.Lerp(minZoom, maxZoom, revealedPercentage);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, newSize, smoothSpeed);
    }
}