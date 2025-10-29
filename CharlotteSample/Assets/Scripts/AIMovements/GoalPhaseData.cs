using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GhostData
{
    public Transform ghostTransform;           // The ghost for this phase
    public List<PhaseWaypoint> waypoints;     // Ghost's waypoints for this phase
}

[System.Serializable]
public class GoalPhaseData
{
    [Tooltip("Waypoints for this phase.")]
    public List<PhaseWaypoint> waypoints = new List<PhaseWaypoint>();

    [Tooltip("Pause at each waypoint before moving to the next.")]
    public float pauseDuration = 1f;

    [Tooltip("Optional: list of ghosts and their custom waypoints for this phase")]
    public List<GhostData> ghostsInPhase = new List<GhostData>();
}
