using UnityEngine;

/// <summary>
/// Defines how a possible enemy spawn position is selected.
/// </summary>
public interface IEnemySpawnStrategy
{
    bool TryGetSpawnPoint(Vector2 playerPosition, EnemySpawnSettings settings, out Vector2 spawnPoint);
}
