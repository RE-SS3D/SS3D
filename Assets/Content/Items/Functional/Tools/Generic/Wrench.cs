using System.Collections.Generic;
using System.Linq;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Content.Items.Functional.Tools.Generic
{
    public class Wrench : Item
    {
        public GameObject LoadingBarPrefab;
        public Fixture TableToConstruct;
        public float Delay;

        public Sprite constructIcon;

        public override void CreateInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions)
        {
            base.CreateInteractions(targets, interactions);
            interactions.Insert(0, new InteractionEntry(targets[0], new TableConstructionInteraction
            {       TableToConstruct = TableToConstruct, 
                Delay = Delay, 
                LoadingBarPrefab = LoadingBarPrefab,
                icon = constructIcon
            };
        }
    }
}