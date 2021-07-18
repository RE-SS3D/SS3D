using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Items.Functional.Tools.Generic
{
    // Needs rework too
    public class Wrench : Item
    {
        public GameObject LoadingBarPrefab;

	// nope
        public TileObjectSO ObjectToConstruct;
        public Direction ObjectDirection;
        public float Delay;
        public LayerMask ObstacleMask;

        public Sprite constructIcon;

        public override void GenerateInteractionsFromSource(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.GenerateInteractionsFromSource(targets, interactions);
            interactions.Insert(0, new InteractionEntry(targets[0], new ItemConstructionInteraction
            {       ObjectToConstruct = ObjectToConstruct, 
                Delay = Delay, 
                LoadingBarPrefab = LoadingBarPrefab,
                icon = constructIcon,
                ObstacleMask = ObstacleMask,
                ObjectDirection = ObjectDirection
            }));
        }
    }
}