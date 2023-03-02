using Serilog.Core;
using Serilog.Events;

namespace Serilog
{
    public sealed class UnityTagEnricher : ILogEventEnricher
    {
        public const string UnityTagKey = "%_DO_NOT_USE_UNITY_TAG_DO_NOT_USE%";

        private readonly LogEventProperty _property;

        public UnityTagEnricher(string tag) =>
            _property = new LogEventProperty(UnityTagKey, new ScalarValue(tag));

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) =>
            logEvent.AddPropertyIfAbsent(_property);
    }
}
