using SS3D.Systems.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    // All commands, that inherit Command will be added to CommandsController and have to contain Command suffix
    public abstract class Command
    {
        protected struct CheckArgsResponse
        {
            public bool IsValid;
            public string InvalidArgs;
        }

		public abstract bool ServerCommand { get; }
        public abstract string ShortDescription { get; }
        public abstract string LongDescription { get; }
        public abstract ServerRoleTypes AccessLevel { get; }
        public abstract string Perform(string[] args);
        protected abstract CheckArgsResponse CheckArgs(string[] args);
        protected const string WrongArgsText = "Wrong args. Type \"(command) help\"";
    }
}