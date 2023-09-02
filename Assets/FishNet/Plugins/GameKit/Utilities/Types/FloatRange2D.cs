﻿using UnityEngine;

namespace GameKit.Utilities.Types
{


    [System.Serializable]

    public struct FloatRange2D
    {
        public FloatRange X;
        public FloatRange Y;

        public FloatRange2D(FloatRange x, FloatRange y)
        {
            X = x;
            Y = y;
        }


        public FloatRange2D(float xMin, float xMax, float yMin, float yMax)
        {
            X = new FloatRange(xMin, xMax);
            Y = new FloatRange(yMin, yMax);
        }

        public Vector2 Clamp(Vector2 original)
        {
            return new Vector2(
                ClampX(original.x),
                ClampY(original.y)
                );
        }

        public Vector3 Clamp(Vector3 original)
        {
            return new Vector3(
                ClampX(original.x),
                ClampY(original.y),
                original.z
                );
        }

        public float ClampX(float original)
        {
            return Mathf.Clamp(original, X.Minimum, X.Maximum);
        }

        public float ClampY(float original)
        {
            return Mathf.Clamp(original, Y.Minimum, Y.Maximum);
        }

    }



}