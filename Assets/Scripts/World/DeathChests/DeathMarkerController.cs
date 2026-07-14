using UnityEngine;

/// <summary>
/// Updates the map marker that points to the player's latest death chest.
/// </summary>
[DisallowMultipleComponent]
public sealed class DeathMarkerController : MonoBehaviour
{
    [SerializeField] private MinimapController _minimap;
    [SerializeField] private WorldMapPanel _worldMap;

    public void AddDeathMarker(string markerId, Vector3 worldPosition)
    {
        var marker = new MapMarkerRuntimeState(markerId, worldPosition);

        _minimap?.AddMarker(marker);
        _worldMap?.AddMarker(marker);
    }

    public void RemoveDeathMarker(string markerId)
    {
        _minimap?.RemoveMarker(markerId);
        _worldMap?.RemoveMarker(markerId);
    }
}
