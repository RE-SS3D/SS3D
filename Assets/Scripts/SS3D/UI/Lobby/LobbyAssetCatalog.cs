using System;
using System.Collections.Generic;
using SS3D.Systems.Entities.Character;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Committed resolved refs for the pre-round lobby UITK shell. Rebuild via
    /// <c>SS3D → Data → Rebuild All UI Catalogs</c>.
    /// </summary>
    [CreateAssetMenu(
        fileName = LobbyAssetPaths.ResourcesCatalogName,
        menuName = "SS3D/UI/Lobby Asset Catalog")]
    public sealed class LobbyAssetCatalog : ScriptableObject, IHumanoidStyleSource
    {
        [SerializeField] private StyleSheet _lobbyStyle;

        /// <summary>The lobby shell (<c>Lobby.uxml</c>, styled by <c>Lobby.uss</c>), lobby effort Phase 4.</summary>
        [SerializeField] private VisualTreeAsset _lobbyTemplate;

        /// <summary>The character window (<c>CharacterWindow.uxml</c>, styled by <c>CharacterWindow.uss</c>), lobby effort Phase 5.</summary>
        [SerializeField] private VisualTreeAsset _characterWindowTemplate;
        [SerializeField] private Sprite _previewPlaceholder;
        [SerializeField] private Sprite _serverInfoBanner;
        [SerializeField] private Texture2D _chevronDown;
        [SerializeField] private GameObject _previewHumanPrefab;
        [SerializeField] private List<LobbyNamedSprite> _jobIcons = new();
        [SerializeField] private List<LobbyNamedTexture> _departmentIcons = new();
        [SerializeField] private List<LobbyNamedSprite> _loadoutThumbs = new();

        /// <summary>Ordered head-hair prefabs. Index 0 = None (no prefab).</summary>
        [SerializeField] private List<LobbyNamedPrefab> _hairStyles = new();

        /// <summary>Ordered facial-hair prefabs. Index 0 = None.</summary>
        [SerializeField] private List<LobbyNamedPrefab> _beardStyles = new();
        [SerializeField] private List<LobbyNamedPrefab> _eyebrowStyles = new();

        /// <summary>
        /// Baked head renders for the style grids, keyed "hair-&lt;id&gt;", "beard-&lt;id&gt;" or "eyebrows-&lt;id&gt;"
        /// (<see cref="LobbyAssetPaths.StyleThumbnailRoot"/>, SS3D → Data → Bake Character Style Thumbnails).
        /// </summary>
        [SerializeField] private List<LobbyNamedSprite> _styleThumbnails = new();

        /// <summary>Hair / skin tint colours shown in the Body colour pickers.</summary>
        [SerializeField] private List<Color> _hairColors = new();

        private static readonly System.Random PreviewOutfitRandom = new();
        [SerializeField] private List<LobbyPreviewOutfit> _previewOutfits = new();
        [SerializeField] private CharacterNameList _characterNames;

        /// <summary>Random character names (<see cref="CharacterNameList.AssetPath"/>); null in an unbuilt catalog.</summary>
        public CharacterNameList CharacterNames => _characterNames;

        public IReadOnlyList<LobbyPreviewOutfit> PreviewOutfits => _previewOutfits;

        public int ChoosePreviewOutfitIndex() => PreviewOutfitRandom.Next(_previewOutfits.Count);

        public LobbyPreviewOutfit GetPreviewOutfit(int index) =>
            _previewOutfits[Mathf.Clamp(index, 0, _previewOutfits.Count - 1)];

        /// <summary>The character creator's stylesheet (<c>LobbyShell.uss</c>) until the Phase 5 character window.</summary>
        public StyleSheet LobbyStyle => _lobbyStyle;

        public VisualTreeAsset LobbyTemplate => _lobbyTemplate;

        public VisualTreeAsset CharacterWindowTemplate => _characterWindowTemplate;

        public Sprite PreviewPlaceholder => _previewPlaceholder;

        public Sprite ServerInfoBanner => _serverInfoBanner;

        public Texture2D ChevronDown => _chevronDown;

        public GameObject PreviewHumanPrefab => _previewHumanPrefab;

        public IReadOnlyList<LobbyNamedPrefab> HairStyles => _hairStyles;

        public IReadOnlyList<LobbyNamedPrefab> BeardStyles => _beardStyles;

        public IReadOnlyList<LobbyNamedPrefab> EyebrowStyles => _eyebrowStyles;

        public GameObject GetEyebrowStyle(string id)
        {
            foreach (LobbyNamedPrefab style in _eyebrowStyles)
                if (style.Id == id) return style.Prefab;
            return null;
        }

        public IReadOnlyList<Color> HairColors => _hairColors;

        /// <summary>The baked thumbnail of a style: <paramref name="kind"/> is hair, beard or eyebrows; "none" is the bare head.</summary>
        public bool TryGetStyleThumbnail(string kind, string id, out Sprite sprite) =>
            TryGetNamedSprite(_styleThumbnails, StyleThumbnailKey(kind, id), out sprite);

        public static string StyleThumbnailKey(string kind, string id) => kind + "-" + (id ?? "none").ToLowerInvariant();

        public bool TryGetJobIcon(string id, out Sprite sprite) => TryGetNamedSprite(_jobIcons, id, out sprite);

        public bool TryGetDepartmentIcon(string id, out Texture2D texture) =>
            TryGetNamedTexture(_departmentIcons, id, out texture);

        public bool TryGetLoadoutThumb(string id, out Sprite sprite) =>
            TryGetNamedSprite(_loadoutThumbs, id, out sprite);

        public GameObject GetHairStyle(int index)
        {
            if (_hairStyles == null || index <= 0 || index >= _hairStyles.Count)
            {
                return null;
            }

            return _hairStyles[index].Prefab;
        }

        public GameObject GetBeardStyle(int index)
        {
            if (_beardStyles == null || index <= 0 || index >= _beardStyles.Count)
            {
                return null;
            }

            return _beardStyles[index].Prefab;
        }

        public Color GetHairColor(int index)
        {
            if (_hairColors == null || _hairColors.Count == 0)
            {
                return new Color(0.5f, 0.28f, 0.19f);
            }

            return _hairColors[Mathf.Clamp(index, 0, _hairColors.Count - 1)];
        }

        public bool HasRequiredAssets(out string missingField)
        {
            if (_lobbyStyle == null)
            {
                missingField = "lobbyStyle";
                return false;
            }

            if (_lobbyTemplate == null)
            {
                missingField = "lobbyTemplate";
                return false;
            }

            if (_characterWindowTemplate == null)
            {
                missingField = "characterWindowTemplate";
                return false;
            }

            if (_previewPlaceholder == null)
            {
                missingField = "previewPlaceholder";
                return false;
            }

            if (_serverInfoBanner == null)
            {
                missingField = "serverInfoBanner";
                return false;
            }

            if (_chevronDown == null)
            {
                missingField = "chevronDown";
                return false;
            }

            if (_previewHumanPrefab == null)
            {
                missingField = "previewHumanPrefab";
                return false;
            }

            if (_previewOutfits == null || _previewOutfits.Count == 0)
            {
                missingField = "previewOutfits";
                return false;
            }
            foreach (LobbyPreviewOutfit outfit in _previewOutfits)
            {
                if (outfit == null || outfit.Jumpsuit == null || outfit.Shoes == null)
                {
                    missingField = "previewOutfit clothing";
                    return false;
                }
            }
            missingField = null;
            return true;
        }

#if UNITY_EDITOR
        public void EditorAssign(
            StyleSheet lobbyStyle,
            Sprite previewPlaceholder,
            Sprite serverInfoBanner,
            Texture2D chevronDown,
            GameObject previewHumanPrefab,
            List<LobbyNamedSprite> jobIcons,
            List<LobbyNamedTexture> departmentIcons,
            List<LobbyNamedSprite> loadoutThumbs,
            List<LobbyNamedPrefab> hairStyles,
            List<LobbyNamedPrefab> beardStyles,
            List<LobbyNamedPrefab> eyebrowStyles,
            List<Color> hairColors,
            List<LobbyPreviewOutfit> previewOutfits,
            CharacterNameList characterNames,
            VisualTreeAsset lobbyTemplate,
            VisualTreeAsset characterWindowTemplate,
            List<LobbyNamedSprite> styleThumbnails)
        {
            _styleThumbnails = styleThumbnails ?? new List<LobbyNamedSprite>();
            _lobbyStyle = lobbyStyle;
            _lobbyTemplate = lobbyTemplate;
            _characterWindowTemplate = characterWindowTemplate;
            _previewPlaceholder = previewPlaceholder;
            _serverInfoBanner = serverInfoBanner;
            _chevronDown = chevronDown;
            _previewHumanPrefab = previewHumanPrefab;
            _jobIcons = jobIcons ?? new List<LobbyNamedSprite>();
            _departmentIcons = departmentIcons ?? new List<LobbyNamedTexture>();
            _loadoutThumbs = loadoutThumbs ?? new List<LobbyNamedSprite>();
            _hairStyles = hairStyles ?? new List<LobbyNamedPrefab>();
            _beardStyles = beardStyles ?? new List<LobbyNamedPrefab>();
            _eyebrowStyles = eyebrowStyles ?? new List<LobbyNamedPrefab>();
            _hairColors = hairColors ?? new List<Color>();
            _previewOutfits = previewOutfits ?? new List<LobbyPreviewOutfit>();
            _characterNames = characterNames;
        }
#endif

        private static bool TryGetNamedSprite(List<LobbyNamedSprite> list, string id, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(id) || list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                LobbyNamedSprite entry = list[i];
                if (entry != null && entry.Id == id && entry.Sprite != null)
                {
                    sprite = entry.Sprite;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetNamedTexture(List<LobbyNamedTexture> list, string id, out Texture2D texture)
        {
            texture = null;
            if (string.IsNullOrEmpty(id) || list == null)
            {
                return false;
            }

            for (int i = 0; i < list.Count; i++)
            {
                LobbyNamedTexture entry = list[i];
                if (entry != null && entry.Id == id && entry.Texture != null)
                {
                    texture = entry.Texture;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Existing clothing assets used as read-only worn-mesh sources for the local booth.</summary>
    [Serializable]
    public sealed class LobbyPreviewOutfit
    {
        public string Id;
        public GameObject Jumpsuit;
        public GameObject Shoes;
    }

    [Serializable]
    public sealed class LobbyNamedPrefab
    {
        public string Id;
        public GameObject Prefab;

        public LobbyNamedPrefab()
        {
        }

        public LobbyNamedPrefab(string id, GameObject prefab)
        {
            Id = id;
            Prefab = prefab;
        }
    }

    [Serializable]
    public sealed class LobbyNamedSprite
    {
        public string Id;
        public Sprite Sprite;

        public LobbyNamedSprite()
        {
        }

        public LobbyNamedSprite(string id, Sprite sprite)
        {
            Id = id;
            Sprite = sprite;
        }
    }

    [Serializable]
    public sealed class LobbyNamedTexture
    {
        public string Id;
        public Texture2D Texture;

        public LobbyNamedTexture()
        {
        }

        public LobbyNamedTexture(string id, Texture2D texture)
        {
            Id = id;
            Texture = texture;
        }
    }
}
