using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.TickManager 
{
    public class TickManager  : MonoBehaviour
    {
        private List<Action> tickActions = new List<Action>();
        private int currentTickIndex = 0;

        private void Start()
        {
            RegisterTick(() => RotateAndLogTick("Tick A"), 1f);
            RegisterTick(() => RotateAndLogTick("Tick B"), 2f);
            RegisterTick(() => RotateAndLogTick("Tick C"), 0.5f);

            // To stop a particular tick after a certain duration
            Invoke("StopCustomTick", 5f);
        }

        private void OnDestroy()
        {
            UnregisterAllTicks();
        }

        private void RegisterTick(Action onTick, float interval)
        {
            tickActions.Add(onTick);
            InvokeRepeating(nameof(InvokeCustomTick), 0f, interval);
        }

        private void InvokeCustomTick()
        {
            if (tickActions.Count > 0)
            {
                tickActions[currentTickIndex]?.Invoke();

                currentTickIndex = (currentTickIndex + 1) % tickActions.Count;
            }
        }

        private void UnregisterTick(string tickName)
        {
            // CancelInvoke method does not support cancelling by name in Unity
            // Therefore clear all invokes and reregister the non cancelled ones
            List<Action> remainingTicks = new List<Action>(tickActions);
            tickActions.Clear();

            foreach (Action tickAction in remainingTicks)
            {
                string tickActionName = tickAction.Method.Name;
                if (tickActionName != tickName)
                {
                    tickActions.Add(tickAction);
                }
            }

            CancelInvoke();
            foreach (Action tickAction in tickActions)
            {
                InvokeRepeating(nameof(InvokeCustomTick), 0f, 1f); // Re-register remaining ticks
            }
        }

        private void UnregisterAllTicks()
        {
            CancelInvoke();
            tickActions.Clear();
            currentTickIndex = 0;
        }

        private void RotateAndLogTick(string tickName)
        {
            transform.Rotate(Vector3.up, 10f);
            Debug.Log("Custom Tick: " + tickName + " - Total Rotation: " + transform.rotation.eulerAngles.y);
        }

        // To stop a particular Tick
        private void StopCustomTick()
        {
            UnregisterTick("Tick A");
             Debug.Log("Unregistered: Tick A");
        }
    }
}
