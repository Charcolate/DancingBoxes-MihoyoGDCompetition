using UnityEditor;
using UnityEngine;

public class SimpleRandomWalker : MonoBehaviour
{
    public float speed = 3f;                 // movement speed (units/sec)
    public float arriveThreshold = 0.2f;     // how close counts as "arrived"
    public float waitTimeAfterArrive = 1f;   // pause before choosing next target
    //Area for within random targets are selected, Chooses between -areaHalfX to +areaHalfX on X axis, and -areaHalfZ to +areaHalfZ on Z axis
    public float areaHalfX = 8f;             // half-width of allowed area on X
    public float areaHalfZ = 8f;             // half-depth of allowed area on Z

    public GameObject indicator; // Visual indicator
    public float indicatorSpeed = 5f; // Speed of indicator movement
    public Vector3 areaCenter = Vector3.zero;// center of allowed area (world coords)

    private Vector3 target;
    private float waitTimer = 0f;
    private bool indicatorReached = false;

    void Start()
    {
        // Will initiate the walker by picking a random target within the defined area
        // Set area center to current position

        // Try to auto-detect the plane bounds if there's a "Ground" object
        GameObject ground = GameObject.Find("Ground");
        if (ground != null)
        {
            Vector3 planeScale = ground.transform.localScale;
            areaCenter = ground.transform.position;
            areaHalfX = 5f * planeScale.x; // Plane mesh is 10x10 units 
            areaHalfZ = 5f * planeScale.z; // 5f because scale is half-extents
        }

        PickNewTarget();
    }


    void Update()
    {
        //debug message
        if (indicator == null)
        {
            Debug.LogWarning("SimpleRandomWalker: No indicator assigned.");
            return;
        }
        // Move the indicator toward the target
        MoveIndicator();

        if (!indicatorReached)
        {
            return; // Wait until indicator reaches target
        }

        // If waiting after arriving, count down
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime; 
            return;
        }

        // Move toward target
        Vector3 pos = transform.position;
        Vector3 direction = (target - pos);
        direction.y = 0f; // keep movement horizontal

        float dist = direction.magnitude;
        //If arrived at target
        if (dist <= arriveThreshold)
        {
            // Arrived: pick new target after waiting
            waitTimer = waitTimeAfterArrive;
            //When the distance is less than the threshold, reset the indicator flag and pick a new target
            indicatorReached = false;
            PickNewTarget();
            return;
        }

        Vector3 move = direction.normalized * speed * Time.deltaTime;
        if (move.magnitude > dist) move = direction; // avoid overshoot

        transform.position += move;

        // Rotate to face movement direction
        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion toRot = Quaternion.LookRotation(move.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRot, 10f * Time.deltaTime);
        }
    }

    void PickNewTarget()
    {
        // Pick a random point within the allowed area
        float x = Random.Range(-areaHalfX, areaHalfX) + areaCenter.x;
        float z = Random.Range(-areaHalfZ, areaHalfZ) + areaCenter.z;
        target = new Vector3(x, transform.position.y, z);
    }

    void MoveIndicator()
    {
        Vector3 pos = indicator.transform.position;
        Vector3 direction = (target - pos);
        direction.y = 0f;
        float dist = direction.magnitude;

        if (dist <= arriveThreshold)
        {
            // Indicator has reached the target
            indicatorReached = true;
            return;

        }

        Vector3 move = direction.normalized * indicatorSpeed * Time.deltaTime;

        if (move.magnitude > dist) move = direction; // avoid overshoot
        indicator.transform.position += move;
        // Rotate to face movement direction
        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion toRot = Quaternion.LookRotation(move.normalized);
            indicator.transform.rotation = Quaternion.Slerp(indicator.transform.rotation, toRot, 10f * Time.deltaTime);
        }
    }

}
