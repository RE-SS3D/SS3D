using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Utils
{
    public static class MathUtility
    {
        public static int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
