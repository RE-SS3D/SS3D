using SS3D.Engine.Inventory;

namespace SS3D.Engine.Interactions.Extensions
{
    public static class InteractionEventExtensions
    {
        public static Item GetSourceItem(this InteractionEvent interactionEvent)
        {
            if (interactionEvent.Source is IGameObjectProvider source)
            {
                Item item = source.GameObject.GetComponent<Item>();
                if (item != null)
                {
                    return item;
                }
            }

            return null;
        }
    }
}