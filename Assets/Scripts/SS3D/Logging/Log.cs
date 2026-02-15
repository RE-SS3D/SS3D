using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace SS3D.Logging
{
    /// <summary>
    /// Wrapper class for Serilog Logger.
    /// Makes mandatory adding a sender object.
    /// Makes mandatory adding additional log context with the Logs enum.
    /// Takes care of adding infoLog and sender properties to Serilog Logger.
    /// </summary>
    public static class Log
	{
		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Verbose"/> level and associated exception.
		/// </summary>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Verbose(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, null, messageTemplate, LogEventLevel.Verbose, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Verbose"/> level and associated exception.
		/// </summary>
		/// <param name="exception">Exception related to the event.</param>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Verbose(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, exception, messageTemplate, LogEventLevel.Verbose, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Debug"/> level.
		/// </summary>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Debug(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, null, messageTemplate, LogEventLevel.Debug, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Debug"/> level and associated exception.
		/// </summary>
		/// <param name="exception">Exception related to the event.</param>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Debug(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, exception, messageTemplate, LogEventLevel.Debug, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Information"/> level and associated exception.
		/// </summary>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Information(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, null, messageTemplate, LogEventLevel.Information, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Information"/> level and associated exception.
		/// </summary>
		/// <param name="exception">Exception related to the event.</param>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Information(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, exception, messageTemplate, LogEventLevel.Information, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Warning"/> level and associated exception.
		/// </summary>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Warning(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, null, messageTemplate, LogEventLevel.Warning, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Warning"/> level and associated exception.
		/// </summary>
		/// <param name="exception">Exception related to the event.</param>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Warning(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, exception, messageTemplate, LogEventLevel.Warning, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Error"/> level and associated exception.
		/// </summary>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Error(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, null, messageTemplate, LogEventLevel.Error, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Error"/> level and associated exception.
		/// </summary>
		/// <param name="exception">Exception related to the event.</param>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Error(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, exception, messageTemplate, LogEventLevel.Error, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Fatal"/> level and associated exception.
		/// </summary>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Fatal(object sender, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, null, messageTemplate, LogEventLevel.Fatal, infoLog, propertyValues);

		/// <summary>
		/// Write a log event with the <see cref="LogEventLevel.Fatal"/> level and associated exception.
		/// </summary>
		/// <param name="exception">Exception related to the event.</param>
		/// <param name="messageTemplate">Message template describing the event.</param>
		/// <param name="propertyValues">Objects positionally formatted into the message template.</param>
		public static void Fatal(object sender, Exception exception, string messageTemplate, Logs infoLog = Logs.Generic, params object[] propertyValues) => InnerLog(sender, exception, messageTemplate, LogEventLevel.Fatal, infoLog, propertyValues);

		private static void InnerLog(object sender, Exception exception, string messageTemplate, LogEventLevel level, Logs infoLog = Logs.Generic, params object[] propertyValues)
		{
            object[] properties = new object[] { infoLog }.Concat(propertyValues).ToArray();
            Serilog.ILogger logger = Serilog.Log.Logger;

			if (sender != null)
			{
				logger = Serilog.Log.ForContext(Constants.SourceContextPropertyName, sender.GetType().Name);
            }

			switch (level)
			{
				case LogEventLevel.Verbose:
					logger.Verbose(exception, "{InfoLog}" + messageTemplate, properties);
					break;
				case LogEventLevel.Debug:
					logger.Debug(exception, "{InfoLog}" + messageTemplate, properties);
					break;
				case LogEventLevel.Information:
					logger.Information(exception, "{InfoLog}" + messageTemplate, properties);
					break;
				case LogEventLevel.Warning:
					logger.Warning(exception, "{InfoLog}" + messageTemplate, properties);
					break;
				case LogEventLevel.Error:
					logger.Error(exception, "{InfoLog}" + messageTemplate, properties);
					break;
				case LogEventLevel.Fatal:
					logger.Fatal(exception, "{InfoLog}" + messageTemplate, properties);
					break;
			}
		}
	}
}