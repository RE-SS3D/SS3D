using NUnit.Framework;
using System.Collections.Generic;
using System.Text;

namespace AssetAudit
{
    /// <summary>
    /// Structural enforcement for Documents/architecture/2026-07_asset-file-structure-taxonomy.md.
    /// These are plain filesystem checks (no asset loading), so they fail fast in CI on any NEW
    /// violation. Pre-existing debt is grandfathered in AssetAuditUtilities and should shrink as
    /// the taxonomy doc's phases ship, never grow to accommodate a fresh violation.
    /// </summary>
    public class AssetTaxonomyTests
    {
        #region Tests

        /// <summary>
        /// Raw art file types (images, audio, models, fonts) belong under Assets/Art/, not
        /// Assets/Content/ — Content is data and composition, not source art.
        /// </summary>
        [Test]
        public void ContentFolderContainsNoUngrandfatheredRawArtFiles()
        {
            List<string> violations = AssetAuditUtilities.GetContentRawArtViolations();
            StringBuilder sb = new();
            foreach (string path in violations)
            {
                sb.Append($"-> '{path}' is a raw art file type under Assets/Content/. Move it under Assets/Art/ " +
                    "(see Documents/architecture/2026-07_asset-file-structure-taxonomy.md).\n");
            }

            Assert.IsTrue(violations.Count == 0, sb.ToString());
        }

        /// <summary>
        /// Icon images (SVG, or PNG with "icon" in the path) belong under Assets/Art/Icons/ —
        /// never under Graphics/, Content/Systems/*, or anywhere else.
        /// </summary>
        [Test]
        public void IconImagesLiveUnderArtIcons()
        {
            List<string> violations = AssetAuditUtilities.GetScatteredIconViolations();
            StringBuilder sb = new();
            foreach (string path in violations)
            {
                sb.Append($"-> '{path}' is an icon image outside Assets/Art/Icons/ " +
                    "(see Documents/architecture/2026-07_asset-file-structure-taxonomy.md).\n");
            }

            Assert.IsTrue(violations.Count == 0, sb.ToString());
        }

        /// <summary>
        /// "Misc" is banned as a folder name going forward — every existing one is grandfathered
        /// pending disposition, but no new one should appear.
        /// </summary>
        [Test]
        public void NoUndocumentedMiscFolders()
        {
            List<string> violations = AssetAuditUtilities.GetUndocumentedMiscFolders();
            StringBuilder sb = new();
            foreach (string path in violations)
            {
                sb.Append($"-> '{path}' is a new \"Misc\" folder. Give the category a real name or " +
                    "recategorize its contents (see Documents/architecture/2026-07_asset-file-structure-taxonomy.md).\n");
            }

            Assert.IsTrue(violations.Count == 0, sb.ToString());
        }

        /// <summary>
        /// First-party C# lives under Assets/Scripts/ with an SS3D.* asmdef; vendored packages
        /// are exempt.
        /// </summary>
        [Test]
        public void FirstPartyAsmdefsLiveUnderScripts()
        {
            List<string> violations = AssetAuditUtilities.GetAsmdefsOutsideScripts();
            StringBuilder sb = new();
            foreach (string path in violations)
            {
                sb.Append($"-> '{path}' is a first-party asmdef outside Assets/Scripts/ " +
                    "(see Documents/architecture/2026-07_asset-file-structure-taxonomy.md).\n");
            }

            Assert.IsTrue(violations.Count == 0, sb.ToString());
        }

        #endregion
    }
}
