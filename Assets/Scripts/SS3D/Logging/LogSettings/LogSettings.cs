using Coimbra;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Serilog.Events;
using System.Reflection;
using UnityEngine.Serialization;

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
	    public List<NamespaceLogLevel> SS3DNameSpaces = GetAllSs3DNamespaces();

	    [FormerlySerializedAs("defaultLogLevel")]
	    public LogEventLevel DefaultLogLevel = LogEventLevel.Verbose;

        /// <summary>
        /// When true, Serilog's file sink writes compact JSON (<c>.json</c>); when false, plain text (<c>.log</c>).
        /// Defaults to true — the multiplayer harness tails <c>LogServer.json</c>/<c>LogClient*.json</c>, and the C#
        /// field default is what Unity writes back if it re-serializes this Project Settings asset on open
        /// (Coimbra <c>OnValidate</c> marks it dirty). A <c>false</c> default was silently reverting the
        /// asset to plain-text logs every Editor launch.
        /// </summary>
        public bool UseCompactJsonFormatter = true;

        /// <summary>
        /// Get all the name of the SS3D namespaces in alphanumerical order.
        /// </summary>
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
}
