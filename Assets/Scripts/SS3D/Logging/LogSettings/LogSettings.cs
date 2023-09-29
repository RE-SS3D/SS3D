using Coimbra;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Serilog.Events;
using System.Reflection;

namespace SS3D.Logging.LogSettings
{
	/// <summary>
    /// Allow user to create a scriptable object representing settings for the log, in particular,
    /// allows to set in inspector the logging level for each namespace.
    /// </summary>
    [CreateAssetMenu(menuName = "ScriptableObjects/LogSettings", order = 1)]
	[ProjectSettings("SS3D")]
    public sealed class LogSettings : ScriptableSettings
    {
	    [SerializeField]
        public List<NamespaceLogLevel> SS3DNameSpaces = GetAllSs3DNamespaces();
        public LogEventLevel defaultLogLevel = LogEventLevel.Verbose;

        /// <summary>
        /// Get all the name of the SS3D namespaces in alphanumerical order.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        private static List<NamespaceLogLevel> GetAllSs3DNamespaces()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<NamespaceLogLevel> projectNamespaces = new List<NamespaceLogLevel>();
            int i = 0;
            foreach (string type in assemblies.Select(assembly => assembly.GetTypes().Select(t => t.Namespace).Distinct()).SelectMany(namespaces => namespaces.Where(type => type != null && type.Contains("SS3D"))))
            {
	            projectNamespaces.Add(new NamespaceLogLevel(type, LogEventLevel.Information));
	            i++;
            }

            return projectNamespaces.OrderBy(o => o.Name).ToList();
        }
    }

	// structure to associate to each namespace a log level.
	[Serializable]
	public struct NamespaceLogLevel
	{
		public NamespaceLogLevel(string name, LogEventLevel level)
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
}
