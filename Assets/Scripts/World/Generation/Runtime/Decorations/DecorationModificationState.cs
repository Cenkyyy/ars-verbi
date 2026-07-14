using System.Collections.Generic;

/// <summary>
/// Remembers decorations removed from generated chunks during the current run.
/// </summary>
public sealed class DecorationModificationState
{
    private readonly HashSet<string> _removedDecorationIds = new HashSet<string>();

    public void MarkRemoved(string decorationInstanceId)
    {
        if (string.IsNullOrEmpty(decorationInstanceId))
            return;

        _removedDecorationIds.Add(decorationInstanceId);
    }

    public bool IsRemoved(string decorationInstanceId)
    {
        if (string.IsNullOrEmpty(decorationInstanceId))
            return false;

        return _removedDecorationIds.Contains(decorationInstanceId);
    }
}
