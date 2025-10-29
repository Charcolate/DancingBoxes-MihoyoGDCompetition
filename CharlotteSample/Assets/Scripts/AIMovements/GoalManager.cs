using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ExtraGhostData
{
    [Tooltip("The Transform of this additional ghost.")]
    public Transform ghostTransform;

    [Tooltip("Custom small phases (waypoints) for this ghost.")]
    public List<GoalPhaseData> ghostPhases = new List<GoalPhaseData>();
}

public class GoalManager : MonoBehaviour
{
    [Header("Phase Configuration")]
    public List<GoalPhaseData> smallPhases = new List<GoalPhaseData>();

    [Header("Characters")]
    public Transform ghost;
    public Transform wanderer;

    [Header("Multiple Ghosts (optional)")]
    [Tooltip("Each entry defines a ghost and its unique waypoint phases.")]
    public List<ExtraGhostData> extraGhosts = new List<ExtraGhostData>();

    [Header("Projectile System")]
    public ProjectileSpawner projectileSpawner;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float reachThreshold = 0.2f;

    [Header("Respawn Settings")]
    public int maxRespawnsPerBigPhase = 3;

    // Internal tracking
    protected int currentSmallPhaseIndex = 0;
    protected int respawnCount = 0;
    protected bool sequenceRunning = false;

    protected Vector3 ghostStartPos;
    protected Vector3 wandererStartPos;
    protected Vector3 bigPhaseStartGhostPos;
    protected Vector3 bigPhaseStartWandererPos;

    protected List<GameObject> activeProjectiles = new List<GameObject>();

    protected virtual void Start()
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

        // Start the main ghost + wanderer sequence
        StartCoroutine(RunSequence());

        // Start sequences for extra ghosts
        foreach (ExtraGhostData ghostData in extraGhosts)
        {
            if (ghostData.ghostTransform == null)
                continue;

            if (ghostData.ghostPhases == null || ghostData.ghostPhases.Count == 0)
            {
                Debug.LogWarning($"⚠️ Extra ghost '{ghostData.ghostTransform.name}' has no phases assigned.");
                continue;
            }

            StartCoroutine(RunExtraGhostSequence(ghostData.ghostTransform, ghostData.ghostPhases));
        }
    }

    protected virtual IEnumerator RunSequence()
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

            // Ghost moves first
            yield return StartCoroutine(MoveCharacterWithProjectiles(ghost, phase));

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

    protected virtual IEnumerator MoveCharacterWithProjectiles(Transform character, GoalPhaseData phase)
    {
        foreach (PhaseWaypoint wp in phase.waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = wp.waypointTransform.position;
            bool projectileFired = false;

            while (Vector3.Distance(character.position, target) > reachThreshold)
            {
                character.position = Vector3.MoveTowards(character.position, target, moveSpeed * Time.deltaTime);

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

    protected void FireProjectiles(Transform character, PhaseWaypoint wp)
    {
        bool showTrajectory = (character == ghost);

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
                    Projectile projScript = proj.GetComponent<Projectile>();
                    if (projScript != null)
                        projScript.showTrajectory = showTrajectory;
                }
            }
        }
    }

    protected void ClearProjectiles()
    {
        foreach (GameObject proj in activeProjectiles)
        {
            if (proj != null) Destroy(proj);
        }
        activeProjectiles.Clear();
    }

    public void ResetPhase()
    {
        respawnCount++;
        Debug.Log($"💥 Wanderer hit — respawn count: {respawnCount}");

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

        // Restart extra ghosts
        foreach (ExtraGhostData ghostData in extraGhosts)
        {
            if (ghostData.ghostTransform == null)
                continue;

            if (ghostData.ghostPhases == null || ghostData.ghostPhases.Count == 0)
                continue;

            StartCoroutine(RunExtraGhostSequence(ghostData.ghostTransform, ghostData.ghostPhases));
        }
    }

    public bool IsGhostSequenceFinished()
    {
        return !sequenceRunning;
    }

    // ----------------- Extra Ghost Coroutine -----------------
    protected IEnumerator RunExtraGhostSequence(Transform ghostTransform, List<GoalPhaseData> ghostPhases)
    {
        if (ghostTransform == null || ghostPhases == null || ghostPhases.Count == 0)
            yield break;

        Debug.Log($"👻 Starting extra ghost: {ghostTransform.name}");

        foreach (GoalPhaseData phase in ghostPhases)
        {
            if (phase == null || phase.waypoints == null || phase.waypoints.Count == 0)
                continue;

            yield return StartCoroutine(MoveCharacterWithProjectiles(ghostTransform, phase));
            ClearProjectiles(); // optional cleanup
        }

        Debug.Log($"👻 Extra ghost '{ghostTransform.name}' completed all its phases.");
    }
}
