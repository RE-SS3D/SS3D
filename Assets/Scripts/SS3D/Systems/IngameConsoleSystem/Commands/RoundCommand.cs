using System.Collections.Generic;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Rounds;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>
    /// Console path for the lobby's admin round controls (admin-tools.md §6): each mode is one
    /// <see cref="RoundAdminAction"/>, applied by <see cref="RoundSubSystem.ServerRoundAdminAction"/>.
    /// No round logic lives here.
    /// </summary>
    public class RoundCommand : Command
    {
        private IReadOnlyList<Subcommand> _subcommands;

        public override string Name => "round";
        public override CommandCategory Category => CommandCategory.Admin;
        public override string Description => "Start, force, extend, cancel or end the round, or hold the end screen";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.Administrator;
        public override CommandType Type => CommandType.Server;
        public override string Example => "end";

        public override IReadOnlyList<Subcommand> Subcommands => _subcommands ??= new[]
        {
            Mode("start", "Start a round from the lobby", RoundAdminAction.Start, "Round start requested", "the round isn't stopped or ended"),
            Mode("force", "Skip the rest of the countdown", RoundAdminAction.ForceStart, "Countdown skipped", "no countdown is running"),
            Mode("extend", "Add a minute to the countdown", RoundAdminAction.ExtendTimer, "Countdown extended by 60 s", "no countdown is running"),
            Mode("cancel", "Stop the countdown and hold auto-start", RoundAdminAction.CancelCountdown, "Countdown cancelled", "no countdown is running"),
            Mode("end", "End the running round; the reveal and summary still run", RoundAdminAction.Stop,
                "Round end requested (unsaved map edits open the save prompt in your lobby)", "no round is running"),
            Mode("hold", "Hold the end screen", RoundAdminAction.HoldEnd, "End screen held", "the round isn't ending"),
            Mode("resume", "Release a held end screen", RoundAdminAction.ResumeEnd, "End screen released", "the round isn't ending"),
        };

        private static Subcommand Mode(string name, string description, RoundAdminAction action, string done, string notApplicable) =>
            new(name, description, (context, _) =>
            {
                if (!SubSystems.TryGet(out RoundSubSystem rounds))
                {
                    return CommandResult.Error("no round system is loaded");
                }

                return rounds.ServerRoundAdminAction(action, context.Connection)
                    ? CommandResult.Success(done)
                    : CommandResult.Error($"can't {name}: {notApplicable}");
            });
    }
}
