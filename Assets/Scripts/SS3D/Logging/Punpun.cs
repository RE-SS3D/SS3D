using SS3D.Utils;
using UnityEngine;

namespace SS3D.Logging
{
    /// <summary>
    /// Custom debugger
    /// </summary>
    public static class Punpun
    {
        /// <summary>
        /// A refined Debug.Log
        /// </summary>
        /// <param name="sender">who sends the message</param>
        /// <param name="message">message</param>
        /// <param name="logType">type of log</param>
        public static void Say(object sender, string message, LogType logType = LogType.Generic, bool colorizeEverything = false)
        {
            Debug.Log(ProcessDebug(sender, message, logType, colorizeEverything));
        }

        /// <summary>
        /// A refined Debug.LogWarning
        /// </summary>
        /// <param name="sender">who sends the message</param>
        /// <param name="message">message</param>
        /// <param name="logType">type of log</param>
        public static void Yell(object sender, string message, LogType logType = LogType.Generic, bool colorizeEverything = false)
        {
            Debug.LogWarning(ProcessDebug(sender, message, logType, colorizeEverything));
        }

        /// <summary>
        /// A refined Debug.LogError
        /// </summary>
        /// <param name="sender">who sends the message</param>
        /// <param name="message">message</param>
        /// <param name="logType">type of log</param>
        public static void Panic(object sender, string message, LogType logType = LogType.Generic, bool colorizeEverything = false)
        {
            Debug.LogError(ProcessDebug(sender, message, logType, colorizeEverything));
        }

        private static string ProcessDebug(object sender, string message, LogType logType = LogType.Generic, bool colorizeEverything = false)
        {
            string color = LogColors.GetLogColor(logType);

            string name = sender.GetType().Name;
            string author = name == "RuntimeType" ? $"{sender}" : $"{name}";
            author = $"[{author}]".Colorize(color);

            if (colorizeEverything)
            {
                message.Colorize(color);
            }

            string log = $"{author} - {message}";

            return log;
        }
    }
}