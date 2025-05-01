using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using System;
using System.IO;
using Serilog.Formatting;
using SS3D.Utils;
using System.Collections.Generic;
using Serilog.Formatting.Json;

namespace SS3D.Logging
{
    /// <summary>
    /// Text formatter for Log message going to Unity console.
    /// Adds colors, properly renders Serializable class and structures.
    /// Heavily inspired by MessageTemplateTextFormatter.
    /// Sadly, since many static methods are internal to Serilog, it was necessary to write an equivalent of some of them in here.
    /// </summary>
    public class SS3DUnityTextFormatter : ITextFormatter
    {
        private static readonly JsonValueFormatter JsonValueFormatter = new("$type");

        private readonly MessageTemplate _outputTemplate;
        private readonly IFormatProvider _formatProvider;

        public SS3DUnityTextFormatter(string outputTemplate, IFormatProvider formatProvider = null)
        {
            _outputTemplate = new MessageTemplateParser().Parse(outputTemplate);
            _formatProvider = formatProvider;
        }

        /// <summary>
        /// Format the logEvent to look nice in Unity console. 
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="output"></param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            foreach (MessageTemplateToken token in _outputTemplate.Tokens)
            {
                if (token is TextToken tt)
                {
                    RenderTextToken(tt, output);
                    continue;
                }

                PropertyToken pt = (PropertyToken)token;

                if (pt.PropertyName == OutputProperties.NewLinePropertyName)
                {
                    continue;
                }

                if (pt.PropertyName == OutputProperties.ExceptionPropertyName)
                {
                    output.Write(logEvent.Exception == null ? "" : logEvent.Exception + Environment.NewLine);
                    continue;
                }

                if (pt.PropertyName == OutputProperties.MessagePropertyName)
                {
                    RenderMessageTemplate(logEvent.MessageTemplate, logEvent.Properties, output);
                    continue;
                }

                if (pt.PropertyName == OutputProperties.TimestampPropertyName)
                {
                    ScalarValue scalarValue = new ScalarValue(logEvent.Timestamp);
                    scalarValue.Render(output, pt.Format, _formatProvider);
                    continue;
                }

                if (pt.PropertyName == "SourceContext")
                {
                    RenderSourceContext(logEvent, output);
                }
            }
        }

        /// <summary>
        /// Simply write the text to output when it's just text.
        /// </summary>
        /// <param name="textToken"></param>
        /// <param name="output"></param>
        private void RenderTextToken(TextToken textToken, TextWriter output)
        {
            output.Write(textToken.Text);
        }

        /// <summary>
        /// Render the message according to the template, including all values in it (structures, scalars, ...).
        /// </summary>
        /// <param name="messageTemplate">The template to use to render the message</param>
        /// <param name="properties"> The properties in the message.</param>
        /// <param name="output"> The output.</param>
        private void RenderMessageTemplate(MessageTemplate messageTemplate, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            foreach (MessageTemplateToken token in messageTemplate.Tokens)
            {
                if (token is TextToken tt)
                {
                    RenderTextToken(tt, output);
                    continue;
                }

                PropertyToken pt = (PropertyToken)token;

                // InfoLog property is only used to color SourceContext, see RenderSourceContext method.
                if (pt.PropertyName == "InfoLog")
                {
                    continue;
                }

                RenderPropertyToken(pt, properties, output);
            }
        }

        private void RenderPropertyToken(PropertyToken pt, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            if (!properties.TryGetValue(pt.PropertyName, out LogEventPropertyValue propertyValue))
            {
                return;
            }

            // Render a property value using the JsonValueFormatter, which include scalar values(int, string, ..), or structures.
            JsonValueFormatter.Format(propertyValue, output);
        }

        private string Colorize(string text, Logs logs = Logs.Generic)
        {
            string color = LogColors.GetLogColor(logs);
            return text.Colorize(color);
        }

        /// <summary>
        /// Add brackets around the source context, and colors it depending on the value of InfoLog.
        /// </summary>
        private void RenderSourceContext(LogEvent logEvent, TextWriter output)
        {
            logEvent.Properties.TryGetValue("InfoLog", out LogEventPropertyValue InfoLogPropertyValue);
            ScalarValue ScalarInfoLogPropertyValue = InfoLogPropertyValue as ScalarValue;

            logEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue SourceContextPropertyValue);
            ScalarValue ScalarSourceContextPropertyValue = SourceContextPropertyValue as ScalarValue;

            if (ScalarSourceContextPropertyValue?.Value is string sourceContext)
            {
                if (ScalarInfoLogPropertyValue?.Value is Logs InfoLogs)
                {
                    sourceContext = $"[{Colorize(sourceContext, InfoLogs)}]";
                }
                else
                {
                    sourceContext = $"[{Colorize(sourceContext)}]";
                }

                output.Write(sourceContext);
            }
        }
    }
}
