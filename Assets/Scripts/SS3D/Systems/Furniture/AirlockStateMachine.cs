﻿using SS3D.Systems.Tile.Connections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// State machine behaviour to update colors upon change of state in the Airlock animator.
    /// This behaviour should go on state Open and Enter of the airlock state machine.
    /// </summary>
    public class AirlockStateMachine : StateMachineBehaviour
    {
        private const string Opening = "Opening";
        private const string Closing = "Closing";

        private readonly Color _openingColor = new Color(.07f, 1f, .32f);
        private readonly Color _closingColor = new Color(1, 0.18f, .2f);
        private readonly Color _idleColor = new Color(0, 0, 0);

        private const int DOOR_LIGHT_MATERIAL_INDEX = 1;


        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            ChangeColors(_idleColor, animator, stateInfo, layerIndex);
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (stateInfo.IsName(Opening))
            {
                ChangeColors(_openingColor, animator, stateInfo, layerIndex);
            }
            if (stateInfo.IsName(Closing))
            {
                ChangeColors(_closingColor, animator, stateInfo, layerIndex);
            }
        }

        private void ChangeColors(Color color, Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            var renderers = animator.GetComponent<AirLockOpener>().MeshesToColor;
            var skinnedRenderers = animator.GetComponent<AirLockOpener>().SkinnedMeshesToColor;
            foreach (var renderer in renderers)
            {
                renderer.materials[DOOR_LIGHT_MATERIAL_INDEX].color = color;
            }

            foreach (var skinnedRenderer in skinnedRenderers)
            {
                if (color == _openingColor)
                {
                    skinnedRenderer.SetBlendShapeWeight(1, 100);
                    skinnedRenderer.SetBlendShapeWeight(2, 0);
                }
                else if (color == _closingColor)
                {
                    skinnedRenderer.SetBlendShapeWeight(1, 0);
                    skinnedRenderer.SetBlendShapeWeight(2, 100);
                }
                else
                {
                    skinnedRenderer.SetBlendShapeWeight(1, 0);
                    skinnedRenderer.SetBlendShapeWeight(2, 0);
                }
                
            }

        }
    }
}
    
