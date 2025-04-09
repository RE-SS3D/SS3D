using System.Collections.Generic;

namespace SS3D.Logging
{
    public static class LogColors
    {
        public const string Blue = "#90C4FE";
        public const string Green = "#88B36B";
        public const string Red = "#8D3131";

        private const string UndefinedLogColor = "#FFFFFF";

        private static readonly Dictionary<Logs, string> Colors = new()
        {
            {
                Logs.None, "#FFFFFF"
            },
            {
                Logs.Generic, "#FFFFFF"
            },
            {
                Logs.Important, "#8D3131"
            },
            {
                Logs.ServerOnly, "#A645A6"
            },
            {
                Logs.External, "#E6DC33"
            },
            {
                Logs.ClientOnly, "#B94949"
            },
            {
                Logs.Physics, "#678DB8"
            },
        };

        public static string GetLogColor(Logs logs)
        {
            Colors.TryGetValue(logs, out string color);

            return color != string.Empty ? color : UndefinedLogColor;
        }
    }
}