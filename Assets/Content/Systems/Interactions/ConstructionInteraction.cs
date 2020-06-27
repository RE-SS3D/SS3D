using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Interactions;
using SS3D.Engine.Tiles;
using UnityEngine;

public abstract class ConstructionInteraction : DelayedInteraction
{
    /// <summary>
    /// A layer mask for objects which block building
    /// </summary>
    public LayerMask ObstacleMask { get; set; }
    /// <summary>
    /// The space required for this construction
    /// </summary>
    public Vector3 BuildDimensions { get; set; } = Vector3.one;
    /// <summary>
    /// The target tile of this construction
    /// </summary>
    protected TileObject TargetTile { get; private set; }

    public override bool CanInteract(InteractionEvent interactionEvent)
    {
        if (interactionEvent.Target is IGameObjectProvider targetBehaviour)
        {
            // Get target tile
            TargetTile = targetBehaviour.GameObject.GetComponentInParent<TileObject>();
            if (TargetTile == null)
            {
                return false;
            }
            
            // Ready if no obstacles
            if (ObstacleMask == 0)
            {
                return true;
            }

            // Check for obstacle
            Vector3 center = TargetTile.gameObject.transform.position;
            bool obstacle = Physics.CheckBox(center, BuildDimensions / 2, Quaternion.identity, ObstacleMask, QueryTriggerInteraction.Ignore);
            return !obstacle;
        }

        return false;
       
        
        
    }
}
