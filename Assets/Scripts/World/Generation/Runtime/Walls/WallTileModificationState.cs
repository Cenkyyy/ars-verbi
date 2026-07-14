using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Remembers modified wall tiles so streamed chunks keep player changes.
/// </summary>
public sealed class WallTileModificationState
{
    private readonly HashSet<Vector2Int> _removedTiles = new HashSet<Vector2Int>();

    public void MarkRemoved(Vector2Int tile)
    {
        _removedTiles.Add(tile);
    }

    public bool IsRemoved(Vector2Int tile)
    {
        return _removedTiles.Contains(tile);
    }
}
