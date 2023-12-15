using SS3D.Interactions;
using SS3D.Interactions.Interfaces;

public interface ICraftable : INetworkObjectProvider, IGameObjectProvider
{
    public void Craft(InteractionEvent interaction);
}
