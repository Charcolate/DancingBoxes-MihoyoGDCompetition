using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    public bool destroyOnHit = true;
    public GameObject hitEffectPrefab;

    private LineRenderer lineRenderer;
    private GoalManager goalManager;
    private Rigidbody rb;

    private float recordInterval = 0.02f;
    private float timeSinceLastRecord;

    private bool frozen = false;
    private PhaseState lastKnownPhase;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        rb = GetComponent<Rigidbody>();

        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = new Color(0.2f, 0.8f, 1f, 0.3f);
    }

    public void SetGoalManager(GoalManager manager)
    {
        goalManager = manager;
        if (goalManager != null)
            lastKnownPhase = goalManager.CurrentPhase;
    }

    private void Update()
    {
        // Record trail points
        timeSinceLastRecord += Time.deltaTime;
        if (timeSinceLastRecord >= recordInterval)
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.position);
            timeSinceLastRecord = 0f;
        }

        // Clear trail after ghost phase
        if (goalManager != null && goalManager.IsGhostSequenceFinished())
        {
            lineRenderer.positionCount = 0;
        }

        // Detect phase change
        if (goalManager != null && lastKnownPhase != goalManager.CurrentPhase)
        {
            lastKnownPhase = goalManager.CurrentPhase;

            // Unfreeze when Wanderer phase starts
            if (goalManager.CurrentPhase == PhaseState.Wanderer && frozen)
            {
                UnfreezeProjectile();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (goalManager == null)
            return;

        // Freeze during ghost phase
        if (goalManager.CurrentPhase == PhaseState.Ghost)
        {
            if (!frozen)
            {
                frozen = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            return;
        }

        // Normal behavior during wanderer phase
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private void UnfreezeProjectile()
    {
        frozen = false;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}
