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

        public Sprite constructIcon;
        
        public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
        {
            List<IInteraction> generateInteractions = base.GenerateInteractions(targets).ToList();
            var wallConstructionInteraction = new WallConstructionInteraction
            {
                WallToConstruct = WallToConstruct,
                FloorToConstruct = FloorToConstruct,
                Delay = Delay,
                LoadingBarPrefab = LoadingBarPrefab,
                icon = constructIcon
            };
            generateInteractions.Insert(0, wallConstructionInteraction);
            return generateInteractions.ToArray();
        }
        
    }
}