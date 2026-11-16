using System.Collections.Generic;
using FishNet.Object;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Area;
using SS3D.Systems.Audio;
using SS3D.Systems.Entities;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Thin dev-console authoring surface for <see cref="AreaSubSystem.SetAreaAmbienceProfileId"/>
    /// (audio.md §2) — no Map Editor UI exists yet. Always targets the area the calling player
    /// currently stands in, same "no mouse to click a tile with" reasoning as
    /// <see cref="AtmosCommand"/>.
    /// </summary>
    public class AreaAmbienceCommand : Command
    {
        private const string ClearToken = "clear";

        private static readonly IReadOnlyList<CommandParameter> Declared = new[]
        {
            P.Text("profile"),
        };

        public override string Name => "areaambience";
        public override CommandCategory Category => CommandCategory.Debug;
        public override string Description => "Set the ambience profile of the area you're standing in, or clear the override with 'clear'";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override IReadOnlyList<CommandParameter> Parameters => Declared;
        public override string Example => "engineering";

        [Server]
        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
            string requested = args.Text("profile");
            string profileId;
            if (requested.Equals(ClearToken, System.StringComparison.OrdinalIgnoreCase))
            {
                profileId = string.Empty;
            }
            else
            {
                AmbienceProfileCatalog catalog = AmbienceProfileCatalog.Instance;
                if (!catalog.TryGet(requested, out AmbienceProfile profile))
                {
                    return CommandResult.Error($"No ambience profile '{requested}'. Available: {string.Join(", ", catalog.ProfileIds)}");
                }

                profileId = profile.Id;
            }

            Entity entity = context.CallerEntity;
            if (entity == null)
            {
                return CommandResult.Error("you have no body to target from — embark first");
            }

            AreaSubSystem areaSubSystem = SubSystems.Get<AreaSubSystem>();
            if (areaSubSystem == null || !areaSubSystem.TryResolveAreaIdForWorldPosition(entity.Transform.position, out AreaId areaId))
            {
                return CommandResult.Error("Your current position isn't inside a claimed area");
            }

            areaSubSystem.SetAreaAmbienceProfileId(areaId, profileId);

            return CommandResult.Success(string.IsNullOrEmpty(profileId)
                ? $"Cleared the ambience override for area {areaId.Value}"
                : $"Area {areaId.Value} ambience set to '{profileId}'");
        }
    }
}
