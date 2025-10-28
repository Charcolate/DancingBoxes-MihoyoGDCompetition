using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public bool freezeOnCollision = true;
    public bool showTrajectory = true; // Only true for Ghost phase

    private Rigidbody rb;
    private LineRenderer lineRenderer;
    private float recordInterval = 0.02f;
    private float timeSinceLastRecord = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (showTrajectory)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 0;
            lineRenderer.widthMultiplier = 0.05f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = new Color(0.2f, 0.8f, 1f, 0.3f);
        }
    }

    private void Update()
    {
        if (showTrajectory && lineRenderer != null)
        {
            timeSinceLastRecord += Time.deltaTime;
            if (timeSinceLastRecord >= recordInterval)
            {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.position);
                timeSinceLastRecord = 0f;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (freezeOnCollision)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
