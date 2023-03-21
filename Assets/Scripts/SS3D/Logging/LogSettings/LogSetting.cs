using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Serilog.Events;

namespace SS3D.Logging.LogSettings
{

    /// <summary>
    /// Allow user to create a scriptable object representing settings for the log, in particular,
    /// allows to set in inspector the logging level for each namespace.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/LogSettings", order = 1)]
    public class LogSetting : ScriptableObject
    {
        // structure to associate to each namespace a log level.
        [Serializable]
        public struct NameSpaceLogLevel
        {
            public NameSpaceLogLevel(string name, LogEventLevel level)
            {
                _name = name;
                _level = level;
            }

            [SerializeField]
            private string _name;
            [SerializeField]
            private LogEventLevel _level;

            public string Name => _name;
            public LogEventLevel Level => _level;
        }

        [SerializeField]
        public List<NameSpaceLogLevel> SS3DNameSpaces = GetAllNameOfSS3DNameSpace();
        public LogEventLevel defaultLogLevel = LogEventLevel.Verbose;

        /// <summary>
        /// Get all the name of the SS3D namespaces in alphanumerical order.
        /// </summary>
        /// <returns></returns>
        public static List<NameSpaceLogLevel> GetAllNameOfSS3DNameSpace()
        {
            var Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var SS3DNameSpaces = new List<NameSpaceLogLevel>();
            int i = 0;
            foreach (var assembly in Assemblies)
            {
                var namespaces = assembly.GetTypes()
                                .Select(t => t.Namespace)
                                .Distinct();


                foreach (var type in namespaces)
                {
                    if (type == null || !type.Contains("SS3D"))
                    {
                        continue;
                    }
                    SS3DNameSpaces.Add(new NameSpaceLogLevel(type, LogEventLevel.Information));
                    i++;
                }
            }

            return SS3DNameSpaces.OrderBy(o => o.Name).ToList();
        }
    }
}
