namespace Serilog
{
    public static class ILoggerExtensions
    {
        /// <summary>
        /// Add <see cref="UnityEngine.Object"/> context for the log.
        /// </summary>
        /// <param name="logger">Original logger</param>
        /// <param name="context"> <see cref="UnityEngine.Object"/> context for the log</param>
        public static ILogger ForContext(this ILogger logger, UnityEngine.Object context) => logger.ForContext(new UnityObjectEnricher(context));

        /// <summary>
        /// Add Unity tag for the log
        /// </summary>
        /// <param name="logger">Original logger</param>
        /// <param name="tag">Unity log tag</param>
        public static ILogger WithUnityTag(this ILogger logger, string tag) => logger.ForContext(new UnityTagEnricher(tag));
    }
}
