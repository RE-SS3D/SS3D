using FishNet.Object;
using SS3D.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InputSystem = SS3D.Systems.Inputs.InputSystem;

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
            Subsystems.Get<InputSystem>().Inputs.Other.SeeContainerContents.performed += ShowOwner;
        }

        private void ShowOwner(InputAction.CallbackContext callbackContext)
        {
            Debug.Log("Owner of gameobject" + gameObject.name + " is " + Owner);
        }
    }
}
