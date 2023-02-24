using Serilog.Events;
using Serilog.Formatting;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ColourTextFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        // use properties in logEvent to format the Unity console stuff.
        throw new System.NotImplementedException();
    }

}
