using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SS3D.Engine.Interactions
{
    public static class GameObjectExtensions
    {
        /**
         * <summary>Gets all interactions attached to the game object from any source</summary>
         */
        public static List<Interaction> GetAllInteractions(this GameObject gameObject, InteractionEvent e)
        {
            return (
                // Collect interaction creators
                gameObject.GetComponents<InteractionCreator>()
                    .Select(creator => creator.Generate(e))
                    .Aggregate<List<Interaction>, IEnumerable<Interaction>>(new List<Interaction>(), (a, b) => a.Concat(b))
                // Collect interactions
                .Concat(gameObject.GetComponents<Interaction>())
            ).ToList();
        }
    }
}
