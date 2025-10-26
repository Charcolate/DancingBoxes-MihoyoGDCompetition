using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Simple projectile behaviour: destroy on collision
    public bool destroyOnHit = true;
    public GameObject hitEffectPrefab; // optional splash effect

    private void OnCollisionEnter(Collision collision)
    {
        // Optionally, skip collisions with other projectiles (or any tag)
        // if (collision.gameObject.CompareTag("Projectile")) return;

        // spawn effect
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}
