using Serilog.Core;
using Serilog.Events;

namespace Serilog
{
    public sealed class UnityObjectEnricher : ILogEventEnricher
    {
        public const string UnityContextKey = "%_DO_NOT_USE_UNITY_ID_DO_NOT_USE%";

        private readonly LogEventProperty _property;

        public UnityObjectEnricher(UnityEngine.Object context) =>
            _property = new LogEventProperty(UnityContextKey, new ScalarValue(context));

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) =>
            logEvent.AddPropertyIfAbsent(_property);
    }
}
