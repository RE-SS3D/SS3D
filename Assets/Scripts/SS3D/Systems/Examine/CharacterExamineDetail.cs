using System.Collections.Generic;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Body of the held-examine panel for a character (examine.md §5, §7): the presented job, the
    /// health lines, and on yourself the identity you are presenting. Range bands apply as for objects.
    /// </summary>
    public static class CharacterExamineDetail
    {
        public const string PresentingKey = "examine.self.presenting";
        public const string PresentingFallback = "You are presenting: {0}, {1}.";
        public const string PresentingNoJobKey = "examine.self.presenting_no_job";
        public const string PresentingNoJobFallback = "You are presenting: {0}.";
        public const string PresentingNothingKey = "examine.self.presenting_nothing";
        public const string PresentingNothingFallback = "You are presenting no ID.";

        /// <summary>English seed for the Examine table.</summary>
        public static readonly (string Key, string English)[] TableEntries =
        {
            (PresentingKey, PresentingFallback),
            (PresentingNoJobKey, PresentingNoJobFallback),
            (PresentingNothingKey, PresentingNothingFallback),
        };

        public static string Build(ExamineContent content, HumanInventory inventory, bool isSelf, bool withinCloseRange)
        {
            List<ExamineSection> sections = new();
            bool presenting = PresentedIdentity.TryResolve(inventory, out PresentedIdentity identity);
            if (!isSelf && presenting && !string.IsNullOrWhiteSpace(identity.Job))
            {
                sections.Add(new ExamineSection(identity.TaggedJob));
            }

            CharacterExamineHealthBuilder.AppendSections(ResolveHealthController(inventory), isSelf, sections);

            if (isSelf)
            {
                sections.Add(new ExamineSection(PresentingLine(presenting, identity)));
            }

            return ExamineDetailText.Build(content, withinCloseRange, sections);
        }

        /// <summary>"You are presenting: [Name], [Job]" — what others read on you (examine.md §7).</summary>
        public static string PresentingLine(bool presenting, PresentedIdentity identity)
        {
            if (!presenting)
            {
                return ExamineText.Get(PresentingNothingKey, PresentingNothingFallback);
            }

            if (string.IsNullOrWhiteSpace(identity.Job))
            {
                return ExamineText.Get(PresentingNoJobKey, PresentingNoJobFallback, identity.Name);
            }

            return ExamineText.Get(PresentingKey, PresentingFallback, identity.Name, identity.TaggedJob);
        }

        private static HumanHealthController ResolveHealthController(HumanInventory inventory)
        {
            if (inventory == null)
            {
                return null;
            }

            if (inventory.TryGetComponent(out HumanHealthController health))
            {
                return health;
            }

            health = inventory.GetComponentInParent<HumanHealthController>();
            return health != null ? health : inventory.GetComponentInChildren<HumanHealthController>();
        }
    }
}
