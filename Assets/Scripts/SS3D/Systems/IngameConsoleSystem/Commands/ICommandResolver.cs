using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Turns target and item tokens into game objects. <see cref="CommandRegistry"/> takes it as a dependency
    /// so EditMode tests can bind arguments with a fake.
    /// </summary>
    public interface ICommandResolver
    {
        /// <summary>Resolve <c>me</c>, a ckey (case-sensitive) or <c>nearest</c>.</summary>
        bool TryResolveTarget(string token, CommandContext context, out CommandTarget target, out string error);

        /// <summary>Resolve an item by asset id or name.</summary>
        bool TryResolveItem(string token, out CommandItem item, out string error);
    }

    /// <summary>A resolved item prefab and the asset id to spawn it by.</summary>
    public sealed class CommandItem
    {
        public CommandItem(string token, Item prefab, string assetId)
        {
            Token = token;
            Prefab = prefab;
            AssetId = assetId;
        }

        public string Token { get; }

        /// <summary>Null only in tests with a fake resolver.</summary>
        public Item Prefab { get; }

        public string AssetId { get; }
    }
}
