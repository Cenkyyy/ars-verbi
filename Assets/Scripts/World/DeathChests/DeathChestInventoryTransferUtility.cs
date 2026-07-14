/// <summary>
/// Moves player inventory contents into a death chest.
/// </summary>
public static class DeathChestInventoryTransferUtility
{
    /// <summary>
    /// Moves backpack stacks into the death chest while leaving hotbar items with the player.
    /// </summary>
    public static void MoveBackpackItemsToDeathChest(PlayerInventory playerInventory, IInventory deathChestInventory)
    {
        if (playerInventory == null || deathChestInventory == null)
            return;

        var backpackRange = playerInventory.GetBackpackSlotRange();
        var deathChestRange = new SlotRange(0, deathChestInventory.Capacity);

        for (int index = backpackRange.StartInclusive; index < backpackRange.EndExclusive; index++)
        {
            var item = playerInventory.GetItemAt(index);
            if (item.IsEmpty)
                continue;

            deathChestInventory.TryAddItemToRange(item, deathChestRange, out var leftover);

            if (leftover.IsEmpty)
                playerInventory.ClearItemAt(index);
            else
                playerInventory.SetItemAt(index, leftover);
        }
    }
}
