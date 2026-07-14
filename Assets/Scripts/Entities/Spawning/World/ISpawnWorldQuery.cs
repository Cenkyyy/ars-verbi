using UnityEngine;

/// <summary>
/// Provides the world checks needed to validate enemy spawn points.
/// </summary>
public interface ISpawnWorldQuery
{
    bool IsWalkable(Vector2 worldPoint, float probeRadius);
    bool TryGetBiome(Vector2 worldPoint, out ItemBiomeAffinity biome);
}
