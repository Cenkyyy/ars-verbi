/// <summary>
/// Stores the generated terrain and biome values of one world tile.
/// </summary>
public struct WorldTile
{
    public WorldBiomeType Biome;
    public WorldTileType TileType;

    public WorldTile(WorldBiomeType biome, WorldTileType tileType)
    {
        Biome = biome;
        TileType = tileType;
    }
}
