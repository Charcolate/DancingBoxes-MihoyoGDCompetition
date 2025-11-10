using UnityEngine;

public class SphericalCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The wanderer transform to follow

    [Header("Camera Settings")]
    public float height = 15f; // Height above the target
    public float distance = 8f; // Distance behind the target (if not purely top-down)
    public Vector3 offset = Vector3.zero; // Additional offset

    [Header("Follow Settings")]
    public float smoothSpeed = 5f; // How smoothly the camera follows
    public bool pureTopDown = true; // If true, camera is directly above looking straight down

    [Header("Rotation Settings")]
    public bool followTargetRotation = false; // Whether to rotate with the target
    public float rotationSmoothSpeed = 5f; // How smoothly the camera rotates

    private Vector3 desiredPosition;
    private Quaternion desiredRotation;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("TopDownFollowCamera: No target assigned!");
            return;
        }

        // Initialize camera position immediately
        UpdateDesiredPositionAndRotation();
        transform.position = desiredPosition;
        transform.rotation = desiredRotation;
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateDesiredPositionAndRotation();

        // Smoothly move to desired position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Smoothly rotate to desired rotation
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    void UpdateDesiredPositionAndRotation()
    {
        if (pureTopDown)
        {
            // Pure top-down: directly above target, looking straight down
            desiredPosition = target.position + Vector3.up * height + offset;
            desiredRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        }
        else
        {
            // Top-down but slightly angled, following target's forward direction
            Vector3 behindOffset = -target.forward * distance;
            desiredPosition = target.position + Vector3.up * height + behindOffset + offset;

            if (followTargetRotation)
            {
                // Match target's rotation but maintain top-down angle
                Vector3 lookDirection = (target.position - desiredPosition).normalized;
                desiredRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            }
            else
            {
                // Standard top-down angled view
                desiredRotation = Quaternion.LookRotation((target.position - desiredPosition).normalized, Vector3.up);
            }
        }
    }

    // Public methods to adjust camera settings at runtime
    public void SetHeight(float newHeight)
    {
        height = newHeight;
    }

    public void SetDistance(float newDistance)
    {
        distance = newDistance;
    }

    public void SetPureTopDown(bool isPureTopDown)
    {
        pureTopDown = isPureTopDown;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    // Snap immediately to target (no smooth transition)
    public void SnapToTarget()
    {
        UpdateDesiredPositionAndRotation();
        transform.position = desiredPosition;
        transform.rotation = desiredRotation;
    }

    // Gizmos to visualize camera relationship in scene view
    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, 0.5f);

        Gizmos.color = Color.blue;
        if (pureTopDown)
        {
            Vector3 topDownPos = target.position + Vector3.up * height;
            Gizmos.DrawWireSphere(topDownPos, 0.3f);
            Gizmos.DrawLine(topDownPos, target.position);
        }
        else
        {
            Vector3 angledPos = target.position + Vector3.up * height - target.forward * distance;
            Gizmos.DrawWireSphere(angledPos, 0.3f);
            Gizmos.DrawLine(angledPos, target.position);
        }
    }
}