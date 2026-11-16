using System.Collections.Generic;
using SS3D.Permissions;
using SS3D.Systems.Combat;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Local preview of energy shot visuals on existing firearms. Damage, ammo and audio stay authoritative.
    /// </summary>
    public class EnergyVfxCommand : Command
    {
        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Choice("style", "laser", "ion", "taser", "off").Default("off"),
        };

        public override string Name => "energyvfx";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Preview energy shot visuals locally; off restores weapon defaults";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;
        public override IReadOnlyList<CommandParameter> Parameters => Declared;
        public override string Example => "laser";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            EnergyWeaponVfx.PreviewStyle = args.Text("style") switch
            {
                "laser" => RangedVisualStyle.Laser,
                "ion" => RangedVisualStyle.Ion,
                "taser" => RangedVisualStyle.Taser,
                _ => null,
            };
            return CommandResult.Success($"Local shot VFX: {EnergyWeaponVfx.PreviewStyle?.ToString() ?? "weapon default"}. Damage and audio unchanged.");
        }
    }
}
