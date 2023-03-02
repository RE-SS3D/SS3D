// Copyright 2019-2020 Serilog Contributors
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
    sealed class ConditionalSink : ILogEventSink, IDisposable
#if FEATURE_ASYNCDISPOSABLE
    , IAsyncDisposable
#endif
    {
        readonly ILogEventSink _wrapped;
        readonly Func<LogEvent, bool> _condition;

        public ConditionalSink(ILogEventSink wrapped, Func<LogEvent, bool> condition)
        {
            _wrapped = Guard.AgainstNull(wrapped);
            _condition = Guard.AgainstNull(condition);
        }

        public void Emit(LogEvent logEvent)
        {
            if (_condition(logEvent))
                _wrapped.Emit(logEvent);
        }

        public void Dispose()
        {
            (_wrapped as IDisposable)?.Dispose();
        }

#if FEATURE_ASYNCDISPOSABLE
    public ValueTask DisposeAsync()
    {
        if (_wrapped is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();

        Dispose();
        return default;
    }
#endif
    }
}