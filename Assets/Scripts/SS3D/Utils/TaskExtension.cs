using JetBrains.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SS3D.Utils
{
    public static class TaskExtension
    {
        private const float MaxDelaySeconds = (int.MaxValue - 1) / 1000f;

        public static async Task WaitWithTimeout([NotNull] this Task task, float timeoutSeconds)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            timeoutSeconds = ClampTimeoutSeconds(timeoutSeconds);

            if (timeoutSeconds == 0f)
            {
                if (!task.IsCompleted)
                    throw new TimeoutException("The operation has timed out.");

                await task;

                return;
            }

            using CancellationTokenSource timeoutCts = new();
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), timeoutCts.Token);

            if (await Task.WhenAny(task, timeoutTask) != task)
                throw new TimeoutException("The operation has timed out.");

            timeoutCts.Cancel();
            await task;
        }

        public static async Task WaitWithTimeout([NotNull] this Task task, TimeSpan timeout) => await task.WaitWithTimeout((float)timeout.TotalSeconds);

        public static async Task<TResult> WaitWithTimeout<TResult>([NotNull] this Task<TResult> task, float timeoutSeconds)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            timeoutSeconds = ClampTimeoutSeconds(timeoutSeconds);

            if (timeoutSeconds == 0f)
            {
                if (task.IsCompleted)
                    return await task;

                throw new TimeoutException("The operation has timed out.");
            }

            using CancellationTokenSource timeoutCts = new();
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), timeoutCts.Token);

            if (await Task.WhenAny(task, timeoutTask) != task)
                throw new TimeoutException("The operation has timed out.");

            timeoutCts.Cancel();

            return await task;
        }

        public static async Task<TResult> WaitWithTimeout<TResult>([NotNull] this Task<TResult> task, TimeSpan timeout) => await task.WaitWithTimeout((float)timeout.TotalSeconds);

        private static float ClampTimeoutSeconds(float timeoutSeconds)
        {
            if (float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds))
                return 0f;

            return Math.Clamp(timeoutSeconds, 0f, MaxDelaySeconds);
        }
    }
}