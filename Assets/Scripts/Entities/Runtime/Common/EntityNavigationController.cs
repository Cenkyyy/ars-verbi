using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles entity path requests, waypoint following, and local detour movement.
/// </summary>
[DisallowMultipleComponent]
public sealed class EntityNavigationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EntityMovementController _movement;

    private readonly List<Vector2> _currentPath = new();
    private int _pathIndex = -1;
    private float _nextAllowedRepathTime;
    private Vector2 _lastRepathTarget;
    private bool _hasLastRepathTarget;

    private void OnDrawGizmosSelected()
    {
        if (_currentPath.Count == 0)
            return;

        Gizmos.color = Color.blue;
        var previous = (Vector2)transform.position;
        for (var i = 0; i < _currentPath.Count; i++)
        {
            var waypoint = _currentPath[i];
            Gizmos.DrawLine(previous, waypoint);
            Gizmos.DrawWireSphere(waypoint, 0.08f);
            previous = waypoint;
        }

        if (_pathIndex < 0 || _pathIndex >= _currentPath.Count)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(_currentPath[_pathIndex], 0.12f);
    }

    public bool CanUsePathPosition(Vector2 worldPosition, Collider2D movementCollider)
    {
        ResolveMovementClearance(movementCollider, out Vector2 probeCenterOffset, out float probeRadius);
        return CanUsePathPosition(worldPosition, probeCenterOffset, probeRadius);
    }

    /// <summary>
    /// Follows a cached path, rebuilds it when needed, and falls back to direct or detour movement.
    /// </summary>
    public Vector2 MoveTo(
        Vector2 worldTarget,
        Collider2D movementCollider,
        float repathIntervalSeconds,
        float pathNodeStep,
        int maxPathIterations,
        bool hasDetourTarget = false,
        Vector2 detourTarget = default)
    {
        ResolveMovementClearance(movementCollider, out Vector2 probeCenterOffset, out float probeRadius);

        var targetShifted = _hasLastRepathTarget && (worldTarget - _lastRepathTarget).sqrMagnitude > pathNodeStep * pathNodeStep;
        if (_pathIndex < 0 || !_hasLastRepathTarget || targetShifted || Time.time >= _nextAllowedRepathTime)
            RebuildPath(worldTarget, probeCenterOffset, probeRadius, repathIntervalSeconds, pathNodeStep, maxPathIterations);

        while (_pathIndex >= 0 && _pathIndex < _currentPath.Count)
        {
            var waypoint = _currentPath[_pathIndex];
            if (!_movement.ArrivedAt(waypoint))
                break;

            _pathIndex++;
        }

        if (_pathIndex >= 0 && _pathIndex < _currentPath.Count)
            return MoveTowards(_currentPath[_pathIndex]);

        if (!CanMoveDirectlyTo(worldTarget, probeCenterOffset, probeRadius))
        {
            if (hasDetourTarget && TryMoveToDetour(detourTarget, probeCenterOffset, probeRadius, out var detourDirection))
                return detourDirection;

            Stop();
            return Vector2.zero;
        }

        return MoveTowards(worldTarget);
    }

    public void Stop()
    {
        _movement.Stop();
    }

    private void RebuildPath(
        Vector2 worldTarget,
        Vector2 probeCenterOffset,
        float probeRadius,
        float repathIntervalSeconds,
        float pathNodeStep,
        int maxPathIterations)
    {
        _nextAllowedRepathTime = Time.time + repathIntervalSeconds;
        _lastRepathTarget = worldTarget;
        _hasLastRepathTarget = true;
        _pathIndex = 0;

        var success = WorldChunkNavigationController.Instance.TryBuildPath(
            startWorld: transform.position,
            targetWorld: worldTarget,
            nodeStep: pathNodeStep,
            probeCenterOffset: probeCenterOffset,
            probeRadius: probeRadius,
            maxIterations: maxPathIterations,
            output: _currentPath
        );

        if (success)
            return;

        _currentPath.Clear();
        _pathIndex = -1;
    }

    private bool CanMoveDirectlyTo(Vector2 worldTarget, Vector2 probeCenterOffset, float probeRadius)
    {
        var from = (Vector2)transform.position;
        var toTarget = worldTarget - from;
        if (toTarget.sqrMagnitude <= _movement.ArrivalEpsilon * _movement.ArrivalEpsilon)
            return true;

        return WorldChunkNavigationController.Instance.CanMoveDirectly(from, worldTarget, probeCenterOffset, probeRadius);
    }

    private bool TryMoveToDetour(Vector2 worldTarget, Vector2 probeCenterOffset, float probeRadius, out Vector2 desiredDirection)
    {
        desiredDirection = Vector2.zero;

        var from = (Vector2)transform.position;
        var toTarget = worldTarget - from;
        if (toTarget.sqrMagnitude <= Mathf.Epsilon)
            return false;

        var lateral = Vector2.Perpendicular(toTarget.normalized);
        var probeDistance = Mathf.Max(probeRadius * 3f, _movement.ArrivalEpsilon * 2f);

        var detourA = from + lateral * probeDistance;
        var detourB = from - lateral * probeDistance;

        var aBlocked = !CanUsePathPosition(detourA, probeCenterOffset, probeRadius);
        var bBlocked = !CanUsePathPosition(detourB, probeCenterOffset, probeRadius);

        if (aBlocked && bBlocked)
            return false;

        var aDistance = (detourA - worldTarget).sqrMagnitude;
        var bDistance = (detourB - worldTarget).sqrMagnitude;
        var chooseA = !aBlocked && (bBlocked || aDistance <= bDistance);
        var detour = chooseA ? detourA : detourB;

        desiredDirection = MoveTowards(detour);
        return true;
    }

    private bool CanUsePathPosition(Vector2 worldPosition, Vector2 probeCenterOffset, float probeRadius)
    {
        return WorldChunkNavigationController.Instance.IsWorldAreaWalkable(worldPosition + probeCenterOffset, probeRadius);
    }

    private void ResolveMovementClearance(Collider2D movementCollider, out Vector2 probeCenterOffset, out float probeRadius)
    {
        if (movementCollider == null)
            throw new MissingReferenceException($"{name} requires a movement collider for navigation.");

        Bounds bounds = movementCollider.bounds;
        probeCenterOffset = (Vector2)bounds.center - (Vector2)transform.position;
        probeRadius = ((Vector2)bounds.extents).magnitude;
    }

    private Vector2 MoveTowards(Vector2 worldTarget)
    {
        _movement.MoveTowards(worldTarget);
        return worldTarget - (Vector2)transform.position;
    }
}
