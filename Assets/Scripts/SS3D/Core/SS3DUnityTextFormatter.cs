using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Parsing;
using System;
using System.IO;
using Serilog.Rendering;
using Serilog.Formatting;
using SS3D.Logging;
using SS3D.Utils;
using System.Collections.Generic;
using Serilog.Formatting.Json;
using System.Linq;

/// <summary>
/// Heavily inspired by MessageTemplateTextFormatter.
/// Sadly, since many static methods are internal to Serilog, it was necessary to write an equivalent of them in here.
/// </summary>
public class SS3DUnityTextFormatter : ITextFormatter
{
    private readonly MessageTemplate _outputTemplate;
    private readonly IFormatProvider _formatProvider;
    private static readonly JsonValueFormatter JsonValueFormatter = new("$type");
    private static readonly char[] PaddingChars = Enumerable.Repeat(' ', 80).ToArray();
    public SS3DUnityTextFormatter(string outputTemplate, IFormatProvider? formatProvider = null)
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

        foreach (var token in _outputTemplate.Tokens)
        {
            if (token is TextToken tt)
            {
                RenderTextToken(tt, output);
                continue;
            }

            var pt = (PropertyToken)token;

            if (pt.PropertyName == OutputProperties.NewLinePropertyName)
            {
                ApplyPadding(output, Environment.NewLine, pt.Alignment);
                continue;
            }
            if (pt.PropertyName == OutputProperties.ExceptionPropertyName)
            {
                var exception = logEvent.Exception == null ? "" : logEvent.Exception + Environment.NewLine;
                ApplyPadding(output, exception, pt.Alignment);
                continue;
            }
            if (pt.PropertyName == OutputProperties.MessagePropertyName)
            {
                RenderMessageTemplate(logEvent.MessageTemplate, logEvent.Properties, output, pt.Format, _formatProvider);
                continue;
            }
            if (pt.PropertyName == OutputProperties.TimestampPropertyName)
            {
                var scalarValue = new ScalarValue(logEvent.Timestamp);
                scalarValue.Render(output, pt.Format, _formatProvider);
                continue;
            }
            if (pt.PropertyName == OutputProperties.PropertiesPropertyName)
            {
                RenderPropertyFormat(logEvent.MessageTemplate, logEvent.Properties, _outputTemplate, output, pt.Format, _formatProvider);
                continue;
            }

            // If a property is missing, don't render anything.
            if (!logEvent.Properties.TryGetValue(pt.PropertyName, out var propertyValue))
                continue;

            var sv = propertyValue as ScalarValue;
            if (sv?.Value is string literalString)
            {
                if(pt.PropertyName == "SourceContext")
                {
                    logEvent.Properties.TryGetValue("InfoLog", out var InfoLogPropertyValue);
                    var ScalarInfoLogPropertyValue = InfoLogPropertyValue as ScalarValue;
                    if (ScalarInfoLogPropertyValue?.Value is Logs InfoLogs)
                        literalString = $"[{Colorize(literalString, InfoLogs)}]";
                    else
                        literalString = $"[{Colorize(literalString)}]";
                }
                output.Write(literalString);
            }
            else
            {
                propertyValue.Render(output, pt.Format, _formatProvider);
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
    /// Render the message according to the template.
    /// </summary>
    /// <param name="messageTemplate">The template to use to render the message</param>
    /// <param name="properties"> The properties in the message.</param>
    /// <param name="output"> The output.</param>
    /// <param name="format"></param>
    /// <param name="formatProvider"></param>
    private void RenderMessageTemplate(MessageTemplate messageTemplate, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output, string format = null, IFormatProvider formatProvider = null)
    {
        bool isLiteral = false, isJson = false;

        if (format != null)
        {
            for (var i = 0; i < format.Length; ++i)
            {
                if (format[i] == 'l')
                    isLiteral = true;
                else if (format[i] == 'j')
                    isJson = true;
            }
        }

        foreach (var token in messageTemplate.Tokens)
        {
            if (token is TextToken tt)
            {
                RenderTextToken(tt, output);
                continue;
            }

            var pt = (PropertyToken)token;
            if(pt.PropertyName == "InfoLog")
            {
                continue;
            }
            RenderPropertyToken(pt, properties, output, formatProvider, isLiteral, isJson);
        }
    }

    private void RenderPropertyToken(PropertyToken pt, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output, IFormatProvider? formatProvider, bool isLiteral, bool isJson)
    {
        if (!properties.TryGetValue(pt.PropertyName, out var propertyValue))
        {
            return;
        }

        RenderValue(propertyValue, isLiteral, isJson, output, pt.Format, formatProvider);
    }

    private string Colorize(string text, Logs logs = Logs.Generic)
    {
        string color = LogColors.GetLogColor(logs);
        return text.Colorize(color);
    }

    private void RenderValue(LogEventPropertyValue propertyValue, bool isLiteral, bool isJson, TextWriter output, string format, IFormatProvider formatProvider)
    {
        if (isLiteral && propertyValue is ScalarValue { Value: string str })
        {
            output.Write(str);
        }
        else if (isJson && format == null)
        {
            JsonValueFormatter.Format(propertyValue, output);
        }
        else
        {
            propertyValue.Render(output, format, formatProvider);
        }
    }

    private void RenderPropertyFormat(MessageTemplate template, IReadOnlyDictionary<string, LogEventPropertyValue> properties, MessageTemplate outputTemplate, TextWriter output, string format, IFormatProvider formatProvider = null)
    {
        if (format?.Contains("j") == true)
        {
            var sv = new StructureValue(properties
                .Where(kvp => !(TemplateContainsPropertyName(template, kvp.Key) ||
                                TemplateContainsPropertyName(outputTemplate, kvp.Key)))
                .Select(kvp => new LogEventProperty(kvp.Key, kvp.Value)));
            JsonValueFormatter.Format(sv, output);
            return;
        }

        output.Write("{ ");

        var delim = "";
        foreach (var kvp in properties)
        {
            if (TemplateContainsPropertyName(template, kvp.Key) ||
                TemplateContainsPropertyName(outputTemplate, kvp.Key))
            {
                continue;
            }

            output.Write(delim);
            delim = ", ";
            output.Write(kvp.Key);
            output.Write(": ");
            kvp.Value.Render(output, null, formatProvider);
        }

        output.Write(" }");
    }

    /// <summary>
    /// Check if a given template contains a given property, using its name.
    /// </summary>
    /// <returns>true if the property name is found in the template.</returns>
    private static bool TemplateContainsPropertyName(MessageTemplate template, string propertyName)
    {
        if (template.Tokens != null)
        {
            foreach (var token in template.Tokens)
            {
                if (token is TextToken tt)
                {
                    continue;
                }

                var pt = (PropertyToken)token;
                if (pt.PropertyName == propertyName)
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    private static void ApplyPadding(TextWriter output, string value, Alignment? alignment)
    {
        if (alignment == null || value.Length >= alignment.Value.Width)
        {
            output.Write(value);
            return;
        }

        var pad = alignment.Value.Width - value.Length;

        if (alignment.Value.Direction == AlignmentDirection.Left)
            output.Write(value);

        if (pad <= PaddingChars.Length)
        {
            output.Write(PaddingChars, 0, pad);
        }
        else
        {
            output.Write(new string(' ', pad));
        }

        if (alignment.Value.Direction == AlignmentDirection.Right)
            output.Write(value);
    }
}
