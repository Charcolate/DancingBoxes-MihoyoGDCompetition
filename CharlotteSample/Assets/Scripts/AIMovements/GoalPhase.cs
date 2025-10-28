using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGoalPhase", menuName = "Goal/GoalPhase", order = 1)]
public class GoalPhase : ScriptableObject
{
    [Tooltip("Waypoints for this phase.")]
    public List<Transform> waypoints;

    [Tooltip("Index of the waypoint that triggers the projectile.")]
    public int triggerWaypointIndex = 1;

    [Tooltip("Pause at each waypoint before moving to the next.")]
    public float pauseDuration = 1f;
}
