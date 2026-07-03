using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemySpellTarget : MonoBehaviour, ICombatTarget, IStatusEffectTarget, ISpellWordEffectivenessTarget
{
    private readonly struct ActiveStatus
    {
        public float ExpiresAt { get; }
        public float Multiplier { get; }

        public ActiveStatus(float expiresAt, float multiplier)
        {
            ExpiresAt = expiresAt;
            Multiplier = multiplier;
        }
    }

    [Header("References")]
    [SerializeField] private EnemyCore _enemyCore;
    [SerializeField] private EntityDamageable _damageable;

    private readonly Dictionary<CombatStatusEffectType, ActiveStatus> _activeStatuses = new();
    private readonly List<CombatStatusEffectType> _cleanupBuffer = new();

    public Vector2 Position => transform.position;

    public bool IsAlive => _damageable != null && _damageable.CanReceiveDamage;

    public bool HasAnyNegativeStatus
    {
        get
        {
            CleanupExpiredStatuses();
            return _activeStatuses.Count > 0;
        }
    }

    public bool HasStatus(CombatStatusEffectType effect)
    {
        CleanupExpiredStatuses();
        return _activeStatuses.ContainsKey(effect);
    }

    public float GetStatusMultiplier(CombatStatusEffectType effect)
    {
        CleanupExpiredStatuses();
        return _activeStatuses.TryGetValue(effect, out ActiveStatus status) ? status.Multiplier : 1f;
    }

    private void Awake()
    {
        if (_enemyCore == null)
            _enemyCore = GetComponent<EnemyCore>() ?? GetComponentInParent<EnemyCore>();

        if (_damageable == null)
            _damageable = GetComponent<EntityDamageable>() ?? GetComponentInParent<EntityDamageable>();
    }

    public bool TryGetSpellWordEffectivenessData(out ItemBiomeAffinity biome, out EnemyRoleTag roleTags)
    {
        if (_enemyCore == null || _enemyCore.Data == null)
        {
            biome = ItemBiomeAffinity.None;
            roleTags = EnemyRoleTag.None;
            return false;
        }

        biome = _enemyCore.Data.HomeBiome;
        roleTags = _enemyCore.Data.RoleTags;
        return true;
    }

    public void ApplyStatus(CombatStatusEffectType effect, float durationSeconds, float multiplier = 1f)
    {
        if (effect == CombatStatusEffectType.None || durationSeconds <= 0f)
            return;

        float expiresAt = Time.time + durationSeconds;
        float normalizedMultiplier = NormalizeMultiplier(effect, multiplier);

        if (_activeStatuses.TryGetValue(effect, out ActiveStatus existingStatus) && existingStatus.ExpiresAt > Time.time)
        {
            expiresAt = Mathf.Max(expiresAt, existingStatus.ExpiresAt);
            normalizedMultiplier = SelectStrongerMultiplier(effect, existingStatus.Multiplier, normalizedMultiplier);
        }

        _activeStatuses[effect] = new ActiveStatus(expiresAt, normalizedMultiplier);

        if (effect == CombatStatusEffectType.Stunned)
            _enemyCore?.StopMovement();
    }

    private static float NormalizeMultiplier(CombatStatusEffectType effect, float multiplier)
    {
        return effect switch
        {
            CombatStatusEffectType.Slow => Mathf.Clamp(multiplier, 0f, 1f),
            CombatStatusEffectType.Weakened => Mathf.Max(1f, multiplier),
            _ => 1f
        };
    }

    private static float SelectStrongerMultiplier(CombatStatusEffectType effect, float current, float incoming)
    {
        return effect switch
        {
            CombatStatusEffectType.Slow => Mathf.Min(current, incoming),
            CombatStatusEffectType.Weakened => Mathf.Max(current, incoming),
            _ => 1f
        };
    }

    private void CleanupExpiredStatuses()
    {
        if (_activeStatuses.Count == 0)
            return;

        _cleanupBuffer.Clear();

        foreach (var pair in _activeStatuses)
        {
            if (pair.Value.ExpiresAt <= Time.time)
                _cleanupBuffer.Add(pair.Key);
        }

        for (var i = 0; i < _cleanupBuffer.Count; i++)
            _activeStatuses.Remove(_cleanupBuffer[i]);
    }
}
