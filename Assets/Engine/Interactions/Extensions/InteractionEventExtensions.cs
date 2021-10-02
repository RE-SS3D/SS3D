using SS3D.Engine.Inventory;
using UnityEngine;
namespace SS3D.Engine.Interactions.Extensions
{
    public static class InteractionEventExtensions
    {
        public static IContainable GetSourceItem(this InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is IGameObjectProvider source)
            {
                IContainable item = source.GameObject.GetComponent<IContainable>();
                if (item != null)
                {
                    return item;
                }
            }
            Debug.LogError("the source gamObject doesn't have a IContainable script, or the interactionEvent source is not a IGameObjectProvider");
            return null;
        }
    }
}