using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 15f;

    public GameObject SpawnOne(Vector3 spawnPosition, Vector3 targetPosition, bool destroyOnCollision = false)
    {
        if (projectilePrefab == null) return null;

        GameObject proj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetTarget(spawnPosition, targetPosition, projectileSpeed, destroyOnCollision);
        }

        return proj;
    }
}