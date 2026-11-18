using SS3D.Systems.Health;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// The scan's body doll: the SS3D-Art <c>Harm*</c> masks (white, all on one 296×585 canvas) stacked
    /// as tinted layers. Hovering a zone or organ shows its readout; clicking pins it. Hit testing reads
    /// the masks' alpha, so the textures must be Read/Write enabled.
    /// </summary>
    [UxmlElement]
    public partial class HealthScanDoll : VisualElement
    {
        private const float AlphaHit = 0.5f;

        private readonly VisualElement[][] _zoneLayers = new VisualElement[HealthConstants.ZoneCount][];
        private readonly VisualElement _skull;
        private readonly VisualElement _ribs;
        private readonly VisualElement _lungs;
        private readonly VisualElement _liver;
        private readonly VisualElement _heart;
        private readonly Label _tooltip;
        private readonly Target[] _targets;

        private HealthScanViewModel _model;
        private int _hovered = -1;
        private int _pinned = -1;

        public HealthScanDoll()
        {
            AddToClassList("health-scan-doll");

            _zoneLayers[(int)BodyZone.Head] = new[] { AddLayer("head") };
            _zoneLayers[(int)BodyZone.Chest] = new[] { AddLayer("chest") };
            _zoneLayers[(int)BodyZone.Groin] = new[] { AddLayer("pelvis") };
            _zoneLayers[(int)BodyZone.LeftArm] = new[] { AddLayer("arm-l1"), AddLayer("arm-l2") };
            _zoneLayers[(int)BodyZone.RightArm] = new[] { AddLayer("arm-r1"), AddLayer("arm-r2") };
            _zoneLayers[(int)BodyZone.LeftLeg] = new[] { AddLayer("leg-l1"), AddLayer("leg-l2") };
            _zoneLayers[(int)BodyZone.RightLeg] = new[] { AddLayer("leg-r1"), AddLayer("leg-r2") };

            _skull = AddLayer("skull");
            _ribs = AddLayer("ribs");
            _heart = AddLayer("heart");
            _lungs = AddLayer("lungs");
            _liver = AddLayer("liver");
            AddLayer("outline");

            _tooltip = new Label();
            _tooltip.AddToClassList("health-scan-doll__tooltip");
            _tooltip.AddToClassList("font-terminal");
            _tooltip.pickingMode = PickingMode.Ignore;
            _tooltip.style.display = DisplayStyle.None;
            Add(_tooltip);

            // Organs sit on top of the torso, so they are tested first.
            _targets = new[]
            {
                new Target(TargetKind.Lungs, -1, _lungs),
                new Target(TargetKind.Liver, -1, _liver),
                new Target(TargetKind.Heart, -1, _heart),
                ZoneTarget(BodyZone.Head),
                ZoneTarget(BodyZone.Chest),
                ZoneTarget(BodyZone.Groin),
                ZoneTarget(BodyZone.LeftArm),
                ZoneTarget(BodyZone.RightArm),
                ZoneTarget(BodyZone.LeftLeg),
                ZoneTarget(BodyZone.RightLeg),
            };

            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerLeaveEvent>(_ => SetHovered(-1));
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

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
                    layer.EnableInClassList("health-scan-doll__layer--severed", zone.Severed);
                }
            }

            _skull.style.opacity = model.Zones[(int)BodyZone.Head].BoneOpacity;
            _ribs.style.opacity = model.Zones[(int)BodyZone.Chest].BoneOpacity;
            _lungs.style.unityBackgroundImageTintColor = model.LungsOverlay.Fill;
            _liver.style.unityBackgroundImageTintColor = model.Organs[(int)OrganType.Liver].Fill;
            _heart.style.unityBackgroundImageTintColor = model.Organs[(int)OrganType.Heart].Fill;
            RefreshTooltip(Vector2.zero, false);
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

        private Target ZoneTarget(BodyZone zone)
        {
            return new Target(TargetKind.Zone, (int)zone, _zoneLayers[(int)zone]);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            SetHovered(HitTest(evt.localPosition));
            RefreshTooltip(evt.localPosition, true);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            int hit = HitTest(evt.localPosition);
            _pinned = hit == _pinned ? -1 : hit;
            RefreshTooltip(evt.localPosition, true);
        }

        private void SetHovered(int index)
        {
            if (_hovered == index)
            {
                return;
            }

            _hovered = index;
            if (index < 0)
            {
                RefreshTooltip(Vector2.zero, false);
            }
        }

        private int HitTest(Vector2 local)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return -1;
            }

            float u = local.x / rect.width;
            float v = 1f - local.y / rect.height;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return -1;
            }

            for (int i = 0; i < _targets.Length; i++)
            {
                if (_targets[i].Contains(u, v))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshTooltip(Vector2 pointer, bool pointerKnown)
        {
            int shown = _pinned >= 0 ? _pinned : _hovered;
            string text = shown >= 0 && _model != null ? _targets[shown].Tooltip(_model) : null;
            if (string.IsNullOrEmpty(text))
            {
                _tooltip.style.display = DisplayStyle.None;
                return;
            }

            _tooltip.text = text;
            _tooltip.style.display = DisplayStyle.Flex;
            if (pointerKnown && (_pinned < 0 || shown == _hovered))
            {
                _tooltip.style.left = pointer.x;
                _tooltip.style.top = pointer.y;
            }
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
            private readonly TargetKind _kind;
            private readonly int _zone;
            private readonly VisualElement[] _layers;

            public Target(TargetKind kind, int zone, params VisualElement[] layers)
            {
                _kind = kind;
                _zone = zone;
                _layers = layers;
            }

            public bool Contains(float u, float v)
            {
                foreach (VisualElement layer in _layers)
                {
                    Texture2D texture = layer.resolvedStyle.backgroundImage.texture;
                    if (texture == null || !texture.isReadable)
                    {
                        continue;
                    }

                    try
                    {
                        if (texture.GetPixelBilinear(u, v).a >= AlphaHit)
                        {
                            return true;
                        }
                    }
                    catch (UnityException)
                    {
                        return false;
                    }
                }

                return false;
            }

            public string Tooltip(HealthScanViewModel model)
            {
                return _kind switch
                {
                    TargetKind.Lungs => model.LungsOverlay.Tooltip,
                    TargetKind.Liver => model.Organs[(int)OrganType.Liver].Tooltip,
                    TargetKind.Heart => model.Organs[(int)OrganType.Heart].Tooltip,
                    _ => _zone >= 0 && _zone < model.Zones.Length ? model.Zones[_zone].Tooltip : string.Empty,
                };
            }
        }
    }
}
