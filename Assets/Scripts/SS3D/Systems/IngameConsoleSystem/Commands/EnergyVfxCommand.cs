using FishNet.Connection;
using SS3D.Permissions;
using SS3D.Systems.Combat;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Local preview of energy shot visuals on existing firearms.
    /// </summary>
    public class EnergyVfxCommand : Command
    {
        public override string ShortDescription => "Preview energy weapon effects";
        public override string Usage => "laser|ion|taser|off — preview local shot visuals; off restores weapon defaults";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Client;

        private record CalculatedValues(string Mode) : ICalculatedValues;

        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            EnergyWeaponVfx.PreviewStyle = values.Mode switch
            {
                "laser" => RangedVisualStyle.Laser,
                "ion" => RangedVisualStyle.Ion,
                "taser" => RangedVisualStyle.Taser,
                _ => null,
            };
            return $"Local shot VFX: {EnergyWeaponVfx.PreviewStyle?.ToString() ?? "weapon default"}. Damage and audio unchanged.";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length == 0)
            {
                return response.MakeValid(new CalculatedValues("off"));
            }

            if (args.Length != 1)
            {
                return response.MakeInvalid(WrongArgsText);
            }

            string mode = args[0].ToLowerInvariant();
            if (mode is not ("laser" or "ion" or "taser" or "off"))
            {
                return response.MakeInvalid("Expected laser, ion, taser, or off");
            }

            return response.MakeValid(new CalculatedValues(mode));
        }
    }
}
