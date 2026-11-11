namespace SS3D.Systems.IngameConsoleSystem.Commands
{
    /// <summary>What a command answers. <see cref="Ok"/> false means it did nothing, and the harness's <c>wait_console</c> retries.</summary>
    public readonly struct CommandResult
    {
        public const string ErrorPrefix = "error: ";

        public bool Ok { get; }

        public string Message { get; }

        private CommandResult(bool ok, string message)
        {
            Ok = ok;
            Message = message ?? string.Empty;
        }

        public static CommandResult Success(string message) => new(true, message);

        /// <summary>A failure; the message is prefixed with <see cref="ErrorPrefix"/>.</summary>
        public static CommandResult Error(string message) => new(false, ErrorPrefix + message);

        /// <summary>A failure whose message is used verbatim (already formatted, e.g. with a usage line).</summary>
        public static CommandResult Failure(string formattedMessage) => new(false, formattedMessage);

        public override string ToString() => Message;
    }
}
