using System;
using System.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SS3D.Logging
{
    /// <summary>
    /// Wrapper class for Serilog Logger. Makes mandatory adding a sender object.
    /// Makes mandatory adding additionnal log context with the Logs enum.
    /// Takes care of adding infoLog and sender properties to Serilog Logger.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Verbose"/> level and associated exception.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Punpun.Verbose(this, "Starting up at {StartedAt} for client {ClientId}.", Logs.ServerOnly, DateTime.Now, connection.ClientId);
        /// Punpun.Verbose(this, "Player set up.");
        /// </example>
        public static void Verbose(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[]{infoLog}.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Verbose("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Verbose"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Punpun.Verbose(this, "Starting up at {StartedAt} for client {ClientId}.", Logs.ServerOnly, DateTime.Now, connection.ClientId);
        /// Punpun.Verbose(this, "Player set up.");
        /// </example>
        public static void Verbose(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Verbose(exception,"{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Debug"/> level.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Punpun.Debug(this, "Starting up at {StartedAt} for client {ClientId}.", Logs.ServerOnly, DateTime.Now, connection.ClientId);
        /// Punpun.Debug(this, "Player set up.");
        /// </example>
        public static void Debug(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Debug("{InfoLog}" + messageTemplate, properties);
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
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Debug(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Information"/> level and associated exception.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Punpun.Verbose(this, "Starting up at {StartedAt} for client {ClientId}.", Logs.ServerOnly, DateTime.Now, connection.ClientId);
        /// Punpun.Verbose(this, "Player set up.");
        /// </example>
        public static void Information(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Information("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Information"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Information(this, new NullException(),  "Failed to load command line arguments in {TimeMs}.", Logs.Generic, sw.ElapsedMilliseconds);
        /// </example>
        public static void Information(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Debug(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Warning"/> level and associated exception.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Punpun.Verbose(this, "Starting up at {StartedAt} for client {ClientId}.", Logs.ServerOnly, DateTime.Now, connection.ClientId);
        /// Punpun.Verbose(this, "Player set up.");
        /// </example>
        public static void Warning(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Warning("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Warning"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Warning(this, new NullException(),  "Failed to load command line arguments in {TimeMs}.", Logs.Generic, sw.ElapsedMilliseconds);
        /// </example>
        public static void Warning(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Debug(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Error"/> level and associated exception.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Punpun.Verbose(this, "Starting up failed at {StartedAt} for client {ClientId}.", Logs.ServerOnly, DateTime.Now, connection.ClientId);
        /// Punpun.Verbose(this, "Player set up.");
        /// </example>
        public static void Error(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Error("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Error"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Error(this, new NullException(),  "Failed to load command line arguments in {TimeMs}.", Logs.Generic, sw.ElapsedMilliseconds);
        /// </example>
        public static void Error(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Error(exception, "{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Fatal"/> level and associated exception.
        /// </summary>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Punpun.Verbose(this, "Starting up failed at {StartedAt} for client {ClientId}.", Logs.ServerOnly, DateTime.Now, connection.ClientId);
        /// Punpun.Verbose(this, "Player set up.");
        /// </example>
        public static void Fatal(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Fatal("{InfoLog}" + messageTemplate, properties);
        }

        /// <summary>
        /// Write a log event with the <see cref="LogEventLevel.Fatal"/> level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="messageTemplate">Message template describing the event.</param>
        /// <param name="propertyValues">Objects positionally formatted into the message template.</param>
        /// <example>
        /// Log.Fatal(this, new NullException(),  "Failed to load command line arguments in {TimeMs}.", Logs.Generic, sw.ElapsedMilliseconds);
        /// </example>
        public static void Fatal(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues)
        {
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name).Fatal(exception, "{InfoLog}" + messageTemplate, properties);
        }
    }
}