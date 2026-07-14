using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the decoration entries available to world generation.
/// </summary>
[CreateAssetMenu(menuName = "World/Decorations/Decorations List Data")]
public sealed class DecorationsListData : ScriptableObject
{
    [SerializeField] private List<DecorationEntryData> _entries = new();

    public IReadOnlyList<DecorationEntryData> Entries => _entries;
}
