using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the foliage entries available to world generation.
/// </summary>
[CreateAssetMenu(menuName = "World/Foliage/Foliage List Data")]
public sealed class FoliageListData : ScriptableObject
{
    [SerializeField] private List<FoliageEntryData> _entries = new();

    public IReadOnlyList<FoliageEntryData> Entries => _entries;
}
