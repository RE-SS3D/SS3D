using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
	[SetUpFixture]
	public class SetUpTest
	{
		private const string QolAttributesDefine = "DISABLE_QOL_ATTRIBUTES";

		[OneTimeSetUp]
		public void RunBeforeAnyTests()
		{
			bool result = RemoveOrAddDefine(QolAttributesDefine, true);
		}

		[OneTimeTearDown]
		public void RunAfterAnyTests()
		{
			bool result = RemoveOrAddDefine(QolAttributesDefine, false);
		}

		private static bool RemoveOrAddDefine(string define, bool removeDefine)
		{
			string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
			HashSet<string> definesHs = new HashSet<string>();
			string[] currentArr = currentDefines.Split(';');

			//Add any define which doesn't contain MIRROR.
			foreach (string item in currentArr)
				definesHs.Add(item);

			int startingCount = definesHs.Count;

			if (removeDefine)
				definesHs.Remove(define);
			else
				definesHs.Add(define);

			bool modified = (definesHs.Count != startingCount);
			if (modified)
			{
				string changedDefines = string.Join(";", definesHs);
				PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, changedDefines);
			}

			return modified;
		}
	}
}
