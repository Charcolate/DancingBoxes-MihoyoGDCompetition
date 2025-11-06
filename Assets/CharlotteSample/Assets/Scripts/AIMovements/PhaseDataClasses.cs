using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PhaseWaypoint
{
    public Transform waypointTransform;
    public bool triggerProjectile = false;
    public float leadTime = 0.5f;
    public List<Transform> customSpawnTransforms = new List<Transform>();
}

[System.Serializable]
public class GhostPhaseData
{
    public Transform ghostTransform;
    public List<PhaseWaypoint> ghostWaypoints = new List<PhaseWaypoint>();

    [Header("Visual Effects")]
    public bool enableGoldTrail = false; // NEW: Toggle for gold trail
    public float trailWidth = 0.2f; // NEW: Trail width
    public Color trailColor = new Color(1f, 0.84f, 0f, 0.7f); // NEW: Gold color with alpha
}

[System.Serializable]
public class GoalPhaseData
{
    [Header("Legacy Waypoints")]
    public List<PhaseWaypoint> waypoints = new List<PhaseWaypoint>();

    [Header("Ghosts")]
    public List<GhostPhaseData> ghostsInPhase = new List<GhostPhaseData>();

    [Header("Wanderer")]
    public List<PhaseWaypoint> wandererWaypoints = new List<PhaseWaypoint>();

    [Header("Phase Settings")]
    public float pauseDuration = 0.5f;
}