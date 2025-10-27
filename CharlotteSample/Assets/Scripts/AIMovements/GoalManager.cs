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
    public float moveSpeed = 3f;
    public float pauseDuration = 1f;
    public float wandererDelayAfterGhost = 1f;
    public int triggerWaypointIndex = 1;

    private Vector3 phaseStartPosition;
    private bool sequenceRunning;
    private bool ghostSequenceFinished;

    private void Start()
    {
        if (waypoints.Count == 0)
        {
            Debug.LogError("GoalManager: No waypoints assigned!");
            return;
        }

        phaseStartPosition = waypoints[0].position;
        StartCoroutine(PlayGhostSequence());
    }

    private IEnumerator PlayGhostSequence()
    {
        sequenceRunning = true;
        ghostSequenceFinished = false;

        for (int i = 0; i < waypoints.Count; i++)
        {
            yield return StartCoroutine(MoveToPoint(ghost, waypoints[i].position));
            yield return new WaitForSeconds(pauseDuration);

            // Fire projectile at chosen waypoint
            if (i == triggerWaypointIndex && projectileSpawner != null)
            {
                projectileSpawner.SpawnOne(this);
            }
        }

        sequenceRunning = false;
        ghostSequenceFinished = true;

        // Wait, then make wanderer follow
        yield return new WaitForSeconds(wandererDelayAfterGhost);
        StartCoroutine(PlayWandererSequence());
    }

    private IEnumerator PlayWandererSequence()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            yield return StartCoroutine(MoveToPoint(wanderer, waypoints[i].position));
            yield return new WaitForSeconds(pauseDuration);

            if (i == triggerWaypointIndex && projectileSpawner != null)
            {
                projectileSpawner.SpawnOne(this);
            }
        }
    }

    private IEnumerator MoveToPoint(Transform target, Vector3 destination)
    {
        while (Vector3.Distance(target.position, destination) > 0.05f)
        {
            target.position = Vector3.MoveTowards(target.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

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
