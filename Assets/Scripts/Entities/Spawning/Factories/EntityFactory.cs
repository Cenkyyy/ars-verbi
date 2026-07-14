using UnityEngine;

/// <summary>
/// Creates entity instances from their data assets.
/// </summary>
public sealed class EntityFactory<TData, TEntity> : IEntityFactory<TData, TEntity>
    where TData : EntityData
    where TEntity : EntityCore
{
    /// <summary>
    /// Instantiates and initializes an entity, or destroys the instance if its expected component is missing.
    /// </summary>
    public TEntity Create(TData data, Vector2 spawnPoint, Transform parent = null)
    {
        if (data == null || data.Prefab == null)
            return null;

        var instance = Object.Instantiate(data.Prefab, spawnPoint, Quaternion.identity, parent);
        
        if (!instance.TryGetComponent(out TEntity entity))
        {
            Object.Destroy(instance);
            return null;
        }

        entity.Initialize(data);

        return entity;
    }
}
