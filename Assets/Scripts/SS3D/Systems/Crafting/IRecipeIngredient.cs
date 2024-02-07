using SS3D.Interactions.Interfaces;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Interface for things that need to be consumed when a crafting interaction leads to consuming them.
    /// </summary>
    public interface IRecipeIngredient : INetworkObjectProvider, IGameObjectProvider
    {
        /// <summary>
        /// Despawn the game object in an expected way.
        /// </summary>
        public void Consume();
    }
}
