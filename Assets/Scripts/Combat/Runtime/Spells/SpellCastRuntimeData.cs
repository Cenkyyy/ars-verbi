using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores mutable state shared by every projectile and beam created by one spell cast.
/// </summary>
public sealed class SpellCastRuntimeData
{
    private int _remainingReclaims;

    public ModifierWordData Modifier => Spell.Modifier;
    public ElementWordData Element => Spell.Element;
    public FormWordData Form => Spell.Form;
    public SpellPhrase Spell { get; }
    public float BaseDamage { get; }

    public SpellCastRuntimeData(SpellPhrase spell, float baseDamage)
    {
        Spell = spell;
        BaseDamage = spell.Modifier.Type == ModifierWordType.Splitting
            ? Mathf.Max(0f, baseDamage) * spell.Modifier.SplitDamageMultiplier
            : Mathf.Max(0f, baseDamage);
        _remainingReclaims = Mathf.Max(0, spell.Modifier.MaxReclaimsPerCast);
    }

    public SpellImpactBudget CreateImpactBudget()
    {
        return new SpellImpactBudget(Modifier.Type == ModifierWordType.Piercing);
    }

    public bool TryConsumeReclaim()
    {
        if (_remainingReclaims <= 0)
            return false;

        _remainingReclaims--;
        return true;
    }
}

/// <summary>
/// Limits effects that should occur once per physical projectile. Beam branches share one budget.
/// </summary>
public sealed class SpellImpactBudget
{
    private readonly bool _allowPoisonCloudPerTarget;
    private readonly HashSet<ICombatTarget> _poisonCloudTargets = new();
    private bool _explosionConsumed;
    private bool _poisonCloudConsumed;

    public SpellImpactBudget(bool allowPoisonCloudPerTarget)
    {
        _allowPoisonCloudPerTarget = allowPoisonCloudPerTarget;
    }

    public bool TryConsumeExplosion()
    {
        if (_explosionConsumed)
            return false;

        _explosionConsumed = true;
        return true;
    }

    public bool TryConsumePoisonCloud(ICombatTarget target)
    {
        if (_allowPoisonCloudPerTarget)
            return target != null && _poisonCloudTargets.Add(target);

        if (_poisonCloudConsumed)
            return false;

        _poisonCloudConsumed = true;
        return true;
    }
}
