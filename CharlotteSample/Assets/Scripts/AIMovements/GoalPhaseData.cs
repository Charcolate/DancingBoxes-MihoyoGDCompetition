using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GoalPhaseData
{
    [Tooltip("Waypoints for this phase.")]
    public List<PhaseWaypoint> waypoints = new List<PhaseWaypoint>();

    [Tooltip("Pause at each waypoint before moving to the next.")]
    public float pauseDuration = 1f;
}
