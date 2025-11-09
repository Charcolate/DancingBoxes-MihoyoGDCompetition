using UnityEngine;
using UnityEngine.InputSystem;

public class SphereSample : MonoBehaviour
{
    public GameObject[] cylinders;
    public float standingOffset = 5f;
    public Transform sphereCenterTransform;
    public float sphereRadius = 10f;

    [Header("UI Panels for Each Cylinder")]
    public GameObject qUI; // UI for Q cylinder
    public GameObject wUI; // UI for W cylinder  
    public GameObject eUI; // UI for E cylinder
    public GameObject rUI; // UI for R cylinder
    public GameObject tUI; // UI for T cylinder

    [Header("Color Tint Filters")]
    public Material screenTintMaterial; // Material with a tint shader
    public Color qTintColor = new Color(1f, 0f, 0f, 0.3f); // Red tint
    public Color wTintColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange tint
    public Color eTintColor = new Color(1f, 1f, 0f, 0.3f); // Yellow tint
    public Color rTintColor = new Color(0f, 1f, 0f, 0.3f); // Green tint
    public Color tTintColor = new Color(0f, 0f, 1f, 0.3f); // Blue tint
    public Color defaultTintColor = new Color(1f, 1f, 1f, 0f); // No tint

    private bool[] isStanding;
    private int activeIndex = 0;
    private int previousActiveIndex = -1;
    private GameObject[] uiPanels;

    void Start()
    {
        isStanding = new bool[cylinders.Length];
        uiPanels = new GameObject[] { qUI, wUI, eUI, rUI, tUI };

        // Initialize all cylinders
        for (int i = 0; i < cylinders.Length; i++)
        {
            cylinders[i].SetActive(true);
            isStanding[i] = false;

            // Generate a random point on the camera-facing hemisphere
            Vector3 randomDir;
            do
            {
                randomDir = Random.onUnitSphere;
            }
            while (Vector3.Dot(randomDir, Camera.main.transform.forward) < 0f);

            Vector3 randomPos = sphereCenterTransform.position + randomDir * sphereRadius;
            cylinders[i].transform.position = randomPos;
            cylinders[i].transform.up = randomDir;
        }

        // Initialize UI and tint
        UpdateUI();
        UpdateScreenTint();
        Debug.Log($"✅ Sphere controller initialized - {cylinders.Length} cylinders with UI switching");
    }

    void Update()
    {
        HandleKeyboardInput();
        MoveActiveCylinderOnSphere();
        HandleMouseClick();
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

        // Update UI and tint if active index changed
        if (indexChanged && previousActiveIndex != activeIndex)
        {
            UpdateUI();
            UpdateScreenTint();
            previousActiveIndex = activeIndex;
        }
    }

    void MoveActiveCylinderOnSphere()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        Vector3 sphereCenter = sphereCenterTransform.position;
        float r = sphereRadius;

        // Ray-sphere intersection
        Vector3 oc = ray.origin - sphereCenter;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2.0f * Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - r * r;
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            // No intersection
            return;
        }

        float sqrtDisc = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtDisc) / (2 * a);
        float t2 = (-b + sqrtDisc) / (2 * a);

        float t = Mathf.Min(t1, t2);
        if (t < 0) t = Mathf.Max(t1, t2);
        if (t < 0) return;

        Vector3 intersectionPoint = ray.origin + ray.direction * t;
        Vector3 dir = (intersectionPoint - sphereCenter).normalized;

        // Apply standing offset
        if (isStanding[activeIndex])
            intersectionPoint += dir * standingOffset;

        cylinders[activeIndex].transform.position = intersectionPoint;
        cylinders[activeIndex].transform.up = dir;
    }

    void HandleMouseClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isStanding[activeIndex] = !isStanding[activeIndex];

            // Update position immediately when standing state changes
            Vector3 sphereCenter = sphereCenterTransform.position;
            Vector3 currentPos = cylinders[activeIndex].transform.position;
            Vector3 dir = (currentPos - sphereCenter).normalized;

            if (isStanding[activeIndex])
            {
                cylinders[activeIndex].transform.position = sphereCenter + dir * (sphereRadius + standingOffset);
                Debug.Log($"🔼 Cylinder {activeIndex} snapped UP with offset {standingOffset}");
            }
            else
            {
                cylinders[activeIndex].transform.position = sphereCenter + dir * sphereRadius;
                Debug.Log($"🔽 Cylinder {activeIndex} snapped DOWN to sphere surface");
            }
        }
    }

    // Update UI panels - show active, hide others
    void UpdateUI()
    {
        for (int i = 0; i < uiPanels.Length; i++)
        {
            if (uiPanels[i] != null)
            {
                uiPanels[i].SetActive(i == activeIndex);
            }
        }

        // Log which UI is active
        string[] cylinderNames = { "Q", "W", "E", "R", "T" };
        Debug.Log($"🎨 Switched to {cylinderNames[activeIndex]} UI");
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
            case 2: // E cylinder - Yellow tint
                targetColor = eTintColor;
                Debug.Log("🟡 E cylinder active - Yellow tint applied");
                break;
            case 3: // R cylinder - Green tint
                targetColor = rTintColor;
                Debug.Log("🟢 R cylinder active - Green tint applied");
                break;
            case 4: // T cylinder - Blue tint
                targetColor = tTintColor;
                Debug.Log("🔵 T cylinder active - Blue tint applied");
                break;
        }

        // Apply the tint color to the material
        screenTintMaterial.color = targetColor;
    }

    // Public method to get current active cylinder info
    public (int index, string name, bool isStanding) GetActiveCylinderInfo()
    {
        string[] names = { "Q", "W", "E", "R", "T" };
        return (activeIndex, names[activeIndex], isStanding[activeIndex]);
    }

    // Clean up when disabled
    private void OnDisable()
    {
        // Hide all UI panels when controller is disabled
        foreach (var uiPanel in uiPanels)
        {
            if (uiPanel != null)
                uiPanel.SetActive(false);
        }

        // Reset tint to default when controller is disabled
        if (screenTintMaterial != null)
        {
            screenTintMaterial.color = defaultTintColor;
        }
    }

    // -------------------- Gizmos --------------------
    void OnDrawGizmos()
    {
        if (sphereCenterTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sphereCenterTransform.position, sphereRadius);

        if (cylinders != null)
        {
            foreach (var cyl in cylinders)
            {
                if (cyl != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(sphereCenterTransform.position, cyl.transform.position);
                    Gizmos.DrawSphere(cyl.transform.position, 0.2f);
                }
            }
        }
    }
}