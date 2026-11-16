using System;
using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Roles;
using UnityEngine;
using Cloth = SS3D.Systems.Inventory.Containers.Cloth;
using Object = UnityEngine.Object;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The live character preview behind the lobby card and the character window (lobby effort Phase 5): the local
    /// booth, what it shows (body, look, and the High job's worn clothes, Decision 14) and the head-crop portraits the
    /// switchers show. <see cref="LobbyUiSubSystem"/> owns it and calls <see cref="Tick"/> each frame.
    /// </summary>
    public sealed class LobbyCharacterPreview
    {
        private const int PortraitSize = 128;

        private readonly LobbyAssetCatalog _catalog;
        private readonly Dictionary<string, RenderTexture> _portraits = new();
        private CharacterPreviewBooth _booth;
        private string _characterId;
        private bool _portraitDirty;
        private int _portraitDirtyFrame;

        /// <summary>A portrait was (re)captured; switchers repaint.</summary>
        public event Action PortraitCaptured;

        public LobbyCharacterPreview(LobbyAssetCatalog catalog)
        {
            _catalog = catalog;
        }

        public Texture Texture => _booth != null ? _booth.PreviewTexture : null;

        /// <summary>A rendered head crop for the switcher, or null for the silhouette.</summary>
        public Texture PortraitOf(string id) =>
            id != null && _portraits.TryGetValue(id, out RenderTexture portrait) ? portrait : null;

        public void Tick()
        {
            if (_portraitDirty) TryCapturePortrait();
        }

        /// <summary>Creates the booth once, or wakes it; false without a preview texture (no human prefab).</summary>
        public bool Ensure()
        {
            if (_booth != null)
            {
                _booth.SetActive(true);
                return _booth.PreviewTexture != null;
            }

            GameObject prefab = _catalog != null ? _catalog.PreviewHumanPrefab : null;
            if (prefab == null)
            {
                return false;
            }

            GameObject host = new("CharacterPreviewBooth");
            Object.DontDestroyOnLoad(host);
            _booth = host.AddComponent<CharacterPreviewBooth>();
            _booth.Initialize(prefab);
            return _booth.PreviewTexture != null;
        }

        public void SetActive(bool active)
        {
            if (_booth != null) _booth.SetActive(active);
        }

        public void Dispose()
        {
            foreach (RenderTexture portrait in _portraits.Values)
            {
                if (portrait == null) continue;
                portrait.Release();
                Object.Destroy(portrait);
            }

            _portraits.Clear();
            if (_booth == null) return;
            _booth.DisposeBooth();
            _booth = null;
        }

        public void ForgetPortrait(string id)
        {
            if (id == null || !_portraits.Remove(id, out RenderTexture portrait) || portrait == null) return;
            portrait.Release();
            Object.Destroy(portrait);
        }

        /// <summary>Shows <paramref name="character"/>: body, look and its High job's clothes (the fallback job's when none).</summary>
        public void ShowCharacter(SavedCharacter character)
        {
            if (character == null) return;
            ApplyBody(character);
            ApplyLook(character);
            string fallback = SubSystems.TryGet(out RoleSubSystem roles) ? roles.FallbackJob : "Assistant";
            ApplyOutfit(character.Preferences?.HighJob ?? fallback, true);
        }

        /// <summary>0 = Front, 1 = Side, 2 = Back.</summary>
        public void SetAngle(int angleIndex)
        {
            MarkPortraitDirty();
            if (_booth != null) _booth.SetAngleIndex(angleIndex);
        }

        public void ApplyBody(SavedCharacter character)
        {
            if (character == null) return;
            _characterId = character.Id;
            MarkPortraitDirty();
            if (_booth == null) return;
            _booth.ApplyBodyMorphs(
                female: character.GetMorph("female", 0f),
                breasts: character.GetMorph("breasts", 0f),
                fat: character.GetMorph("fat", 0f),
                muscle: character.GetMorph("muscle", 0f),
                jaw: character.GetMorph("jaw", 0.5f),
                height: character.GetMorph("height", 0.5f));
        }

        /// <summary>Hair, facial hair, eyebrows, skin and eyes.</summary>
        public void ApplyLook(SavedCharacter character)
        {
            if (character == null) return;
            _characterId = character.Id;
            MarkPortraitDirty();
            if (_booth == null || _catalog == null) return;
            _booth.ApplyStyle(_catalog.GetHairStyle(character.HairStyleIndex), _catalog.GetBeardStyle(character.BeardStyleIndex),
                _catalog.GetHairColor(character.HairColorIndex), _catalog.GetHairColor(character.BeardColorIndex));
            _booth.ApplyFace(_catalog.GetSkinColor(character.SkinColorIndex), _catalog.GetEyeColor(character.EyeColorIndex),
                _catalog.GetEyebrowStyle(CharacterAppearance.NormalizeEyebrowStyle(character.Eyebrows)),
                _catalog.GetHairColor(character.EyebrowColorIndex));
        }

        /// <summary>
        /// Dresses the booth in <paramref name="job"/>'s worn loadout, or strips it (Decision 14). Without the role
        /// assets (not connected yet) it falls back to the catalog's first preview outfit.
        /// </summary>
        public void ApplyOutfit(string job, bool showClothing)
        {
            MarkPortraitDirty();
            if (_booth == null) return;
            List<Cloth> worn = new();
            if (showClothing)
            {
                worn = WornClothesOf(job);
                if (worn.Count == 0 && _catalog != null && _catalog.PreviewOutfits.Count > 0)
                {
                    LobbyPreviewOutfit outfit = _catalog.GetPreviewOutfit(0);
                    worn.Add(outfit.Jumpsuit.GetComponent<Cloth>());
                    worn.Add(outfit.Shoes.GetComponent<Cloth>());
                }
            }

            _booth.ApplyOutfit(worn);
        }

        /// <summary>Every item in <paramref name="job"/>'s loadout equipment that shows on the body.</summary>
        private static List<Cloth> WornClothesOf(string job)
        {
            List<Cloth> worn = new();
            if (!SubSystems.TryGet(out RoleSubSystem roles)) return worn;
            foreach (GameObject prefab in roles.LoadoutEquipmentOf(job))
            {
                if (prefab.TryGetComponent(out Cloth cloth)) worn.Add(cloth);
            }

            return worn;
        }

        /// <summary>Recapture the shown character's portrait once the booth renders the change.</summary>
        private void MarkPortraitDirty()
        {
            _portraitDirty = true;
            _portraitDirtyFrame = Time.frameCount;
        }

        private void TryCapturePortrait()
        {
            // The booth camera renders at end of frame: wait one full frame after the change.
            if (_booth == null || Time.frameCount <= _portraitDirtyFrame + 1 || string.IsNullOrEmpty(_characterId)) return;
            if (!_portraits.TryGetValue(_characterId, out RenderTexture portrait) || portrait == null)
            {
                portrait = new RenderTexture(PortraitSize, PortraitSize, 0, RenderTextureFormat.ARGB32)
                {
                    name = "CharacterPortraitRT",
                    filterMode = FilterMode.Bilinear,
                };
                portrait.Create();
                _portraits[_characterId] = portrait;
            }

            // Side/Back angles keep the flag set until the preview returns to Front.
            if (!_booth.TryCapturePortrait(portrait)) return;
            _portraitDirty = false;
            PortraitCaptured?.Invoke();
        }
    }
}
