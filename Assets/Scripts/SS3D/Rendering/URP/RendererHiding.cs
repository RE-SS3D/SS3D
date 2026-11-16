using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Rendering.URP
{
    /// <summary>
    /// Shares <see cref="Renderer.forceRenderingOff"/> between owners that hide renderers for different reasons,
    /// so one showing a renderer never undoes another's hide: worn clothes covering a body part, and the field
    /// of view culling a whole person (2026-11_fov-rework D9). A renderer renders when no reason holds.
    /// </summary>
    public static class RendererHiding
    {
        [Flags]
        public enum Reason : byte
        {
            CoveredByClothes = 1 << 0,
            OutOfSight = 1 << 1,
        }

        private const int PruneAbove = 4096;

        private static readonly Dictionary<Renderer, Reason> s_Hidden = new(ReferenceComparer<Renderer>.Instance);
        private static readonly List<Renderer> s_Dead = new();

        public static void Set(Renderer renderer, Reason reason, bool hidden)
        {
            if (renderer == null)
                return;

            s_Hidden.TryGetValue(renderer, out Reason current);
            Reason next = hidden ? current | reason : current & ~reason;
            if (next == current && renderer.forceRenderingOff == (next != 0))
                return;

            if (next == 0)
                s_Hidden.Remove(renderer);
            else
                s_Hidden[renderer] = next;

            renderer.forceRenderingOff = next != 0;
            if (s_Hidden.Count > PruneAbove)
                PruneDestroyed();
        }

        public static bool IsHidden(Renderer renderer, Reason reason) =>
            renderer != null && s_Hidden.TryGetValue(renderer, out Reason current) && (current & reason) != 0;

        private static void PruneDestroyed()
        {
            s_Dead.Clear();
            foreach (Renderer renderer in s_Hidden.Keys)
            {
                if (renderer == null)
                    s_Dead.Add(renderer);
            }

            foreach (Renderer renderer in s_Dead)
                s_Hidden.Remove(renderer);
        }

        // Play sessions without a domain reload must not inherit the last session's renderers.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_Hidden.Clear();
    }
}
