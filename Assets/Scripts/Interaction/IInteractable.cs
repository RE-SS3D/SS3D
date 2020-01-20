namespace Interaction
{
    public interface IInteractable
    {
        void Advertise(Interactable interactable);
        void Handle(InteractionEvent e);
    }
}