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

    // Overload: spawns projectile toward a position instead of a fixed Transform
    public void SpawnOne(GoalManager goalManager, Vector3 targetPosition)
    {
        if (projectilePrefab == null) return;

        GameObject proj = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        Projectile projectileScript = proj.GetComponent<Projectile>();

        if (rb == null) return;

        // Calculate ballistic direction
        Vector3 dir = (targetPosition - spawnPoint.position);
        float distance = dir.magnitude;
        dir.Normalize();

        // Add some arc height based on distance
        float heightOffset = distance * 0.2f;
        Vector3 velocity = dir * projectileSpeed + Vector3.up * heightOffset;

        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.linearVelocity = velocity;

        if (projectileScript != null)
            projectileScript.SetGoalManager(goalManager);

        Destroy(proj, projectileLifetime);
    }
}
