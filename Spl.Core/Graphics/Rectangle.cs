namespace Spl.Core.Graphics
{
    public struct Rectangle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public static Rectangle Empty = new Rectangle();

        public int Left => X;
        public int Right => X + Width;
        public int Top => Y;
        public int Bottom => Y + Height;

        public bool IsEmpty => Width == 0 && Height == 0 && X == 0 && Y == 0;
        public Point Location => new Point(X, Y);
        public Point Size => new Point(Width, Height);
        public Point Center => new Point(X + Width / 2, Y + Height / 2);
    }
}
