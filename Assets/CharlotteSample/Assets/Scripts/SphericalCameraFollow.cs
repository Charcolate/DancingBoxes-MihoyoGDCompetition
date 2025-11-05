using UnityEngine;
using System.Collections;

public class SphericalCameraFollow : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform[] cameraPositions; // Array of spawn points for camera positions
    public Transform focusPoint; // The point the camera should always look at

    [Header("Movement Settings")]
    public float movementSpeed = 2.0f; // Speed of camera movement
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Phase Integration")]
    public GoalManager goalManager; // Reference to the GoalManager to track phases

    private int currentPositionIndex = 0;
    private bool isMoving = false;
    private bool isActive = false;
    private int lastCompletedPhase = -1;

    void Start()
    {
        // Validate setup
        if (cameraPositions == null || cameraPositions.Length == 0)
        {
            Debug.LogError("No camera positions assigned!");
            return;
        }

        if (focusPoint == null)
        {
            Debug.LogError("No focus point assigned!");
            return;
        }

        if (goalManager == null)
        {
            Debug.LogError("No GoalManager assigned!");
            return;
        }

        // Start at first position
        transform.position = cameraPositions[0].position;
        LookAtFocusPoint();

        // Start automatic movement
        StartAutomaticMovement();
    }

    void Update()
    {
        if (!isActive || goalManager == null) return;

        // Check if a small phase has completed using the public property
        int currentPhase = goalManager.CurrentSmallPhaseIndex;

        // If we've moved to a new phase and we're not already moving the camera
        if (currentPhase > lastCompletedPhase && !isMoving)
        {
            lastCompletedPhase = currentPhase;
            MoveToNextPosition();
            Debug.Log($"📸 Camera moving due to phase completion: Phase {currentPhase}");
        }

        // Always look at focus point when not moving
        if (focusPoint != null && !isMoving)
        {
            LookAtFocusPoint();
        }
    }

    public void StartAutomaticMovement()
    {
        isActive = true;
        lastCompletedPhase = goalManager != null ? goalManager.CurrentSmallPhaseIndex : -1;
        Debug.Log("Automatic camera movement started (phase-based)");
    }

    public void StopAutomaticMovement()
    {
        isActive = false;
        Debug.Log("Automatic camera movement stopped");
    }

    public void MoveToPosition(int positionIndex)
    {
        if (isMoving || cameraPositions == null || positionIndex < 0 || positionIndex >= cameraPositions.Length)
            return;

        StartCoroutine(MoveCameraToPosition(positionIndex));
    }

    public void MoveToNextPosition()
    {
        if (cameraPositions == null || cameraPositions.Length == 0)
            return;

        int nextIndex = currentPositionIndex + 1;

        // Handle looping or stopping at the end
        if (nextIndex >= cameraPositions.Length)
        {
            nextIndex = 0; // Always loop for phase-based movement
        }

        MoveToPosition(nextIndex);
    }

    public void MoveToPreviousPosition()
    {
        if (cameraPositions == null || cameraPositions.Length == 0)
            return;

        int prevIndex = currentPositionIndex - 1;

        if (prevIndex < 0)
        {
            prevIndex = cameraPositions.Length - 1;
        }

        MoveToPosition(prevIndex);
    }

    private IEnumerator MoveCameraToPosition(int newPositionIndex)
    {
        isMoving = true;

        Transform targetPosition = cameraPositions[newPositionIndex];
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        float journey = 0f;

        while (journey <= 1f)
        {
            journey += Time.deltaTime * movementSpeed;
            float percent = movementCurve.Evaluate(journey);

            // Move position
            transform.position = Vector3.Lerp(startPosition, targetPosition.position, percent);

            // Smoothly rotate to look at focus point during movement
            Vector3 directionToFocus = focusPoint.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(directionToFocus);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, percent);

            yield return null;
        }

        // Ensure final position and rotation
        transform.position = targetPosition.position;
        LookAtFocusPoint();

        currentPositionIndex = newPositionIndex;
        isMoving = false;

        Debug.Log($"📸 Camera moved to position {newPositionIndex + 1}/{cameraPositions.Length} (Phase {lastCompletedPhase})");
    }

    private void LookAtFocusPoint()
    {
        if (focusPoint != null)
        {
            transform.LookAt(focusPoint);
        }
    }

    // Public methods for control from other scripts
    public void SetFocusPoint(Transform newFocusPoint)
    {
        focusPoint = newFocusPoint;
        LookAtFocusPoint();
    }

    public void SetMovementSpeed(float speed)
    {
        movementSpeed = Mathf.Max(0.1f, speed);
    }

    // Method to manually trigger camera move (useful for testing)
    public void TriggerPhaseBasedMove()
    {
        if (goalManager != null)
        {
            lastCompletedPhase = goalManager.CurrentSmallPhaseIndex;
            MoveToNextPosition();
        }
    }

    public void JumpToPosition(int positionIndex)
    {
        if (cameraPositions != null && positionIndex >= 0 && positionIndex < cameraPositions.Length)
        {
            StopAllCoroutines();
            transform.position = cameraPositions[positionIndex].position;
            LookAtFocusPoint();
            currentPositionIndex = positionIndex;
            isMoving = false;
        }
    }

    public bool IsMoving
    {
        get { return isMoving; }
    }

    public int CurrentPositionIndex
    {
        get { return currentPositionIndex; }
    }

    public int TotalPositions
    {
        get { return cameraPositions != null ? cameraPositions.Length : 0; }
    }

    // Gizmos for visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        if (cameraPositions != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform pos in cameraPositions)
            {
                if (pos != null)
                {
                    Gizmos.DrawWireSphere(pos.position, 0.1f);
                    if (focusPoint != null)
                    {
                        Gizmos.DrawLine(pos.position, focusPoint.position);
                    }
                }
            }
        }

        if (focusPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(focusPoint.position, 0.2f);
        }
    }
}