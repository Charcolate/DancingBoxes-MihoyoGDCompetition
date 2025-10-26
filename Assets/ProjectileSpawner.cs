using System.Collections;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform spawnPoint;      // where projectiles are instantiated
    public Transform target;          // the wanderer to aim at
    public float spawnInterval = 1.0f;
    public float projectileSpeed = 12f;
    [Tooltip("Max horizontal angle (degrees) to randomly deviate from perfect aim")]
    public float spreadAngleDegrees = 15f;
    public float projectileLifetime = 6f;

    private void Start()
    {
        if (spawnPoint == null) spawnPoint = transform;
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            SpawnOne();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        if (projectilePrefab == null || target == null) return;

        // instantiate
        GameObject proj = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);

        // compute direction from spawn to target
        Vector3 aimDir = (target.position - spawnPoint.position).normalized;
        // add random horizontal spread (rotate around Y) and small vertical variance
        float yaw = Random.Range(-spreadAngleDegrees, spreadAngleDegrees);
        float pitch = Random.Range(-spreadAngleDegrees * 0.3f, spreadAngleDegrees * 0.3f); // less vertical
        Quaternion spreadRot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 finalDir = spreadRot * aimDir;

        // give velocity via Rigidbody
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // use the non-obsolete API
            rb.linearVelocity = finalDir * projectileSpeed;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // destroy after lifetime if still around
        Destroy(proj, projectileLifetime);
    }
}
