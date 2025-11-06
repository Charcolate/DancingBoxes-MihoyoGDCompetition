using UnityEngine;
using System.Collections;

public class FlatPlaneGhostShooter : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform target;
    public float fireInterval = 2f;
    public float projectileSpeed = 8f;

    private void Start()
    {
        StartCoroutine(FireProjectiles());
    }

    IEnumerator FireProjectiles()
    {
        while (true)
        {
            yield return new WaitForSeconds(fireInterval);

            if (projectilePrefab != null && target != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                projectile.tag = "PracticeObject";

                // Calculate direction to target
                Vector3 direction = (target.position - transform.position).normalized;

                // Add projectile component and set velocity
                Rigidbody rb = projectile.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = direction * projectileSpeed;
                    rb.useGravity = false; // Important for flat plane
                }

                // Add visual trail for better visibility
                TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
                trail.time = 1f;
                trail.startWidth = 0.2f;
                trail.endWidth = 0.02f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = Color.red;
                trail.endColor = new Color(1, 0, 0, 0);

                // Destroy projectile after some time
                Destroy(projectile, 5f);
            }
        }
    }
}