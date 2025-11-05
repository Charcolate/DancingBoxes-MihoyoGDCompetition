using UnityEngine;
using UnityEngine.InputSystem;

public class SphereSample : MonoBehaviour
{
    public GameObject[] cylinders;
    public float standingOffset = 5f;
    public Transform sphereCenterTransform;
    public float sphereRadius = 10f;

    private bool[] isStanding;
    private int activeIndex = 0;

    void Start()
    {
        isStanding = new bool[cylinders.Length];

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
            while (Vector3.Dot(randomDir, Camera.main.transform.forward) < 0f); // repeat until in front

            Vector3 randomPos = sphereCenterTransform.position + randomDir * sphereRadius;
            cylinders[i].transform.position = randomPos;
            cylinders[i].transform.up = randomDir;
        }
    }

    void Update()
    {
        HandleKeyboardInput();
        MoveActiveCylinderOnSphere();
        HandleMouseClick();
    }

    void HandleKeyboardInput()
    {
        if (Keyboard.current.qKey.wasPressedThisFrame) activeIndex = 0;
        if (Keyboard.current.wKey.wasPressedThisFrame) activeIndex = 1;
        if (Keyboard.current.eKey.wasPressedThisFrame) activeIndex = 2;
        if (Keyboard.current.rKey.wasPressedThisFrame) activeIndex = 3;
        if (Keyboard.current.tKey.wasPressedThisFrame) activeIndex = 4;
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
        if (t < 0) t = Mathf.Max(t1, t2); // choose positive t (in front of camera)
        if (t < 0) return; // both behind camera

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