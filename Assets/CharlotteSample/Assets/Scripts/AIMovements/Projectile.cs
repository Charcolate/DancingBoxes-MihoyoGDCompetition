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

    private TrailRenderer trailRenderer;
    private Vector3 startPos;
    private Vector3 targetPos;
    private float moveSpeed;
    private bool isMoving = false;
    private bool hasReachedDestination = false;
    private Collider projectileCollider;
    private Vector3 currentDirection;
    private GoalManager goalManager;

    void Awake()
    {
        // Get or add collider
        projectileCollider = GetComponent<Collider>();
        if (projectileCollider == null)
        {
            projectileCollider = gameObject.AddComponent<SphereCollider>();
            Debug.Log("🔵 Added SphereCollider to projectile");
        }

        // Make sure collider is NOT a trigger so it can physically interact with cylinders
        projectileCollider.isTrigger = false;

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false; // We need physics for reflection
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Debug.Log("🔵 Added Rigidbody to projectile");
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

    void Update()
    {
        if (!isMoving && hasReachedDestination)
        {
            // Don't destroy automatically - wait for external command
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
        HandleCollision(collision.gameObject, collision.contacts[0].normal);
    }

    private void HandleCollision(GameObject collidedObject, Vector3 collisionNormal)
    {
        // Check if it's a ghost or wanderer
        bool isGhost = collidedObject.CompareTag("Ghost");
        bool isWanderer = collidedObject.CompareTag("Player") || collidedObject.CompareTag("Wanderer");
        bool isCylinder = collidedObject.CompareTag("Cylinder") ||
                         collidedObject.name.ToLower().Contains("cylinder") ||
                         IsCylinderObject(collidedObject);

        // GHOST PHASE: Stop on any collision (detected by looking for ghost objects in scene)
        if (IsInGhostPhase())
        {
            Debug.Log($"👻 Ghost phase - Projectile stopped by: {collidedObject.name}");

            // Stop movement immediately
            isMoving = false;
            hasReachedDestination = true;
            GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

            // Only destroy if this is a wanderer projectile hitting a valid target
            if (destroyOnCollision && (isGhost || isWanderer))
            {
                Debug.Log("💥 Wanderer projectile hit target during ghost phase - destroying");
                Destroy(gameObject);
            }
            return;
        }

        // WANDERER PHASE: Reflection logic for cylinders
        if (isCylinder && canReflectOffCylinders)
        {
            Debug.Log($"🛡️ Projectile reflected off cylinder during wanderer phase: {collidedObject.name}");

            // Calculate reflection direction
            Vector3 reflectDirection = Vector3.Reflect(currentDirection, collisionNormal).normalized;
            currentDirection = reflectDirection;

            // Apply speed multiplier
            moveSpeed *= reflectionSpeedMultiplier;

            // Update target position for continuous movement
            targetPos = transform.position + reflectDirection * 100f;

            Debug.Log($"   Reflection: {currentDirection} at speed {moveSpeed}");
            return;
        }

        // Only react to ghosts or wanderer during wanderer phase
        if (!isGhost && !isWanderer)
        {
            Debug.Log($"🚫 Projectile ignored collision with: {collidedObject.name} during wanderer phase");
            return;
        }

        Debug.Log($"💥 Projectile collided with: {collidedObject.name} during wanderer phase");

        // Stop movement immediately
        isMoving = false;
        hasReachedDestination = true;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        // Only destroy if this is a wanderer projectile
        if (destroyOnCollision)
        {
            Debug.Log("💥 Wanderer projectile hit target - destroying");
            Destroy(gameObject);
        }
        else
        {
            // For ghost projectiles, just stop moving but don't destroy
            Debug.Log("👻 Ghost projectile hit target - stopping movement");
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