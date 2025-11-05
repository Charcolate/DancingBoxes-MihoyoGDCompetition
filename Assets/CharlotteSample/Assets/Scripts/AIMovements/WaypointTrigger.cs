using UnityEngine;
using System.Collections.Generic;

public class WaypointTrigger : MonoBehaviour
{
    public GoalManager goalManager;

    // Track objects currently in the trigger
    private HashSet<GameObject> cylindersInTrigger = new HashSet<GameObject>();
    private bool wandererIsInTrigger = false;
    private bool hasTriggeredRespawn = false;

    private void Start()
    {
        Debug.Log($"🔧 WaypointTrigger started: {gameObject.name} at position {transform.position}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (goalManager == null) return;

        // Check if the wanderer entered the trigger
        if (other.CompareTag("Player") || (goalManager.wanderer != null && other.transform == goalManager.wanderer))
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
        if (goalManager == null) return;

        // Check if the wanderer left the trigger
        if (other.CompareTag("Player") || (goalManager.wanderer != null && other.transform == goalManager.wanderer))
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
        if (goalManager == null || goalManager.cylinderManager == null) return;

        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null) return;

        Debug.Log($"🔍 Checking for existing cylinders in trigger...");
        int foundCount = 0;

        foreach (var cylinder in goalManager.cylinderManager.cylinders)
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

            if (goalManager != null)
            {
                goalManager.OnWandererInWaypointWithoutCylinders(GetComponent<Collider>());
            }
            else
            {
                Debug.LogError($"❌ GoalManager is null - cannot trigger respawn!");
            }
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
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                $"Cylinders: {cylindersInTrigger.Count}\nWanderer: {wandererIsInTrigger}");
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