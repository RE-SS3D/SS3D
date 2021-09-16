using SS3D.Engine.Inventory;
using UnityEngine;
namespace SS3D.Engine.Interactions.Extensions
{
    public static class InteractionEventExtensions
    {
        public static IContainerizable GetSourceItem(this InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is IGameObjectProvider source)
            {
                IContainerizable item = source.GameObject.GetComponent<IContainerizable>();
                if (item != null)
                {
                    return item;
                }
            }
            Debug.LogError("the source gamObject doesn't have a IContainerizable script, or the interactionEvent source is not a IGameObjectProvider");
            return null;
        }
    }
}