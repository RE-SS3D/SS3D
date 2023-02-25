using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ColourTextFormatter : MessageTemplateTextFormatter
{
    public ColourTextFormatter(string outputTemplate, IFormatProvider? formatProvider = null) : base(outputTemplate, formatProvider)
    {
        
    }

    public new void Format(LogEvent logEvent, TextWriter output)
    {
        base.Format(logEvent, output);
        
        // use properties in logEvent to format the Unity console stuff.
        throw new System.NotImplementedException();
    }

}
