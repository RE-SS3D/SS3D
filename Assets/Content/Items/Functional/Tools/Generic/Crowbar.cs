using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Items.Functional.Tools.Generic
{
    public class Crowbar : Item
    {
        public GameObject LoadingBarPrefab;
        public Turf WallToConstruct;
        public Turf FloorToConstruct;
        public float Delay;
        public LayerMask ObstacleMask;

        public Sprite constructIcon;
     
        public override void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.CreateInteractions(targets, interactions);
            var wallConstructionInteraction = new WallConstructionInteraction
            {
                WallToConstruct = WallToConstruct,
                FloorToConstruct = FloorToConstruct,
                Delay = Delay,
                LoadingBarPrefab = LoadingBarPrefab,
                icon = constructIcon,
                ObstacleMask = ObstacleMask
            };
            interactions.Insert(0, new InteractionEntry(targets[0], wallConstructionInteraction));
        }

    }
}