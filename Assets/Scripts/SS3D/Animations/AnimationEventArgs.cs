using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Animation
{
    public class StateMachineEventArgs : EventArgs
    {
        public Animator Animator { get; set; }

        public AnimatorStateInfo StateInfo { get; set; }

        public int LayerIndex { get; set; }
    }
}
