using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereGoalManager : MonoBehaviour
{
    [Header("Phase Configuration")]
    public List<GoalPhaseData> smallPhases = new List<GoalPhaseData>();

    [Header("Characters")]
    public Transform wanderer;

    [Header("Sphere Settings")]
    public Transform sphereCenterTransform;
    public float sphereRadius = 10f;

    [Header("Optional")]
    public bool snapWaypointsToSphere = true;

    [Header("Projectile System")]
    public ProjectileSpawner projectileSpawner;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float reachThreshold = 0.2f;

    [Header("Respawn Settings")]
    public int maxRespawnsPerBigPhase = 3;

    protected int currentSmallPhaseIndex = 0;
    protected int respawnCount = 0;
    protected bool sequenceRunning = false;

    protected Vector3 wandererStartPos;
    protected Vector3 bigPhaseStartWandererPos;
    protected List<GameObject> activeProjectiles = new List<GameObject>();

    // ----------------- Start -----------------
    private IEnumerator Start()
    {
        if (sphereCenterTransform == null)
        {
            Debug.LogError("[SphereGoalManager] Sphere Center not assigned!");
            yield break;
        }

        // Snap wanderer to sphere at runtime
        if (wanderer != null)
        {
            Vector3 dir = (wanderer.position - sphereCenterTransform.position).normalized;
            wanderer.position = sphereCenterTransform.position + dir * sphereRadius;
        }

        yield return StartCoroutine(RunSequence());
    }

    // ----------------- Main Sequence -----------------
    protected IEnumerator RunSequence()
    {
        sequenceRunning = true;

        while (currentSmallPhaseIndex < smallPhases.Count)
        {
            var phase = smallPhases[currentSmallPhaseIndex];
            if (phase == null) { currentSmallPhaseIndex++; continue; }

            wandererStartPos = wanderer.position;
            if (currentSmallPhaseIndex % 5 == 0)
                bigPhaseStartWandererPos = wanderer.position;

            // ---------------- Activate ghosts for this phase ----------------
            if (phase.ghostsInPhase != null)
            {
                foreach (var gd in phase.ghostsInPhase)
                {
                    if (gd.ghostTransform == null) continue;

                    gd.ghostTransform.gameObject.SetActive(true);
                    gd.ghostTransform.position = wanderer.position; // spawn at wanderer
                }
            }

            // ---------------- Move ghosts simultaneously ----------------
            if (phase.ghostsInPhase != null && phase.ghostsInPhase.Count > 0)
            {
                List<Coroutine> ghostCoroutines = new List<Coroutine>();
                foreach (var gd in phase.ghostsInPhase)
                {
                    if (gd.ghostTransform != null && gd.waypoints != null && gd.waypoints.Count > 0)
                    {
                        Coroutine c = StartCoroutine(MoveCharacterWithProjectilesOnSphere(
                            gd.ghostTransform, gd.waypoints, phase.pauseDuration));
                        ghostCoroutines.Add(c);
                    }
                }

                foreach (var c in ghostCoroutines)
                    yield return c;

                ClearProjectiles();
            }

            // ---------------- Move wanderer ----------------
            if (phase.waypoints != null && phase.waypoints.Count > 0)
                yield return StartCoroutine(MoveCharacterWithProjectilesOnSphere(wanderer, phase.waypoints, phase.pauseDuration));

            // ---------------- Deactivate unused ghosts ----------------
            if (currentSmallPhaseIndex + 1 < smallPhases.Count)
            {
                var nextPhase = smallPhases[currentSmallPhaseIndex + 1];
                if (nextPhase.ghostsInPhase != null)
                {
                    foreach (var gd in phase.ghostsInPhase)
                    {
                        if (!nextPhase.ghostsInPhase.Contains(gd) && gd.ghostTransform != null)
                            gd.ghostTransform.gameObject.SetActive(false);
                    }
                }
            }

            currentSmallPhaseIndex++;
            if (currentSmallPhaseIndex % 5 == 0)
                respawnCount = 0;
        }

        sequenceRunning = false;
        Debug.Log("🎯 All small phases complete.");
    }

    // ----------------- Sphere Movement -----------------
    protected IEnumerator MoveCharacterWithProjectilesOnSphere(Transform character, List<PhaseWaypoint> waypoints, float pauseDuration)
    {
        foreach (var wp in waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = sphereCenterTransform.position + (wp.waypointTransform.position - sphereCenterTransform.position).normalized * sphereRadius;
            bool projectileFired = false;

            while (Vector3.Distance(character.position, target) > reachThreshold)
            {
                float step = moveSpeed * Time.deltaTime;
                character.position = SphereMover.MoveOnSphere(character.position, target, sphereCenterTransform.position, sphereRadius, step);
                character.up = (character.position - sphereCenterTransform.position).normalized;

                if (wp.triggerProjectile && !projectileFired)
                {
                    float remainingDistance = Vector3.Distance(character.position, target);
                    if (remainingDistance <= moveSpeed * wp.leadTime)
                    {
                        projectileSpawner.SpawnOne(this, wp.waypointTransform.position, character.position);
                        projectileFired = true;
                    }
                }

                yield return null;
            }

            yield return new WaitForSeconds(pauseDuration);
        }
    }

    // ----------------- Projectiles -----------------
    protected void ClearProjectiles()
    {
        foreach (var proj in activeProjectiles)
            if (proj != null) Destroy(proj);

        activeProjectiles.Clear();
    }

    // ----------------- Reset Phase -----------------
    public void ResetPhase()
    {
        respawnCount++;
        ClearProjectiles();
        wanderer.position = wandererStartPos;

        if (respawnCount >= maxRespawnsPerBigPhase)
        {
            int currentBigPhaseIndex = currentSmallPhaseIndex / 5;
            currentSmallPhaseIndex = currentBigPhaseIndex * 5;
            respawnCount = 0;
            wanderer.position = bigPhaseStartWandererPos;
        }

        StopAllCoroutines();
        StartCoroutine(RunSequence());
    }

    // ----------------- Gizmos for Editor -----------------
    private void OnDrawGizmos()
    {
        if (sphereCenterTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sphereCenterTransform.position, sphereRadius);

        if (smallPhases == null) return;

        foreach (var phase in smallPhases)
        {
            if (phase == null) continue;

            // Ghost waypoints
            if (phase.ghostsInPhase != null)
            {
                foreach (var gd in phase.ghostsInPhase)
                {
                    if (gd.waypoints != null)
                    {
                        foreach (var wp in gd.waypoints)
                        {
                            if (wp == null || wp.waypointTransform == null) continue;

                            Vector3 dir = (wp.waypointTransform.position - sphereCenterTransform.position).normalized;
                            Vector3 snappedPos = sphereCenterTransform.position + dir * sphereRadius;

                            Gizmos.color = Color.green;
                            Gizmos.DrawLine(sphereCenterTransform.position, snappedPos);
                            Gizmos.DrawSphere(snappedPos, 0.15f);

                            if (snapWaypointsToSphere && !Application.isPlaying)
                                wp.waypointTransform.position = snappedPos;
                        }
                    }
                }
            }

            // Wanderer waypoints
            if (phase.waypoints != null)
            {
                foreach (var wp in phase.waypoints)
                {
                    if (wp == null || wp.waypointTransform == null) continue;

                    Vector3 dir = (wp.waypointTransform.position - sphereCenterTransform.position).normalized;
                    Vector3 snappedPos = sphereCenterTransform.position + dir * sphereRadius;

                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(sphereCenterTransform.position, snappedPos);
                    Gizmos.DrawSphere(snappedPos, 0.15f);

                    if (snapWaypointsToSphere && !Application.isPlaying)
                        wp.waypointTransform.position = snappedPos;
                }
            }
        }

        // Ghost positions
        foreach (var phase in smallPhases)
        {
            if (phase?.ghostsInPhase == null) continue;

            foreach (var gd in phase.ghostsInPhase)
            {
                if (gd.ghostTransform != null)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(gd.ghostTransform.position, 0.2f);
                }
            }
        }

        // Wanderer position
        if (wanderer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(wanderer.position, 0.25f);
        }
    }
}
