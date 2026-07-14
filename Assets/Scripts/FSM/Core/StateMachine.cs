using System.Collections.Generic;

/// <summary>
/// Stores registered states and controls which state is active.
/// </summary>
public sealed class StateMachine
{
    private readonly Dictionary<StateId, IState> _statesById = new();

    public StateId CurrentStateId { get; private set; } = StateId.None;
    public IState ActiveState { get; private set; }

    public bool Register(StateId stateId, IState state)
    {
        if (stateId == StateId.None || state == null || _statesById.ContainsKey(stateId))
            return false;

        _statesById.Add(stateId, state);
        return true;
    }

    /// <summary>
    /// Changes the active state. A forced reset exits and enters the state again if it is already active.
    /// </summary>
    public bool Set(StateId stateId, bool forceReset = false)
    {
        return _statesById.TryGetValue(stateId, out var state) &&
               Set(stateId, state, forceReset);
    }

    public void Do()
    {
        ActiveState?.Do();
    }

    public void FixedDo()
    {
        ActiveState?.FixedDo();
    }

    /// <summary>
    /// Checks the transition rules and changes to the first valid target state.
    /// </summary>
    public bool TryTransition(StateTransitionTable transitionTable)
    {
        return transitionTable != null &&
               transitionTable.TryGetNextState(CurrentStateId, out var nextStateId, out var forceReset) &&
               Set(nextStateId, forceReset);
    }

    /// <summary>
    /// Exits the current state, switches the active state and then enters the new one.
    /// </summary>
    private bool Set(StateId stateId, IState newState, bool forceReset)
    {
        if (newState == null)
            return false;

        if (ActiveState == newState && !forceReset)
            return false;

        ActiveState?.OnExit();
        ActiveState = newState;
        CurrentStateId = stateId;

        ActiveState.OnEnter();
        return true;
    }
}
