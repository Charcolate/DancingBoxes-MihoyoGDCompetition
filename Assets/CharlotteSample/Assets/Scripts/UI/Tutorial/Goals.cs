using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goals : MonoBehaviour
{
    [Header("Phase Configuration")]
    public List<GoalPhaseData> smallPhases = new List<GoalPhaseData>();

    [Header("Characters")]
    public Transform wanderer;

    [Header("Ghost System")]
    public GameObject ghostPrefab;

    [Header("Projectile System")]
    public ProjectileSpawner projectileSpawner;

    [Header("Cylinder System")]
    public ColliderController_NewInput cylinderManager;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float reachThreshold = 0.2f;

    [Header("Respawn Settings")]
    public int maxRespawnsPerBigPhase = 3;

    [Header("Gizmos Settings")]
    public bool showWaypointsGizmos = true;
    public float waypointSphereSize = 0.3f;

    // Internal tracking
    protected int currentSmallPhaseIndex = 0;
    public int CurrentSmallPhaseIndex => currentSmallPhaseIndex;
    protected int respawnCount = 0;
    protected bool sequenceRunning = false;

    protected Vector3 wandererStartPos;
    protected Vector3 bigPhaseStartWandererPos;
    protected Vector3 currentSmallPhaseStartPos;

    protected List<GameObject> activeProjectiles = new List<GameObject>();
    protected List<GameObject> wandererProjectiles = new List<GameObject>();

    protected Dictionary<Transform, List<GameObject>> ghostProjectiles = new Dictionary<Transform, List<GameObject>>();
    public List<GameObject> spawnedGhosts = new List<GameObject>();

    protected Dictionary<Transform, TrailRenderer> ghostTrails = new Dictionary<Transform, TrailRenderer>();
    protected Dictionary<Collider, PhaseWaypoint> waypointTriggers = new Dictionary<Collider, PhaseWaypoint>();

    public int GetCurrentSmallPhaseIndex()
    {
        return currentSmallPhaseIndex;
    }

    protected virtual void Start()
    {
        if (wanderer == null)
        {
            Debug.LogError("Goals: Wanderer not assigned!");
            return;
        }

        if (smallPhases.Count == 0)
        {
            Debug.LogError("Goals: No small phases configured!");
            return;
        }

        if (ghostPrefab == null)
        {
            Debug.LogError("Goals: Ghost prefab not assigned!");
            return;
        }

        InitializeWaypointTriggers();

        wandererStartPos = wanderer.position;
        bigPhaseStartWandererPos = wanderer.position;
        currentSmallPhaseStartPos = wanderer.position;

        StartCoroutine(RunSequence());
    }

    protected virtual void InitializeWaypointTriggers()
    {
        waypointTriggers.Clear();

        foreach (var phase in smallPhases)
        {
            if (phase == null) continue;

            if (phase.waypoints != null)
            {
                foreach (var wp in phase.waypoints)
                {
                    if (wp?.waypointTransform != null)
                    {
                        AddTriggerToWaypoint(wp);
                    }
                }
            }

            if (phase.ghostsInPhase != null)
            {
                foreach (var ghostData in phase.ghostsInPhase)
                {
                    if (ghostData?.ghostWaypoints != null)
                    {
                        foreach (var wp in ghostData.ghostWaypoints)
                        {
                            if (wp?.waypointTransform != null)
                            {
                                AddTriggerToWaypoint(wp);
                            }
                        }
                    }
                }
            }

            if (phase.wandererWaypoints != null)
            {
                foreach (var wp in phase.wandererWaypoints)
                {
                    if (wp?.waypointTransform != null)
                    {
                        AddTriggerToWaypoint(wp);
                    }
                }
            }
        }
    }

    protected virtual void AddTriggerToWaypoint(PhaseWaypoint wp)
    {
        if (wp.waypointTransform == null) return;

        Collider collider = wp.waypointTransform.GetComponent<Collider>();
        if (collider == null)
        {
            SphereCollider sphereCollider = wp.waypointTransform.gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 1.0f;
            collider = sphereCollider;
        }
        else if (!collider.isTrigger)
        {
            collider.isTrigger = true;
        }

        WaypointTrigger trigger = wp.waypointTransform.GetComponent<WaypointTrigger>();
        if (trigger == null)
        {
            trigger = wp.waypointTransform.gameObject.AddComponent<WaypointTrigger>();
            trigger.goals = this; // Make sure this line is setting the Goals reference
        }

        waypointTriggers[collider] = wp;
    }

    public bool AreCylindersInWaypoint(Collider waypointCollider)
    {
        if (cylinderManager == null || cylinderManager.cylinders == null) return false;

        foreach (var cylinder in cylinderManager.cylinders)
        {
            if (cylinder != null && cylinder.activeInHierarchy)
            {
                Collider cylinderCollider = cylinder.GetComponent<Collider>();
                if (cylinderCollider != null)
                {
                    if (waypointCollider.bounds.Contains(cylinder.transform.position))
                    {
                        return true;
                    }

                    SphereCollider sphereCollider = waypointCollider as SphereCollider;
                    if (sphereCollider != null)
                    {
                        float distance = Vector3.Distance(cylinder.transform.position, waypointCollider.transform.position);
                        if (distance <= sphereCollider.radius * waypointCollider.transform.lossyScale.x)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    public void OnWandererEnterWaypoint(Collider waypointCollider)
    {
        if (!sequenceRunning) return;
        Debug.Log($"Wanderer entered waypoint - starting continuous cylinder monitoring");
    }

    public void OnWandererInWaypointWithoutCylinders(Collider waypointCollider)
    {
        if (!sequenceRunning) return;
        Debug.Log($"Wanderer in waypoint without cylinders - triggering immediate respawn!");
        ResetPhase();
    }

    protected virtual IEnumerator RunSequence()
    {
        sequenceRunning = true;

        while (currentSmallPhaseIndex < smallPhases.Count)
        {
            GoalPhaseData phase = smallPhases[currentSmallPhaseIndex];
            if (phase == null) { currentSmallPhaseIndex++; continue; }

            currentSmallPhaseStartPos = wanderer.position;
            wandererStartPos = wanderer.position;

            if (currentSmallPhaseIndex % 5 == 0)
            {
                bigPhaseStartWandererPos = wanderer.position;
                Debug.Log($"Starting new big phase at position: {bigPhaseStartWandererPos}");
            }

            Debug.Log($"Starting small phase {currentSmallPhaseIndex + 1} at position: {currentSmallPhaseStartPos}");

            DestroyAllGhosts();
            ghostProjectiles.Clear();
            ghostTrails.Clear();

            yield return StartCoroutine(SpawnGhostsForPhase(phase));

            if (phase.waypoints != null && phase.waypoints.Count > 0)
            {
                foreach (var wp in phase.waypoints)
                {
                    yield return StartCoroutine(MoveCharacterWithProjectiles(null, new List<PhaseWaypoint> { wp }, phase.pauseDuration));
                }
            }

            List<Coroutine> ghostCoroutines = new List<Coroutine>();
            if (phase.ghostsInPhase != null)
            {
                foreach (var ghostData in phase.ghostsInPhase)
                {
                    Transform spawnedGhost = FindSpawnedGhost(ghostData);
                    if (spawnedGhost != null && ghostData.ghostWaypoints != null && ghostData.ghostWaypoints.Count > 0)
                    {
                        if (!ghostProjectiles.ContainsKey(spawnedGhost))
                        {
                            ghostProjectiles[spawnedGhost] = new List<GameObject>();
                        }

                        if (ghostData.enableGoldTrail && ghostTrails.ContainsKey(spawnedGhost))
                        {
                            ghostTrails[spawnedGhost].emitting = true;
                            Debug.Log($"Gold trail ENABLED for ghost");
                        }

                        ghostCoroutines.Add(StartCoroutine(
                            MoveGhostWithProjectiles(spawnedGhost, ghostData.ghostWaypoints, phase.pauseDuration)
                        ));
                    }
                }
            }

            foreach (var c in ghostCoroutines) yield return c;

            foreach (var trail in ghostTrails.Values)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                    Debug.Log("Gold trail DISABLED - ghost stopped moving");
                }
            }

            DestroyAllGhostProjectiles();

            if (phase.wandererWaypoints != null && phase.wandererWaypoints.Count > 0)
            {
                yield return StartCoroutine(MoveWandererWithProjectiles(phase.wandererWaypoints, phase.pauseDuration));
            }

            DestroyAllGhosts();

            Debug.Log($"Small phase {currentSmallPhaseIndex + 1} completed");

            currentSmallPhaseIndex++;

            if (currentSmallPhaseIndex % 5 == 0)
            {
                respawnCount = 0;
                Debug.Log("Big phase completed — respawn accumulation reset.");
            }
        }

        sequenceRunning = false;
        Debug.Log("All small phases complete.");
    }

    protected virtual IEnumerator SpawnGhostsForPhase(GoalPhaseData phase)
    {
        if (ghostPrefab == null)
        {
            Debug.LogError("Ghost prefab is null!");
            yield break;
        }

        if (phase.ghostsInPhase == null || phase.ghostsInPhase.Count == 0)
        {
            Debug.Log("No ghosts configured for this phase");
            yield break;
        }

        int spawnedCount = 0;
        foreach (var ghostData in phase.ghostsInPhase)
        {
            Vector3 spawnPosition = wanderer.position;
            GameObject ghost = Instantiate(ghostPrefab, spawnPosition, Quaternion.identity);

            if (ghost == null)
            {
                Debug.LogError("Failed to instantiate ghost!");
                continue;
            }

            spawnedGhosts.Add(ghost);
            spawnedCount++;

            if (ghostData.enableGoldTrail)
            {
                TrailRenderer trail = ghost.AddComponent<TrailRenderer>();
                SetupGoldTrail(trail, ghostData);
                ghostTrails[ghost.transform] = trail;
                Debug.Log($"Gold trail ADDED to ghost '{ghost.name}'");
            }

            ghostData.ghostTransform = ghost.transform;

            Debug.Log($"Spawned ghost '{ghost.name}' at position: {spawnPosition}");
        }

        yield return new WaitForSeconds(0.1f);
        Debug.Log($"Successfully spawned {spawnedCount} ghosts for phase");
    }

    protected virtual void SetupGoldTrail(TrailRenderer trail, GhostPhaseData ghostData)
    {
        if (trail == null) return;

        trail.time = 1f;
        trail.startWidth = ghostData.trailWidth;
        trail.endWidth = 0f;
        trail.material = CreateTrailMaterial(ghostData.trailColor);
        trail.colorGradient = CreateGoldGradient(ghostData.trailColor);
        trail.emitting = false;
        trail.minVertexDistance = 0.1f;

        if (trail.material == null)
        {
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = ghostData.trailColor;
        }
    }

    protected virtual Material CreateTrailMaterial(Color trailColor)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = trailColor;
        return mat;
    }

    protected virtual Gradient CreateGoldGradient(Color baseColor)
    {
        Gradient gradient = new Gradient();
        gradient.colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(baseColor, 0f),
            new GradientColorKey(baseColor, 0.7f),
            new GradientColorKey(new Color(baseColor.r, baseColor.g, baseColor.b, 0f), 1f)
        };
        gradient.alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(0.8f, 0f),
            new GradientAlphaKey(0.5f, 0.5f),
            new GradientAlphaKey(0f, 1f)
        };
        return gradient;
    }

    protected Transform FindSpawnedGhost(GhostPhaseData ghostData)
    {
        return ghostData.ghostTransform;
    }

    protected virtual void DestroyAllGhosts()
    {
        if (spawnedGhosts == null)
        {
            Debug.LogWarning("spawnedGhosts list is null!");
            return;
        }

        int initialCount = spawnedGhosts.Count;
        spawnedGhosts.RemoveAll(ghost => ghost == null);
        int removedNulls = initialCount - spawnedGhosts.Count;

        if (removedNulls > 0)
        {
            Debug.Log($"Cleaned up {removedNulls} null ghosts from list");
        }

        foreach (GameObject ghost in spawnedGhosts)
        {
            if (ghost != null)
            {
                // ANIMATION: Stop ghost animation before destroying
                GhostMovementAnimator ghostAnimator = ghost.GetComponent<GhostMovementAnimator>();
                if (ghostAnimator != null)
                {
                    ghostAnimator.StopMoving();
                }

                Destroy(ghost);
                Debug.Log("Destroyed ghost");
            }
        }
        spawnedGhosts.Clear();
        ghostTrails.Clear();

        Debug.Log($"Destroyed all ghosts. List count: {spawnedGhosts.Count}");
    }

    protected virtual IEnumerator MoveGhostWithProjectiles(Transform ghost, List<PhaseWaypoint> waypoints, float pauseDuration)
    {
        // ANIMATION: Get the ghost's animator component and start moving
        GhostMovementAnimator ghostAnimator = ghost.GetComponent<GhostMovementAnimator>();
        if (ghostAnimator != null)
        {
            ghostAnimator.StartMoving();
        }

        bool hasTrail = ghostTrails.ContainsKey(ghost) && ghostTrails[ghost] != null;
        if (hasTrail)
        {
            ghostTrails[ghost].emitting = true;
            ghostTrails[ghost].Clear();
            Debug.Log($"Gold trail STARTED for ghost");
        }

        foreach (PhaseWaypoint wp in waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = wp.waypointTransform.position;
            bool projectileFired = false;

            while (ghost != null && Vector3.Distance(ghost.position, target) > reachThreshold)
            {
                // FLAT PLANE MOVEMENT - Simple MoveTowards
                ghost.position = Vector3.MoveTowards(ghost.position, target, moveSpeed * Time.deltaTime);

                if (wp.triggerProjectile && !projectileFired)
                {
                    float remainingDistance = Vector3.Distance(ghost.position, target);
                    float leadDistance = moveSpeed * wp.leadTime;

                    if (remainingDistance <= leadDistance)
                    {
                        FireGhostProjectiles(ghost, wp);
                        projectileFired = true;
                    }
                }

                yield return null;
            }

            if (ghost != null)
            {
                ghost.position = target;
            }

            yield return new WaitForSeconds(pauseDuration);
        }

        // ANIMATION: Stop the ghost's jump animation
        if (ghostAnimator != null)
        {
            ghostAnimator.StopMoving();
        }

        if (hasTrail)
        {
            ghostTrails[ghost].emitting = false;
            Debug.Log($"Gold trail STOPPED for ghost");
        }
    }

    protected virtual IEnumerator MoveCharacterWithProjectiles(Transform character, List<PhaseWaypoint> waypoints, float pauseDuration)
    {
        foreach (PhaseWaypoint wp in waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = wp.waypointTransform.position;
            bool projectileFired = false;

            while (character != null && Vector3.Distance(character.position, target) > reachThreshold)
            {
                // FLAT PLANE MOVEMENT - Simple MoveTowards
                character.position = Vector3.MoveTowards(character.position, target, moveSpeed * Time.deltaTime);

                if (wp.triggerProjectile && !projectileFired)
                {
                    float remainingDistance = Vector3.Distance(character.position, target);
                    float leadDistance = moveSpeed * wp.leadTime;

                    if (remainingDistance <= leadDistance)
                    {
                        FireProjectiles(character ?? wanderer, wp);
                        projectileFired = true;
                    }
                }

                yield return null;
            }

            if (character != null)
            {
                character.position = target;
            }

            yield return new WaitForSeconds(pauseDuration);
        }
    }

    protected virtual IEnumerator MoveWandererWithProjectiles(List<PhaseWaypoint> waypoints, float pauseDuration)
    {
        Debug.Log($"Starting wanderer movement with {waypoints?.Count} waypoints");

        // ANIMATION: START MOVING - Trigger jump animation
        WandererMovementAnimator movementAnimator = wanderer.GetComponent<WandererMovementAnimator>();
        if (movementAnimator != null)
        {
            movementAnimator.StartMoving();
        }
        else
        {
            Debug.LogError("No WandererMovementAnimator found on wanderer!");
        }

        ClearAllProjectileTrails();

        foreach (PhaseWaypoint wp in waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = wp.waypointTransform.position;
            bool wandererProjectileFired = false;

            while (wanderer != null && Vector3.Distance(wanderer.position, target) > reachThreshold)
            {
                // FLAT PLANE MOVEMENT - Simple MoveTowards
                wanderer.position = Vector3.MoveTowards(wanderer.position, target, moveSpeed * Time.deltaTime);

                if (wp.triggerProjectile && !wandererProjectileFired)
                {
                    float remainingDistance = Vector3.Distance(wanderer.position, target);
                    float leadDistance = moveSpeed * wp.leadTime;

                    if (remainingDistance <= leadDistance)
                    {
                        FireWandererProjectiles(wp);
                        wandererProjectileFired = true;
                        Debug.Log("Wanderer projectile fired (no visual)");
                    }
                }

                yield return null;
            }

            if (wanderer != null)
            {
                wanderer.position = target;
            }

            yield return new WaitForSeconds(pauseDuration);
        }

        // ANIMATION: STOP MOVING - Return to idle
        if (movementAnimator != null)
        {
            movementAnimator.StopMoving();
        }

        Debug.Log("Wanderer movement completed");
    }

    protected void FireProjectiles(Transform character, PhaseWaypoint wp, float customSpeed = -1f)
    {
        if (projectileSpawner == null)
        {
            Debug.LogWarning("Goals: ProjectileSpawner not assigned!");
            return;
        }

        List<Transform> spawnTransforms = (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
            ? wp.customSpawnTransforms
            : new List<Transform> { projectileSpawner.transform };

        foreach (var spawn in spawnTransforms)
        {
            if (spawn == null) continue;

            float speedToUse = customSpeed > 0f ? customSpeed : projectileSpawner.projectileSpeed;
            GameObject proj = projectileSpawner.SpawnOne(spawn.position, wp.waypointTransform.position, false);
            if (proj != null)
            {
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.SetTarget(spawn.position, wp.waypointTransform, speedToUse, false);
                }
                activeProjectiles.Add(proj);
            }
        }
    }

    protected void FireGhostProjectiles(Transform ghost, PhaseWaypoint wp, float customSpeed = -1f)
    {
        if (projectileSpawner == null)
        {
            Debug.LogWarning("Goals: ProjectileSpawner not assigned!");
            return;
        }

        List<Transform> spawnTransforms = (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
            ? wp.customSpawnTransforms
            : new List<Transform> { projectileSpawner.transform };

        foreach (var spawn in spawnTransforms)
        {
            if (spawn == null) continue;

            float speedToUse = customSpeed > 0f ? customSpeed : projectileSpawner.projectileSpeed;
            GameObject proj = projectileSpawner.SpawnOne(spawn.position, wp.waypointTransform.position, false);
            if (proj != null)
            {
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.showTrajectory = true;
                    projectile.SetTarget(spawn.position, wp.waypointTransform, speedToUse, false);
                }

                activeProjectiles.Add(proj);
                if (ghostProjectiles.ContainsKey(ghost))
                {
                    ghostProjectiles[ghost].Add(proj);
                }
            }
        }
    }

    protected void FireWandererProjectiles(PhaseWaypoint wp, float customSpeed = -1f)
    {
        if (projectileSpawner == null)
        {
            Debug.LogWarning("Goals: ProjectileSpawner not assigned!");
            return;
        }

        List<Transform> spawnTransforms = (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
            ? wp.customSpawnTransforms
            : new List<Transform> { projectileSpawner.transform };

        Debug.Log($"Firing WANDERER projectile - should destroy on collision!");

        foreach (var spawn in spawnTransforms)
        {
            if (spawn == null) continue;

            float speedToUse = customSpeed > 0f ? customSpeed : projectileSpawner.projectileSpeed;

            GameObject proj = projectileSpawner.SpawnOne(spawn.position, wp.waypointTransform.position, true);
            if (proj != null)
            {
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.showTrajectory = false;
                    projectile.SetTarget(spawn.position, wp.waypointTransform, speedToUse, false);
                    Debug.Log($"Wanderer projectile created - destroyOnCollision: {projectile.destroyOnCollision}");
                }
                wandererProjectiles.Add(proj);
            }
            else
            {
                Debug.LogError("FAILED to spawn wanderer projectile!");
            }
        }
    }

    protected void DestroyAllGhostProjectiles()
    {
        foreach (var ghostProjList in ghostProjectiles.Values)
        {
            ghostProjList.RemoveAll(proj => proj == null);

            foreach (GameObject proj in ghostProjList)
            {
                if (proj != null)
                {
                    Projectile projectile = proj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        projectile.DestroyImmediately();
                    }
                }
            }
            ghostProjList.Clear();
        }

        activeProjectiles.RemoveAll(proj => proj == null);
        Debug.Log("All ghost projectiles destroyed - small phase complete");
    }

    protected void ClearProjectiles()
    {
        activeProjectiles.RemoveAll(proj => proj == null);
        wandererProjectiles.RemoveAll(proj => proj == null);

        foreach (var ghostProjList in ghostProjectiles.Values)
        {
            ghostProjList.RemoveAll(proj => proj == null);
        }

        foreach (GameObject proj in activeProjectiles)
        {
            if (proj != null) Destroy(proj);
        }
        foreach (GameObject proj in wandererProjectiles)
        {
            if (proj != null) Destroy(proj);
        }
        activeProjectiles.Clear();
        wandererProjectiles.Clear();
        ghostProjectiles.Clear();
    }

    public void ClearAllProjectileTrails()
    {
        activeProjectiles.RemoveAll(proj => proj == null);
        wandererProjectiles.RemoveAll(proj => proj == null);

        foreach (var ghostProjList in ghostProjectiles.Values)
        {
            ghostProjList.RemoveAll(proj => proj == null);
        }

        foreach (GameObject proj in activeProjectiles)
        {
            if (proj != null)
            {
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.ClearTrail();
                }
            }
        }
        foreach (GameObject proj in wandererProjectiles)
        {
            if (proj != null)
            {
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.ClearTrail();
                }
            }
        }
    }

    public void ResetPhase()
    {
        Debug.Log("ResetPhase called!");

        if (!sequenceRunning)
        {
            Debug.Log("Sequence not running, cannot reset");
            return;
        }

        // ANIMATION: Reset wanderer animations
        if (wanderer != null)
        {
            WandererMovementAnimator movementAnimator = wanderer.GetComponent<WandererMovementAnimator>();
            if (movementAnimator != null)
            {
                movementAnimator.ResetAnimator();
            }
        }

        respawnCount++;
        Debug.Log($"Wanderer hit — respawn count: {respawnCount}");
        Debug.Log($"Current small phase start position: {currentSmallPhaseStartPos}");

        ClearAllProjectileTrails();
        ClearProjectiles();
        DestroyAllGhosts();

        if (currentSmallPhaseIndex >= smallPhases.Count)
        {
            Debug.Log("❌ Current phase index out of range");
            return;
        }

        GoalPhaseData currentPhase = smallPhases[currentSmallPhaseIndex];

        if (respawnCount < maxRespawnsPerBigPhase)
        {
            Debug.Log($"Resetting to current small phase start (respawn {respawnCount}/{maxRespawnsPerBigPhase})");

            wanderer.position = currentSmallPhaseStartPos;
            Debug.Log($"Wanderer reset to small phase start position: {wanderer.position}");

            // FIX: Restart just the current phase instead of the entire sequence
            StopAllCoroutines();
            StartCoroutine(RunCurrentPhaseOnly());
        }
        else
        {
            int currentBigPhaseIndex = currentSmallPhaseIndex / 5;
            currentSmallPhaseIndex = currentBigPhaseIndex * 5;
            respawnCount = 0;

            wanderer.position = bigPhaseStartWandererPos;
            Debug.Log($"Wanderer reset to big phase start: {wanderer.position}");

            Debug.Log($"Respawn limit reached — restarting big phase {currentBigPhaseIndex + 1}");

            StopAllCoroutines();
            StartCoroutine(RunSequence());
        }

        Debug.Log("ResetPhase completed successfully");
    }

    // ADD THIS NEW METHOD to run only the current phase
    private IEnumerator RunCurrentPhaseOnly()
    {
        sequenceRunning = true;

        if (currentSmallPhaseIndex < smallPhases.Count)
        {
            GoalPhaseData phase = smallPhases[currentSmallPhaseIndex];
            if (phase == null)
            {
                sequenceRunning = false;
                yield break;
            }

            Debug.Log($"Resuming small phase {currentSmallPhaseIndex + 1}");

            // Re-spawn ghosts and continue from current phase
            yield return StartCoroutine(SpawnGhostsForPhase(phase));

            if (phase.waypoints != null && phase.waypoints.Count > 0)
            {
                foreach (var wp in phase.waypoints)
                {
                    yield return StartCoroutine(MoveCharacterWithProjectiles(null, new List<PhaseWaypoint> { wp }, phase.pauseDuration));
                }
            }

            List<Coroutine> ghostCoroutines = new List<Coroutine>();
            if (phase.ghostsInPhase != null)
            {
                foreach (var ghostData in phase.ghostsInPhase)
                {
                    Transform spawnedGhost = FindSpawnedGhost(ghostData);
                    if (spawnedGhost != null && ghostData.ghostWaypoints != null && ghostData.ghostWaypoints.Count > 0)
                    {
                        if (!ghostProjectiles.ContainsKey(spawnedGhost))
                        {
                            ghostProjectiles[spawnedGhost] = new List<GameObject>();
                        }

                        if (ghostData.enableGoldTrail && ghostTrails.ContainsKey(spawnedGhost))
                        {
                            ghostTrails[spawnedGhost].emitting = true;
                            Debug.Log($"Gold trail ENABLED for ghost");
                        }

                        ghostCoroutines.Add(StartCoroutine(
                            MoveGhostWithProjectiles(spawnedGhost, ghostData.ghostWaypoints, phase.pauseDuration)
                        ));
                    }
                }
            }

            foreach (var c in ghostCoroutines) yield return c;

            foreach (var trail in ghostTrails.Values)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                    Debug.Log("Gold trail DISABLED - ghost stopped moving");
                }
            }

            DestroyAllGhostProjectiles();

            if (phase.wandererWaypoints != null && phase.wandererWaypoints.Count > 0)
            {
                yield return StartCoroutine(MoveWandererWithProjectiles(phase.wandererWaypoints, phase.pauseDuration));
            }

            DestroyAllGhosts();

            Debug.Log($"Small phase {currentSmallPhaseIndex + 1} completed");

            // Move to next phase and continue the sequence
            currentSmallPhaseIndex++;
            if (currentSmallPhaseIndex < smallPhases.Count)
            {
                StartCoroutine(RunSequence());
            }
            else
            {
                sequenceRunning = false;
                Debug.Log("All small phases complete.");
            }
        }
        else
        {
            sequenceRunning = false;
        }
    }

    public bool IsSequenceFinished()
    {
        return !sequenceRunning && currentSmallPhaseIndex >= smallPhases.Count;
    }

    protected virtual void Update()
    {
        activeProjectiles.RemoveAll(proj => proj == null);
        wandererProjectiles.RemoveAll(proj => proj == null);

        foreach (var ghostProjList in ghostProjectiles.Values)
        {
            ghostProjList.RemoveAll(proj => proj == null);
        }

        spawnedGhosts.RemoveAll(ghost => ghost == null);
    }

    private void OnDrawGizmos()
    {
        if (!showWaypointsGizmos || smallPhases == null) return;

        foreach (var phase in smallPhases)
        {
            if (phase == null) continue;

            if (phase.waypoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var wp in phase.waypoints)
                {
                    if (wp?.waypointTransform != null)
                    {
                        Gizmos.DrawSphere(wp.waypointTransform.position, waypointSphereSize);
                    }
                }
            }

            if (phase.ghostsInPhase != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var ghostData in phase.ghostsInPhase)
                {
                    if (ghostData?.ghostWaypoints == null) continue;
                    foreach (var wp in ghostData.ghostWaypoints)
                    {
                        if (wp?.waypointTransform != null)
                        {
                            Gizmos.DrawSphere(wp.waypointTransform.position, waypointSphereSize);
                        }
                    }
                }
            }

            if (phase.wandererWaypoints != null)
            {
                Gizmos.color = Color.magenta;
                foreach (var wp in phase.wandererWaypoints)
                {
                    if (wp?.waypointTransform != null)
                    {
                        Gizmos.DrawSphere(wp.waypointTransform.position, waypointSphereSize);
                    }
                }
            }

            Gizmos.color = Color.red;
            foreach (var wp in phase.waypoints)
            {
                if (wp?.waypointTransform != null && wp.triggerProjectile)
                {
                    Vector3 start = (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0 && wp.customSpawnTransforms[0] != null)
                        ? wp.customSpawnTransforms[0].position
                        : (projectileSpawner != null ? projectileSpawner.transform.position : Vector3.zero);

                    Vector3 end = wp.waypointTransform.position;

                    if (start != Vector3.zero)
                    {
                        Vector3 previous = start;
                        int resolution = 20;
                        for (int i = 1; i <= resolution; i++)
                        {
                            float t = i / (float)resolution;
                            Vector3 midpoint = Vector3.Lerp(start, end, t);
                            midpoint.y += Mathf.Sin(t * Mathf.PI) * 2.0f;
                            Gizmos.DrawLine(previous, midpoint);
                            previous = midpoint;
                        }
                    }
                }
            }

            foreach (var ghostData in phase.ghostsInPhase)
            {
                if (ghostData?.ghostWaypoints == null) continue;

                foreach (var wp in ghostData.ghostWaypoints)
                {
                    if (wp?.waypointTransform != null && wp.triggerProjectile)
                    {
                        Vector3 start = (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0 && wp.customSpawnTransforms[0] != null)
                            ? wp.customSpawnTransforms[0].position
                            : (projectileSpawner != null ? projectileSpawner.transform.position : Vector3.zero);

                        Vector3 end = wp.waypointTransform.position;

                        if (start != Vector3.zero)
                        {
                            Vector3 previous = start;
                            int resolution = 20;
                            for (int i = 1; i <= resolution; i++)
                            {
                                float t = i / (float)resolution;
                                Vector3 midpoint = Vector3.Lerp(start, end, t);
                                midpoint.y += Mathf.Sin(t * Mathf.PI) * 1.5f;
                                Gizmos.DrawLine(previous, midpoint);
                                previous = midpoint;
                            }
                        }
                    }
                }
            }
        }
    }
}