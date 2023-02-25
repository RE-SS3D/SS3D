// Copyright 2013-2020 Serilog Contributors
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
using System.Collections.Generic;
using System.Linq;
using Serilog.Debugging;
using Serilog.Events;

namespace Serilog.Core.Sinks
{
    class FilteringSink : ILogEventSink
    {
        readonly ILogEventSink _sink;
        readonly bool _propagateExceptions;
        readonly ILogEventFilter[] _filters;

        public FilteringSink(ILogEventSink sink, IEnumerable<ILogEventFilter> filters, bool propagateExceptions)
        {
            _sink = Guard.AgainstNull(sink);
            _filters = Guard.AgainstNull(filters).ToArray();
            _propagateExceptions = propagateExceptions;
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                foreach (var logEventFilter in _filters)
                {
                    if (!logEventFilter.IsEnabled(logEvent))
                        return;
                }

                _sink.Emit(logEvent);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Caught exception while applying filters: {0}", ex);

                if (_propagateExceptions)
                    throw;
            }
        }
    }
}
