using UnityEngine;
using UnityEngine.InputSystem;

public class ColliderController_NewInput : MonoBehaviour
{
    public GameObject[] cylinders; // Q, W, E, R, T
    public float standingHeight = 5f;

    [Header("Plane Restriction")]
    public Transform restrictionPlane; // Reference to the plane in your scene
    public float planeOffset = 0.1f; // Small offset above the plane surface

    [Header("Color Tint Filters")]
    public Material screenTintMaterial; // Material with a tint shader
    public Color qTintColor = new Color(1f, 0f, 0f, 0.3f); // Red tint
    public Color wTintColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange tint
    public Color defaultTintColor = new Color(1f, 1f, 1f, 0f); // No tint

    private bool[] isStanding;
    private int activeIndex = 0;
    private int previousActiveIndex = -1;

    void Start()
    {
        isStanding = new bool[cylinders.Length];

        for (int i = 0; i < cylinders.Length; i++)
        {
            cylinders[i].SetActive(true);
            isStanding[i] = false;

            // Snap cylinder to plane on start
            SnapCylinderToPlane(cylinders[i].transform);
        }

        // Initialize tint to default (no tint)
        UpdateScreenTint();

        Debug.Log($"✅ Cylinder controller initialized - {cylinders.Length} cylinders restricted to plane");
    }

    void Update()
    {
        HandleKeyboardInput();
        MoveActiveCylinderWithMouse();
        HandleMouseClick();

        // Constrain all cylinders to the plane (only non-standing ones)
        ConstrainAllCylindersToPlane();
    }

    void HandleKeyboardInput()
    {
        bool indexChanged = false;

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            activeIndex = 0;
            indexChanged = true;
        }
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            activeIndex = 1;
            indexChanged = true;
        }
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            activeIndex = 2;
            indexChanged = true;
        }
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            activeIndex = 3;
            indexChanged = true;
        }
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            activeIndex = 4;
            indexChanged = true;
        }

        // Update screen tint if active index changed
        if (indexChanged && previousActiveIndex != activeIndex)
        {
            UpdateScreenTint();
            previousActiveIndex = activeIndex;
        }
    }

    void MoveActiveCylinderWithMouse()
    {
        if (Mouse.current == null || restrictionPlane == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (isStanding[activeIndex])
        {
            // CHANGED: For standing cylinders, create a plane at the cylinder's current height
            Vector3 cylinderCurrentPos = cylinders[activeIndex].transform.position;
            Plane heightPlane = new Plane(restrictionPlane.up, cylinderCurrentPos);

            if (heightPlane.Raycast(ray, out float distance))
            {
                Vector3 targetPos = ray.GetPoint(distance);
                cylinders[activeIndex].transform.position = targetPos;
            }
        }
        else
        {
            // Original behavior for non-standing cylinders
            Plane restrictionPlaneGeometry = new Plane(restrictionPlane.up, restrictionPlane.position);

            if (restrictionPlaneGeometry.Raycast(ray, out float distance))
            {
                Vector3 targetPos = ray.GetPoint(distance);
                targetPos += restrictionPlane.up * planeOffset;
                cylinders[activeIndex].transform.position = targetPos;
            }
        }
    }

    void HandleMouseClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isStanding[activeIndex] = !isStanding[activeIndex];

            // Immediately apply the height change
            if (cylinders[activeIndex] != null)
            {
                Vector3 currentPlanePos = GetPlanePosition(cylinders[activeIndex].transform.position);

                if (isStanding[activeIndex])
                {
                    // Move to plane position + standing height
                    cylinders[activeIndex].transform.position = currentPlanePos + restrictionPlane.up * standingHeight;
                    Debug.Log($"🔼 Cylinder {activeIndex} snapped UP to height {standingHeight}");
                }
                else
                {
                    // Snap back to plane
                    cylinders[activeIndex].transform.position = currentPlanePos;
                    Debug.Log($"🔽 Cylinder {activeIndex} snapped DOWN to plane");
                }
            }
        }
    }

    // Only constrain cylinders that are NOT standing
    void ConstrainAllCylindersToPlane()
    {
        if (restrictionPlane == null) return;

        for (int i = 0; i < cylinders.Length; i++)
        {
            if (cylinders[i] != null && !isStanding[i])
            {
                SnapCylinderToPlane(cylinders[i].transform);
            }
        }
    }

    // Only snap cylinders that are NOT standing
    void SnapCylinderToPlane(Transform cylinderTransform)
    {
        if (restrictionPlane == null) return;

        int cylinderIndex = System.Array.IndexOf(cylinders, cylinderTransform.gameObject);
        if (cylinderIndex == -1) return;

        // Only snap to plane if the cylinder is NOT standing
        if (!isStanding[cylinderIndex])
        {
            // Project the cylinder's position onto the plane
            Vector3 planeNormal = restrictionPlane.up;
            Vector3 planePoint = restrictionPlane.position;
            Vector3 cylinderPos = cylinderTransform.position;

            // Calculate the projection onto the plane
            Vector3 projectedPos = cylinderPos - Vector3.Dot(cylinderPos - planePoint, planeNormal) * planeNormal;

            // Apply offset along the plane's normal
            projectedPos += planeNormal * planeOffset;

            cylinderTransform.position = projectedPos;
        }
    }

    // Public method to get the plane-restricted position (with rotation)
    public Vector3 GetPlanePosition(Vector3 worldPosition)
    {
        if (restrictionPlane != null)
        {
            Vector3 planeNormal = restrictionPlane.up;
            Vector3 planePoint = restrictionPlane.position;

            // Project onto the plane
            worldPosition = worldPosition - Vector3.Dot(worldPosition - planePoint, planeNormal) * planeNormal;

            // Apply offset
            worldPosition += planeNormal * planeOffset;
        }
        return worldPosition;
    }

    // Update screen tint based on active cylinder
    void UpdateScreenTint()
    {
        if (screenTintMaterial == null)
        {
            Debug.LogWarning("Screen tint material not assigned!");
            return;
        }

        Color targetColor = defaultTintColor;

        switch (activeIndex)
        {
            case 0: // Q cylinder - Red tint
                targetColor = qTintColor;
                Debug.Log("🔴 Q cylinder active - Red tint applied");
                break;
            case 1: // W cylinder - Orange tint
                targetColor = wTintColor;
                Debug.Log("🟠 W cylinder active - Orange tint applied");
                break;
            default: // E, R, T cylinders - No tint
                targetColor = defaultTintColor;
                Debug.Log("⚪ Other cylinder active - No tint");
                break;
        }

        // Apply the tint color to the material
        screenTintMaterial.color = targetColor;
    }

    // Public method to check if a position is on the plane (with rotation)
    public bool IsPositionOnPlane(Vector3 position)
    {
        if (restrictionPlane == null) return true;

        Vector3 planeNormal = restrictionPlane.up;
        Vector3 planePoint = restrictionPlane.position + planeNormal * planeOffset;

        // Calculate distance from point to plane
        float distance = Vector3.Dot(position - planePoint, planeNormal);
        return Mathf.Abs(distance) < 0.01f;
    }

    // Get the current plane's world position and normal
    public (Vector3 position, Vector3 normal) GetPlaneInfo()
    {
        if (restrictionPlane != null)
        {
            Vector3 planePosition = restrictionPlane.position + restrictionPlane.up * planeOffset;
            return (planePosition, restrictionPlane.up);
        }
        return (Vector3.zero, Vector3.up);
    }

    // Clean up when disabled
    private void OnDisable()
    {
        // Reset tint to default when controller is disabled
        if (screenTintMaterial != null)
        {
            screenTintMaterial.color = defaultTintColor;
        }
    }

    // Optional: Visualize the plane in Scene view (with rotation)
    private void OnDrawGizmos()
    {
        if (restrictionPlane != null)
        {
            Gizmos.color = Color.cyan;

            // Draw the plane as a rotated quad
            Vector3 planeCenter = restrictionPlane.position + restrictionPlane.up * planeOffset;
            Vector3 planeRight = restrictionPlane.right;
            Vector3 planeForward = restrictionPlane.forward;

            float size = 10f;
            Vector3 corner1 = planeCenter + (planeRight * size) + (planeForward * size);
            Vector3 corner2 = planeCenter + (planeRight * size) - (planeForward * size);
            Vector3 corner3 = planeCenter - (planeRight * size) - (planeForward * size);
            Vector3 corner4 = planeCenter - (planeRight * size) + (planeForward * size);

            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner4);
            Gizmos.DrawLine(corner4, corner1);

            // Draw plane normal
            Gizmos.color = Color.red;
            Gizmos.DrawRay(planeCenter, restrictionPlane.up * 2f);

            // Draw lines from cylinders to plane
            Gizmos.color = Color.yellow;
            if (cylinders != null)
            {
                foreach (var cylinder in cylinders)
                {
                    if (cylinder != null)
                    {
                        Vector3 cylinderPos = cylinder.transform.position;
                        Vector3 projectedPos = GetPlanePosition(cylinderPos);
                        Gizmos.DrawLine(cylinderPos, projectedPos);
                        Gizmos.DrawWireSphere(projectedPos, 0.2f);
                    }
                }
            }
        }
    }
}