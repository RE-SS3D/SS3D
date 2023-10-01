using Serilog.Events;
using System;
using UnityEngine;

namespace SS3D.Logging.LogSettings
{
	[Serializable]
	public struct NamespaceLogLevel
	{
		[SerializeField]
		private string _name;

		[SerializeField]
		private LogEventLevel _level;

		public NamespaceLogLevel(string name, LogEventLevel level)
		{
			_name = name;
			_level = level;
		}

		public string Name => _name;

		public LogEventLevel Level => _level;
	}
}