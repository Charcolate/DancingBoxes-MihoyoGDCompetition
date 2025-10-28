using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalManager : MonoBehaviour
{
    [Header("Phase Configuration")]
    public List<GoalPhaseData> smallPhases = new List<GoalPhaseData>();

    [Header("Characters")]
    public Transform ghost;
    public Transform wanderer;

    [Header("Projectile System")]
    public ProjectileSpawner projectileSpawner;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float reachThreshold = 0.2f;

    [Header("Respawn Settings")]
    public int maxRespawnsPerBigPhase = 3;

    // Internal tracking
    private int currentSmallPhaseIndex = 0;
    private int respawnCount = 0;
    private bool sequenceRunning = false;

    // Track start positions
    private Vector3 ghostStartPos;
    private Vector3 wandererStartPos;
    private Vector3 bigPhaseStartGhostPos;
    private Vector3 bigPhaseStartWandererPos;

    // Track active projectiles
    private List<GameObject> activeProjectiles = new List<GameObject>();

    private void Start()
    {
        if (ghost == null || wanderer == null)
        {
            Debug.LogError("GoalManager: Ghost or Wanderer not assigned!");
            return;
        }

        if (smallPhases.Count == 0)
        {
            Debug.LogError("GoalManager: No small phases configured!");
            return;
        }

        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        sequenceRunning = true;

        while (currentSmallPhaseIndex < smallPhases.Count)
        {
            GoalPhaseData phase = smallPhases[currentSmallPhaseIndex];
            if (phase == null || phase.waypoints.Count == 0)
            {
                currentSmallPhaseIndex++;
                continue;
            }

            ghostStartPos = ghost.position;
            wandererStartPos = wanderer.position;

            if (currentSmallPhaseIndex % 5 == 0)
            {
                bigPhaseStartGhostPos = ghost.position;
                bigPhaseStartWandererPos = wanderer.position;
            }

            // Ghost moves and fires projectiles slightly before waypoint
            yield return StartCoroutine(MoveCharacterWithProjectiles(ghost, phase));

            // Destroy active projectiles when Ghost finishes small phase
            ClearProjectiles();

            // Wanderer moves after Ghost
            yield return StartCoroutine(MoveCharacterWithProjectiles(wanderer, phase));

            currentSmallPhaseIndex++;

            if (currentSmallPhaseIndex % 5 == 0)
            {
                respawnCount = 0;
                Debug.Log("✅ Big phase completed — respawn accumulation reset.");
            }
        }

        sequenceRunning = false;
        Debug.Log("🎯 All small phases complete.");
    }

    private IEnumerator MoveCharacterWithProjectiles(Transform character, GoalPhaseData phase)
    {
        foreach (PhaseWaypoint wp in phase.waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = wp.waypointTransform.position;
            float leadDistance = moveSpeed * wp.leadTime;
            bool projectileFired = false;

            while (Vector3.Distance(character.position, target) > reachThreshold)
            {
                float remainingDistance = Vector3.Distance(character.position, target);

                // Fire projectiles slightly before reaching waypoint
                if (wp.triggerProjectile && !projectileFired && remainingDistance <= leadDistance)
                {
                    FireProjectiles(character, wp);
                    projectileFired = true;
                }

                character.position = Vector3.MoveTowards(character.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(phase.pauseDuration);
        }
    }

    private void FireProjectiles(Transform character, PhaseWaypoint wp)
    {
        bool showTrajectory = (character == ghost); // Only show when Ghost is moving

        if (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
        {
            for (int j = 0; j < wp.projectileCount && j < wp.customSpawnTransforms.Count; j++)
            {
                Transform spawnTransform = wp.customSpawnTransforms[j];
                if (spawnTransform != null)
                {
                    GameObject proj = projectileSpawner.SpawnOne(this, spawnTransform.position, character.position);
                    if (proj != null)
                    {
                        activeProjectiles.Add(proj);

                        // Enable trajectory
                        Projectile projScript = proj.GetComponent<Projectile>();
                        if (projScript != null)
                            projScript.showTrajectory = showTrajectory;
                    }
                }
            }
        }
        else
        {
            for (int j = 0; j < wp.projectileCount; j++)
            {
                GameObject proj = projectileSpawner.SpawnOne(this, projectileSpawner.spawnPoint.position, character.position);
                if (proj != null)
                {
                    activeProjectiles.Add(proj);

                    // Enable trajectory
                    Projectile projScript = proj.GetComponent<Projectile>();
                    if (projScript != null)
                        projScript.showTrajectory = showTrajectory;
                }
            }
        }
    }

    private void ClearProjectiles()
    {
        foreach (GameObject proj in activeProjectiles)
        {
            if (proj != null)
                Destroy(proj);
        }
        activeProjectiles.Clear();
    }

    public void ResetPhase()
    {
        respawnCount++;
        Debug.Log($"💥 Wanderer hit — respawn count: {respawnCount}");

        // Destroy all active projectiles on screen
        ClearProjectiles();

        if (respawnCount < maxRespawnsPerBigPhase)
        {
            ghost.position = ghostStartPos;
            wanderer.position = wandererStartPos;
        }
        else
        {
            int currentBigPhaseIndex = currentSmallPhaseIndex / 5;
            currentSmallPhaseIndex = currentBigPhaseIndex * 5;
            respawnCount = 0;

            ghost.position = bigPhaseStartGhostPos;
            wanderer.position = bigPhaseStartWandererPos;

            Debug.Log($"🔁 Respawn limit reached — restarting big phase {currentBigPhaseIndex + 1}");
        }

        StopAllCoroutines();
        StartCoroutine(RunSequence());
    }

    public bool IsGhostSequenceFinished()
    {
        return !sequenceRunning;
    }
}
