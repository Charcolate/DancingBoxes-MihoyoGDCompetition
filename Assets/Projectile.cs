using UnityEngine;

public class Projectile : MonoBehaviour
{
    public bool destroyOnHit = true;
    public GameObject hitEffectPrefab;
    public void OnCollisionEnter(Collision collision)
    {
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
