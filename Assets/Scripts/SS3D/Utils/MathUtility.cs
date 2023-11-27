using System;
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
        /// <summary>
        /// Bernstein algorithm for drawing lines in a grid.
        /// </summary>
        /// <returns>Tiles, that line passes through</returns>
        public static Vector2[] FindTilesOnLine(Vector2 firstPoint, Vector2 secondPoint)
        {
            List<Vector2> list = new();
            int x = (int)firstPoint.x;
            int y = (int)firstPoint.y;
            int x2 = (int)secondPoint.x;
            int y2 = (int)secondPoint.y;
            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) 
                dx1 = -1; 
            else if (w > 0) 
                dx1 = 1;
            if (h < 0) 
                dy1 = -1; 
            else if (h > 0) 
                dy1 = 1;
            if (w < 0) 
                dx2 = -1; 
            else if (w > 0) 
                dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (!(longest > shortest)) 
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) 
                    dy2 = -1; 
                else if (h > 0) 
                    dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1 ;
            for (int i = 0; i <= longest; i++) 
            {
                list.Add(new(x, y));
                numerator += shortest;
                if (!(numerator < longest)) 
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                } 
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }
            return list.ToArray();
        }
    }
}
