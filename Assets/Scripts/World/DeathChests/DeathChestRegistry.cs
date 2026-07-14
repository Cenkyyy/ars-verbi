using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active death chests by their identifier and inventory.
/// </summary>
[DisallowMultipleComponent]
public sealed class DeathChestRegistry : MonoBehaviour
{
    private readonly Dictionary<string, DeathChestRuntimeData> _byId = new Dictionary<string, DeathChestRuntimeData>();
    private readonly Dictionary<IInventory, DeathChestRuntimeData> _byInventory = new Dictionary<IInventory, DeathChestRuntimeData>();

    public IReadOnlyCollection<DeathChestRuntimeData> ActiveChests => _byId.Values;

    /// <summary>
    /// Adds a valid death chest to both lookup collections.
    /// </summary>
    public void Register(DeathChestRuntimeData handle)
    {
        if (handle == null || string.IsNullOrEmpty(handle.DeathChestId) || handle.Inventory == null || handle.Inventory.Inventory == null)
            return;

        _byId[handle.DeathChestId] = handle;
        _byInventory[handle.Inventory.Inventory] = handle;
    }

    /// <summary>
    /// Removes a death chest and its inventory lookup.
    /// </summary>
    public void Unregister(string deathChestId)
    {
        if (string.IsNullOrEmpty(deathChestId))
            return;

        if (!_byId.TryGetValue(deathChestId, out var handle))
            return;

        _byId.Remove(deathChestId);

        if (handle.Inventory != null && handle.Inventory.Inventory != null)
            _byInventory.Remove(handle.Inventory.Inventory);
    }

    /// <summary>
    /// Finds the death chest that owns the given inventory.
    /// </summary>
    public bool TryGetByInventory(IInventory inventory, out DeathChestRuntimeData handle)
    {
        return _byInventory.TryGetValue(inventory, out handle);
    }
}
