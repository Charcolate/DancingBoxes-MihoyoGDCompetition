using UnityEngine;

public class GhostMovementAnimator : MonoBehaviour
{
    private Animator animator;
    private bool isMoving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("❌ No Animator found on ghost!");
        }
    }

    public void StartMoving()
    {
        if (!isMoving)
        {
            isMoving = true;
            TriggerRandomJump();
        }
    }

    public void StopMoving()
    {
        if (isMoving)
        {
            isMoving = false;
            ReturnToIdle();
        }
    }

    private void TriggerRandomJump()
    {
        if (animator != null)
        {
            int randomJump = Random.Range(1, 4); // 1, 2, or 3
            // Use DIFFERENT parameter name for ghost
            animator.SetInteger("GhostJump", randomJump);
            Debug.Log($"👻 Ghost jump animation {randomJump} triggered - GhostJump: {animator.GetInteger("GhostJump")}");
        }
    }

    private void ReturnToIdle()
    {
        if (animator != null)
        {
            // Use DIFFERENT parameter name for ghost
            animator.SetInteger("GhostJump", 0);
        }
    }

    public void ResetAnimator()
    {
        if (animator != null)
        {
            animator.SetInteger("GhostJump", 0);
            isMoving = false;
        }
    }
}