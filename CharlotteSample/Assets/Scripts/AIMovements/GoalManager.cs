using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalManager : MonoBehaviour
{
    [Header("References")]
    public Transform ghost;
    public Transform wanderer;
    public ProjectileSpawner projectileSpawner;

    [Header("Phase Settings")]
    public List<GoalPhase> phases; // all phases
    private int currentPhaseIndex = 0;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float wandererDelayAfterGhost = 1f;
    public float fireLeadTime = 5f;

    public PhaseState CurrentPhase { get; private set; } = PhaseState.Ghost;

    private Vector3 phaseStartPosition;
    private bool ghostSequenceFinished;

    private void Start()
    {
        if (phases == null || phases.Count == 0)
        {
            Debug.LogError("No phases assigned to GoalManager!");
            return;
        }

        StartPhase(currentPhaseIndex);
    }

    private void StartPhase(int phaseIndex)
    {
        GoalPhase phase = phases[phaseIndex];
        if (phase.waypoints == null || phase.waypoints.Count == 0)
        {
            Debug.LogError($"Phase {phaseIndex} has no waypoints!");
            return;
        }

        phaseStartPosition = phase.waypoints[0].position;
        ghost.position = phaseStartPosition;
        wanderer.position = phaseStartPosition;

        StartCoroutine(PlayGhostSequence(phase));
    }

    private IEnumerator PlayGhostSequence(GoalPhase phase)
    {
        CurrentPhase = PhaseState.Ghost;
        ghostSequenceFinished = false;

        for (int i = 0; i < phase.waypoints.Count - 1; i++)
        {
            Vector3 start = phase.waypoints[i].position;
            Vector3 end = phase.waypoints[i + 1].position;
            float distanceToNext = Vector3.Distance(start, end);
            float travelTime = distanceToNext / moveSpeed;

            if (i + 1 == phase.triggerWaypointIndex && projectileSpawner != null)
            {
                float fireTime = Mathf.Max(travelTime - fireLeadTime, 0.1f);
                StartCoroutine(FireBeforeTrigger(fireTime, end));
            }

            yield return StartCoroutine(MoveToPoint(ghost, end));
            yield return new WaitForSeconds(phase.pauseDuration);
        }

        ghostSequenceFinished = true;

        yield return new WaitForSeconds(wandererDelayAfterGhost);
        StartCoroutine(PlayWandererSequence(phase));
    }

    private IEnumerator PlayWandererSequence(GoalPhase phase)
    {
        CurrentPhase = PhaseState.Wanderer;

        for (int i = 0; i < phase.waypoints.Count - 1; i++)
        {
            Vector3 start = phase.waypoints[i].position;
            Vector3 end = phase.waypoints[i + 1].position;
            float distanceToNext = Vector3.Distance(start, end);
            float travelTime = distanceToNext / moveSpeed;

            if (i + 1 == phase.triggerWaypointIndex && projectileSpawner != null)
            {
                float fireTime = Mathf.Max(travelTime - fireLeadTime, 0.1f);
                StartCoroutine(FireBeforeTrigger(fireTime, end));
            }

            yield return StartCoroutine(MoveToPoint(wanderer, end));
            yield return new WaitForSeconds(phase.pauseDuration);
        }

        // Phase complete — automatically start next phase
        currentPhaseIndex++;
        if (currentPhaseIndex < phases.Count)
        {
            StartPhase(currentPhaseIndex);
        }
        else
        {
            Debug.Log("All phases completed!");
        }
    }

    private IEnumerator FireBeforeTrigger(float waitTime, Vector3 targetPos)
    {
        yield return new WaitForSeconds(waitTime);
        projectileSpawner.SpawnOne(this, targetPos);
    }

    private IEnumerator MoveToPoint(Transform target, Vector3 destination)
    {
        while (Vector3.Distance(target.position, destination) > 0.05f)
        {
            target.position = Vector3.MoveTowards(target.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Resets the current phase (used by Falling triggers or projectile hits)
    /// </summary>
    public void ResetPhase()
    {
        StopAllCoroutines();
        StartPhase(currentPhaseIndex);
    }

    /// <summary>
    /// Returns true if the ghost has finished its current phase sequence.
    /// Used by Projectiles for trail clearing.
    /// </summary>
    public bool IsGhostSequenceFinished()
    {
        return ghostSequenceFinished;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (phases == null || phases.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (var phase in phases)
        {
            if (phase.waypoints == null) continue;
            for (int i = 0; i < phase.waypoints.Count; i++)
            {
                Gizmos.DrawSphere(phase.waypoints[i].position, 0.2f);
                if (i < phase.waypoints.Count - 1)
                    Gizmos.DrawLine(phase.waypoints[i].position, phase.waypoints[i + 1].position);
            }
        }
    }
#endif
}