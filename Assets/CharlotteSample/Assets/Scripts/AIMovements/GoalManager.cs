using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// SphereMover class for spherical movement
public static class SphereMover
{
    public static (Vector3 position, Quaternion rotation) MoveOnSphere(Vector3 currentPos, Vector3 targetPos, Vector3 sphereCenter, float radius, float step)
    {
        Vector3 dirCurrent = (currentPos - sphereCenter).normalized;
        Vector3 dirTarget = (targetPos - sphereCenter).normalized;

        float angle = Vector3.Angle(dirCurrent, dirTarget);
        if (angle < 0.001f)
        {
            Quaternion finalRotation = Quaternion.LookRotation(CalculateForwardDirection(dirTarget), dirTarget);
            return (sphereCenter + dirTarget * radius, finalRotation);
        }

        float t = Mathf.Min(1f, step / angle);
        Vector3 newDir = Vector3.Slerp(dirCurrent, dirTarget, t).normalized;

        // Calculate rotation to align with sphere surface
        Quaternion newRotation = Quaternion.LookRotation(CalculateForwardDirection(newDir), newDir);

        return (sphereCenter + newDir * radius, newRotation);
    }

    // Calculate a forward direction that's tangent to the sphere surface
    private static Vector3 CalculateForwardDirection(Vector3 surfaceNormal)
    {
        // Use world up as reference, but project it onto the tangent plane
        Vector3 referenceUp = Vector3.up;

        // If the surface normal is nearly parallel to world up, use a different reference
        if (Mathf.Abs(Vector3.Dot(surfaceNormal, referenceUp)) > 0.99f)
        {
            referenceUp = Vector3.forward;
        }

        // Calculate tangent vector (forward direction)
        Vector3 tangent = Vector3.Cross(surfaceNormal, referenceUp).normalized;
        return tangent;
    }

    // Alternative method that just aligns the up vector with the sphere normal
    public static Quaternion AlignToSphereSurface(Vector3 position, Vector3 sphereCenter)
    {
        Vector3 surfaceNormal = (position - sphereCenter).normalized;

        // Calculate forward direction that's tangent to the sphere
        Vector3 forward = CalculateForwardDirection(surfaceNormal);

        return Quaternion.LookRotation(forward, surfaceNormal);
    }
}

public class GoalManager : MonoBehaviour
{
    [Header("Phase Configuration")]
    public List<GoalPhaseData> smallPhases = new List<GoalPhaseData>();

    [Header("Characters")]
    public Transform wanderer;

    [Header("Ghost System")]
    public GameObject ghostPrefab; // Reference to the ghost prefab

    [Header("Projectile System")]
    public ProjectileSpawner projectileSpawner;

    [Header("Cylinder System")]
    public ColliderController_NewInput cylinderManager; // Reference to the cylinder manager

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float reachThreshold = 0.2f;

    [Header("Sphere Settings")]
    public Transform sphereCenter; // Reference to the sphere center
    public float sphereRadius = 10f; // Radius of the sphere

    [Header("Respawn Settings")]
    public int maxRespawnsPerBigPhase = 3;

    [Header("Gizmos Settings")]
    public bool showWaypointsGizmos = true;
    public bool showSphereGizmo = true;
    public float waypointSphereSize = 0.3f;

    // Internal tracking
    protected int currentSmallPhaseIndex = 0;
    public int CurrentSmallPhaseIndex => currentSmallPhaseIndex;
    protected int respawnCount = 0;
    protected bool sequenceRunning = false;

    protected Vector3 wandererStartPos;
    protected Vector3 bigPhaseStartWandererPos;
    protected Vector3 currentSmallPhaseStartPos; // Track start position of current small phase

    protected List<GameObject> activeProjectiles = new List<GameObject>();
    protected List<GameObject> wandererProjectiles = new List<GameObject>();

    // Track spawned ghosts and their projectiles for each small phase
    protected Dictionary<Transform, List<GameObject>> ghostProjectiles = new Dictionary<Transform, List<GameObject>>();
    public List<GameObject> spawnedGhosts = new List<GameObject>(); // Changed to public

    // NEW: Track trail renderers for ghosts
    protected Dictionary<Transform, TrailRenderer> ghostTrails = new Dictionary<Transform, TrailRenderer>();

    // Track waypoint triggers
    protected Dictionary<Collider, PhaseWaypoint> waypointTriggers = new Dictionary<Collider, PhaseWaypoint>();

    public int GetCurrentSmallPhaseIndex()
    {
        return currentSmallPhaseIndex;
    }

    protected virtual void Start()
    {
        if (wanderer == null)
        {
            Debug.LogError("GoalManager: Wanderer not assigned!");
            return;
        }

        // DEBUG: Check animator components
        Animator animator = wanderer.GetComponent<Animator>();
        WandererMovementAnimator movementAnimator = wanderer.GetComponent<WandererMovementAnimator>();

        Debug.Log($"🔍 Wanderer Animator Check:");
        Debug.Log($"   - Animator component: {animator != null}");
        Debug.Log($"   - WandererMovementAnimator: {movementAnimator != null}");

        if (animator != null)
        {
            Debug.Log($"   - Animator controller: {animator.runtimeAnimatorController != null}");
            Debug.Log($"   - Parameters count: {animator.parameterCount}");
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                Debug.Log($"     Parameter: {param.name} (Type: {param.type})");
            }
        }

        if (smallPhases.Count == 0)
        {
            Debug.LogError("GoalManager: No small phases configured!");
            return;
        }

        if (ghostPrefab == null)
        {
            Debug.LogError("GoalManager: Ghost prefab not assigned!");
            return;
        }

        // If no sphere center is assigned, use this transform
        if (sphereCenter == null)
        {
            sphereCenter = this.transform;
            Debug.LogWarning("GoalManager: No sphere center assigned, using GoalManager transform");
        }

        // Initialize waypoint triggers
        InitializeWaypointTriggers();

        wandererStartPos = wanderer.position;
        bigPhaseStartWandererPos = wanderer.position;
        currentSmallPhaseStartPos = wanderer.position;

        // Snap initial wanderer position to sphere surface
        SnapToSphereSurface(wanderer);

        StartCoroutine(RunSequence());
    }

    protected virtual void InitializeWaypointTriggers()
    {
        waypointTriggers.Clear();

        foreach (var phase in smallPhases)
        {
            if (phase == null) continue;

            // Legacy waypoints
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

            // Ghost waypoints
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

            // Wanderer waypoints
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

        // Add trigger collider if it doesn't exist
        Collider collider = wp.waypointTransform.GetComponent<Collider>();
        if (collider == null)
        {
            SphereCollider sphereCollider = wp.waypointTransform.gameObject.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 1.0f; // Adjust size as needed
            collider = sphereCollider;
        }
        else if (!collider.isTrigger)
        {
            collider.isTrigger = true;
        }

        // Add waypoint trigger component if it doesn't exist
        WaypointTrigger trigger = wp.waypointTransform.GetComponent<WaypointTrigger>();
        if (trigger == null)
        {
            trigger = wp.waypointTransform.gameObject.AddComponent<WaypointTrigger>();
            trigger.goalManager = this;
        }

        // Store the mapping
        waypointTriggers[collider] = wp;
    }

    // Method to check if any cylinders are in a waypoint trigger
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
                    // Check if cylinder is inside the waypoint trigger
                    if (waypointCollider.bounds.Contains(cylinder.transform.position))
                    {
                        return true;
                    }

                    // Additional check for sphere collider overlap
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

    // Method called when wanderer enters a waypoint trigger
    public void OnWandererEnterWaypoint(Collider waypointCollider)
    {
        if (!sequenceRunning) return;

        Debug.Log($"🎯 Wanderer entered waypoint - starting continuous cylinder monitoring");
    }

    // NEW METHOD: Called when wanderer is in waypoint without cylinders
    public void OnWandererInWaypointWithoutCylinders(Collider waypointCollider)
    {
        if (!sequenceRunning) return;

        Debug.Log($"💥 Wanderer in waypoint without cylinders - triggering immediate respawn!");
        ResetPhase();
    }

    protected virtual IEnumerator RunSequence()
    {
        sequenceRunning = true;

        while (currentSmallPhaseIndex < smallPhases.Count)
        {
            GoalPhaseData phase = smallPhases[currentSmallPhaseIndex];
            if (phase == null) { currentSmallPhaseIndex++; continue; }

            // Store the start position of this small phase BEFORE any movement
            currentSmallPhaseStartPos = wanderer.position;
            wandererStartPos = wanderer.position;

            if (currentSmallPhaseIndex % 5 == 0)
            {
                bigPhaseStartWandererPos = wanderer.position;
                Debug.Log($"🔁 Starting new big phase at position: {bigPhaseStartWandererPos}");
            }

            Debug.Log($"🔁 Starting small phase {currentSmallPhaseIndex + 1} at position: {currentSmallPhaseStartPos}");

            // Clear previous phase ghosts and projectiles
            DestroyAllGhosts();
            ghostProjectiles.Clear();
            ghostTrails.Clear(); // NEW: Clear trail references

            // -----------------------
            // SPAWN GHOSTS at wanderer's current position
            yield return StartCoroutine(SpawnGhostsForPhase(phase));

            // -----------------------
            // Move legacy main ghost waypoints first (optional)
            if (phase.waypoints != null && phase.waypoints.Count > 0)
            {
                foreach (var wp in phase.waypoints)
                {
                    // Snap waypoint to sphere surface
                    SnapWaypointToSphere(wp);
                    yield return StartCoroutine(MoveCharacterWithProjectiles(null, new List<PhaseWaypoint> { wp }, phase.pauseDuration));
                }
            }

            // -----------------------
            // Move all spawned ghosts per their own waypoints
            List<Coroutine> ghostCoroutines = new List<Coroutine>();
            if (phase.ghostsInPhase != null)
            {
                foreach (var ghostData in phase.ghostsInPhase)
                {
                    // Find the spawned ghost for this ghost data
                    Transform spawnedGhost = FindSpawnedGhost(ghostData);
                    if (spawnedGhost != null && ghostData.ghostWaypoints != null && ghostData.ghostWaypoints.Count > 0)
                    {
                        // Initialize projectile list for this ghost
                        if (!ghostProjectiles.ContainsKey(spawnedGhost))
                        {
                            ghostProjectiles[spawnedGhost] = new List<GameObject>();
                        }

                        // NEW: Enable trail if configured
                        if (ghostData.enableGoldTrail && ghostTrails.ContainsKey(spawnedGhost))
                        {
                            ghostTrails[spawnedGhost].emitting = true;
                            Debug.Log($"✨ Gold trail ENABLED for ghost");
                        }

                        // Snap all ghost waypoints to sphere surface
                        foreach (var wp in ghostData.ghostWaypoints)
                        {
                            SnapWaypointToSphere(wp);
                        }

                        ghostCoroutines.Add(StartCoroutine(
                            MoveGhostWithProjectiles(spawnedGhost, ghostData.ghostWaypoints, phase.pauseDuration)
                        ));
                    }
                    else
                    {
                        Debug.LogWarning($"❌ Could not find spawned ghost for ghost data");
                    }
                }
            }

            // Wait for all ghosts to complete their entire small phase movement
            foreach (var c in ghostCoroutines) yield return c;

            // NEW: Disable all trails after movement completes
            foreach (var trail in ghostTrails.Values)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                    Debug.Log("✨ Gold trail DISABLED - ghost stopped moving");
                }
            }

            // Destroy all ghost projectiles now that the small phase is complete
            DestroyAllGhostProjectiles();

            // -----------------------
            // Move wanderer with ONE projectile (no visual)
            if (phase.wandererWaypoints != null && phase.wandererWaypoints.Count > 0)
            {
                // Snap all wanderer waypoints to sphere surface
                foreach (var wp in phase.wandererWaypoints)
                {
                    SnapWaypointToSphere(wp);
                }

                yield return StartCoroutine(MoveWandererWithProjectiles(phase.wandererWaypoints, phase.pauseDuration));
            }

            // Destroy ghosts at the end of the small phase
            DestroyAllGhosts();

            Debug.Log($"✅ Small phase {currentSmallPhaseIndex + 1} completed - camera should move now");

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

    // Method to snap a transform to the sphere surface
    protected void SnapToSphereSurface(Transform target)
    {
        if (sphereCenter == null) return;

        Vector3 direction = (target.position - sphereCenter.position).normalized;
        target.position = sphereCenter.position + direction * sphereRadius;
        target.rotation = SphereMover.AlignToSphereSurface(target.position, sphereCenter.position);
    }

    // Method to snap a waypoint to the sphere surface
    protected void SnapWaypointToSphere(PhaseWaypoint wp)
    {
        if (wp?.waypointTransform == null || sphereCenter == null) return;

        Vector3 direction = (wp.waypointTransform.position - sphereCenter.position).normalized;
        wp.waypointTransform.position = sphereCenter.position + direction * sphereRadius;
    }

    protected virtual IEnumerator SpawnGhostsForPhase(GoalPhaseData phase)
    {
        if (ghostPrefab == null)
        {
            Debug.LogError("❌ Ghost prefab is null!");
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
            // Always spawn a new ghost - don't rely on existing ghostTransform
            Vector3 spawnPosition = wanderer.position;
            GameObject ghost = Instantiate(ghostPrefab, spawnPosition, Quaternion.identity);

            if (ghost == null)
            {
                Debug.LogError("❌ Failed to instantiate ghost!");
                continue;
            }

            // Snap ghost to sphere surface
            SnapToSphereSurface(ghost.transform);

            spawnedGhosts.Add(ghost);
            spawnedCount++;

            // NEW: Add trail renderer if gold trail is enabled
            if (ghostData.enableGoldTrail)
            {
                TrailRenderer trail = ghost.AddComponent<TrailRenderer>();
                SetupGoldTrail(trail, ghostData);
                ghostTrails[ghost.transform] = trail;
                Debug.Log($"✨ Gold trail ADDED to ghost '{ghost.name}'");
            }

            // Update the ghostData with the new ghost transform
            ghostData.ghostTransform = ghost.transform;

            Debug.Log($"👻 Spawned ghost '{ghost.name}' at position: {spawnPosition}");
        }

        // Small delay to ensure ghosts are properly spawned
        yield return new WaitForSeconds(0.1f);

        Debug.Log($"✅ Successfully spawned {spawnedCount} ghosts for phase");
    }

    // NEW: Method to set up the gold trail renderer
    protected virtual void SetupGoldTrail(TrailRenderer trail, GhostPhaseData ghostData)
    {
        if (trail == null) return;

        trail.time = 0.5f; // How long the trail lasts
        trail.startWidth = ghostData.trailWidth;
        trail.endWidth = 0f;
        trail.material = CreateTrailMaterial(ghostData.trailColor);
        trail.colorGradient = CreateGoldGradient(ghostData.trailColor);
        trail.emitting = false; // Start with trail disabled
        trail.minVertexDistance = 0.1f;

        // Optional: Add a simple material if none exists
        if (trail.material == null)
        {
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = ghostData.trailColor;
        }
    }

    // NEW: Create a simple material for the trail
    protected virtual Material CreateTrailMaterial(Color trailColor)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = trailColor;
        return mat;
    }

    // NEW: Create a gold color gradient for the trail
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
        // Return the ghost transform from the ghost data (which we just updated in SpawnGhostsForPhase)
        return ghostData.ghostTransform;
    }

    protected virtual void DestroyAllGhosts()
    {
        if (spawnedGhosts == null)
        {
            Debug.LogWarning("spawnedGhosts list is null!");
            return;
        }

        // Clean up destroyed ghosts from the list first
        int initialCount = spawnedGhosts.Count;
        spawnedGhosts.RemoveAll(ghost => ghost == null);
        int removedNulls = initialCount - spawnedGhosts.Count;

        if (removedNulls > 0)
        {
            Debug.Log($"🧹 Cleaned up {removedNulls} null ghosts from list");
        }

        foreach (GameObject ghost in spawnedGhosts)
        {
            if (ghost != null)
            {
                // Stop animation before destroying
                GhostMovementAnimator ghostAnimator = ghost.GetComponent<GhostMovementAnimator>();

                if (ghostAnimator != null)
                {
                    ghostAnimator.StopMoving();
                }

                Destroy(ghost);
                Debug.Log("👻 Destroyed ghost");
            }
        }
        spawnedGhosts.Clear();
        ghostTrails.Clear(); // NEW: Clear trail references

        Debug.Log($"✅ Destroyed all ghosts. List count: {spawnedGhosts.Count}");
    }


    protected virtual IEnumerator MoveGhostWithProjectiles(Transform ghost, List<PhaseWaypoint> waypoints, float pauseDuration)
{
        // Get the ghost's animator component
        GhostMovementAnimator ghostAnimator = ghost.GetComponent<GhostMovementAnimator>();

        if (ghostAnimator != null)
    {
        ghostAnimator.StartMoving();
        Debug.Log($"👻 Ghost started moving with jump animation");
    }

    // NEW: Enable trail at start of movement if configured
    bool hasTrail = ghostTrails.ContainsKey(ghost) && ghostTrails[ghost] != null;
    if (hasTrail)
    {
        ghostTrails[ghost].emitting = true;
        ghostTrails[ghost].Clear(); // Clear any existing trail
        Debug.Log($"✨ Gold trail STARTED for ghost");
    }

    // Move through all waypoints in the small phase
    foreach (PhaseWaypoint wp in waypoints)
    {
        if (wp.waypointTransform == null) continue;

        Vector3 target = wp.waypointTransform.position;
        bool projectileFired = false;

        while (ghost != null && Vector3.Distance(ghost.position, target) > reachThreshold)
        {
            Vector3 moveDirection = (target - ghost.position).normalized;
            
            // Use sphere movement with rotation for ghosts
            if (sphereCenter != null)
            {
                var (newPosition, newRotation) = SphereMover.MoveOnSphere(
                    ghost.position, target, sphereCenter.position, sphereRadius, moveSpeed * Time.deltaTime);
                ghost.position = newPosition;
                
                // FORCE facing the tangent direction of movement on sphere
                if (moveDirection != Vector3.zero)
                {
                    // Calculate tangent direction for sphere movement
                    Vector3 radialDirection = (ghost.position - sphereCenter.position).normalized;
                    Vector3 tangentDirection = Vector3.Cross(radialDirection, moveDirection).normalized;
                    tangentDirection = Vector3.Cross(tangentDirection, radialDirection).normalized;
                    
                    if (tangentDirection != Vector3.zero)
                    {
                        ghost.rotation = Quaternion.LookRotation(tangentDirection, radialDirection);
                    }
                }
            }
            else
            {
                // Fallback to straight line movement
                ghost.position = Vector3.MoveTowards(ghost.position, target, moveSpeed * Time.deltaTime);
                
                // Face the movement direction
                if (moveDirection != Vector3.zero)
                {
                    ghost.rotation = Quaternion.LookRotation(moveDirection);
                }
            }

            if (wp.triggerProjectile && !projectileFired)
            {
                float remainingDistance = Vector3.Distance(ghost.position, target);
                float leadDistance = moveSpeed * wp.leadTime;

                if (remainingDistance <= leadDistance)
                {
                    // Calculate exact speed for projectile to arrive at same time as ghost
                    float projectileDistance = Vector3.Distance(
                        (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
                            ? wp.customSpawnTransforms[0].position
                            : projectileSpawner.transform.position,
                        target
                    );

                    float timeToArrival = remainingDistance / moveSpeed;
                    float requiredProjectileSpeed = projectileDistance / timeToArrival;

                    // Fire ghost projectile (does NOT destroy on collision, WITH visual)
                    FireGhostProjectiles(ghost, wp, requiredProjectileSpeed);
                    projectileFired = true;
                }
            }

            yield return null;
        }

        if (ghost != null)
        {
            // Ensure final position and rotation are correct
            if (sphereCenter != null)
            {
                SnapToSphereSurface(ghost);
            }
            else
            {
                ghost.position = target;
            }
        }

        yield return new WaitForSeconds(pauseDuration);
    }

    // Stop the ghost's jump animation
    if (ghostAnimator != null)
    {
        ghostAnimator.StopMoving();
        Debug.Log($"👻 Ghost stopped moving");
    }

    // NEW: Disable trail at end of movement
    if (hasTrail)
    {
        ghostTrails[ghost].emitting = false;
        Debug.Log($"✨ Gold trail STOPPED for ghost");
    }

    // Ghost has completed all waypoints in this small phase
    // Projectiles will be destroyed after ALL ghosts finish (in RunSequence)
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
                // Use sphere movement with rotation
                if (sphereCenter != null)
                {
                    var (newPosition, newRotation) = SphereMover.MoveOnSphere(
                        character.position, target, sphereCenter.position, sphereRadius, moveSpeed * Time.deltaTime);
                    character.position = newPosition;
                    character.rotation = newRotation;
                }
                else
                {
                    // Fallback to straight line movement
                    character.position = Vector3.MoveTowards(character.position, target, moveSpeed * Time.deltaTime);
                }

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
                // Ensure final position and rotation are correct
                if (sphereCenter != null)
                {
                    SnapToSphereSurface(character);
                }
                else
                {
                    character.position = target;
                }
            }

            yield return new WaitForSeconds(pauseDuration);
        }
    }

    protected virtual IEnumerator MoveWandererWithProjectiles(List<PhaseWaypoint> waypoints, float pauseDuration)
    {
        Debug.Log($"🎬 Starting wanderer movement with {waypoints?.Count} waypoints");

        // START MOVING - Trigger jump animation
        WandererMovementAnimator movementAnimator = wanderer.GetComponent<WandererMovementAnimator>();
        if (movementAnimator != null)
        {
            movementAnimator.StartMoving();
        }
        else
        {
            Debug.LogError("❌ No WandererMovementAnimator found on wanderer!");
        }
        // Clear all existing projectile trails when wanderer starts moving
        ClearAllProjectileTrails();

        foreach (PhaseWaypoint wp in waypoints)
        {
            if (wp.waypointTransform == null) continue;

            Vector3 target = wp.waypointTransform.position;
            bool wandererProjectileFired = false;

            while (wanderer != null && Vector3.Distance(wanderer.position, target) > reachThreshold)
            {
                // Use sphere movement with rotation for wanderer
                Vector3 moveDirection = (target - wanderer.position).normalized;

                // Use sphere movement with rotation for wanderer
                if (sphereCenter != null)
                {
                    var (newPosition, newRotation) = SphereMover.MoveOnSphere(
                        wanderer.position, target, sphereCenter.position, sphereRadius, moveSpeed * Time.deltaTime);
                    wanderer.position = newPosition;

                    // Force facing the tangent direction of movement on sphere
                    if (moveDirection != Vector3.zero)
                    {
                        // Calculate tangent direction for sphere movement
                        Vector3 radialDirection = (wanderer.position - sphereCenter.position).normalized;
                        Vector3 tangentDirection = Vector3.Cross(radialDirection, moveDirection).normalized;
                        tangentDirection = Vector3.Cross(tangentDirection, radialDirection).normalized;

                        if (tangentDirection != Vector3.zero)
                        {
                            wanderer.rotation = Quaternion.LookRotation(tangentDirection, radialDirection);
                        }
                    }
                }
                else
                {
                    // Fallback to straight line movement
                    wanderer.position = Vector3.MoveTowards(wanderer.position, target, moveSpeed * Time.deltaTime);

                    // Face the movement direction
                    if (moveDirection != Vector3.zero)
                    {
                        wanderer.rotation = Quaternion.LookRotation(moveDirection);
                    }
                }


                // Fire ONE wanderer projectile (no visual, destroys on collision)
                if (wp.triggerProjectile && !wandererProjectileFired)
                {
                    float remainingDistance = Vector3.Distance(wanderer.position, target);
                    float leadDistance = moveSpeed * wp.leadTime;

                    if (remainingDistance <= leadDistance)
                    {
                        // Fire wanderer's projectile (NO visual, destroys on collision)
                        float projectileDistance = Vector3.Distance(
                            (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
                                ? wp.customSpawnTransforms[0].position
                                : projectileSpawner.transform.position,
                            target
                        );

                        float timeToArrival = remainingDistance / moveSpeed;
                        float requiredProjectileSpeed = projectileDistance / timeToArrival;

                        FireWandererProjectiles(wp, requiredProjectileSpeed);
                        wandererProjectileFired = true;
                        Debug.Log("🎯 Wanderer projectile fired (no visual)");
                    }
                }

                yield return null;
            }

            if (wanderer != null)
            {
                // Ensure final position and rotation are correct
                if (sphereCenter != null)
                {
                    SnapToSphereSurface(wanderer);
                }
                else
                {
                    wanderer.position = target;
                }
            }

            yield return new WaitForSeconds(pauseDuration);
        }

        // STOP MOVING - Return to idle
        if (movementAnimator != null)
        {
            movementAnimator.StopMoving();
        }

        Debug.Log("🎬 Wanderer movement completed");
    }

    protected void FireProjectiles(Transform character, PhaseWaypoint wp, float customSpeed = -1f)
    {
        if (projectileSpawner == null)
        {
            Debug.LogWarning("GoalManager: ProjectileSpawner not assigned!");
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
                    projectile.SetTarget(spawn.position, wp.waypointTransform.position, speedToUse, false);
                }
                activeProjectiles.Add(proj);
            }
        }
    }

    protected void FireGhostProjectiles(Transform ghost, PhaseWaypoint wp, float customSpeed = -1f)
    {
        if (projectileSpawner == null)
        {
            Debug.LogWarning("GoalManager: ProjectileSpawner not assigned!");
            return;
        }

        List<Transform> spawnTransforms = (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
            ? wp.customSpawnTransforms
            : new List<Transform> { projectileSpawner.transform };

        foreach (var spawn in spawnTransforms)
        {
            if (spawn == null) continue;

            float speedToUse = customSpeed > 0f ? customSpeed : projectileSpawner.projectileSpeed;
            GameObject proj = projectileSpawner.SpawnOne(spawn.position, wp.waypointTransform.position, false); // false = don't destroy on collision
            if (proj != null)
            {
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    // Ghost projectiles have visual trails enabled
                    projectile.showTrajectory = true;
                    projectile.SetTarget(spawn.position, wp.waypointTransform.position, speedToUse, false);
                }

                // Add to both general list and ghost-specific list
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
            Debug.LogWarning("GoalManager: ProjectileSpawner not assigned!");
            return;
        }

        List<Transform> spawnTransforms = (wp.customSpawnTransforms != null && wp.customSpawnTransforms.Count > 0)
            ? wp.customSpawnTransforms
            : new List<Transform> { projectileSpawner.transform };

        Debug.Log($"🎯 Firing WANDERER projectile - should destroy on collision!");

        foreach (var spawn in spawnTransforms)
        {
            if (spawn == null) continue;

            float speedToUse = customSpeed > 0f ? customSpeed : projectileSpawner.projectileSpeed;

            // Spawn wanderer projectile with NO visual and DESTROY ON COLLISION
            GameObject proj = projectileSpawner.SpawnOne(spawn.position, wp.waypointTransform.position, true); // true = destroy on collision
            if (proj != null)
            {
                Projectile projectile = proj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    // Disable trail visual for wanderer projectiles
                    projectile.showTrajectory = false;
                    // Set to DESTROY on collision
                    projectile.SetTarget(spawn.position, wp.waypointTransform.position, speedToUse, true);
                    Debug.Log($"🎯 Wanderer projectile created - destroyOnCollision: {projectile.destroyOnCollision}");
                }
                wandererProjectiles.Add(proj);
            }
            else
            {
                Debug.LogError("🎯 FAILED to spawn wanderer projectile!");
            }
        }
    }

    protected void DestroyAllGhostProjectiles()
    {
        // Destroy all projectiles from all ghosts in this small phase
        foreach (var ghostProjList in ghostProjectiles.Values)
        {
            // Clean up destroyed projectiles from the list first
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

        // Also clean up the general activeProjectiles list
        activeProjectiles.RemoveAll(proj => proj == null);

        Debug.Log("🧹 All ghost projectiles destroyed - small phase complete");
    }

    protected void ClearProjectiles()
    {
        // Clean up destroyed projectiles from the lists first
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

    // Add this method to clear projectile trails
    public void ClearAllProjectileTrails()
    {
        // Clean up destroyed projectiles from the lists first
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
        Debug.Log("🔄 ResetPhase called!");

        if (!sequenceRunning)
        {
            Debug.Log("❌ Sequence not running, cannot reset");
            return;
        }

        // Reset wanderer animations
        if (wanderer != null)
        {
            WandererMovementAnimator movementAnimator = wanderer.GetComponent<WandererMovementAnimator>();
            if (movementAnimator != null)
            {
                movementAnimator.ResetAnimator();
            }
        }

        respawnCount++;
        Debug.Log($"💥 Wanderer hit — respawn count: {respawnCount}");
        Debug.Log($"📍 Current small phase start position: {currentSmallPhaseStartPos}");

        // Clear trails when resetting phase
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
            Debug.Log($"🔄 Resetting to current small phase start (respawn {respawnCount}/{maxRespawnsPerBigPhase})");

            // Reset to CURRENT SMALL PHASE START position, NOT waypoint position
            wanderer.position = currentSmallPhaseStartPos;
            // Ensure wanderer is on sphere surface
            SnapToSphereSurface(wanderer);
            Debug.Log($"📍 Wanderer reset to small phase start position: {wanderer.position}");
        }
        else
        {
            // Reset to big phase start
            int currentBigPhaseIndex = currentSmallPhaseIndex / 5;
            currentSmallPhaseIndex = currentBigPhaseIndex * 5;
            respawnCount = 0;

            wanderer.position = bigPhaseStartWandererPos;
            // Ensure wanderer is on sphere surface
            SnapToSphereSurface(wanderer);
            Debug.Log($"📍 Wanderer reset to big phase start: {wanderer.position}");

            // Ghosts will be respawned automatically in RunSequence
            Debug.Log($"🔁 Respawn limit reached — restarting big phase {currentBigPhaseIndex + 1}");
        }

        StopAllCoroutines();
        StartCoroutine(RunSequence());

        Debug.Log("✅ ResetPhase completed successfully");
    }

    public bool IsSequenceFinished()
    {
        return !sequenceRunning && currentSmallPhaseIndex >= smallPhases.Count;
    }

    // Update method to clean up destroyed projectiles
    protected virtual void Update()
    {
        // Clean up any projectiles that were destroyed elsewhere
        activeProjectiles.RemoveAll(proj => proj == null);
        wandererProjectiles.RemoveAll(proj => proj == null);

        foreach (var ghostProjList in ghostProjectiles.Values)
        {
            ghostProjList.RemoveAll(proj => proj == null);
        }

        // Clean up destroyed ghosts
        spawnedGhosts.RemoveAll(ghost => ghost == null);
    }

    private void OnDrawGizmos()
    {
        if (showSphereGizmo && sphereCenter != null)
        {
            // Draw sphere wireframe
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(sphereCenter.position, sphereRadius);
        }

        if (!showWaypointsGizmos || smallPhases == null) return;

        foreach (var phase in smallPhases)
        {
            if (phase == null) continue;

            // Legacy waypoints
            if (phase.waypoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var wp in phase.waypoints)
                {
                    if (wp?.waypointTransform != null)
                    {
                        // Draw waypoint sphere
                        Gizmos.DrawSphere(wp.waypointTransform.position, waypointSphereSize);

                        // Draw line to sphere center
                        if (sphereCenter != null)
                        {
                            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                            Gizmos.DrawLine(wp.waypointTransform.position, sphereCenter.position);
                            Gizmos.color = Color.green;
                        }
                    }
                }
            }

            // Ghosts
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
                            // Draw waypoint sphere
                            Gizmos.DrawSphere(wp.waypointTransform.position, waypointSphereSize);

                            // Draw line to sphere center
                            if (sphereCenter != null)
                            {
                                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                                Gizmos.DrawLine(wp.waypointTransform.position, sphereCenter.position);
                                Gizmos.color = Color.cyan;
                            }
                        }
                    }
                }
            }

            // Wanderer
            if (phase.wandererWaypoints != null)
            {
                Gizmos.color = Color.magenta;
                foreach (var wp in phase.wandererWaypoints)
                {
                    if (wp?.waypointTransform != null)
                    {
                        // Draw waypoint sphere
                        Gizmos.DrawSphere(wp.waypointTransform.position, waypointSphereSize);

                        // Draw line to sphere center
                        if (sphereCenter != null)
                        {
                            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
                            Gizmos.DrawLine(wp.waypointTransform.position, sphereCenter.position);
                            Gizmos.color = Color.magenta;
                        }
                    }
                }
            }

            // Projectile arc visualization (spawn → target)
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
                            midpoint.y += Mathf.Sin(t * Mathf.PI) * 2.0f; // parabolic arc
                            Gizmos.DrawLine(previous, midpoint);
                            previous = midpoint;
                        }
                    }
                }
            }

            // Also draw projectile arcs for ghost waypoints
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
                                midpoint.y += Mathf.Sin(t * Mathf.PI) * 1.5f; // slightly smaller arc
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