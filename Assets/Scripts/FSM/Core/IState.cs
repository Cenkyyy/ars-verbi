/// <summary>
/// Defines the lifecycle of a state used by the state machine.
/// </summary>
public interface IState
{
    void OnEnter();
    void Do();
    void FixedDo();
    void OnExit();
}
