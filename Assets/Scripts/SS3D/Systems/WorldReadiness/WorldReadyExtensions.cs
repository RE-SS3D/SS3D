using Cysharp.Threading.Tasks;
using SS3D.Core.WorldReadiness;
using System;
using System.Threading;

namespace SS3D.Systems.WorldReadiness
{
    /// <summary>
    /// UniTask helpers for <see cref="IWorldReady"/> (kept out of Core so Core stays UniTask-free).
    /// </summary>
    public static class WorldReadyExtensions
    {
        public static async UniTask WhenReadyAsync(this IWorldReady ready, CancellationToken cancellationToken = default)
        {
            if (ready == null)
            {
                throw new ArgumentNullException(nameof(ready));
            }

            if (ready.IsReady)
            {
                return;
            }

            bool signaled = false;
            void Handler() => signaled = true;

            ready.WhenReady += Handler;
            try
            {
                await UniTask.WaitUntil(() => signaled || ready.IsReady, cancellationToken: cancellationToken);
            }
            finally
            {
                ready.WhenReady -= Handler;
            }
        }
    }
}
