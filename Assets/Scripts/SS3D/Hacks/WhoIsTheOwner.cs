using FishNet.Object;
using SS3D.Core;
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
	/// Simply press F to show the owner in console.
	/// </summary>
	public class WhoIsTheOwner : NetworkBehaviour
	{

        private void Start()
        {
            SubSystems.Get<InputSubSystem>().Inputs.Other.SeeContainerContents.performed += ShowOwner;
        }

        private void OnDestroy()
        {
            SubSystems.Get<InputSubSystem>().Inputs.Other.SeeContainerContents.performed -= ShowOwner;
        }

        private void ShowOwner(InputAction.CallbackContext callbackContext)
        {
            Debug.Log("Owner of gameobject" + gameObject.name + " is " + Owner);
        }
    }
}
