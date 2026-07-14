/// <summary>
/// Defines how entity data is selected for a biome.
/// </summary>
public interface IEntitySelectionStrategy<TData> where TData : EntityData
{
    bool TrySelect(ItemBiomeAffinity biome, out TData data);
}
