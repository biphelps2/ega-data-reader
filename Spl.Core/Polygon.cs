using JetBrains.Annotations;
using Spl.Core.Monogame;

namespace Spl.Core
{
    [PublicAPI]
    public class Polygon
    {
        public Vector2[] Points { get; }
        public (Vector2, Vector2)[] Lines
        {
            get
            {
                var lines = new (Vector2, Vector2)[Points.Length];

                var j = Points.Length - 1;
                for (var i = 0; i < Points.Length; i++)
                {
                    lines[i] = (Points[i], Points[j]);
                    j = i;
                }

                return lines;
            }
        }

        public Polygon(Vector2[] p)
        {
            Points = p;
        }

        // https://stackoverflow.com/a/14998816/11678918
        public bool PointIntersects(Vector2 testPoint)
        {
            var result = false;
            var j = Points.Length - 1;
            for (var i = 0; i < Points.Length; i++)
            {
                if (Points[i].Y < testPoint.Y && Points[j].Y >= testPoint.Y || Points[j].Y < testPoint.Y && Points[i].Y >= testPoint.Y)
                {
                    if (Points[i].X + (testPoint.Y - Points[i].Y) / (Points[j].Y - Points[i].Y) * (Points[j].X - Points[i].X) < testPoint.X)
                    {
                        result = !result;
                    }
                }

                j = i;
            }

            return result;
        }

        // https://stackoverflow.com/a/1968345/11678918
        public static (bool, Vector2) LineIntersects(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var s1X = p1.X - p0.X;
            var s1Y = p1.Y - p0.Y;
            var s2X = p3.X - p2.X;
            var s2Y = p3.Y - p2.Y;

            var s = (-s1Y * (p0.X - p2.X) + s1X * (p0.Y - p2.Y)) / (-s2X * s1Y + s1X * s2Y);
            var t = ( s2X * (p0.Y - p2.Y) - s2Y * (p0.X - p2.X)) / (-s2X * s1Y + s1X * s2Y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            {
                // Collision detected.
                return (true, new Vector2(p0.X + (t * s1X), p0.Y + (t * s1Y)));
            }

            // No collision.
            return (false, Vector2.Zero);
        }
    }
}
