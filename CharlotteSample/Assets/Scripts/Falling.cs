using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Falling : MonoBehaviour
{
    [Tooltip("Tag of the object that should trigger a reset (e.g. 'Player' or 'Wanderer').")]
    public string triggeringTag = "Wanderer";

    private GoalManager goalManager;

    private void Awake()
    {
        // Make sure the collider is a trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Cache the GoalManager
        goalManager = FindObjectOfType<GoalManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryReset(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryReset(collision.gameObject);
    }

    private void TryReset(GameObject obj)
    {
        if (string.IsNullOrEmpty(triggeringTag) || obj.CompareTag(triggeringTag))
        {
            Debug.Log($"[Falling] {obj.name} entered or collided — restarting phase.");
            if (goalManager != null)
                goalManager.ResetPhase();
            else
                Debug.LogWarning("[Falling] No GoalManager found in scene!");
        }
    }
}
