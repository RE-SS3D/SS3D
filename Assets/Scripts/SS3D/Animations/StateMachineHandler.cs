using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Animation
{
    public class StateMachineHandler : MonoBehaviour
    {
        public StateMachineEventHandler stateMachineEventHandler;

        public void Awake()
        {
            stateMachineEventHandler = ScriptableObject.CreateInstance<StateMachineEventHandler>();
        }

        public class StateMachineEventHandler : StateMachineBehaviour
        {
            // Event that will be invoked when the animation state exits
            public event EventHandler<StateMachineEventArgs> OnStateExitEvent;

            public event EventHandler<StateMachineEventArgs> OnStateEnterEvent;

            public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
            {
                var data = new StateMachineEventArgs();
                data.Animator = animator;
                data.StateInfo = stateInfo;
                data.LayerIndex = layerIndex;
                // Check if the event is not null before invoking it
                OnStateExitEvent?.Invoke(this, data);
            }

            public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
            {
                var data = new StateMachineEventArgs();
                data.Animator = animator;
                data.StateInfo = stateInfo;
                data.LayerIndex = layerIndex;
                // Check if the event is not null before invoking it
                OnStateEnterEvent?.Invoke(this, data);
            }
        }
    }
    
}
