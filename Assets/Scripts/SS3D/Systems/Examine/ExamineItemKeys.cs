using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Examine
{
    /// <summary>String-table keys for the generic lines every item shows (examine.md §7).</summary>
    public static class ExamineItemKeys
    {
        public const string Size = "examine.item.size";
        public const string SizeFallback = "Size: {0}.";

        public const string Stack = "examine.item.stack";
        public const string StackFallback = "{0} in the stack.";

        public const string SizeTiny = "examine.item.size.tiny";
        public const string SizeSmall = "examine.item.size.small";
        public const string SizeNormal = "examine.item.size.normal";
        public const string SizeBulky = "examine.item.size.bulky";
        public const string SizeHuge = "examine.item.size.huge";

        public static (string Key, string English) SizeWord(SizeClass size) => size switch
        {
            SizeClass.Tiny => (SizeTiny, "tiny"),
            SizeClass.Small => (SizeSmall, "small"),
            SizeClass.Bulky => (SizeBulky, "bulky"),
            SizeClass.Huge => (SizeHuge, "huge"),
            _ => (SizeNormal, "normal"),
        };

        /// <summary>Every key with its English text, for the table seed.</summary>
        public static readonly (string Key, string English)[] All =
        {
            (Size, SizeFallback), (Stack, StackFallback),
            SizeWord(SizeClass.Tiny), SizeWord(SizeClass.Small), SizeWord(SizeClass.Normal),
            SizeWord(SizeClass.Bulky), SizeWord(SizeClass.Huge),
        };
    }
}
