/// <summary>
/// Defines an object that can be used by the player.
/// </summary>
public interface IInteractable
{
    bool CanInteract();
    void Interact();
}
