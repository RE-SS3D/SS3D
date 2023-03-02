#nullable enable
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;

namespace Serilog.Sinks.Unity3D
{
    public static class UnitySinkExtensions
    {
        private const string DefaultDebugOutputTemplate = "[{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Writes log events to <see cref="UnityEngine.ILogger"/>. Defaults to <see cref="UnityEngine.Debug.unityLogger"/>.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is <code>"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"</code>.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="unityLogger">Specify a Unity-native logger. Defaults to <see cref="UnityEngine.Debug.unityLogger"/>.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration Unity3D(
            this LoggerSinkConfiguration sinkConfiguration,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string outputTemplate = DefaultDebugOutputTemplate,
            IFormatProvider? formatProvider = null,
            LoggingLevelSwitch? levelSwitch = null,
            UnityEngine.ILogger? unityLogger = null)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (outputTemplate == null) throw new ArgumentNullException(nameof(outputTemplate));

#pragma warning disable IDE0074 // Use compound assignment
            if (unityLogger == null) unityLogger = UnityEngine.Debug.unityLogger;
#pragma warning restore IDE0074 // Use compound assignment

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return sinkConfiguration.Unity3D(formatter, restrictedToMinimumLevel, levelSwitch, unityLogger);
        }

        /// <summary>
        /// Writes log events to <see cref="UnityEngine.ILogger"/>. Defaults to <see cref="UnityEngine.Debug.unityLogger"/>.
        /// </summary>
        /// <param name="sinkConfiguration">Logger sink configuration.</param>
        /// <param name="formatter">Controls the rendering of log events into text, for example to log JSON. To
        /// control plain text formatting, use the overload that accepts an output template.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="unityLogger">Specify a Unity-native logger. Defaults to <see cref="UnityEngine.Debug"/>.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration Unity3D(
            this LoggerSinkConfiguration sinkConfiguration,
            ITextFormatter formatter,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch? levelSwitch = null,
            UnityEngine.ILogger? unityLogger = null)
        {
            if (sinkConfiguration == null) throw new ArgumentNullException(nameof(sinkConfiguration));
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

#pragma warning disable IDE0074 // Use compound assignment
            if (unityLogger == null) unityLogger = UnityEngine.Debug.unityLogger;
#pragma warning restore IDE0074 // Use compound assignment

            return sinkConfiguration.Sink(new Unity3DLogEventSink(formatter, unityLogger), restrictedToMinimumLevel, levelSwitch);
        }
    }
}
