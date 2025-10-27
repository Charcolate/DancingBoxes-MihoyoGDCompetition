using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Projectile : MonoBehaviour
{
    public bool destroyOnHit = true;
    public GameObject hitEffectPrefab;

    private LineRenderer lineRenderer;
    private GoalManager goalManager;
    private float recordInterval = 0.02f;
    private float timeSinceLastRecord;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = new Color(0.2f, 0.8f, 1f, 0.3f);
    }

    public void SetGoalManager(GoalManager manager)
    {
        goalManager = manager;
    }

    private void Update()
    {
        // Add trail points while projectile moves
        timeSinceLastRecord += Time.deltaTime;
        if (timeSinceLastRecord >= recordInterval)
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.position);
            timeSinceLastRecord = 0f;
        }

        // If ghost's sequence finished, clear the line
        if (goalManager != null && goalManager.IsGhostSequenceFinished())
        {
            lineRenderer.positionCount = 0;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
