using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Systems.Entities.Data;

namespace EditorTests
{
    /// <summary>
    /// Guards against a copy-pasted Animator.StringToHash string: a typo like StringToHash("Sit")
    /// vs. StringToHash("sit") on two different parameters would otherwise fail silently at
    /// Unity-integration time (a mismatched hash is just a no-op, not an error) instead of being
    /// caught here.
    /// </summary>
    public class AnimationsHashTests
    {
        [Test]
        public void AllHumanoidAnimatorParameterHashesAreNonZero()
        {
            foreach (FieldInfo field in GetHumanoidParameterFields())
            {
                int hash = (int)field.GetValue(null);
                Assert.AreNotEqual(0, hash, $"{field.Name} hashed to 0 - likely an empty parameter name string");
            }
        }

        [Test]
        public void NoTwoHumanoidAnimatorParametersShareTheSameHash()
        {
            FieldInfo[] fields = GetHumanoidParameterFields();
            int[] hashes = fields.Select(field => (int)field.GetValue(null)).ToArray();

            Assert.AreEqual(hashes.Length, hashes.Distinct().Count(),
                "Two or more Animations.Humanoid parameters hash to the same value - check for a copy-pasted Animator.StringToHash string");
        }

        private static FieldInfo[] GetHumanoidParameterFields()
        {
            return typeof(Animations.Humanoid)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(int))
                .ToArray();
        }
    }
}
