using UnityEngine;
using System.Collections;

public class WandererHit : MonoBehaviour
{
    public float flashDuration = 0.25f;
    public Color hitColor = Color.red;
    public float fallAnimationTime = 1.0f;

    private Renderer rend;
    private Color originalColor;
    private GoalManager goalManager;
    private WandererMovementAnimator movementAnimator;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        goalManager = FindObjectOfType<GoalManager>();
        movementAnimator = GetComponent<WandererMovementAnimator>();

        if (movementAnimator == null)
        {
            Debug.LogError("❌ No WandererMovementAnimator found on wanderer!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            Debug.Log("💥 Wanderer hit by projectile!");
            StartCoroutine(HandleHit());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile"))
        {
            Debug.Log("💥 Wanderer hit by projectile (trigger)!");
            StartCoroutine(HandleHit());
        }
    }

    private IEnumerator HandleHit()
    {
        Debug.Log("🎬 HandleHit started");

        // Flash red
        if (rend != null)
            rend.material.color = hitColor;

        // Trigger fall animation
        if (movementAnimator != null)
        {
            movementAnimator.TriggerFall();
        }
        else
        {
            Debug.LogError("❌ No movementAnimator in HandleHit!");
        }

        // Wait for fall animation
        yield return new WaitForSeconds(fallAnimationTime);

        // Reset color
        if (rend != null)
            rend.material.color = originalColor;

        // Trigger respawn
        if (goalManager != null)
            goalManager.ResetPhase();
        else
            Debug.LogError("❌ GoalManager not found!");

        Debug.Log("🎬 HandleHit completed");
    }
}