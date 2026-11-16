using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Selection;
using Unity.Profiling;
using UnityEngine;

namespace SS3D.Systems.Interactions
{
    /// <summary>
    /// Owner-client hover outline feedback (green/yellow/blue). Extracted from
    /// <see cref="InteractionController"/> — LateUpdate must use
    /// <see cref="InteractionPipeline.TryEvaluateOutlineInteractability"/> (no full Discover).
    /// </summary>
    public sealed class InteractionOutlineDriver
    {
        private static readonly ProfilerMarker OutlinePerformanceMarker = new("SS3D.Interactions.Outline");

        /// <summary>Range / viability refresh rate while hovering the same selectable.</summary>
        private const float OutlineEvalIntervalSeconds = 0.1f;

        private Selectable _activeOutlineSelectable;
        private InteractionOutlineView _activeOutlineView;
        private readonly List<IInteractionTarget> _outlineTargets = new(8);
        private InteractionEvent _outlineEvent;
        private IInteractionSource _outlineEventSource;
        private Selectable _entityCheckSelectable;
        private bool _entityCheckExcluded;
        private float _nextOutlineEvalTime = -1f;
        private InteractionOutlineView.OutlineState _cachedOutlineState = InteractionOutlineView.OutlineState.Hidden;
        private bool _hasCachedOutlineState;

        /// <summary>
        /// True while the hovered selectable has a viable target-bound interaction for the held source (the
        /// outline shows Available). A construction ghost hides then, so it never shows where the click goes
        /// to the target instead.
        /// </summary>
        public bool HoveredHasInteraction => _activeOutlineSelectable != null && _hasCachedOutlineState
            && _cachedOutlineState == InteractionOutlineView.OutlineState.Available;

        public void Refresh(
            SelectionSubSystem selectionSystem,
            Camera camera,
            IntentType intent,
            IInteractionSource source)
        {
            using (OutlinePerformanceMarker.Auto())
            {
                RefreshUnguarded(selectionSystem, camera, intent, source);
            }
        }

        public void Clear()
        {
            // Unity fake-null: destroyed views compare unequal to null via ==.
            if (_activeOutlineView)
            {
                _activeOutlineView.SetState(InteractionOutlineView.OutlineState.Hidden);
            }

            _activeOutlineView = null;
            _activeOutlineSelectable = null;
            _hasCachedOutlineState = false;
            _nextOutlineEvalTime = -1f;
        }

        private void RefreshUnguarded(
            SelectionSubSystem selectionSystem,
            Camera camera,
            IntentType intent,
            IInteractionSource source)
        {
            Selectable current = selectionSystem != null ? selectionSystem.GetCurrentSelectable() : null;
            InteractionOutlineView.ClearPendingExcept(current);

            if (current == null || IsEntityOutlineExcludedCached(current))
            {
                Clear();
                return;
            }

            if (InteractionOutlineView.IsPending(current))
            {
                if (current != _activeOutlineSelectable)
                {
                    Clear();
                    _activeOutlineSelectable = current;
                    _activeOutlineView = InteractionOutlineView.GetOrCreate(current);
                }

                if (_activeOutlineView)
                {
                    _activeOutlineView.SetState(InteractionOutlineView.OutlineState.Pending);
                }

                _hasCachedOutlineState = false;
                return;
            }

            if (current != _activeOutlineSelectable)
            {
                Clear();
                _activeOutlineSelectable = current;
                _activeOutlineView = InteractionOutlineView.GetOrCreate(current);
                _nextOutlineEvalTime = -1f;
                _hasCachedOutlineState = false;
            }

            if (!_activeOutlineView)
            {
                return;
            }

            float now = Time.unscaledTime;
            bool selectableChanged = !_hasCachedOutlineState;
            if (!selectableChanged && now < _nextOutlineEvalTime)
            {
                _activeOutlineView.SetState(_cachedOutlineState);
                return;
            }

            _nextOutlineEvalTime = now + OutlineEvalIntervalSeconds;

            if (!TryEvaluateInteractability(current, camera, intent, source, out bool hasViableInteractions))
            {
                _cachedOutlineState = InteractionOutlineView.OutlineState.Hidden;
                _hasCachedOutlineState = true;
                _activeOutlineView.SetState(InteractionOutlineView.OutlineState.Hidden);
                return;
            }

            _cachedOutlineState = hasViableInteractions
                ? InteractionOutlineView.OutlineState.Available
                : InteractionOutlineView.OutlineState.Unavailable;
            _hasCachedOutlineState = true;
            _activeOutlineView.SetState(_cachedOutlineState);
        }

        private bool IsEntityOutlineExcludedCached(Selectable selectable)
        {
            if (selectable != _entityCheckSelectable)
            {
                _entityCheckSelectable = selectable;
                _entityCheckExcluded = selectable != null && selectable.GetComponentInParent<Entity>() != null;
            }

            return _entityCheckExcluded;
        }

        private bool TryEvaluateInteractability(
            Selectable selectable,
            Camera camera,
            IntentType intent,
            IInteractionSource source,
            out bool hasViableInteractions)
        {
            hasViableInteractions = false;

            if (source == null)
            {
                return false;
            }

            bool hasPoint = SelectionTargetUtility.TryResolveInteractionPoint(camera, selectable, out Vector3 point, out Vector3 normal);
            InteractionDiscovery.CollectTargetsInto(source, selectable.gameObject, _outlineTargets);

            InteractionEvent outlineEvent = GetOrCreateOutlineEvent(source);
            if (hasPoint)
            {
                outlineEvent.SetResolvedHit(point, normal);
            }
            else
            {
                outlineEvent.ClearResolvedHit();
            }

            outlineEvent.Target = null;

            // Outline LateUpdate must not run full Discover (source-only Drop, ToArray, Filter lists).
            return InteractionPipeline.TryEvaluateOutlineInteractability(
                source,
                _outlineTargets,
                outlineEvent,
                intent,
                out hasViableInteractions);
        }

        private InteractionEvent GetOrCreateOutlineEvent(IInteractionSource source)
        {
            if (_outlineEvent == null || !ReferenceEquals(_outlineEventSource, source))
            {
                _outlineEvent = new InteractionEvent(source, null);
                _outlineEventSource = source;
            }

            return _outlineEvent;
        }
    }
}
