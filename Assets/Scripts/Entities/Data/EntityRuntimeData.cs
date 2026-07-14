using System;
using UnityEngine;

/// <summary>
/// Tracks the health and lifetime state of an entity instance.
/// </summary>
[Serializable]
public class EntityRuntimeData
{
    public int MaxHealth { get; protected set; }
    public int CurrentHealth { get; protected set; }
    public bool IsDead => CurrentHealth <= 0;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    /// <summary>
    /// Resets the runtime health from the entity configuration.
    /// </summary>
    public virtual void InitializeFrom(EntityData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        MaxHealth = data.MaxHealth;
        CurrentHealth = MaxHealth;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    /// <summary>
    /// Applies damage and raises the death event when health reaches zero.
    /// </summary>
    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
            return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth > 0)
            return false;

        OnDied?.Invoke();
        return true;
    }
}
