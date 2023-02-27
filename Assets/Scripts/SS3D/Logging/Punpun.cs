using System;
using System.Linq;
using System.Security.Cryptography;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Logging
{
    /// <summary>
    /// Wrapper class for Serilog Logger.
    /// Makes logging easier in most cases.
    /// </summary>
    public static class Punpun
    {
        /// <summary>
        /// A refined Debug.Log
        /// </summary>
        /// <param name="sender">who sends the message</param>
        /// <param name="message">message</param>
        /// <param name="logs">type of log</param>
        public static void Say(object sender, string message, Logs logs = Logs.Generic, bool colorizeEverything = false)
        {
            string debug = ProcessDebug(sender, message, logs, colorizeEverything);

            UnityEngine.Debug.Log(debug);
        }

        /// <summary>
        /// A refined Debug.LogWarning
        /// </summary>
        /// <param name="sender">who sends the message</param>
        /// <param name="message">message</param>
        /// <param name="logs">type of log</param>
        public static void Yell(object sender, string message, Logs logs = Logs.Generic, bool colorizeEverything = false)
        {
            string debug = ProcessDebug(sender, message, logs, colorizeEverything);

            UnityEngine.Debug.LogWarning(debug);
        }

        /// <summary>
        /// A refined Debug.LogError
        /// </summary>
        /// <param name="sender">who sends the message</param>
        /// <param name="message">message</param>
        /// <param name="logs">type of log</param>
        public static void Panic(object sender, string message, Logs logs = Logs.Generic, bool colorizeEverything = false)
        {
            string debug = ProcessDebug(sender, message, logs, colorizeEverything);

            UnityEngine.Debug.LogError(debug);
        }

        private static string ProcessDebug(object sender, string message, Logs logs = Logs.Generic, bool colorizeEverything = false)
        {
            string color = LogColors.GetLogColor(logs);

            string name = sender.GetType().Name;
            string author = name == "RuntimeType" ? $"{sender}" : $"{name}";
            author = $"[{author}]".Colorize(color);

            if (colorizeEverything)
            {
                message.Colorize(color);
            }

            string log = $"{author} {message}";

            return log;
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Verbose"/> level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose("Staring into space, wondering if we're alone.");
        /// </example>
        public static void Verbose(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[]{infoLog}.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Verbose("{InfoLog}" + messageTemplate, properties);
        }
     
        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Verbose"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Verbose(ex, "Staring into space, wondering where this comet came from.");
        /// </example>
        public static void Verbose(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Verbose(exception,"{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Debug"/> level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Debug("Starting up at {StartedAt}.", DateTime.Now);
        /// </example>
        public static void Debug(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Debug("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Debug"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Debug(ex, "Swallowing a mundane exception.");
        /// </example>
        public static void Debug(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Debug(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Information"/> level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information("Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public static void Information(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Information("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Information"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information(ex, "Processed {RecordCount} records in {TimeMS}.", records.Length, sw.ElapsedMilliseconds);
        /// </example>
        public static void Information(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Debug(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Warning"/> level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning("Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public static void Warning(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Warning("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Warning"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning(ex, "Skipped {SkipCount} records.", skippedRecords.Length);
        /// </example>
        public static void Warning(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Debug(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Error"/> level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error("Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public static void Error(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Error("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Error"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <example>
        /// Log.Error(ex, "Failed {ErrorCount} records.", brokenRecords.Length);
        /// </example>
        public static void Error(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Error(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Fatal"/> level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Fatal("Process terminating.");
        /// </example>
        public static void Fatal(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Fatal("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Fatal"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Fatal(ex, "Process terminating.");
        /// </example>
        public static void Fatal(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            var properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Log.ForContext(sender.GetType()).Fatal(exception, "{InfoLog}" + messageTemplate, properties);
        }
        
    }
}