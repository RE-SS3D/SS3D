using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Atmospherics
{
    public static class AtmosHelper
    {
        public static float[] ArrayDiff(float[] arr1, float[] arr2, float mixRate)
        {
            float[] difference = new float[AtmosManager.numOfGases];

            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                difference[i] = (arr1[i] - arr2[i]) * mixRate;
            }

            return difference;
        }

        public static float[] ArrayNorm(float[] arr, float normal)
        {
            float div = ArraySize(arr);

            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                arr[i] = arr[i] / div * normal;
            }

            return arr;
        }

       public static float[] ArraySum(float[] arr1, float[] arr2)
        {
            float[] sum = new float[AtmosManager.numOfGases];

            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                sum[i] = arr1[i] + arr2[i];
            }

            return sum;
        }

        public static float ArraySize(float[] arr)
        {
            float size = 0f;

            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                size += arr[i];
            }

            return size;
        }

        public static bool ArrayZero(float[] arr, float mixRate)
        {
            for (int i = 0; i < AtmosManager.numOfGases; ++i)
            {
                if (arr[i] / mixRate > 0.1f) { return false; }
            }
            return true;
        }
    }
}
