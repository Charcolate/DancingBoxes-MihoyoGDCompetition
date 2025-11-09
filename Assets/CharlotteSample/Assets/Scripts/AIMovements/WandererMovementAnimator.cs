using UnityEngine;

public class WandererMovementAnimator : MonoBehaviour
{
    private Animator animator;
    private bool isMoving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("❌ No Animator found on wanderer!");
        }
    }

    public void StartMoving()
    {
        if (!isMoving)
        {
            isMoving = true;
            Debug.Log($"🎬 StartMoving called - Animator exists: {animator != null}");
            TriggerRandomJump();
        }
    }

    public void StopMoving()
    {
        if (isMoving)
        {
            isMoving = false;
            Debug.Log($"🎬 StopMoving called");
            ReturnToIdle();
        }
    }

    public void TriggerFall()
    {
        if (animator != null)
        {
            Debug.Log($"🎬 TriggerFall called - Resetting jump and triggering fall");
            // Reset jump first
            animator.SetInteger("JumpVariation", 0);
            // Trigger fall
            animator.SetTrigger("Fall");

            // Force update to see if parameters are set
            Debug.Log($"🎬 Fall trigger set: {animator.GetBool("Fall")}, JumpVariation: {animator.GetInteger("JumpVariation")}");
        }
        else
        {
            Debug.LogError("❌ No animator found in TriggerFall!");
        }
    }

    private void TriggerRandomJump()
    {
        if (animator != null)
        {
            int randomJump = Random.Range(1, 4); // 1, 2, or 3
            animator.SetInteger("JumpVariation", randomJump);
            Debug.Log($"🎬 Jump animation {randomJump} triggered - JumpVariation parameter: {animator.GetInteger("JumpVariation")}");
        }
        else
        {
            Debug.LogError("❌ No animator found in TriggerRandomJump!");
        }
    }

    private void ReturnToIdle()
    {
        if (animator != null)
        {
            animator.SetInteger("JumpVariation", 0);
        }
    }

    public void ResetAnimator()
    {
        if (animator != null)
        {
            animator.SetInteger("JumpVariation", 0);
            animator.ResetTrigger("Fall");
            isMoving = false;
        }
    }
}