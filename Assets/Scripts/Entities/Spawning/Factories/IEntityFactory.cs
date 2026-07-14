using UnityEngine;

/// <summary>
/// Defines how an entity is created from its data and spawn position.
/// </summary>
public interface IEntityFactory<in TData, out TEntity>
    where TData : EntityData
    where TEntity : EntityCore
{
    TEntity Create(TData data, Vector2 spawnPoint, Transform parent = null);
}
