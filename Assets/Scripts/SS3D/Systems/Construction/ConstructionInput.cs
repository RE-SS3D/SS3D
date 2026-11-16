using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Construction
{
    /// <summary>The two tools and three materials of construction.md §2.</summary>
    public enum ConstructionInput : byte
    {
        Welder = 0,
        Crowbar = 1,
        SteelSheet = 2,
        GlassSheet = 3,
        FloorTile = 4,
    }

    public static class ConstructionInputs
    {
        /// <summary>Whether <paramref name="input"/> is spent by count (a material) rather than kept (a tool).</summary>
        public static bool IsMaterial(ConstructionInput input) => input >= ConstructionInput.SteelSheet;

        /// <summary>The construction input this held item provides, from its <see cref="ConstructionTool"/> or <see cref="ConstructionMaterial"/>.</summary>
        public static bool TryGet(Item item, out ConstructionInput input)
        {
            input = default;
            if (item == null)
            {
                return false;
            }

            if (item.TryGetComponent(out ConstructionTool tool))
            {
                input = tool.Input;
                return true;
            }

            if (item.TryGetComponent(out ConstructionMaterial material))
            {
                input = material.Input;
                return true;
            }

            return false;
        }

        public static string Label(ConstructionInput input) =>
            input switch
            {
                ConstructionInput.Welder => "welder",
                ConstructionInput.Crowbar => "crowbar",
                ConstructionInput.SteelSheet => "steel sheets",
                ConstructionInput.GlassSheet => "glass sheets",
                ConstructionInput.FloorTile => "floor tiles",
                _ => input.ToString(),
            };
    }
}
