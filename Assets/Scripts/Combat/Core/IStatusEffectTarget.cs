/// <summary>
/// Contract for combat targets that can receive and expose combat status effects.
/// </summary>
public interface IStatusEffectTarget
{
    bool HasAnyNegativeStatus { get; }
    bool HasStatus(CombatStatusEffectType effect);
    float GetStatusMultiplier(CombatStatusEffectType effect);
    void ApplyStatus(CombatStatusEffectType effect, float durationSeconds, float multiplier = 1f);
}
