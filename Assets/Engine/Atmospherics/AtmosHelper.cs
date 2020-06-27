using System.Runtime.CompilerServices;

namespace SS3D.Engine.Atmospherics
{
    public static class AtmosHelper
    {
        public static float[] ArrayNorm(float[] arr, float normal)
        {
            float div = ArraySize(arr);

            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = arr[i] / div * normal;
            }

            return arr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ArraySize(float[] arr)
        {
            float size = 0f;

            foreach (float t in arr)
            {
                size += t;
            }

            return size;
        }

        public static bool ArrayZero(float[] arr, float mixRate)
        {
            foreach (float t in arr)
            {
                if (t / mixRate > 0.1f) { return false; }
            }

            return true;
        }
    }
}
