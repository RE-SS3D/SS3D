using System;
using System.Collections;
using System.Collections.Generic;
using SS3D;
using SS3D.Engine.Tiles;
using UnityEngine;

public class LoadingSceneUIHelper : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void Awake()
    {
        SceneLoaderManager.mapLoaded += SetAnimatorTrue;
        TileManager.TileManagerLoaded += SetAnimatorFalse;
    }

	public void SetAnimatorTrue()
	{
		Toggle(true);
	}
	
	public void SetAnimatorFalse()
	{
		Toggle(false);
	}	
	
	public void OnDestroy()
	{
        SceneLoaderManager.mapLoaded -= SetAnimatorTrue;
        TileManager.TileManagerLoaded -= SetAnimatorFalse;		
	}

    public void Toggle(bool state)
    {
        if (!animator.enabled) animator.enabled = true;
        animator.SetBool("Toggle", state);
    }
}
