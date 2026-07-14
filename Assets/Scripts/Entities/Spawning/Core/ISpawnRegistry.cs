using UnityEngine;

/// <summary>
/// Tracks spawned entities and provides distance-based queries and cleanup.
/// </summary>
public interface ISpawnRegistry<in TEntity> where TEntity : EntityCore
{
    int AliveCount { get; }
    void Register(TEntity entity);
    bool HasAnyWithin(Vector2 point, float minDistance);
    void DespawnOutsideRadius(Vector2 center, float radius);
    int DespawnInsideRadius(Vector2 center, float radius);
}
