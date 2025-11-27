using System.Collections.Generic;

namespace SS3D.Systems.Interactions
{
    public struct InteractionFolder
    {
        public string SourceObjectName;
        public List<RadialInteractionItem> Interactions;

        public InteractionFolder(List<RadialInteractionItem> interactions, string sourceObjectName)
        {
            Interactions = interactions;
            SourceObjectName = sourceObjectName;
        }

        public void AddInteraction(RadialInteractionItem interaction)
        {
            Interactions ??= new List<RadialInteractionItem>();

            Interactions.Add(interaction);
        }
    }
}
