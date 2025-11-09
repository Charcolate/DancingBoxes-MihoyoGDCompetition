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
            Debug.Log($"👻 GHOST StartMoving called on: {gameObject.name}");
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
            int randomJump = Random.Range(1, 4);
            animator.SetInteger("GhostJump", randomJump);

            // ADD ONLY THIS DEBUG:
            Debug.Log($"👻 GHOST jumping: {gameObject.name} - GhostJump: {randomJump}");
        }
    }

    private void ReturnToIdle()
    {
        if (animator != null)
        {
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