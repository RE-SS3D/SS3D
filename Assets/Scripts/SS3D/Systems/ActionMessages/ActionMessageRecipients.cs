using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.ActionMessages
{
    /// <summary>
    /// Server-side recipient split (examine feedback Decision 9). Public lines go to everyone within
    /// hearing range and each client drops what its own vision can't see; Close and TargetOnly are
    /// filtered here, because those are the lines that must not leak. The actor never gets their own line.
    /// </summary>
    public static class ActionMessageRecipients
    {
        public static void Select<T>(
            ActionVisibility level,
            Vector3 origin,
            T actor,
            T target,
            IReadOnlyList<(T Listener, Vector3 Position)> listeners,
            List<T> recipients)
            where T : class
        {
            recipients.Clear();
            if (level == ActionVisibility.TargetOnly)
            {
                if (target != null && !ReferenceEquals(target, actor))
                {
                    recipients.Add(target);
                }

                return;
            }

            float range = level == ActionVisibility.Close ? ActionMessageConfig.CloseRange : ActionMessageConfig.PublicRange;
            float sqrRange = range * range;
            for (int i = 0; i < listeners.Count; i++)
            {
                (T listener, Vector3 position) = listeners[i];
                if (listener == null || ReferenceEquals(listener, actor) || recipients.Contains(listener))
                {
                    continue;
                }

                if ((position - origin).sqrMagnitude <= sqrRange)
                {
                    recipients.Add(listener);
                }
            }
        }
    }
}
