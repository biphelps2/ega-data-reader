using System;
using JetBrains.Annotations;
using Spl.Core.Graphics;

namespace Spl.Core
{
    [PublicAPI]
    public static class ColourHelpers
    {
        // https://stackoverflow.com/a/8510751/11678918
        public static Color RotateHue(Color c, float degrees)
        {
            int Clamp(double x)
            {
                if (x < 0)
                {
                    return 0;
                }

                if (x > 255)
                {
                    return 255;
                }

                return (int)(x + 0.5);
            }

            var cosA = Math.Cos(MathHelper.ToRadians(degrees));
            var sinA = Math.Sin(MathHelper.ToRadians(degrees));

            var oneThird = 1 / 3f;

            var m0 = cosA + (1 - cosA) / 3f;
            var m1 = oneThird * (1 - cosA) - Math.Sqrt(oneThird) * sinA;
            var m2 = oneThird * (1 - cosA) + Math.Sqrt(oneThird) * sinA;
            var m3 = oneThird * (1 - cosA) + Math.Sqrt(oneThird) * sinA;
            var m4 = cosA + oneThird * (1 - cosA);
            var m5 = oneThird * (1 - cosA) - Math.Sqrt(oneThird) * sinA;
            var m6 = oneThird * (1 - cosA) - Math.Sqrt(oneThird) * sinA;
            var m7 = oneThird * (1 - cosA) + Math.Sqrt(oneThird) * sinA;
            var m8 = cosA + oneThird * (1 - cosA);

            var rx = c.R * m0 + c.G * m1 + c.B * m2;
            var gx = c.R * m3 + c.G * m4 + c.B * m5;
            var bx = c.R * m6 + c.G * m7 + c.B * m8;
            return new Color(Clamp(rx) / 255f, Clamp(gx) / 255f, Clamp(bx) / 255f);
        }

        // Hue/Chroma/Luma to Red/Green/Blue
        // algorithm taken from Wiki, modified to fit [0, 1] hue range
        // https://en.wikipedia.org/wiki/HSL_and_HSV#From_luma.2Fchroma.2Fhue
        public static Color HCYToRGB(int h, float c, float y)
        {
            // R, G, B is coefficients for red/green/blue.
            // This is h * 6 because if we were using range 0-360,
            // the algorithm calls for h / 60degrees.
            var hm = h / 60f;
            var x = c * (1 - Math.Abs(hm % 2 - 1));
            float r1, g1, b1;
            if (hm >= 0 && hm <= 1)
            {
                r1 = c;
                g1 = x;
                b1 = 0;
            }
            else if (hm >= 1 && hm < 2)
            {
                r1 = x;
                g1 = c;
                b1 = 0;
            }
            else if (hm >= 2 && hm <= 3)
            {
                r1 = 0;
                g1 = c;
                b1 = x;
            }
            else if (hm >= 3 && hm < 4)
            {
                r1 = 0;
                g1 = x;
                b1 = c;
            }
            else if (hm >= 4 && hm < 5)
            {
                r1 = x;
                g1 = 0;
                b1 = c;
            }
            else if (hm >= 5 && hm <= 6)
            {
                r1 = c;
                g1 = 0;
                b1 = x;
            }
            else
            {
                r1 = 0;
                g1 = 0;
                b1 = 0;
            }
            // Rec. 709 luma coefficients
            // https://en.wikipedia.org/wiki/Luma_%28video%29
            const float r = 0.3f;
            const float g = 0.59f;
            const float b = 0.11f;

            float m = y - (r * r1 + g * g1 + b * b1);

            return new Color(Math.Clamp(r1 + m, 0, 1), Math.Clamp(g1 + m, 0, 1), Math.Clamp(b1 + m, 0, 1));
        }
    }
}
