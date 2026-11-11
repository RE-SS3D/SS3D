using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    public class QuitCommand : Command
    {
        public override string Name => "quit";
        public override CommandCategory Category => CommandCategory.Session;
        public override string Description => "Close the game.";
        public override ServerRoleTypes AccessLevel => ServerRoleTypes.User;
        public override CommandType Type => CommandType.Local;
        public override string Example => "";

        public override CommandResult Perform(CommandContext context, CommandArgs args)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
            return CommandResult.Success("Done");
        }
    }
}
