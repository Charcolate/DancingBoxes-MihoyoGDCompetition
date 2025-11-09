using UnityEngine;

public class WandererMovementAnimator : MonoBehaviour
{
    private Animator animator;
    private bool isMoving = false;
    private bool isInFallAnimation = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("❌ No Animator found on wanderer!");
        }
    }

    void Update()
    {
        // Check if we're currently in fall animation and when it ends
        if (isInFallAnimation)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // Check if we're still in fall state or back to idle
            if (stateInfo.IsName("Idle") || stateInfo.normalizedTime >= 1.0f)
            {
                isInFallAnimation = false;
                Debug.Log("🔄 Fall animation completed naturally");
            }
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

    public void TriggerFall()
    {
        if (animator != null)
        {
            // Reset jump first
            animator.SetInteger("JumpVariation", 0);
            // Trigger fall
            animator.SetTrigger("Fall");
            isInFallAnimation = true;
        }
    }

    public bool IsFallAnimationComplete()
    {
        return !isInFallAnimation;
    }

    private void TriggerRandomJump()
    {
        if (animator != null)
        {
            int randomJump = Random.Range(1, 4);
            animator.SetInteger("JumpVariation", randomJump);
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
            isInFallAnimation = false;
        }
    }
}