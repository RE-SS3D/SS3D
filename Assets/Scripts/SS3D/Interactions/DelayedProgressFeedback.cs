using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Shows a timed step's progress ring. Implemented by the UI Toolkit ring in SS3D.Systems, which
    /// registers itself on <see cref="DelayedProgressFeedback.Presenter"/>; the interactions assembly
    /// does not reference the UI shell.
    /// </summary>
    public interface IDelayedProgressPresenter
    {
        /// <param name="key">One ring per key (the hand running it); a new Begin on the same key replaces it.</param>
        /// <param name="anchor">Followed while alive; may be null.</param>
        /// <param name="worldPoint">Where the ring sits; with an anchor, kept as an offset from it.</param>
        /// <returns>Id of this ring, for <see cref="Complete"/> and <see cref="Cancel"/>.</returns>
        int Begin(int key, Transform anchor, Vector3 worldPoint, float duration);

        /// <summary>The running (not done or cancelled) ring on this key.</summary>
        bool TryGetRunning(int key, out int ringId);

        /// <summary>The step finished; the ring shows done briefly. A later cancel still replaces it.</summary>
        void Complete(int ringId);

        /// <summary>Shows the reason briefly, then removes the ring. No-op when that ring was replaced.</summary>
        void Cancel(int ringId, DelayedCancelReason reason);

        /// <summary><see cref="Cancel"/> for every running ring (the local player's Cancel key).</summary>
        void CancelAll(DelayedCancelReason reason);

        /// <summary>Removes the ring on this key with no message.</summary>
        void Clear(int key);

        /// <summary>Removes every ring with no message (server rejected the start).</summary>
        void ClearAll();
    }

    /// <summary>
    /// Null-safe entry to the progress ring for every <see cref="DelayedInteraction"/> and construction
    /// cell action (D6 of 2026-11_construction-mvp1, construction.md §11). Client only.
    /// </summary>
    public static class DelayedProgressFeedback
    {
        private static readonly Vector3 SourceFallbackOffset = new(0f, 1.2f, 0f);

        public static IDelayedProgressPresenter Presenter { get; set; }

        public const int NoRing = -1;

        public static int Begin(int key, Transform anchor, Vector3 worldPoint, float duration) =>
            Presenter != null ? Presenter.Begin(key, anchor, worldPoint, duration) : NoRing;

        public static bool TryGetRunning(int key, out int ringId)
        {
            ringId = NoRing;
            return Presenter != null && Presenter.TryGetRunning(key, out ringId);
        }

        public static void Complete(int ringId) => Presenter?.Complete(ringId);

        public static void Cancel(int ringId, DelayedCancelReason reason) => Presenter?.Cancel(ringId, reason);

        public static void CancelAll(DelayedCancelReason reason) => Presenter?.CancelAll(reason);

        public static void Clear(int key) => Presenter?.Clear(key);

        public static void ClearAll() => Presenter?.ClearAll();

        /// <summary>Ring key for an interaction: the root source (the hand) that runs it.</summary>
        public static bool TryGetKey(InteractionEvent interactionEvent, out int key, out Transform sourceTransform)
        {
            key = 0;
            sourceTransform = null;
            if (interactionEvent?.Source?.GetRootSource() is not IGameObjectProvider source || source.GameObject == null)
            {
                return false;
            }

            sourceTransform = source.GameObject.transform;
            key = KeyFor(sourceTransform);
            return true;
        }

        public static int KeyFor(Transform sourceTransform) => sourceTransform.GetInstanceID();

        /// <summary>
        /// Anchors the ring on the target: the clicked point when known, else the target's transform,
        /// else above the source.
        /// </summary>
        public static void ResolveAnchor(InteractionEvent interactionEvent, Transform sourceTransform,
            out Transform anchor, out Vector3 worldPoint)
        {
            anchor = interactionEvent?.Target?.GetGameObject() is GameObject target && target != null ? target.transform : null;

            if (interactionEvent != null && interactionEvent.HasPoint)
            {
                worldPoint = interactionEvent.Point;
                return;
            }

            if (anchor != null)
            {
                worldPoint = anchor.position;
                return;
            }

            anchor = sourceTransform;
            worldPoint = sourceTransform != null ? sourceTransform.position + SourceFallbackOffset : Vector3.zero;
        }
    }
}
