using SS3D.Engine.Tiles.State;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.Tiles.State
{
    [Serializable]
    public struct FixtureState
    {
        // Can be extended with other info like construction state
        public Rotation rotation;
    }

    [ExecuteAlways]
    public class FixtureStateMaintainer : TileStateMaintainer<FixtureState>
    {
        protected override void OnStateUpdate(FixtureState prevState = new FixtureState())
        {

            switch (TileState.rotation)
            {
                case Rotation.North:
                    transform.localEulerAngles = new Vector3(0, 0);
                    break;
                case Rotation.East:
                    transform.localEulerAngles = new Vector3(0, 90);
                    break;
                case Rotation.South:
                    transform.localEulerAngles = new Vector3(0, 180);
                    break;
                case Rotation.West:
                    transform.localEulerAngles = new Vector3(0, 270);
                    break;
            }

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

