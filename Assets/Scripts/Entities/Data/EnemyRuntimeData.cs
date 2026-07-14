using System;
using UnityEngine;

/// <summary>
/// Holds the values that can change for a spawned enemy.
/// </summary>
[Serializable]
public sealed class EnemyRuntimeData : EntityRuntimeData
{
    public void InitializeFrom(EnemyData data)
    {
        base.InitializeFrom(data);
    }
}
