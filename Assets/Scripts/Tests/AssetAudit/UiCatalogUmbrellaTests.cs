using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SS3D.Tests.AssetAudit
{
    /// <summary>
    /// Guardrail G5 of the tech-debt paydown: every UI path catalog (<c>*AssetCatalog</c> ScriptableObject) is rebuilt by
    /// the one umbrella, <c>SS3D/Data/Rebuild All UI Catalogs</c> (<c>UiCatalogRebuildAll.TryRebuildAll</c>), so no
    /// surface needs its own Rebuild MenuItem (AGENTS.md).
    /// </summary>
    public sealed class UiCatalogUmbrellaTests
    {
        private static string UmbrellaSource =>
            File.ReadAllText(Path.Combine(Application.dataPath, "Scripts", "SS3D", "Editor", "UiCatalogRebuildAll.cs"));

        [Test]
        public void EveryUiAssetCatalog_IsRebuiltByTheUmbrella()
        {
            List<Type> catalogs = AssetCatalogTypes().ToList();
            Assert.That(catalogs.Count, Is.GreaterThan(5), "Found almost no *AssetCatalog types; the scan is broken.");

            string umbrella = UmbrellaSource;
            List<string> missing = catalogs
                .Select(type => type.Name + "Builder")
                .Where(builder => !umbrella.Contains(builder + ".TryRebuildCatalog"))
                .OrderBy(builder => builder)
                .ToList();

            Assert.That(missing, Is.Empty,
                "Call these builders from UiCatalogRebuildAll.TryRebuildAll (and drop any per-surface Rebuild MenuItem):\n  "
                + string.Join("\n  ", missing));
        }

        private static IEnumerable<Type> AssetCatalogTypes()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.StartsWith("SS3D", StringComparison.Ordinal))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (Type type in types)
                {
                    if (!type.IsAbstract && type.Name.EndsWith("AssetCatalog", StringComparison.Ordinal)
                        && typeof(ScriptableObject).IsAssignableFrom(type))
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
