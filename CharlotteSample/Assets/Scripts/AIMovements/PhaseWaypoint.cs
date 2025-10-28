using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PhaseWaypoint
{
    [Tooltip("Drag the Transform of the waypoint in the scene.")]
    public Transform waypointTransform;

    [Tooltip("Should projectiles be fired at this waypoint?")]
    public bool triggerProjectile = false;

    [Tooltip("Number of projectiles to fire if triggered.")]
    public int projectileCount = 1;

    [Tooltip("Optional: custom spawn points for projectiles. Drag Transforms here.")]
    public List<Transform> customSpawnTransforms;

    [Tooltip("Fire projectiles this many seconds before reaching the waypoint.")]
    public float leadTime = 0.2f;
}
