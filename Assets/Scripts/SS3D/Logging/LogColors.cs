using System.Collections.Generic;

namespace SS3D.Logging
{
    public static class LogColors
    {
        private const string UndefinedLogColor = "#FFFFFF";

        private static readonly Dictionary<LogType, string> Colors = new()
        {
            { LogType.None, "#FFFFFF"},
            { LogType.Generic, "#FFFFFF"},
            { LogType.Important, "#8D3131"},
            { LogType.ServerOnly, "#A645A6"},
            { LogType.External, "#E6DC33"},
        };

        public static string GetLogColor(LogType logType)
        {
            Colors.TryGetValue(logType, out string color);

            return color != string.Empty ? color : UndefinedLogColor;
        } 
    }
}