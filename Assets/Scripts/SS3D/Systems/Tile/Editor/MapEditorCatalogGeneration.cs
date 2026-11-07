#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// What <c>SS3D/Map Editor/Regenerate Catalog</c> writes into <c>MapEditorCatalog.asset</c>. Lives in the
    /// Systems assembly so the EditMode drift test can compare the committed asset against it.
    /// </summary>
    public static class MapEditorCatalogGeneration
    {
        public const string CatalogPath = "Assets/Content/Systems/Tile/MapEditorCatalog.asset";

        /// <summary>
        /// Every tile asset under a <c>Resources</c> folder, filed by <see cref="MapEditorCatalogHeuristics"/>,
        /// in name order. Assets with no sub-tab are skipped with a warning.
        /// </summary>
        /// <exception cref="InvalidOperationException">A variant family is mixed (see <see cref="FamilyProblems"/>).</exception>
        public static List<MapEditorCatalogEntry> BuildEntries()
        {
            List<MapEditorCatalogEntry> entries = new();
            List<FamilyMember> members = new();
            var assets = Resources.LoadAll<GenericObjectSo>("")
                .Select(asset => (Asset: asset, Path: UnityAssetDatabase.GetAssetPath(asset)))
                .OrderBy(pair => pair.Asset.NameString, StringComparer.Ordinal)
                .ThenBy(pair => pair.Path, StringComparer.Ordinal);

            foreach ((GenericObjectSo asset, string path) in assets)
            {
                MapEditorCatalogEntry entry = MapEditorCatalogHeuristics.Infer(asset, path);
                if (entry == null)
                {
                    Debug.LogWarning($"[MapEditorCatalog] {path} has no palette sub-tab; move it or fix its layer.");
                    continue;
                }

                entries.Add(entry);
                string familyFolder = MapEditorCatalogHeuristics.FamilyFolderOf(path);
                if (familyFolder != null)
                    members.Add(new FamilyMember(familyFolder, entry, asset is TileObjectSo tile ? tile.layer : null));
            }

            List<string> problems = FamilyProblems(members);
            if (problems.Count > 0)
                throw new InvalidOperationException("[MapEditorCatalog] Mixed variant families:\n" + string.Join("\n", problems));

            AssignFamilies(members);
            return entries;
        }

        /// <summary>An entry inside a <c>…Variants/</c> folder; <see cref="Layer"/> is null for items.</summary>
        public readonly struct FamilyMember
        {
            public readonly string Folder;
            public readonly MapEditorCatalogEntry Entry;
            public readonly TileLayer? Layer;

            public FamilyMember(string folder, MapEditorCatalogEntry entry, TileLayer? layer)
            {
                Folder = folder;
                Entry = entry;
                Layer = layer;
            }
        }

        /// <summary>
        /// Cycling a variant must never change what a placement occupies: a family's members share
        /// sub-tab, heading and layer, and two family folders in one sub-tab can't share a label.
        /// </summary>
        public static List<string> FamilyProblems(IReadOnlyList<FamilyMember> members)
        {
            List<string> problems = new();
            foreach (IGrouping<string, FamilyMember> family in members.GroupBy(member => member.Folder))
            {
                FamilyMember first = family.First();
                foreach (FamilyMember member in family.Skip(1))
                {
                    if (member.Entry.Subcategory != first.Entry.Subcategory)
                        problems.Add($"{family.Key}: {member.Entry.AssetName} is {member.Entry.Subcategory}, {first.Entry.AssetName} is {first.Entry.Subcategory}.");
                    else if (member.Entry.Heading != first.Entry.Heading)
                        problems.Add($"{family.Key}: {member.Entry.AssetName} heads under {member.Entry.Heading}, {first.Entry.AssetName} under {first.Entry.Heading}.");
                    else if (member.Layer != first.Layer)
                        problems.Add($"{family.Key}: {member.Entry.AssetName} sits on {member.Layer?.ToString() ?? "no layer"}, {first.Entry.AssetName} on {first.Layer?.ToString() ?? "no layer"}.");
                }
            }

            foreach (var clash in members
                         .GroupBy(member => (member.Entry.Subcategory, Label: MapEditorCatalogHeuristics.FamilyLabelOf(member.Folder)))
                         .Where(group => group.Select(member => member.Folder).Distinct().Count() > 1))
            {
                problems.Add($"{clash.Key.Subcategory}: two family folders read \"{clash.Key.Label}\": " +
                    string.Join(", ", clash.Select(member => member.Folder).Distinct()) + ".");
            }

            return problems;
        }

        /// <summary>Bakes <c>Family</c> and <c>VariantLabel</c>; a family folder with one member stays a plain entry.</summary>
        public static void AssignFamilies(IReadOnlyList<FamilyMember> members)
        {
            foreach (IGrouping<string, FamilyMember> family in members.GroupBy(member => member.Folder))
            {
                FamilyMember[] inFamily = family.ToArray();
                if (inFamily.Length < 2)
                    continue;

                string label = MapEditorCatalogHeuristics.FamilyLabelOf(family.Key);
                string[] variants = MapEditorCatalogHeuristics.VariantLabels(inFamily.Select(member => member.Entry.AssetName).ToArray());
                for (int i = 0; i < inFamily.Length; i++)
                {
                    inFamily[i].Entry.Family = label;
                    inFamily[i].Entry.VariantLabel = variants[i];
                }
            }
        }
    }
}
#endif
