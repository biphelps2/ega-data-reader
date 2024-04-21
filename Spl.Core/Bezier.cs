using System;
using JetBrains.Annotations;
using Spl.Core.Monogame;

namespace Spl.Core
{
    [PublicAPI]
    public static class Beziers
    {
        // https://stackoverflow.com/a/6712095
        public static Vector2 FindP1(Vector2 p0, Vector2 pc, Vector2 p2, float ct)
        {
            // Transform function and solve for p1:
            // Pc = P0*.25 + P1*2*.25 + P2*.25
            // P1 = (Pc - P0 * .25 - P2 * .25) / .5
            //    = 2 * Pc - P0 / 2 - P2 / 2

            // x1 = 2*xc - x0/2 - x2/2
            // y1 = 2*yc - y0/2 - y2/2

            // - (p1 * 2 * ct * (1 - ct)) = p0 * (float)Math.Pow(ct, 2) + p2 * (float)Math.Pow(1 - ct, 2) - pc;
            // (p1 * 2 * ct * (1 - ct) = pc - p0 * (float)Math.Pow(ct, 2) - p2 * (float)Math.Pow(1 - ct, 2);
            // pt = (pc - p0 * (float)Math.Pow(ct, 2) - p2 * (float)Math.Pow(1 - ct, 2)) / (2 * ct * (1 - ct))
            return (pc - p0 * (float)Math.Pow(ct, 2) - p2 * (float)Math.Pow(1 - ct, 2)) / (2 * ct * (1 - ct));
        }

        public static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            // Definition of a quad bezier:
            // P(t) = P0*t^2 + P1*2*t*(1-t) + P2*(1-t)^2

            return p0 * (float)Math.Pow(t, 2) + p1 * 2 * t * (1 - t) + p2 * (float)Math.Pow(1 - t, 2);
        }

        public static Vector2 BezierForPoint(Vector2 p0, Vector2 pc, Vector2 p2, float t, float ct)
        {
            var p1 = FindP1(p0, pc, p2, ct);
            return Bezier(p0, p1, p2, t);
        }
    }
}
