using SS3D.Core.Debugging;
using FishNet.Object;
using SS3D.Core;
using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Hacks
{
	/// <summary>
	/// Simple script to check the owner of a game object this script is on.
	/// Logs the owner when the F1 debug window's "List container contents" runs.
	/// </summary>
	public class WhoIsTheOwner : NetworkBehaviour
	{

        private void Start()
        {
            DebugTools.Get(DebugToolNames.ListContainerContents).Invoked += ShowOwner;
        }

        private void OnDestroy()
        {
            DebugTools.Get(DebugToolNames.ListContainerContents).Invoked -= ShowOwner;
        }

        private void ShowOwner()
        {
            Log.Debug(this, "Owner of {gameObjectName} is {owner}", Logs.Generic, gameObject.name, Owner);
        }
    }
}
