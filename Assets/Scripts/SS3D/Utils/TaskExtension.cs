using JetBrains.Annotations;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SS3D.Utils
{
    /// <summary>
    /// Provides timeout-aware extension methods for awaiting <see cref="Task"/> instances.
    /// </summary>
    public static class TaskExtension
    {
        /// <summary>
        /// Largest timeout that can be safely converted to milliseconds for <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
        /// </summary>
        private const float MaxDelaySeconds = (int.MaxValue - 1) / 1000f;

        /// <summary>
        /// Waits for the task to complete within the provided timeout.
        /// </summary>
        /// <param name="task">The task to await.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
        /// <exception cref="TimeoutException">Thrown when the task does not complete before the timeout expires.</exception>
        public static async Task WaitWithTimeout([NotNull] this Task task, float timeoutSeconds)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            timeoutSeconds = ClampTimeoutSeconds(timeoutSeconds);

            if (timeoutSeconds == 0f)
            {
                // A zero or invalid timeout is treated as an immediate completion check.
                if (!task.IsCompleted)
                    throw new TimeoutException("The operation has timed out.");

                await task;

                return;
            }

            using CancellationTokenSource timeoutCts = new();
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), timeoutCts.Token);

            if (await Task.WhenAny(task, timeoutTask) != task)
                throw new TimeoutException("The operation has timed out.");

            // Cancel the delay task so the timer is released as soon as the main task finishes.
            timeoutCts.Cancel();
            await task;
        }

        /// <summary>
        /// Waits for the task to complete within the provided timeout.
        /// </summary>
        /// <param name="task">The task to await.</param>
        /// <param name="timeout">The timeout value.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
        /// <exception cref="TimeoutException">Thrown when the task does not complete before the timeout expires.</exception>
        public static async Task WaitWithTimeout([NotNull] this Task task, TimeSpan timeout) => await task.WaitWithTimeout((float)timeout.TotalSeconds);

        /// <summary>
        /// Waits for the task to complete within the provided timeout and returns its result.
        /// </summary>
        /// <typeparam name="TResult">The task result type.</typeparam>
        /// <param name="task">The task to await.</param>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <returns>The completed task result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
        /// <exception cref="TimeoutException">Thrown when the task does not complete before the timeout expires.</exception>
        public static async Task<TResult> WaitWithTimeout<TResult>([NotNull] this Task<TResult> task, float timeoutSeconds)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            timeoutSeconds = ClampTimeoutSeconds(timeoutSeconds);

            if (timeoutSeconds == 0f)
            {
                // A zero or invalid timeout is treated as an immediate completion check.
                if (task.IsCompleted)
                    return await task;

                throw new TimeoutException("The operation has timed out.");
            }

            using CancellationTokenSource timeoutCts = new();
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), timeoutCts.Token);

            if (await Task.WhenAny(task, timeoutTask) != task)
                throw new TimeoutException("The operation has timed out.");

            // Cancel the delay task so the timer is released as soon as the main task finishes.
            timeoutCts.Cancel();

            return await task;
        }

        /// <summary>
        /// Waits for the task to complete within the provided timeout and returns its result.
        /// </summary>
        /// <typeparam name="TResult">The task result type.</typeparam>
        /// <param name="task">The task to await.</param>
        /// <param name="timeout">The timeout value.</param>
        /// <returns>The completed task result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="task"/> is <see langword="null"/>.</exception>
        /// <exception cref="TimeoutException">Thrown when the task does not complete before the timeout expires.</exception>
        public static async Task<TResult> WaitWithTimeout<TResult>([NotNull] this Task<TResult> task, TimeSpan timeout) => await task.WaitWithTimeout((float)timeout.TotalSeconds);

        /// <summary>
        /// Normalizes timeout values so they are non-negative and safe for <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
        /// </summary>
        /// <param name="timeoutSeconds">The timeout in seconds.</param>
        /// <returns>A timeout clamped to the supported range, or zero for invalid values.</returns>
        private static float ClampTimeoutSeconds(float timeoutSeconds)
        {
            if (float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds))
                return 0f;

            return Math.Clamp(timeoutSeconds, 0f, MaxDelaySeconds);
        }
    }
}