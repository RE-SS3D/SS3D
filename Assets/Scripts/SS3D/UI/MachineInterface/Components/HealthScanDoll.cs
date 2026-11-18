using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// The scan's body doll ("Vitals Scan Result Spec" design): the SS3D-Art <c>Harm*</c> masks (white, all on
    /// one 296×585 canvas, drawn at 0.75 scale) stacked as tinted layers, with a heart icon on the chest.
    /// Hovering a zone or organ shows a card anchored to it; clicking pins it, as does <see cref="PinZone"/>.
    /// Hit testing reads the masks' alpha, so the Harm textures must be Read/Write enabled.
    /// UI Toolkit has no blur filter, so a zone's glow is a rim: four copies of its mask offset under it.
    /// </summary>
    [UxmlElement]
    public partial class HealthScanDoll : VisualElement
    {
        private const float AlphaHit = 0.5f;
        private const float RimOffset = 1.5f;

        private static readonly Vector2[] RimOffsets =
        {
            new(-RimOffset, 0f), new(RimOffset, 0f), new(0f, -RimOffset), new(0f, RimOffset),
        };

        private readonly List<VisualElement>[] _zoneLayers = new List<VisualElement>[HealthConstants.ZoneCount];
        private readonly List<VisualElement>[] _zoneRims = new List<VisualElement>[HealthConstants.ZoneCount];
        private readonly VisualElement _skull;
        private readonly VisualElement _ribs;
        private readonly VisualElement _lungs;
        private readonly VisualElement _liver;
        private readonly VisualElement _heart;
        private readonly VisualElement _card;
        private readonly Label _cardTitle;
        private readonly Label _cardTag;
        private readonly Label _cardDetail;
        private readonly Label _cardFlags;
        private readonly Target[] _targets;

        private HealthScanViewModel _model;
        private int _hovered = -1;
        private int _pinned = -1;

        public HealthScanDoll()
        {
            AddToClassList("health-scan-doll");

            AddZone(BodyZone.Head, "head");
            AddZone(BodyZone.Chest, "chest");
            AddZone(BodyZone.Groin, "pelvis");
            AddZone(BodyZone.LeftArm, "arm-l1", "arm-l2");
            AddZone(BodyZone.RightArm, "arm-r1", "arm-r2");
            AddZone(BodyZone.LeftLeg, "leg-l1", "leg-l2");
            AddZone(BodyZone.RightLeg, "leg-r1", "leg-r2");

            _skull = AddLayer("skull");
            _ribs = AddLayer("ribs");
            _lungs = AddLayer("lungs");
            _liver = AddLayer("liver");
            _heart = new VisualElement();
            _heart.AddToClassList("health-scan-doll__heart");
            _heart.pickingMode = PickingMode.Ignore;
            Add(_heart);
            AddLayer("outline");

            _card = new VisualElement();
            _card.AddToClassList("health-scan-doll__card");
            _card.pickingMode = PickingMode.Ignore;
            VisualElement header = new();
            header.AddToClassList("health-scan-doll__card-header");
            _cardTitle = CardLabel(header, "health-scan-doll__card-title");
            _cardTag = CardLabel(header, "health-scan-doll__card-tag");
            _card.Add(header);
            _cardDetail = CardLabel(_card, "health-scan-doll__card-detail");
            _cardFlags = CardLabel(_card, "health-scan-doll__card-flags");
            _card.style.display = DisplayStyle.None;
            Add(_card);

            // Organs sit on top of the torso, so they are tested first. Anchors are where the card points,
            // in the doll's 222×439 frame (the design's tooltip positions).
            _targets = new[]
            {
                Target.Organ(TargetKind.Heart, _heart, new Vector2(125f, 118f)),
                Target.Organ(TargetKind.Lungs, _lungs, new Vector2(112f, 100f)),
                Target.Organ(TargetKind.Liver, _liver, new Vector2(101f, 157f)),
                Target.Zone(BodyZone.Head, _zoneLayers[(int)BodyZone.Head], new Vector2(111f, 15f)),
                Target.Zone(BodyZone.Chest, _zoneLayers[(int)BodyZone.Chest], new Vector2(111f, 90f)),
                Target.Zone(BodyZone.Groin, _zoneLayers[(int)BodyZone.Groin], new Vector2(111f, 189f)),
                Target.Zone(BodyZone.LeftArm, _zoneLayers[(int)BodyZone.LeftArm], new Vector2(177f, 92f)),
                Target.Zone(BodyZone.RightArm, _zoneLayers[(int)BodyZone.RightArm], new Vector2(45f, 92f)),
                Target.Zone(BodyZone.LeftLeg, _zoneLayers[(int)BodyZone.LeftLeg], new Vector2(135f, 217f)),
                Target.Zone(BodyZone.RightLeg, _zoneLayers[(int)BodyZone.RightLeg], new Vector2(87f, 218f)),
            };

            RegisterCallback<PointerMoveEvent>(evt => SetHovered(HitTest(evt.localPosition)));
            RegisterCallback<PointerLeaveEvent>(_ => SetHovered(-1));
            RegisterCallback<PointerDownEvent>(evt => TogglePin(HitTest(evt.localPosition)));
        }

        /// <summary>The zone the pinned card shows, or −1 when nothing (or an organ) is pinned.</summary>
        public int PinnedZone => _pinned >= 0 && _targets[_pinned].Kind == TargetKind.Zone ? _targets[_pinned].ZoneIndex : -1;

        public event System.Action PinChanged;

        public void Bind(HealthScanViewModel model)
        {
            _model = model;
            if (model == null)
            {
                return;
            }

            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                HealthScanZoneView zone = model.Zones[i];
                foreach (VisualElement layer in _zoneLayers[i])
                {
                    layer.style.unityBackgroundImageTintColor = zone.Fill;
                    layer.style.opacity = zone.Opacity;
                }

                foreach (VisualElement rim in _zoneRims[i])
                {
                    rim.style.unityBackgroundImageTintColor = zone.Glow;
                    rim.style.display = zone.Glow.a > 0f ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            _skull.style.opacity = model.Zones[(int)BodyZone.Head].BoneOpacity;
            _ribs.style.opacity = model.Zones[(int)BodyZone.Chest].BoneOpacity;
            _lungs.style.unityBackgroundImageTintColor = model.Lungs.Fill;
            _liver.style.unityBackgroundImageTintColor = model.Organs[(int)OrganType.Liver].Fill;
            _heart.style.unityBackgroundImageTintColor = model.Organs[(int)OrganType.Heart].Fill;
            RefreshCard();
        }

        /// <summary>Pins (or, if already pinned, unpins) a body zone's card — the wound list's click.</summary>
        public void PinZone(int zone)
        {
            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].Kind == TargetKind.Zone && _targets[i].ZoneIndex == zone)
                {
                    TogglePin(i);
                    return;
                }
            }
        }

        private void AddZone(BodyZone zone, params string[] parts)
        {
            var layers = new List<VisualElement>();
            var rims = new List<VisualElement>();
            foreach (string part in parts)
            {
                foreach (Vector2 offset in RimOffsets)
                {
                    VisualElement rim = AddLayer(part);
                    rim.AddToClassList("health-scan-doll__rim");
                    rim.style.translate = new Translate(offset.x, offset.y);
                    rim.style.display = DisplayStyle.None;
                    rims.Add(rim);
                }
            }

            foreach (string part in parts)
            {
                layers.Add(AddLayer(part));
            }

            _zoneLayers[(int)zone] = layers;
            _zoneRims[(int)zone] = rims;
        }

        private VisualElement AddLayer(string part)
        {
            VisualElement layer = new();
            layer.AddToClassList("health-scan-doll__layer");
            layer.AddToClassList($"health-scan-doll__layer--{part}");
            layer.pickingMode = PickingMode.Ignore;
            Add(layer);
            return layer;
        }

        private static Label CardLabel(VisualElement parent, string className)
        {
            Label label = new();
            label.AddToClassList(className);
            label.pickingMode = PickingMode.Ignore;
            parent.Add(label);
            return label;
        }

        private void SetHovered(int index)
        {
            if (_hovered == index)
            {
                return;
            }

            _hovered = index;
            RefreshCard();
        }

        private void TogglePin(int index)
        {
            _pinned = index == _pinned ? -1 : index;
            RefreshCard();
            PinChanged?.Invoke();
        }

        private int HitTest(Vector2 local)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return -1;
            }

            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].Contains(local, rect))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshCard()
        {
            int shown = _hovered >= 0 ? _hovered : _pinned;
            if (shown < 0 || _model == null)
            {
                _card.style.display = DisplayStyle.None;
                return;
            }

            Target target = _targets[shown];
            if (target.Kind == TargetKind.Zone)
            {
                HealthScanZoneView zone = _model.Zones[target.ZoneIndex];
                SetCard(zone.Label, zone.TierLabel, zone.TierTone, zone.NumbersText, zone.FlagsText,
                    zone.Bleeding ? StatusTone.Danger : StatusTone.Neutral);
            }
            else
            {
                HealthScanOrganView organ = target.Kind switch
                {
                    TargetKind.Lungs => _model.Lungs,
                    TargetKind.Liver => _model.Organs[(int)OrganType.Liver],
                    _ => _model.Organs[(int)OrganType.Heart],
                };
                SetCard(organ.Label, organ.StatusWord, organ.Tone, organ.Detail, string.Empty, StatusTone.Neutral);
            }

            Rect rect = contentRect;
            _card.style.left = target.Anchor.x / 222f * rect.width;
            _card.style.top = target.Anchor.y / 439f * rect.height;
            _card.style.display = DisplayStyle.Flex;
        }

        private void SetCard(string title, string tag, StatusTone tagTone, string detail, string flags, StatusTone flagTone)
        {
            _cardTitle.text = title.ToUpperInvariant();
            _cardTag.text = tag;
            StatusToneUtility.ApplyTone(_cardTag, tagTone);
            _cardDetail.text = detail;
            _cardFlags.text = flags;
            _cardFlags.style.display = string.IsNullOrEmpty(flags) ? DisplayStyle.None : DisplayStyle.Flex;
            StatusToneUtility.ApplyTone(_cardFlags, flagTone);
        }

        private enum TargetKind
        {
            Zone,
            Lungs,
            Liver,
            Heart,
        }

        private readonly struct Target
        {
            private readonly IReadOnlyList<VisualElement> _layers;

            private Target(TargetKind kind, int zone, IReadOnlyList<VisualElement> layers, Vector2 anchor)
            {
                Kind = kind;
                ZoneIndex = zone;
                _layers = layers;
                Anchor = anchor;
            }

            public TargetKind Kind { get; }

            public int ZoneIndex { get; }

            public Vector2 Anchor { get; }

            public static Target Zone(BodyZone zone, IReadOnlyList<VisualElement> layers, Vector2 anchor)
            {
                return new Target(TargetKind.Zone, (int)zone, layers, anchor);
            }

            public static Target Organ(TargetKind kind, VisualElement layer, Vector2 anchor)
            {
                return new Target(kind, -1, new[] { layer }, anchor);
            }

            public bool Contains(Vector2 local, Rect rect)
            {
                foreach (VisualElement layer in _layers)
                {
                    if (layer.resolvedStyle.opacity < 0.5f)
                    {
                        continue;
                    }

                    Background background = layer.resolvedStyle.backgroundImage;
                    Texture2D texture = background.texture;
                    if (texture == null)
                    {
                        // A vector icon (the heart) has no pixels to read: its box is the hit area.
                        if (layer.layout.Contains(local))
                        {
                            return true;
                        }

                        continue;
                    }

                    if (!texture.isReadable)
                    {
                        continue;
                    }

                    float u = local.x / rect.width;
                    float v = 1f - local.y / rect.height;
                    if (u >= 0f && u <= 1f && v >= 0f && v <= 1f && texture.GetPixelBilinear(u, v).a >= AlphaHit)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
