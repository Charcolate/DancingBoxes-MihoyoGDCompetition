using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public Transform spawnPoint;
    public float projectileSpeed = 12f;
    public float projectileLifetime = 6f;

    private void Start()
    {
        if (spawnPoint == null)
            spawnPoint = transform;
    }

    // Returns the spawned GameObject for tracking
    public GameObject SpawnOne(GoalManager goalManager, Vector3 spawnPosition, Vector3 targetPosition)
    {
        if (projectilePrefab == null) return null;

        GameObject proj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();

        if (rb == null) return null;

        Vector3 dir = (targetPosition - spawnPosition).normalized;
        rb.linearVelocity = dir * projectileSpeed;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Destroy(proj, projectileLifetime);

        return proj;
    }
}
