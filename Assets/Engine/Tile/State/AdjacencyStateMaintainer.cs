using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.State
{
    [Serializable]
    public struct AdjacencyState
    {
        public byte blockedDirection;
    }

    [ExecuteAlways]
    public class AdjacencyStateMaintainer : TileStateMaintainer<AdjacencyState>
    {
        protected override void OnStateUpdate(AdjacencyState prevState = new AdjacencyState())
        {

        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EditorApplication.delayCall += () => {
                if (this)
                {
                    OnStateUpdate();
                }
            };
        }
#endif
    }
}