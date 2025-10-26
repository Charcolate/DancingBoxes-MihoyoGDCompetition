using UnityEngine;
using UnityEngine.InputSystem;

public class ColliderController_NewInput : MonoBehaviour
{
    public GameObject[] cylinders; // Q, W, E, R, T
    public float standingHeight = 5f;

    private bool[] isStanding;
    private int activeIndex = 0;

    void Start()
    {
        isStanding = new bool[cylinders.Length];

        for (int i = 0; i < cylinders.Length; i++)
        {
            cylinders[i].SetActive(true);
            isStanding[i] = false;
        }
    }

    void Update()
    {
        HandleKeyboardInput();
        MoveActiveCylinderWithMouse();
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

    void MoveActiveCylinderWithMouse()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // Use cylinder's current Y as the plane height
        float planeY = isStanding[activeIndex] ? standingHeight : 0;

        if (ray.direction.y != 0)
        {
            float distance = (planeY - ray.origin.y) / ray.direction.y;
            Vector3 targetPos = ray.GetPoint(distance);

            targetPos.y = planeY; // ensure Y stays correct
            cylinders[activeIndex].transform.position = targetPos;
        }
    }

    void HandleMouseClick()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isStanding[activeIndex] = !isStanding[activeIndex];
        }
    }
}
