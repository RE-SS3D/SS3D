using System.Collections.Generic;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Interactions;
using SS3D.Systems.Inputs;
using SS3D.Systems.Interactions.UI;
using SS3D.Systems.Screens;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// UI Toolkit progress ring for every timed step: <see cref="DelayedInteraction"/>s and construction
    /// cell actions (D6 of 2026-11_construction-mvp1, construction.md §11). Anchored to the target in the
    /// <see cref="UiShellSubSystem"/> overlay layer; shows done, or the cancel reason, briefly. Replaces the
    /// world-space uGUI LoadingBar. Self-bootstraps on clients, like the bug report.
    /// </summary>
    public sealed class DelayedProgressRingSubSystem : SubSystem, IDelayedProgressPresenter
    {
        private const float RingSize = 44f;
        private const float DoneHoldSeconds = 0.45f;
        private const float CancelHoldSeconds = 1.3f;

        /// <summary>A running ring nobody completes (lost confirm) is dropped this long after its timer.</summary>
        private const float OrphanSeconds = 3f;

        private static readonly Color RunningColor = new(0.55f, 0.85f, 1f, 1f);
        private static readonly Color DoneColor = new(0.45f, 0.95f, 0.5f, 1f);
        private static readonly Color CancelColor = new(1f, 0.45f, 0.4f, 1f);

        private enum RingState { Running, Done, Cancelled }

        private sealed class Ring
        {
            public int Id;
            public int Key;
            public Transform Anchor;
            public Vector3 Offset;
            public Vector3 LastWorld;
            public float StartTime;
            public float Duration;
            public RingState State;
            public float StateTime;
            public VisualElement Root;
            public ProgressRingElement Arc;
            public Label Caption;
        }

        private readonly List<Ring> _rings = new();
        private VisualElement _container;
        private Camera _camera;
        private int _nextId = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (UnityEngine.Application.isBatchMode || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                return;
            if (SubSystems.TryGet(out DelayedProgressRingSubSystem _))
                return;

            GameObject host = new(nameof(DelayedProgressRingSubSystem));
            DontDestroyOnLoad(host);
            host.AddComponent<DelayedProgressRingSubSystem>();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            DelayedProgressFeedback.Presenter = this;
        }

        protected override void OnDestroyed()
        {
            if (ReferenceEquals(DelayedProgressFeedback.Presenter, this))
            {
                DelayedProgressFeedback.Presenter = null;
            }

            ClearAll();
            _container?.RemoveFromHierarchy();
            _container = null;
            base.OnDestroyed();
        }

        public int Begin(int key, Transform anchor, Vector3 worldPoint, float duration)
        {
            RemoveKey(key);

            if (!EnsureContainer())
            {
                return DelayedProgressFeedback.NoRing;
            }

            Ring ring = new()
            {
                Id = _nextId++,
                Key = key,
                Anchor = anchor,
                Offset = anchor != null ? worldPoint - anchor.position : Vector3.zero,
                LastWorld = worldPoint,
                StartTime = Time.time,
                Duration = Mathf.Max(0.01f, duration),
                State = RingState.Running,
            };

            BuildElements(ring);
            _rings.Add(ring);
            PlaceRing(ring);
            return ring.Id;
        }

        public bool TryGetRunning(int key, out int ringId)
        {
            foreach (Ring ring in _rings)
            {
                if (ring.Key == key && ring.State == RingState.Running)
                {
                    ringId = ring.Id;
                    return true;
                }
            }

            ringId = DelayedProgressFeedback.NoRing;
            return false;
        }

        public void Complete(int ringId)
        {
            Ring ring = Find(ringId);
            if (ring == null || ring.State != RingState.Running)
            {
                return;
            }

            SetState(ring, RingState.Done, string.Empty);
        }

        public void Cancel(int ringId, DelayedCancelReason reason)
        {
            Ring ring = Find(ringId);
            if (ring == null || ring.State == RingState.Cancelled)
            {
                return;
            }

            SetState(ring, RingState.Cancelled, DelayedCancelMessages.Format(reason));
        }

        public void CancelAll(DelayedCancelReason reason)
        {
            foreach (Ring ring in _rings)
            {
                if (ring.State == RingState.Running)
                {
                    SetState(ring, RingState.Cancelled, DelayedCancelMessages.Format(reason));
                }
            }
        }

        public void Clear(int key) => RemoveKey(key);

        public void ClearAll()
        {
            foreach (Ring ring in _rings)
            {
                ring.Root.RemoveFromHierarchy();
            }

            _rings.Clear();
        }

        private void Update()
        {
            if (_rings.Count == 0)
            {
                return;
            }

            float now = Time.time;
            for (int i = _rings.Count - 1; i >= 0; i--)
            {
                Ring ring = _rings[i];
                bool expired = ring.State switch
                {
                    RingState.Done => now - ring.StateTime > DoneHoldSeconds,
                    RingState.Cancelled => now - ring.StateTime > CancelHoldSeconds,
                    _ => now - ring.StartTime > ring.Duration + OrphanSeconds,
                };

                if (expired)
                {
                    ring.Root.RemoveFromHierarchy();
                    _rings.RemoveAt(i);
                    continue;
                }

                if (ring.State == RingState.Running)
                {
                    ring.Arc.Progress = (now - ring.StartTime) / ring.Duration;
                }

                PlaceRing(ring);
            }
        }

        private void PlaceRing(Ring ring)
        {
            if (ring.Anchor != null)
            {
                ring.LastWorld = ring.Anchor.position + ring.Offset;
            }

            Camera camera = ResolveCamera();
            IPanel panel = _container?.panel;
            if (camera == null || panel == null)
            {
                ring.Root.style.display = DisplayStyle.None;
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(ring.LastWorld);
            if (screen.z <= 0f)
            {
                ring.Root.style.display = DisplayStyle.None;
                return;
            }

            Vector2 panelPoint = InputInterface.ScreenToPanel(panel, new Vector2(screen.x, screen.y));
            ring.Root.style.display = DisplayStyle.Flex;
            ring.Root.style.left = panelPoint.x - RingSize * 0.5f;
            ring.Root.style.top = panelPoint.y - RingSize * 0.5f;
        }

        private static void SetState(Ring ring, RingState state, string caption)
        {
            ring.State = state;
            ring.StateTime = Time.time;
            ring.Arc.ArcColor = state == RingState.Done ? DoneColor : CancelColor;
            if (state == RingState.Done)
            {
                ring.Arc.Progress = 1f;
            }

            ring.Caption.text = caption;
            ring.Caption.style.display = string.IsNullOrEmpty(caption) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void BuildElements(Ring ring)
        {
            ring.Root = new VisualElement { name = "delayed-progress-ring", pickingMode = PickingMode.Ignore };
            ring.Root.style.position = UnityEngine.UIElements.Position.Absolute;
            ring.Root.style.width = RingSize;
            ring.Root.style.alignItems = Align.Center;

            ring.Arc = new ProgressRingElement { ArcColor = RunningColor };
            ring.Arc.style.width = RingSize;
            ring.Arc.style.height = RingSize;
            ring.Root.Add(ring.Arc);

            // Wider than the ring so "Not enough materials" fits on one line, centred under it.
            ring.Caption = new Label { pickingMode = PickingMode.Ignore };
            ring.Caption.style.position = UnityEngine.UIElements.Position.Absolute;
            ring.Caption.style.top = RingSize + 2f;
            ring.Caption.style.width = 200f;
            ring.Caption.style.left = (RingSize - 200f) * 0.5f;
            ring.Caption.style.unityTextAlign = TextAnchor.UpperCenter;
            ring.Caption.style.color = Color.white;
            ring.Caption.style.fontSize = 13f;
            ring.Caption.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = new Color(0f, 0f, 0f, 0.8f) };
            ring.Caption.style.display = DisplayStyle.None;
            ring.Root.Add(ring.Caption);

            _container.Add(ring.Root);
        }

        private bool EnsureContainer()
        {
            if (_container != null && _container.panel != null)
            {
                return true;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem shell) || !shell.TryGetLayer(UiLayer.Overlay, out VisualElement layer))
            {
                return false;
            }

            _container = new VisualElement { name = "delayed-progress-rings", pickingMode = PickingMode.Ignore };
            _container.style.position = UnityEngine.UIElements.Position.Absolute;
            _container.style.left = 0f;
            _container.style.top = 0f;
            _container.style.right = 0f;
            _container.style.bottom = 0f;
            layer.Add(_container);
            return true;
        }

        private Camera ResolveCamera()
        {
            if (_camera != null && _camera.isActiveAndEnabled)
            {
                return _camera;
            }

            Actor playerCamera = SubSystems.TryGet(out CameraSubSystem cameras) ? cameras.PlayerCamera : null;
            _camera = playerCamera != null ? playerCamera.GetComponent<Camera>() : Camera.main;
            return _camera;
        }

        private Ring Find(int ringId)
        {
            foreach (Ring ring in _rings)
            {
                if (ring.Id == ringId)
                {
                    return ring;
                }
            }

            return null;
        }

        private void RemoveKey(int key)
        {
            for (int i = _rings.Count - 1; i >= 0; i--)
            {
                if (_rings[i].Key == key)
                {
                    _rings[i].Root.RemoveFromHierarchy();
                    _rings.RemoveAt(i);
                }
            }
        }
    }
}
