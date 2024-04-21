using System;

namespace Spl.Core
{
    public static class MathHelper
    {
        public static double ToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }

        public static float Lerp(float value1, float value2, float amount)
        {
            return value1 + (value2 - value1) * amount;
        }

        public static readonly float TwoPi = (float)Math.PI * 2;
    }
}
