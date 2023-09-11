using FishNet.Connection;
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

        /// <summary>
        /// Is the command going to be executed server or client side ?
        /// </summary>
        public abstract CommandType Type { get; }

        public abstract string ShortDescription { get; }

        public abstract string LongDescription { get; }

        /// <summary>
        /// The requested role to be able to perform this command.
        /// </summary>
        public abstract ServerRoleTypes AccessLevel { get; }

        /// <summary>
        /// Perform the given command.
        /// </summary>
        /// <param name="args"> An array of arguments for the command.</param>
        /// <param name="conn"> The connection of the player executing this command, sometimes useful. </param>
        /// <returns> A message indicating if the command performed as expected, or requested informations.</returns>
        public abstract string Perform(string[] args, NetworkConnection conn = null);

        /// <summary>
        /// Validate the arguments of the command.
        /// </summary>
        protected abstract CheckArgsResponse CheckArgs(string[] args);
        protected const string WrongArgsText = "Wrong args. Type \"(command) help\"";
    }
}