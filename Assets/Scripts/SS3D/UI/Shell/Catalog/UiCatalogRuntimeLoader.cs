using UnityEngine;

namespace SS3D.UI.Shell.Catalog
{
    /// <summary>
    /// Shared <c>Resources.Load</c> + required-assets check for <see cref="UiAssetCatalogBase"/>
    /// catalogs, so each host doesn't hand-write the same load-and-validate boilerplate.
    /// </summary>
    public static class UiCatalogRuntimeLoader
    {
        public static bool TryLoad<TCatalog>(string resourcesName, string rebuildMenuPath, Object context, out TCatalog catalog)
            where TCatalog : UiAssetCatalogBase
        {
            catalog = Resources.Load<TCatalog>(resourcesName);
            if (catalog == null)
            {
                Debug.LogError(
                    $"Could not load Resources/{resourcesName}. Run {rebuildMenuPath} and commit the asset.",
                    context);
                return false;
            }

            if (!catalog.HasRequiredAssets(out string missingField))
            {
                Debug.LogError(
                    $"{resourcesName} is missing required assets ({missingField}). Run {rebuildMenuPath}.",
                    context);
                catalog = null;
                return false;
            }

            return true;
        }
    }
}
