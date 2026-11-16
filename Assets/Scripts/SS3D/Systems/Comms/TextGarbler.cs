using System.Text;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Client-side, deterministic scramble of a muffled line's text - signals "someone's talking,
    /// I can't quite make it out" (comms.md §3) without revealing content. Seeded from message
    /// identity rather than a fresh random draw per call, so repeated re-renders of the same line
    /// (UI rebuild, moving back into muffled range) show a stable pattern instead of flickering.
    /// </summary>
    public static class TextGarbler
    {
        private const string GarbleGlyphs = "·-–_¬~";

        public static string Garble(string source, int seed)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            StringBuilder builder = new(source.Length);
            int state = seed == 0 ? 1 : seed;

            foreach (char character in source)
            {
                if (character == ' ' || char.IsPunctuation(character))
                {
                    builder.Append(character);
                    continue;
                }

                state = (state * 9301 + 49297) % 233280;
                float roll = state / 233280f;

                // Keep a minority of the real characters so the line still reads as speech-shaped,
                // not a solid block of noise - matches the mockup's demo garble ratio.
                if (roll < 0.22f)
                {
                    builder.Append(character);
                }
                else
                {
                    int glyphIndex = (int)(roll * GarbleGlyphs.Length) % GarbleGlyphs.Length;
                    builder.Append(GarbleGlyphs[glyphIndex]);
                }
            }

            return builder.ToString();
        }
    }
}
