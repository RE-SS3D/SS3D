using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Utils
{
    public static class MathUtility
    {
        /// <summary>
        /// Real modulus operation working with negative numbers as well.
        /// e.g mod(-3,5) = 2.
        /// This is necessary because % operator is not a modulus operation,
        /// it would return -3 instead of 2.
        /// </summary>
        public static int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
