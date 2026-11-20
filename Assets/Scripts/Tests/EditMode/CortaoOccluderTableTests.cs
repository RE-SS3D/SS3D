using NUnit.Framework;
using SS3D.Rendering.URP;

namespace EditorTests
{
    public class CortaoOccluderTableTests
    {
        // Fingerprint of tables/occluder/24x24x16x16_R16_UINT.bin from https://github.com/theagentd/CORTAO
        // (commit 420b168): FNV-1a over the 16-bit entries in file order.
        private const uint ReferenceFnv1a = 0x3480dc60u;
        private const int ReferenceSetBits = 19284;
        private const int ReferenceNonZeroEntries = 4117;

        [Test]
        public void Generate_MatchesCortaoReferenceTable()
        {
            uint[] table = CortaoOccluderTable.Generate();

            Assert.That(table.Length, Is.EqualTo(24 * 24 * 16));

            uint hash = 0x811C9DC5u;
            int setBits = 0;
            int nonZero = 0;
            foreach (uint entry in table)
            {
                Assert.That(entry, Is.LessThanOrEqualTo(0xFFFFu), "16 rays must fit in the low 16 bits.");
                hash = unchecked((hash ^ entry) * 0x01000193u);
                setBits += CountBits(entry);
                if (entry != 0u)
                {
                    nonZero++;
                }
            }

            Assert.That(setBits, Is.EqualTo(ReferenceSetBits));
            Assert.That(nonZero, Is.EqualTo(ReferenceNonZeroEntries));
            Assert.That(table[(8 * 24 + 12) * 24 + 12], Is.EqualTo(383u), "Centre texel, middle radius slice.");
            Assert.That(hash, Is.EqualTo(ReferenceFnv1a));
        }

        [Test]
        public void Generate_FirstTexelIsEmpty()
        {
            // The main pass ORs table[0,0,0] into every pixel's ray mask to preload the
            // buffer, which is only harmless while that texel blocks no rays.
            Assert.That(CortaoOccluderTable.Generate()[0], Is.EqualTo(0u));
        }

        private static int CountBits(uint value)
        {
            int count = 0;
            while (value != 0u)
            {
                value &= value - 1u;
                count++;
            }

            return count;
        }
    }
}
