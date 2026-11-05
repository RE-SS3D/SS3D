using FishNet.Connection;
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
    /// <see cref="AtmosDebugCommand"/>.
    /// </summary>
    public class AreaAmbienceCommand : Command
    {
        public override string ShortDescription => "Set the ambience profile of the area you're standing in";
        public override string Usage => "(profileId | clear)\nexample: areaambience engineering\nexample: areaambience clear";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;

        private record CalculatedValues(string ProfileId) : ICalculatedValues;

        [Server]
        public override string Perform(string[] args, NetworkConnection conn = null)
        {
            if (!ReceiveCheckResponse(args, out CheckArgsResponse response, out CalculatedValues values))
            {
                return response.InvalidArgs;
            }

            if (!SubSystems.Get<EntitySubSystem>().TryGetSpawnedEntity(conn, out Entity entity))
            {
                return "You have no spawned entity to target from — embark first";
            }

            AreaSubSystem areaSubSystem = SubSystems.Get<AreaSubSystem>();
            if (areaSubSystem == null || !areaSubSystem.TryResolveAreaIdForWorldPosition(entity.Transform.position, out AreaId areaId))
            {
                return "Your current position isn't inside a claimed area";
            }

            areaSubSystem.SetAreaAmbienceProfileId(areaId, values.ProfileId);

            return string.IsNullOrEmpty(values.ProfileId)
                ? $"Cleared the ambience override for area {areaId.Value}"
                : $"Area {areaId.Value} ambience set to '{values.ProfileId}'";
        }

        protected override CheckArgsResponse CheckArgs(string[] args)
        {
            CheckArgsResponse response = new();

            if (args.Length != 1)
            {
                return response.MakeInvalid(WrongArgsText);
            }

            if (args[0].Equals("clear", System.StringComparison.OrdinalIgnoreCase))
            {
                return response.MakeValid(new CalculatedValues(string.Empty));
            }

            AmbienceProfileCatalog catalog = AmbienceProfileCatalog.Instance;
            return catalog.TryGet(args[0], out AmbienceProfile profile)
                ? response.MakeValid(new CalculatedValues(profile.Id))
                : response.MakeInvalid($"No ambience profile '{args[0]}'. Available: {string.Join(", ", catalog.ProfileIds)}");
        }
    }
}
