using UnityEngine;

/// <summary>
/// Defines one element word and the elemental combat effects it can apply.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Words/Element Word", fileName = "ElementWordData")]
public sealed class ElementWordData : WordData
{
    [field: Header("Identity")]
    [field: SerializeField] public ElementWordType Type { get; private set; }

    [field: Header("Visuals")]
    [field: SerializeField] public Material Material { get; private set; }

    [Header("Status")]
    [field: SerializeField, Min(-1f)] public float StatusDuration { get; private set; }
    [field: SerializeField, Min(-1f)] public float DamageOverTimePerSecond { get; private set; }

    [field: Header("Frost")]
    [field: SerializeField, Min(-1f)] public float FrostMoveSpeedMultiplier { get; private set; } = 0.6f;

    [field: Header("Dark")]
    [field: SerializeField, Min(-1f)] public float DarkIncomingDamageMultiplier { get; private set; } = 1.25f;

    [Header("Lightning")]
    [field: SerializeField, Min(-1f)] public float LightningBonusMultiplier { get; private set; }

    [Header("Poison")]
    [field: SerializeField, Min(-1f)] public float PoisonCloudDuration { get; private set; }
    [field: SerializeField] public GameObject PoisonCloudPrefab { get; private set; }

    public override WordCategory Category => WordCategory.Element;
    public override bool IsValid => System.Enum.IsDefined(typeof(ElementWordType), Type);

    protected override void OnValidate()
    {
        base.OnValidate();

        StatusDuration = UsesStatusDuration() ? Mathf.Max(0f, StatusDuration) : -1f;
        DamageOverTimePerSecond = UsesDamageOverTime() ? Mathf.Max(0f, DamageOverTimePerSecond) : -1f;
        FrostMoveSpeedMultiplier = Type == ElementWordType.Frost ? Mathf.Clamp(FrostMoveSpeedMultiplier, 0.1f, 1f) : -1f;
        DarkIncomingDamageMultiplier = Type == ElementWordType.Dark ? Mathf.Max(1f, DarkIncomingDamageMultiplier) : -1f;
        LightningBonusMultiplier = Type == ElementWordType.Lightning ? Mathf.Max(0f, LightningBonusMultiplier) : -1f;
        PoisonCloudDuration = Type == ElementWordType.Poison ? Mathf.Max(0f, PoisonCloudDuration) : -1f;
        PoisonCloudPrefab = Type == ElementWordType.Poison ? PoisonCloudPrefab : null;
    }

    private bool UsesStatusDuration()
    {
        return Type == ElementWordType.Frost ||
               Type == ElementWordType.Ember ||
               Type == ElementWordType.Dark;
    }

    private bool UsesDamageOverTime()
    {
        return Type == ElementWordType.Ember ||
               Type == ElementWordType.Poison;
    }
}
