using UnityEngine;

public class StageLightController : MonoBehaviour
{
    public Camera mainCamera;         // assign in Inspector or will auto-find Camera.main
    public float groundY = 0f;        // world Y of the floor / stage
    public float yOffset = 2f;        // how much above the ground the cylinder's center should sit
    public float followSpeed = 12f;   // smoothing
    public bool lockRotationDown = true; // make cylinder point downwards (like a light)

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update()
    {
        // Create a horizontal plane at groundY
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));

        // Cast a ray from the camera through the mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter); // point on the plane under the mouse

            // Target position: same X,Z as hit; Y = groundY + yOffset
            Vector3 targetPos = new Vector3(hitPoint.x, groundY + yOffset, hitPoint.z);

            // Smoothly follow
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            // Optionally keep the cylinder pointing down like a stage light
            if (lockRotationDown)
            {
                // Adjust rotation so cylinder's local -Y (or local up depending on model) points to ground
                // Commonly you'd rotate 90 degrees on X if your cylinder's axis is Y; adapt if needed.
                transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                // If that doesn't orient correctly, try:
                // transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }
    }
}
