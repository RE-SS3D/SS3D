// Copyright 2013-2015 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using Serilog.Events;

namespace Serilog.Core.Sinks
{
    /// <summary>
    /// Forwards log events to another logging pipeline. Copies the events so
    /// that mutations performed on the copies do not affect the originals.
    /// </summary>
    /// <remarks>The properties dictionary is copied, however the values within
    /// the dictionary (of type <see cref="LogEventProperty"/> are expected to
    /// be immutable.</remarks>
    sealed class SecondaryLoggerSink : ILogEventSink, IDisposable
#if FEATURE_ASYNCDISPOSABLE
    , IAsyncDisposable
#endif
    {
        readonly ILogger _logger;
        readonly bool _attemptDispose;

        public SecondaryLoggerSink(ILogger logger, bool attemptDispose = false)
        {
            _logger = Guard.AgainstNull(logger);
            _attemptDispose = attemptDispose;
        }

        public void Emit(LogEvent logEvent)
        {
            Guard.AgainstNull(logEvent);

            var copy = logEvent.Copy();
            _logger.Write(copy);
        }

        public void Dispose()
        {
            if (!_attemptDispose)
                return;

            (_logger as IDisposable)?.Dispose();
        }

#if FEATURE_ASYNCDISPOSABLE
    public ValueTask DisposeAsync()
    {
        if (_logger is IAsyncDisposable asyncDisposable)
        {
            if (!_attemptDispose)
                return default;

            return asyncDisposable.DisposeAsync();
        }

        Dispose();
        return default;
    }
#endif
    }
}