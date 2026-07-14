using System;
using UnityEngine;

/// <summary>
/// Pairs an item status effect with its configured value.
/// </summary>
[Serializable]
public struct ItemStatusEffect
{
    [field: Header("Effect Settings")]
    [field: SerializeField] public ItemStatusEffectType StatusEffectType { get; private set; }
    [field: SerializeField] public float Value { get; private set; }

    public ItemStatusEffect(ItemStatusEffectType statusEffectType, float value)
    {
        StatusEffectType = statusEffectType;
        Value = value;
    }
}
