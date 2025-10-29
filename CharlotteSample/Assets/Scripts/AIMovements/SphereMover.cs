using UnityEngine;

public static class SphereMover
{
    public static Vector3 MoveOnSphere(Vector3 currentPos, Vector3 targetPos, Vector3 sphereCenter, float radius, float step)
    {
        Vector3 dirCurrent = (currentPos - sphereCenter).normalized;
        Vector3 dirTarget = (targetPos - sphereCenter).normalized;

        float angle = Vector3.Angle(dirCurrent, dirTarget);
        if (angle < 0.001f) return sphereCenter + dirTarget * radius;

        float t = Mathf.Min(1f, step / angle);
        Vector3 newDir = Vector3.Slerp(dirCurrent, dirTarget, t).normalized;

        return sphereCenter + newDir * radius;
    }
}
