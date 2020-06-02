using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public static class AtmosHelper
    {
        public static bool ArrayZero(float[] arr, float mixRate)
        {
            for (int i = 0; i < Gas.numOfGases; ++i)
            {
                if (arr[i] / mixRate > 0.1f) { return false; }
            }
            return true;
        }
    }
}
