using System.Collections;
using UnityEngine;

public class WandererHit : MonoBehaviour
{
    public float flashDuration = 0.25f;
    public Color hitColor = Color.red;

    private Renderer rend;
    private Color originalColor;
    private Coroutine flashCoroutine;
    private GoalManager goalManager;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        goalManager = FindObjectOfType<GoalManager>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collision is with a projectile
        if (collision.gameObject.CompareTag("Projectile"))
        {
            Debug.Log("💥 Wanderer hit by projectile!");

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());

            if (goalManager != null)
                goalManager.ResetPhase();
            else
                Debug.LogError("❌ GoalManager not found!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Also check for trigger collisions (in case projectiles use triggers)
        if (other.CompareTag("Projectile"))
        {
            Debug.Log("💥 Wanderer hit by projectile (trigger)!");

            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());

            if (goalManager != null)
                goalManager.ResetPhase();
            else
                Debug.LogError("❌ GoalManager not found!");
        }
    }

    private IEnumerator FlashRoutine()
    {
        if (rend == null) yield break;

        rend.material.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        rend.material.color = originalColor;
    }
}