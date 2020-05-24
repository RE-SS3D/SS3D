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
        
        public override IInteraction[] GenerateInteractions(IInteractionTarget[] targets)
        {
            List<IInteraction> generateInteractions = base.GenerateInteractions(targets).ToList();
            var tableConstruction = new TableConstructionInteraction
            {
                TableToConstruct = TableToConstruct, Delay = Delay, LoadingBarPrefab = LoadingBarPrefab
            };
            generateInteractions.Insert(0, tableConstruction);
            return generateInteractions.ToArray();
        }
    }
}