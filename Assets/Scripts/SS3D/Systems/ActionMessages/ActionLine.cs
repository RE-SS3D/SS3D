using SS3D.Systems.Entities;

namespace SS3D.Systems.ActionMessages
{
    /// <summary>A received action line, formatted for this viewer.</summary>
    public readonly struct ActionLine
    {
        public ActionLine(string text, Entity actor, ActionVisibility level, bool aimedAtViewer, bool hostile)
        {
            Text = text;
            Actor = actor;
            Level = level;
            AimedAtViewer = aimedAtViewer;
            Hostile = hostile;
        }

        public string Text { get; }

        /// <summary>The character the chip anchors to; null for a line with no visible actor.</summary>
        public Entity Actor { get; }

        public ActionVisibility Level { get; }

        public bool AimedAtViewer { get; }

        public bool Hostile { get; }
    }
}
