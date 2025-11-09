using UnityEngine;
using System.Collections;

public class WandererHit : MonoBehaviour
{
    public float flashDuration = 0.25f;
    public Color hitColor = Color.red;

    private Renderer rend;
    private Color originalColor;
    private GoalManager goalManager;
    private WandererMovementAnimator movementAnimator;
    private bool isHandlingHit = false;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        goalManager = FindObjectOfType<GoalManager>();
        movementAnimator = GetComponent<WandererMovementAnimator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile") && !isHandlingHit)
        {
            Debug.Log("💥 Wanderer hit by projectile!");
            StartCoroutine(HandleHit());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile") && !isHandlingHit)
        {
            Debug.Log("💥 Wanderer hit by projectile (trigger)!");
            StartCoroutine(HandleHit());
        }
    }

    private IEnumerator HandleHit()
    {
        isHandlingHit = true;

        // Flash red
        if (rend != null)
            rend.material.color = hitColor;

        // Trigger fall animation
        if (movementAnimator != null)
        {
            movementAnimator.TriggerFall();
            Debug.Log("🔄 Playing fall animation before respawn");
        }

        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);

        // Reset color
        if (rend != null)
            rend.material.color = originalColor;

        // Wait for fall animation to complete
        yield return new WaitUntil(() => movementAnimator.IsFallAnimationComplete());

        Debug.Log("🎯 Fall animation complete, now respawning");

        // NOW trigger respawn after fall animation completes
        if (goalManager != null)
            goalManager.ResetPhase();
        else
            Debug.LogError("❌ GoalManager not found!");

        isHandlingHit = false;
    }
}