using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Helper class for moving along a sphere surface
public static class SphereMover
{
    public static Vector3 MoveOnSphere(Vector3 currentPos, Vector3 targetPos, Vector3 sphereCenter, float radius, float step)
    {
        Vector3 dirCurrent = (currentPos - sphereCenter).normalized;
        Vector3 dirTarget = (targetPos - sphereCenter).normalized;

        float angle = Vector3.Angle(dirCurrent, dirTarget);
        if (angle < 0.001f) return sphereCenter + dirTarget * radius;

        float t = Mathf.Min(1f, step / angle);
        Vector3 newDir = Vector3.Slerp(dirCurrent, dirTarget, t).normalized;

        return sphereCenter + newDir * radius;
    }
}

public class SphereGoalManager : GoalManager
{
    [Header("Sphere Settings")]
    public Transform sphereCenterTransform;
    public float sphereRadius = 10f;

    [Header("Optional")]
    public bool snapWaypointsToSphere = true;

    private void Awake()
    {
        if (sphereCenterTransform == null)
            Debug.LogError("[SphereGoalManager] Sphere Center not assigned!");
    }

    private void Start()
    {
        // Snap Ghost and Wanderer to sphere surface
        if (ghost != null)
        {
            Vector3 dir = (ghost.position - sphereCenterTransform.position).normalized;
            ghost.position = sphereCenterTransform.position + dir * sphereRadius;
        }

        if (wanderer != null)
        {
            Vector3 dir = (wanderer.position - sphereCenterTransform.position).normalized;
            wanderer.position = sphereCenterTransform.position + dir * sphereRadius;
        }

        // Optionally snap all waypoints to the sphere surface
        if (snapWaypointsToSphere)
        {
            foreach (GoalPhaseData phase in smallPhases)
            {
                foreach (PhaseWaypoint wp in phase.waypoints)
                {
                    if (wp.waypointTransform != null)
                    {
                        Vector3 dir = (wp.waypointTransform.position - sphereCenterTransform.position).normalized;
                        wp.waypointTransform.position = sphereCenterTransform.position + dir * sphereRadius;
                    }
                }
            }
        }

        // Start the sequence
        StartCoroutine(RunSequence());
    }

    // Override movement to move along the sphere
    protected override IEnumerator MoveCharacterWithProjectiles(Transform character, GoalPhaseData phase)
    {
        foreach (PhaseWaypoint wp in phase.waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = sphereCenterTransform.position + (wp.waypointTransform.position - sphereCenterTransform.position).normalized * sphereRadius;

            bool projectileFired = false;

            while (Vector3.Distance(character.position, target) > reachThreshold)
            {
                float step = moveSpeed * Time.deltaTime;

                // Move character along the sphere
                character.position = SphereMover.MoveOnSphere(character.position, target, sphereCenterTransform.position, sphereRadius, step);

                // Orient up vector
                character.up = (character.position - sphereCenterTransform.position).normalized;

                // Debug log for movement
                Debug.Log($"[SphereGoalManager] Moving {character.name} towards {target}");

                // Fire projectiles slightly before reaching waypoint
                if (wp.triggerProjectile && !projectileFired)
                {
                    float remainingDistance = Vector3.Distance(character.position, target);
                    float leadDistance = moveSpeed * wp.leadTime;
                    if (remainingDistance <= leadDistance)
                    {
                        FireProjectiles(character, wp);
                        projectileFired = true;
                    }
                }

                yield return null;
            }

            yield return new WaitForSeconds(phase.pauseDuration);
        }
    }

    // ------------------- Gizmos -------------------
    private void OnDrawGizmos()
    {
        if (sphereCenterTransform == null) return;

        // Draw wire sphere for the movement radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sphereCenterTransform.position, sphereRadius);

        // Draw lines from center to waypoints
        if (smallPhases != null)
        {
            foreach (GoalPhaseData phase in smallPhases)
            {
                if (phase?.waypoints == null) continue;
                foreach (PhaseWaypoint wp in phase.waypoints)
                {
                    if (wp?.waypointTransform == null) continue;
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(sphereCenterTransform.position, wp.waypointTransform.position);
                    Gizmos.DrawSphere(wp.waypointTransform.position, 0.2f);
                }
            }
        }

        // Draw Ghost and Wanderer positions
        if (ghost != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(ghost.position, 0.25f);
        }
        if (wanderer != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(wanderer.position, 0.25f);
        }
    }
}
