using FishNet.Connection;
using SS3D.Permissions;

namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    // All commands, that inherit Command will be added to CommandsController and have to contain Command suffix
    public abstract class Command
    {
        protected struct CheckArgsResponse
        {
            public bool IsValid;
            public string InvalidArgs;
            public ICalculatedValues CalculatedValues;
            public CheckArgsResponse MakeInvalid(string invalidArgs)
            {
                InvalidArgs = invalidArgs;
                IsValid = false;
                return this;
            }
            public CheckArgsResponse MakeValid(ICalculatedValues calculatedValues)
            {
                CalculatedValues = calculatedValues;
                IsValid = true;
                return this;
            }
        }
        /// <summary>
        /// Struct to transfer calculated values between CheckArgs and Perform methods
        /// </summary>
        protected interface ICalculatedValues { }

        /// <summary>
        /// Is the command going to be executed server or client side ?
        /// </summary>
        public abstract CommandType Type { get; }

        /// <summary>
        /// Desription, that will be shown after "help" command
        /// </summary>
        public abstract string ShortDescription { get; }

        /// <summary>
        /// Detailed description of the command. Will be shown after "help (command name)" command
        /// </summary>
        public virtual string LongDescription => ShortDescription;

        /// <summary>
        /// how to use the command.
        /// Syntax for writing usage: arguments in () - necessary; [] - optional; {} - list of arguments with undefined size
        /// </summary>
        public virtual string Usage => "";

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

        /// <summary>
        /// Store CheckArgs response and get if response is valid.
        /// </summary>
        /// <returns>If respons is valid</returns>
        protected bool ReceiveCheckResponse<T>(string[] args, out CheckArgsResponse response, out T calculatedValues) where T: ICalculatedValues
        {
            response = CheckArgs(args);
            calculatedValues = (T)response.CalculatedValues;
            return response.IsValid;
        }
    }
}