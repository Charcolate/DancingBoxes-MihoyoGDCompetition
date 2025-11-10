using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    [Header("Collision Settings")]
    public bool destroyOnCollision = false;

    [Header("Visual Settings")]
    public bool showTrajectory = true; // Control whether to show trail

    [Header("Trail Settings")]
    public float trailWidth = 0.3f;
    public float trailTime = 60f;
    public Material trailMaterial;

    [Header("Cylinder Reflection")]
    public bool canReflectOffCylinders = true;
    public float reflectionSpeedMultiplier = 1f;

    [Header("Target Height Offset")]
    public float targetHeightOffset = 1.0f; // How much higher the destination should be

    private TrailRenderer trailRenderer;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float moveSpeed;
    private bool isMoving = false;
    private bool hasReachedDestination = false;
    private Collider projectileCollider;
    private Vector3 currentDirection;
    private GoalManager goalManager;
    private bool shouldDestroyAfterGhostMovement = false;
    private Collider collisionCollider;
    private Transform actualTargetTransform; // Store the actual target transform

    void Awake()
    {
        // Get or add collider
        projectileCollider = GetComponent<Collider>();
        if (projectileCollider == null)
        {
            projectileCollider = gameObject.AddComponent<SphereCollider>();
            Debug.Log("Added SphereCollider to projectile");
        }

        // Make sure collider is NOT a trigger so it can physically interact with cylinders
        projectileCollider.isTrigger = false;

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false; // We need physics for reflection
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Debug.Log("Added Rigidbody to projectile");
        }

        // Set the projectile tag
        gameObject.tag = "Projectile";

        // Find GoalManager reference
        goalManager = FindObjectOfType<GoalManager>();

        // Only create TrailRenderer if we want to show trajectory
        if (showTrajectory)
        {
            // Add TrailRenderer component
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.widthMultiplier = trailWidth;
            trailRenderer.time = trailTime;
            trailRenderer.minVertexDistance = 0.01f;
            trailRenderer.autodestruct = false;
            trailRenderer.emitting = true;

            // Set trail material
            if (trailMaterial != null)
            {
                trailRenderer.material = trailMaterial;
            }
            else
            {
                trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }

            // Trail colors with better visibility
            trailRenderer.startColor = new Color(0f, 1f, 1f, 1f);
            trailRenderer.endColor = new Color(0f, 0.5f, 1f, 0.8f);

            // Optimizations
            trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;
            trailRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }
    }

    public void SetTarget(Vector3 start, Vector3 target, float speed, bool shouldDestroyOnCollision = false)
    {
        startPos = start;
        targetPos = target;
        moveSpeed = speed;
        destroyOnCollision = shouldDestroyOnCollision;
        transform.position = startPos;
        isMoving = true;
        hasReachedDestination = false;

        // Calculate initial direction
        currentDirection = (target - start).normalized;

        // Clear any existing trail and ensure emitting
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }
    }

    // NEW OVERLOADED METHOD: Accepts a transform and applies height offset
    public void SetTarget(Vector3 start, Transform targetTransform, float speed, bool shouldDestroyOnCollision = false)
    {
        startPos = start;
        actualTargetTransform = targetTransform;

        // Calculate target position with height offset
        Vector3 baseTargetPos = targetTransform.position;
        targetPos = new Vector3(baseTargetPos.x, baseTargetPos.y + targetHeightOffset, baseTargetPos.z);

        moveSpeed = speed;
        destroyOnCollision = shouldDestroyOnCollision;
        transform.position = startPos;
        isMoving = true;
        hasReachedDestination = false;

        // Calculate initial direction
        currentDirection = (targetPos - start).normalized;

        // Clear any existing trail and ensure emitting
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = true;
        }

        Debug.Log($"Projectile target: {baseTargetPos} -> {targetPos} (height offset: {targetHeightOffset})");
    }

    void Update()
    {
        if (!isMoving && hasReachedDestination)
        {
            // Check if we should destroy after ghost movement is complete
            if (shouldDestroyAfterGhostMovement && !IsInGhostPhase())
            {
                Debug.Log("Ghost movement finished - destroying projectile");
                Destroy(gameObject);
            }
            return;
        }

        if (isMoving)
        {
            // Use physics-based movement for reflection capability
            // We'll handle movement in FixedUpdate for physics consistency
        }
    }

    void FixedUpdate()
    {
        if (isMoving && !hasReachedDestination)
        {
            // Move using physics for consistent collision detection
            GetComponent<Rigidbody>().linearVelocity = currentDirection * moveSpeed;
        }
        else
        {
            // Stop physics movement
            GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collider we hit is NOT a trigger
        if (!collision.collider.isTrigger)
        {
            HandleCollision(collision.gameObject, collision.contacts[0].normal);
        }
        else
        {
            Debug.Log($"Projectile passed through trigger collider: {collision.gameObject.name}");
        }
    }

    private void HandleCollision(GameObject collidedObject, Vector3 collisionNormal)
    {
        Debug.Log($" Projectile collided with non-trigger: {collidedObject.name}");

        // Stop movement immediately for ALL non-trigger collisions
        isMoving = false;
        hasReachedDestination = true;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        // Check object types
        bool isGhost = collidedObject.CompareTag("Ghost");
        bool isWanderer = collidedObject.CompareTag("Player") || collidedObject.CompareTag("Wanderer");
        bool isCylinder = collidedObject.CompareTag("Cylinder") ||
                         collidedObject.name.ToLower().Contains("cylinder") ||
                         IsCylinderObject(collidedObject);

        // Handle cylinder reflection during wanderer phase
        if (isCylinder && canReflectOffCylinders && !IsInGhostPhase())
        {
            Debug.Log($"Projectile reflected off cylinder during wanderer phase: {collidedObject.name}");

            // Calculate reflection direction
            Vector3 reflectDirection = Vector3.Reflect(currentDirection, collisionNormal).normalized;
            currentDirection = reflectDirection;

            // Apply speed multiplier
            moveSpeed *= reflectionSpeedMultiplier;

            // Resume movement with new direction
            isMoving = true;
            hasReachedDestination = false;

            Debug.Log($"Reflection: {currentDirection} at speed {moveSpeed}");
            return;
        }

        // GHOST PHASE: Wait for ghost to finish moving before destroying
        if (IsInGhostPhase())
        {
            Debug.Log($"Ghost phase - Projectile stopped, waiting for ghost movement to finish");

            if (destroyOnCollision)
            {
                // Mark for destruction after ghost phase ends
                shouldDestroyAfterGhostMovement = true;
                collisionCollider = collidedObject.GetComponent<Collider>();
                Debug.Log("Projectile marked for destruction after ghost movement");
            }
            return;
        }

        // WANDERER PHASE: Immediate destruction logic
        if (destroyOnCollision)
        {
            Debug.Log("Wanderer projectile hit target - destroying immediately");
            Destroy(gameObject);
        }
        else
        {
            // For ghost projectiles during wanderer phase, just stop moving
            Debug.Log("Ghost projectile hit target - stopping movement");
        }
    }

    private bool IsCylinderObject(GameObject obj)
    {
        // Check if this object is a cylinder by checking if it's in the cylinder manager
        if (goalManager != null && goalManager.cylinderManager != null && goalManager.cylinderManager.cylinders != null)
        {
            foreach (var cylinder in goalManager.cylinderManager.cylinders)
            {
                if (cylinder != null && cylinder == obj)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsInGhostPhase()
    {
        // Find all ghost objects in the scene by tag
        GameObject[] ghostsInScene = GameObject.FindGameObjectsWithTag("Ghost");

        // If there are any active ghost objects with the "Ghost" tag, we're in a ghost phase
        if (ghostsInScene != null && ghostsInScene.Length > 0)
        {
            foreach (var ghost in ghostsInScene)
            {
                if (ghost != null && ghost.activeInHierarchy)
                {
                    // Additionally check if the ghost is currently moving
                    // You might want to add a more sophisticated check here based on your ghost movement system
                    return true;
                }
            }
        }

        return false;
    }

    public void ClearTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
            trailRenderer.emitting = false;
        }
    }

    public void DestroyImmediately()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Clean up any trail renderer
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }
    }
}