#nullable enable
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System;
using System.IO;
using UnityEngine;

namespace Serilog.Sinks.Unity3D
{
    public sealed class Unity3DLogEventSink : ILogEventSink
    {
        private readonly ITextFormatter _formatter;
        private readonly UnityEngine.ILogger _unityLogger;

        public Unity3DLogEventSink(ITextFormatter formatter, UnityEngine.ILogger unityLogger)
        {
            _formatter = formatter;
            _unityLogger = unityLogger;
        }

        public void Emit(LogEvent logEvent)
        {
            using var buffer = new StringWriter();

            _formatter.Format(logEvent, buffer);
            var logType = logEvent.Level switch
            {
                LogEventLevel.Verbose or LogEventLevel.Debug or LogEventLevel.Information => LogType.Log,
                LogEventLevel.Warning => LogType.Warning,
                LogEventLevel.Error or LogEventLevel.Fatal => LogType.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(logEvent.Level), "Unknown log level"),
            };

            object message = buffer.ToString().Trim();

            UnityEngine.Object? unityContext = null;
            if (logEvent.Properties.TryGetValue(UnityObjectEnricher.UnityContextKey, out var contextPropertyValue) && contextPropertyValue is ScalarValue contextScalarValue)
            {
                unityContext = contextScalarValue.Value as UnityEngine.Object;
            }

            string? unityTag = null;
            if (logEvent.Properties.TryGetValue(UnityTagEnricher.UnityTagKey, out var tagPropertyValue) && tagPropertyValue is ScalarValue tagScalarValue)
            {
                unityTag = tagScalarValue.Value as string;
            }


            if (unityContext != null)
            {
                if (unityTag != null)
                {
                    _unityLogger.Log(logType, unityTag, message, unityContext);
                }
                else
                {
                    _unityLogger.Log(logType, message, unityContext);
                }
            }
            else if (unityTag != null)
            {
                _unityLogger.Log(logType, unityTag, message);
            }
            else
            {
                _unityLogger.Log(logType, message);
            }
        }
    }
}