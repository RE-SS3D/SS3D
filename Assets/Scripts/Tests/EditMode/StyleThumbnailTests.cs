using NUnit.Framework;
using SS3D.Systems.Entities.Character;
using SS3D.UI.Lobby;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Every hair, facial hair and eyebrow style in the lobby catalog has a baked thumbnail for the character window's
    /// grids. A new style without one: run SS3D → Data → Bake Character Style Thumbnails.
    /// </summary>
    public sealed class StyleThumbnailTests
    {
        [Test]
        public void EveryStyle_HasABakedThumbnail()
        {
            LobbyAssetCatalog catalog = Resources.Load<LobbyAssetCatalog>(LobbyAssetPaths.ResourcesCatalogName);
            Assert.IsNotNull(catalog);
            foreach (LobbyNamedPrefab style in catalog.HairStyles)
                Assert.IsTrue(catalog.TryGetStyleThumbnail("hair", style.Id, out _), "hair " + style.Id);
            foreach (LobbyNamedPrefab style in catalog.BeardStyles)
                Assert.IsTrue(catalog.TryGetStyleThumbnail("beard", style.Id, out _), "beard " + style.Id);
            Assert.IsTrue(catalog.TryGetStyleThumbnail("eyebrows", "none", out _), "eyebrows none");
            foreach (LobbyNamedPrefab style in catalog.EyebrowStyles)
                Assert.IsTrue(catalog.TryGetStyleThumbnail("eyebrows", CharacterAppearance.NormalizeEyebrowStyle(style.Id), out _),
                    "eyebrows " + style.Id);
        }
    }
}
