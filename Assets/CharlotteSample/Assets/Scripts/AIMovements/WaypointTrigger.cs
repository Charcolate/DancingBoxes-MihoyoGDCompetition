using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointTrigger : MonoBehaviour
{
    [Header("Goal Manager Reference (Sphere Movement)")]
    public GoalManager goalManager;

    [Header("Goals Reference (Flat Plane Movement)")]
    public Goals goals;

    // Track objects currently in the trigger
    private HashSet<GameObject> cylindersInTrigger = new HashSet<GameObject>();
    private bool wandererIsInTrigger = false;
    private bool hasTriggeredRespawn = false;

    // Helper property to get the active goal system
    private MonoBehaviour ActiveGoalSystem
    {
        get
        {
            if (goalManager != null) return goalManager;
            if (goals != null) return goals;
            return null;
        }
    }

    // Helper property to get the wanderer transform
    private Transform Wanderer
    {
        get
        {
            if (goalManager != null && goalManager.wanderer != null) return goalManager.wanderer;
            if (goals != null && goals.wanderer != null) return goals.wanderer;
            return null;
        }
    }

    // Helper property to get the cylinder manager
    private ColliderController_NewInput CylinderManager
    {
        get
        {
            if (goalManager != null) return goalManager.cylinderManager;
            if (goals != null) return goals.cylinderManager;
            return null;
        }
    }

    private void Start()
    {
        Debug.Log($"🔧 WaypointTrigger started: {gameObject.name} at position {transform.position}");

        // Log which goal system is active
        if (goalManager != null)
            Debug.Log($"🎯 Connected to GoalManager (Sphere Movement)");
        else if (goals != null)
            Debug.Log($"🎯 Connected to Goals (Flat Plane Movement)");
        else
            Debug.LogError($"❌ WaypointTrigger not connected to any goal system!");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Notify the appropriate goal system
        if (other.CompareTag("Wanderer"))
        {
            if (goalManager != null)
                goalManager.OnWandererEnterWaypoint(GetComponent<Collider>());
            else if (goals != null)
                goals.OnWandererEnterWaypoint(GetComponent<Collider>());
        }

        if (ActiveGoalSystem == null) return;

        // Check if the wanderer entered the trigger
        Transform currentWanderer = Wanderer;
        if (other.CompareTag("Player") || (currentWanderer != null && other.transform == currentWanderer))
        {
            Debug.Log($"🎯 Wanderer entered waypoint trigger: {gameObject.name}");
            wandererIsInTrigger = true;
            hasTriggeredRespawn = false;

            // Immediately check for any cylinders already in the trigger
            CheckForExistingCylinders();
            CheckCylinderPresence();
        }
        // Check if a cylinder entered the trigger
        else if (IsCylinderObject(other.gameObject))
        {
            Debug.Log($"🔵 Cylinder '{other.gameObject.name}' entered waypoint trigger: {gameObject.name}");
            cylindersInTrigger.Add(other.gameObject);
            Debug.Log($"   Cylinders in trigger now: {cylindersInTrigger.Count}");
            CheckCylinderPresence();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (ActiveGoalSystem == null) return;

        // Check if the wanderer left the trigger
        Transform currentWanderer = Wanderer;
        if (other.CompareTag("Player") || (currentWanderer != null && other.transform == currentWanderer))
        {
            Debug.Log($"🎯 Wanderer left waypoint trigger: {gameObject.name}");
            wandererIsInTrigger = false;
            hasTriggeredRespawn = false;
        }
        // Check if a cylinder left the trigger
        else if (IsCylinderObject(other.gameObject))
        {
            Debug.Log($"🔵 Cylinder '{other.gameObject.name}' left waypoint trigger: {gameObject.name}");
            cylindersInTrigger.Remove(other.gameObject);
            Debug.Log($"   Cylinders in trigger now: {cylindersInTrigger.Count}");
            CheckCylinderPresence();
        }
    }

    private void Update()
    {
        // Continuous check while wanderer is in trigger and hasn't respawned yet
        if (wandererIsInTrigger && !hasTriggeredRespawn)
        {
            // MANUALLY check if cylinders are still in the trigger every frame
            ManualCylinderCheck();
            CheckCylinderPresence();
        }
    }

    private void ManualCylinderCheck()
    {
        // This manually checks if cylinders that we think are in the trigger are actually still there
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null) return;

        List<GameObject> cylindersToRemove = new List<GameObject>();

        foreach (var cylinder in cylindersInTrigger)
        {
            if (cylinder == null)
            {
                cylindersToRemove.Add(cylinder);
                continue;
            }

            // Check if cylinder is actually still within the trigger bounds
            if (!triggerCollider.bounds.Contains(cylinder.transform.position))
            {
                Debug.Log($"🔍 MANUAL CHECK: Cylinder '{cylinder.name}' is no longer in trigger bounds!");
                cylindersToRemove.Add(cylinder);
            }
        }

        // Remove cylinders that left the trigger
        foreach (var cylinder in cylindersToRemove)
        {
            cylindersInTrigger.Remove(cylinder);
            Debug.Log($"🔵 Removed cylinder '{cylinder?.name}' via manual check");
        }

        if (cylindersToRemove.Count > 0)
        {
            Debug.Log($"🔍 Manual check completed - Removed {cylindersToRemove.Count} cylinders, {cylindersInTrigger.Count} remaining");
        }
    }

    private void CheckForExistingCylinders()
    {
        ColliderController_NewInput currentCylinderManager = CylinderManager;
        if (currentCylinderManager == null || currentCylinderManager.cylinders == null) return;

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null) return;

        Debug.Log($"🔍 Checking for existing cylinders in trigger...");
        int foundCount = 0;

        foreach (var cylinder in currentCylinderManager.cylinders)
        {
            if (cylinder != null && cylinder.activeInHierarchy)
            {
                bool inBounds = triggerCollider.bounds.Contains(cylinder.transform.position);

                if (inBounds)
                {
                    if (!cylindersInTrigger.Contains(cylinder))
                    {
                        cylindersInTrigger.Add(cylinder);
                        foundCount++;
                        Debug.Log($"     ✅ ADDED {cylinder.name} to trigger tracking");
                    }
                }
            }
        }

        Debug.Log($"🔍 Found {foundCount} existing cylinders in trigger");
    }

    private bool IsCylinderObject(GameObject obj)
    {
        ColliderController_NewInput currentCylinderManager = CylinderManager;
        if (currentCylinderManager != null && currentCylinderManager.cylinders != null)
        {
            foreach (var cylinder in currentCylinderManager.cylinders)
            {
                if (cylinder != null && cylinder == obj)
                {
                    return true;
                }
            }
        }

        if (obj.name.ToLower().Contains("cylinder")) return true;
        if (obj.CompareTag("Cylinder")) return true;

        return false;
    }

    private void CheckCylinderPresence()
    {
        if (!wandererIsInTrigger || hasTriggeredRespawn) return;

        bool cylindersPresent = cylindersInTrigger.Count > 0;

        Debug.Log($"🔍 Cylinder Check - Wanderer: {wandererIsInTrigger}, Cylinders: {cylindersInTrigger.Count}, HasRespawned: {hasTriggeredRespawn}");

        if (!cylindersPresent)
        {
            Debug.Log($"💥💥💥 TRIGGERING RESPAWN - No cylinders in waypoint!");
            hasTriggeredRespawn = true;

            // Trigger fall animation before respawn
            StartCoroutine(TriggerFallAndRespawn());
        }
    }

    private IEnumerator TriggerFallAndRespawn()
    {
        // Get the wanderer's movement animator
        Transform currentWanderer = Wanderer;
        if (currentWanderer != null)
        {
            WandererMovementAnimator movementAnimator = currentWanderer.GetComponent<WandererMovementAnimator>();
            if (movementAnimator != null)
            {
                movementAnimator.TriggerFall();
                Debug.Log("🔄 Playing fall animation (no cylinders in waypoint)");

                // Wait for fall animation to complete
                yield return new WaitForSeconds(1.0f);
            }
        }

        // Trigger respawn after fall animation
        if (goalManager != null)
        {
            goalManager.OnWandererInWaypointWithoutCylinders(GetComponent<Collider>());
        }
        else if (goals != null)
        {
            goals.OnWandererInWaypointWithoutCylinders(GetComponent<Collider>());
        }
        else
        {
            Debug.LogError($"❌ No goal system connected - cannot trigger respawn!");
        }
    }
    // Clean up destroyed objects
    private void FixedUpdate()
    {
        int removedCount = cylindersInTrigger.RemoveWhere(obj => obj == null);
        if (removedCount > 0)
        {
            Debug.Log($"🧹 Removed {removedCount} null cylinders from tracking");
        }
    }

    // DEBUG: Draw the trigger area and cylinder positions in the scene view
    private void OnDrawGizmos()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.color = wandererIsInTrigger ?
                (cylindersInTrigger.Count > 0 ? Color.green : Color.red) :
                Color.blue;
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);

            // Draw text showing cylinder count
#if UNITY_EDITOR
            string goalType = goalManager != null ? "Sphere" : (goals != null ? "Flat" : "None");
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"Cylinders: {cylindersInTrigger.Count}\nWanderer: {wandererIsInTrigger}\nType: {goalType}");
#endif
        }

        Gizmos.color = Color.yellow;
        foreach (var cyl in cylindersInTrigger)
        {
            if (cyl != null)
            {
                Gizmos.DrawLine(transform.position, cyl.transform.position);
                Gizmos.DrawWireSphere(cyl.transform.position, 0.5f);
            }
        }
    }
}