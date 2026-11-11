using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace SS3D.Tests.AssetAudit
{
    /// <summary>
    /// Guardrail G4 of the tech-debt paydown (Documents/architecture/2026-11_tech-debt-paydown.md, Phase 0): dependency
    /// cycles between <c>Systems/*</c> folders. It reads source and ratchets: the stored count may only fall. The point is a
    /// visible diff at review time. Shrinking a number is welcome; raising one needs a reason in the PR.
    /// </summary>
    public sealed class SystemsFolderCycleTests
    {
        /// <summary>
        /// G4: pairs of top-level <c>Systems/*</c> folders whose runtime code (outside <c>Editor/</c>) has a
        /// <c>using SS3D.Systems.&lt;Other&gt;</c> in both directions. 52 on 2026-11-10; 50 on 2026-11-11 after Phase 6. Phases 5, 6 and 8 lower it.
        /// </summary>
        private const int MaxMutualSystemsFolderPairs = 50;

        private static readonly Regex SystemsUsing = new(@"^\s*using\s+(?:\w+\s*=\s*)?SS3D\.Systems\.(\w+)", RegexOptions.Compiled);

        private static string ScriptsRoot => Path.Combine(Application.dataPath, "Scripts", "SS3D");

        [Test]
        public void SystemsFolderCycles_DoNotGrow()
        {
            List<string> pairs = MutualSystemsFolderPairs();
            Assert.That(pairs.Count, Is.LessThanOrEqualTo(MaxMutualSystemsFolderPairs),
                $"G4: {pairs.Count} pairs of Systems folders now depend on each other (ratchet {MaxMutualSystemsFolderPairs}). "
                + "Depend in one direction (an interface in the lower folder, an event), don't add a new cycle. Pairs:\n  "
                + string.Join("\n  ", pairs));
            if (pairs.Count < MaxMutualSystemsFolderPairs)
            {
                Debug.Log($"[G4] Systems folder cycles fell to {pairs.Count}; lower MaxMutualSystemsFolderPairs to lock it in.");
            }
        }

        private static List<string> MutualSystemsFolderPairs()
        {
            string systems = Path.Combine(ScriptsRoot, "Systems");
            HashSet<string> folders = new(Directory.GetDirectories(systems).Select(Path.GetFileName));
            HashSet<(string From, string To)> edges = new();
            foreach (string folder in folders)
            {
                foreach (string file in Directory.EnumerateFiles(Path.Combine(systems, folder), "*.cs", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(systems.Length + 1).Replace('\\', '/');
                    if (relative.Split('/').Contains("Editor"))
                    {
                        continue;
                    }

                    foreach (string line in File.ReadLines(file))
                    {
                        Match match = SystemsUsing.Match(line);
                        if (match.Success && match.Groups[1].Value != folder && folders.Contains(match.Groups[1].Value))
                        {
                            edges.Add((folder, match.Groups[1].Value));
                        }
                    }
                }
            }

            return edges.Where(e => string.CompareOrdinal(e.From, e.To) < 0 && edges.Contains((e.To, e.From)))
                .Select(e => $"{e.From} <-> {e.To}")
                .OrderBy(p => p)
                .ToList();
        }
    }
}
