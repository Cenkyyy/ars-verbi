using System;
using UnityEngine;

/// <summary>
/// Creates a death chest and transfers the player's items after death.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerDeathChestDropController : MonoBehaviour
{
    [SerializeField] private DeathChestFactory _deathChestFactory;
    [SerializeField] private DeathChestRegistry _deathChestRegistry;
    [SerializeField] private DeathMarkerController _deathMarkerController;

    /// <summary>
    /// Creates a death chest only when at least one backpack item can be transferred into it.
    /// </summary>
    public void CreateDeathChestFromBackpack(Player player, Vector3 deathWorldPosition)
    {
        if (player == null || player.Inventory == null)
            return;

        if (_deathChestFactory == null)
            return;

        string deathChestId = Guid.NewGuid().ToString("N");
        var deathChestHandle = _deathChestFactory.Create(deathChestId, deathWorldPosition);
        if (deathChestHandle == null)
            return;

        DeathChestInventoryTransferUtility.MoveBackpackItemsToDeathChest(player.Inventory, deathChestHandle.Inventory.Inventory);

        deathChestHandle.LifecycleController.Initialize(deathChestId, deathChestHandle.Inventory.Inventory, _deathMarkerController, _deathChestRegistry);
        _deathChestRegistry?.Register(deathChestHandle);
        _deathMarkerController?.AddDeathMarker(deathChestId, deathChestHandle.WorldPosition);
    }
}
