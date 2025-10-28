using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    public bool destroyOnHit = true;
    public GameObject hitEffectPrefab;

    [Header("Trail Settings")]
    private LineRenderer lineRenderer;
    private float recordInterval = 0.02f;
    private float timeSinceLastRecord;

    [Header("References")]
    private GoalManager goalManager;

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

        // Clear trail if ghost sequence finished
        if (goalManager != null && goalManager.IsGhostSequenceFinished())
        {
            lineRenderer.positionCount = 0;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only destroy if allowed
        if (destroyOnHit)
        {
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

    #region Backward Compatibility for Multi-Phase GoalManager

    public bool IsGhostSequenceFinished()
    {
        if (goalManager == null) return false;
        return goalManager.IsGhostSequenceFinished();
    }


    #endregion
}
