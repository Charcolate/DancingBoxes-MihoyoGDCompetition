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