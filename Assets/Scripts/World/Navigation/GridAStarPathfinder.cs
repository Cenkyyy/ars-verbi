using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds grid paths while checking world positions for movement clearance.
/// </summary>
public static class GridAStarPathfinder
{
    private const float BlockedGoalPenalty = 5f;

    /// <summary>
    /// Stores a cell and its scores while it is waiting to be visited.
    /// </summary>
    private readonly struct GridNode
    {
        public readonly Vector2Int Cell;
        public readonly float FScore;
        public readonly float HScore;

        public GridNode(Vector2Int cell, float fScore, float hscore)
        {
            Cell = cell;
            FScore = fScore;
            HScore = hscore;
        }
    }

    /// <summary>
    /// Searches the grid with A* and can return the closest partial path when the goal is unreachable.
    /// </summary>
    public static bool TryBuildPath(
        Vector2 startWorld,
        Vector2 goalWorld,
        float nodeStep,
        Vector2 probeCenterOffset,
        float probeRadius,
        int maxIterations,
        List<Vector2> output,
        Func<Vector2, float, bool> canUseWorldPosition,
        bool allowPartialPath = true)
    {
        output.Clear();

        var startCell = ToCell(startWorld, nodeStep);
        var goalCell = ToCell(goalWorld, nodeStep);
        var startHeuristic = Heuristic(startCell, goalCell);
        var openList = new List<GridNode> { new(startCell, startHeuristic, startHeuristic) };
        var closedList = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [startCell] = 0f };
        var blockedByCell = new Dictionary<Vector2Int, bool>();
        var bestCell = startCell;
        var bestHeuristic = startHeuristic;

        var iterations = 0;
        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            var currentIndex = GetBestOpenNodeIndex(openList);
            var current = openList[currentIndex].Cell;
            openList.RemoveAt(currentIndex);

            if (current == goalCell)
            {
                ReconstructPath(current, cameFrom, nodeStep, output);
                return output.Count > 0;
            }

            var currentHeuristic = Heuristic(current, goalCell) + (IsBlocked(current, nodeStep, probeCenterOffset, probeRadius, blockedByCell, canUseWorldPosition) ? BlockedGoalPenalty : 0f);
            if (currentHeuristic < bestHeuristic)
            {
                bestHeuristic = currentHeuristic;
                bestCell = current;
            }

            closedList.Add(current);
            VisitNeighbors(current, goalCell, nodeStep, probeCenterOffset, probeRadius, canUseWorldPosition, blockedByCell, closedList, cameFrom, gScore, openList);
        }

        if (allowPartialPath && bestCell != startCell)
        {
            ReconstructPath(bestCell, cameFrom, nodeStep, output);
            return output.Count > 0;
        }

        return false;
    }

    /// <summary>
    /// Selects the lowest-cost open node, using distance to the goal as a tie breaker.
    /// </summary>
    private static int GetBestOpenNodeIndex(List<GridNode> openList)
    {
        var currentIndex = 0;
        var bestF = openList[0].FScore;
        for (var i = 1; i < openList.Count; i++)
        {
            if (openList[i].FScore < bestF || (Mathf.Approximately(openList[i].FScore, bestF) && openList[i].HScore < openList[currentIndex].HScore))
            {
                bestF = openList[i].FScore;
                currentIndex = i;
            }
        }

        return currentIndex;
    }

    /// <summary>
    /// Expands the walkable neighbours and updates them when a cheaper route is found.
    /// </summary>
    private static void VisitNeighbors(
        Vector2Int current,
        Vector2Int goalCell,
        float nodeStep,
        Vector2 probeCenterOffset,
        float probeRadius,
        Func<Vector2, float, bool> canUseWorldPosition,
        Dictionary<Vector2Int, bool> blockedByCell,
        HashSet<Vector2Int> closedList,
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Dictionary<Vector2Int, float> gScore,
        List<GridNode> openList)
    {
        foreach (var neighbor in GetNeighbors(current, nodeStep, probeCenterOffset, probeRadius, blockedByCell, canUseWorldPosition))
        {
            if (closedList.Contains(neighbor))
                continue;

            if (IsBlocked(neighbor, nodeStep, probeCenterOffset, probeRadius, blockedByCell, canUseWorldPosition))
                continue;

            var tentativeG = gScore[current] + StepCost(current, neighbor);
            if (gScore.TryGetValue(neighbor, out var knownG) && tentativeG >= knownG)
                continue;

            cameFrom[neighbor] = current;
            gScore[neighbor] = tentativeG;
            var h = Heuristic(neighbor, goalCell);
            var f = tentativeG + h;

            if (TryUpdateOpenNode(openList, neighbor, f, h))
                continue;

            openList.Add(new GridNode(neighbor, f, h));
        }
    }

    /// <summary>
    /// Replaces a queued node when a cheaper route to its cell is found.
    /// </summary>
    private static bool TryUpdateOpenNode(List<GridNode> openList, Vector2Int neighbor, float f, float h)
    {
        for (var i = 0; i < openList.Count; i++)
        {
            if (openList[i].Cell != neighbor)
                continue;

            openList[i] = new GridNode(neighbor, f, h);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Enumerates adjacent cells while filtering invalid diagonal shortcuts.
    /// </summary>
    private static IEnumerable<Vector2Int> GetNeighbors(
        Vector2Int cell,
        float nodeStep,
        Vector2 probeCenterOffset,
        float probeRadius,
        Dictionary<Vector2Int, bool> blockedByCell,
        Func<Vector2, float, bool> canUseWorldPosition)
    {
        for (var y = -1; y <= 1; y++)
        {
            for (var x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                var neighbor = new Vector2Int(cell.x + x, cell.y + y);
                if (x != 0 && y != 0 && IsDiagonalBlocked(cell, x, y, nodeStep, probeCenterOffset, probeRadius, blockedByCell, canUseWorldPosition))
                    continue;

                yield return neighbor;
            }
        }
    }

    /// <summary>
    /// Prevents diagonal movement through a corner when either side of that corner is blocked.
    /// </summary>
    private static bool IsDiagonalBlocked(
        Vector2Int cell,
        int x,
        int y,
        float nodeStep,
        Vector2 probeCenterOffset,
        float probeRadius,
        Dictionary<Vector2Int, bool> blockedByCell,
        Func<Vector2, float, bool> canUseWorldPosition)
    {
        var horizontal = new Vector2Int(cell.x + x, cell.y);
        var vertical = new Vector2Int(cell.x, cell.y + y);
        return IsBlocked(horizontal, nodeStep, probeCenterOffset, probeRadius, blockedByCell, canUseWorldPosition) ||
               IsBlocked(vertical, nodeStep, probeCenterOffset, probeRadius, blockedByCell, canUseWorldPosition);
    }

    private static float StepCost(Vector2Int a, Vector2Int b)
    {
        var dx = Mathf.Abs(a.x - b.x);
        var dy = Mathf.Abs(a.y - b.y);
        return dx + dy == 1 ? 1f : 1.4142135f;
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2Int.Distance(a, b);
    }

    /// <summary>
    /// Checks movement clearance once and caches the result for the current search.
    /// </summary>
    private static bool IsBlocked(
        Vector2Int cell,
        float nodeStep,
        Vector2 probeCenterOffset,
        float probeRadius,
        Dictionary<Vector2Int, bool> blockedByCell,
        Func<Vector2, float, bool> canUseWorldPosition)
    {
        if (blockedByCell.TryGetValue(cell, out var cachedBlocked))
            return cachedBlocked;

        var world = ToWorld(cell, nodeStep);
        blockedByCell[cell] = !canUseWorldPosition(world + probeCenterOffset, probeRadius);
        return blockedByCell[cell];
    }

    private static Vector2Int ToCell(Vector2 world, float step)
    {
        return new Vector2Int(Mathf.RoundToInt(world.x / step), Mathf.RoundToInt(world.y / step));
    }

    private static Vector2 ToWorld(Vector2Int cell, float step)
    {
        return new Vector2(cell.x * step, cell.y * step);
    }

    /// <summary>
    /// Walks the parent map back to the start and removes the starting cell from the final path.
    /// </summary>
    private static void ReconstructPath(Vector2Int current, Dictionary<Vector2Int, Vector2Int> cameFrom, float nodeStep, List<Vector2> output)
    {
        output.Add(ToWorld(current, nodeStep));

        while (cameFrom.TryGetValue(current, out var parent))
        {
            current = parent;
            output.Add(ToWorld(current, nodeStep));
        }

        output.Reverse();

        if (output.Count > 0)
            output.RemoveAt(0);
    }
}
