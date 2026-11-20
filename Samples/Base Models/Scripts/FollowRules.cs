using UnityEngine;

public static class FollowRules
{
    public static Vector3 Flat(Vector3 position)
    {
        return new Vector3(position.x, 0f, position.z);
    }

    public static bool IsOutsideComfortBand(Vector3 followerPosition, Vector3 leaderPosition, float minimumDistance, float maximumDistance)
    {
        var distance = Vector3.Distance(Flat(followerPosition), Flat(leaderPosition));
        return distance < minimumDistance || distance > maximumDistance;
    }

    public static bool HasLeaderTurnedAway(Vector3 followerPosition, Vector3 desiredPosition, Vector3 leaderPosition, float thresholdDegrees)
    {
        var current = Flat(followerPosition - leaderPosition);
        var desired = Flat(desiredPosition - leaderPosition);
        if (current.sqrMagnitude < Mathf.Epsilon || desired.sqrMagnitude < Mathf.Epsilon)
        {
            return false;
        }

        return Vector3.Angle(current, desired) > thresholdDegrees;
    }

    public static Vector3 ClampToZone(Vector3 position, Vector3 zoneCentre, float zoneRadius)
    {
        var offset = Flat(position - zoneCentre);
        if (offset.magnitude <= zoneRadius)
        {
            return position;
        }

        var inside = Flat(zoneCentre) + offset.normalized * zoneRadius;
        return new Vector3(inside.x, position.y, inside.z);
    }

    public static bool IsInsideZone(Vector3 position, Vector3 zoneCentre, float zoneRadius)
    {
        return Flat(position - zoneCentre).magnitude <= zoneRadius;
    }
}
