using System.Collections;
using System.Collections.Generic;
using Coimbra.Services;
using Coimbra.Services.Events;
using NUnit.Framework;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using UnityEngine;

namespace SS3D.Tests
{
    /// <summary>
    /// Observes round state transitions during PlayMode lifecycle tests.
    /// </summary>
    public static class RoundLifecycleTestHelpers
    {
        private static EventHandle _roundStateHandle;
        private static bool _isObserving;
        private static RoundState _currentState = RoundState.Stopped;
        private static readonly List<RoundState> _stateHistory = new();
        private static int _endingStateCount;

        public static RoundState CurrentState => _currentState;

        public static IReadOnlyList<RoundState> StateHistory => _stateHistory;

        public static int EndingStateCount => _endingStateCount;

        public static void StartObserving()
        {
            StopObserving();
            Reset();
            _roundStateHandle = RoundStateUpdated.AddListener(HandleRoundStateUpdated);
            _isObserving = true;
        }

        public static void StopObserving()
        {
            if (!_isObserving)
            {
                return;
            }

            ServiceLocator.GetChecked<IEventService>().RemoveListener(_roundStateHandle);
            _roundStateHandle = default;
            _isObserving = false;
        }

        public static void Reset()
        {
            _currentState = RoundState.Stopped;
            _stateHistory.Clear();
            _endingStateCount = 0;
        }

        public static IEnumerator WaitForRoundState(RoundState target, float timeout = 30f)
        {
            float startTime = Time.time;

            while (_currentState != target)
            {
                if (Time.time - startTime > timeout)
                {
                    Assert.Fail($"Expected round state {target} but was {_currentState} after {timeout} seconds. History: {string.Join(" -> ", _stateHistory)}");
                }

                yield return null;
            }
        }

        public static IEnumerator WaitUntilStopped(float timeout = 30f)
        {
            yield return WaitForRoundState(RoundState.Stopped, timeout);
        }

        private static void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            _currentState = e.RoundState;
            _stateHistory.Add(e.RoundState);

            if (e.RoundState == RoundState.Ending)
            {
                _endingStateCount++;
            }
        }
    }
}
