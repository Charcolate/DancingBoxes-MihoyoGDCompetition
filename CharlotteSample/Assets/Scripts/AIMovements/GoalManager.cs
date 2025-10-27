using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalManager : MonoBehaviour
{
    [Header("References")]
    public Transform ghost;
    public Transform wanderer;
    public List<Transform> waypoints;
    public ProjectileSpawner projectileSpawner;

    [Header("Settings")]
    [Tooltip("Movement speed for both ghost and wanderer.")]
    public float moveSpeed = 3f;

    [Tooltip("Pause at each waypoint before continuing.")]
    public float pauseDuration = 1f;

    [Tooltip("Delay before wanderer starts after ghost finishes.")]
    public float wandererDelayAfterGhost = 1f;

    [Tooltip("Index of waypoint that triggers a projectile shot.")]
    public int triggerWaypointIndex = 1;

    [Tooltip("How many seconds before reaching the trigger waypoint the projectile should fire.")]
    public float fireLeadTime = 5f;

    private Vector3 phaseStartPosition;
    private bool sequenceRunning;
    private bool ghostSequenceFinished;

    // Current state of the system (Ghost phase or Wanderer phase)
    public PhaseState CurrentPhase { get; private set; } = PhaseState.Ghost;

    private void Start()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            Debug.LogError("GoalManager: No waypoints assigned!");
            return;
        }

        phaseStartPosition = waypoints[0].position;
        StartCoroutine(PlayGhostSequence());
    }

    /// <summary>
    /// Controls the movement sequence for the ghost.
    /// </summary>
    private IEnumerator PlayGhostSequence()
    {
        sequenceRunning = true;
        ghostSequenceFinished = false;
        CurrentPhase = PhaseState.Ghost;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 start = waypoints[i].position;
            Vector3 end = waypoints[i + 1].position;
            float distanceToNext = Vector3.Distance(start, end);
            float travelTime = distanceToNext / moveSpeed;

            // If the next waypoint is the trigger one, schedule an early shot.
            if (i + 1 == triggerWaypointIndex && projectileSpawner != null)
            {
                float fireTime = Mathf.Max(travelTime - fireLeadTime, 0.1f);
                StartCoroutine(FireBeforeTrigger(fireTime, end));
            }

            yield return StartCoroutine(MoveToPoint(ghost, end));
            yield return new WaitForSeconds(pauseDuration);
        }

        sequenceRunning = false;
        ghostSequenceFinished = true;

        // Start wanderer sequence after delay
        yield return new WaitForSeconds(wandererDelayAfterGhost);
        StartCoroutine(PlayWandererSequence());
    }

    /// <summary>
    /// Controls the movement sequence for the wanderer.
    /// </summary>
    private IEnumerator PlayWandererSequence()
    {
        CurrentPhase = PhaseState.Wanderer;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 start = waypoints[i].position;
            Vector3 end = waypoints[i + 1].position;
            float distanceToNext = Vector3.Distance(start, end);
            float travelTime = distanceToNext / moveSpeed;

            // If the next waypoint is the trigger one, schedule an early shot.
            if (i + 1 == triggerWaypointIndex && projectileSpawner != null)
            {
                float fireTime = Mathf.Max(travelTime - fireLeadTime, 0.1f);
                StartCoroutine(FireBeforeTrigger(fireTime, end));
            }

            yield return StartCoroutine(MoveToPoint(wanderer, end));
            yield return new WaitForSeconds(pauseDuration);
        }
    }

    /// <summary>
    /// Waits for a specific amount of time, then fires a projectile toward a target position.
    /// </summary>
    private IEnumerator FireBeforeTrigger(float waitTime, Vector3 targetPos)
    {
        yield return new WaitForSeconds(waitTime);
        projectileSpawner.SpawnOne(this, targetPos);
    }

    /// <summary>
    /// Moves a transform smoothly toward a destination.
    /// </summary>
    private IEnumerator MoveToPoint(Transform target, Vector3 destination)
    {
        while (Vector3.Distance(target.position, destination) > 0.05f)
        {
            target.position = Vector3.MoveTowards(target.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Resets the system back to the start of the ghost phase.
    /// </summary>
    public void ResetPhase()
    {
        StopAllCoroutines();
        StartCoroutine(ResetAndRestart());
    }

    private IEnumerator ResetAndRestart()
    {
        ghost.position = phaseStartPosition;
        wanderer.position = phaseStartPosition;
        ghost.rotation = Quaternion.identity;
        wanderer.rotation = Quaternion.identity;

        yield return new WaitForSeconds(1f);
        StartCoroutine(PlayGhostSequence());
    }

    public bool IsGhostSequenceFinished()
    {
        return ghostSequenceFinished;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Gizmos.DrawSphere(waypoints[i].position, 0.2f);
            if (i < waypoints.Count - 1)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
#endif
}
