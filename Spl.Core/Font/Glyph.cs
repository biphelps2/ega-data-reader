using Spl.Core.Graphics;

namespace Spl.Core.Font
{
    public class Glyph
    {
        public int OffsetX { get; init; }
        public int OffsetY { get; init; }
        public int SizeX { get; init; }
        public int SizeY { get; init; }
        public int BaselineOffsetY { get; init; }

        public Rectangle GetSourceRect()
        {
            return new Rectangle(OffsetX, OffsetY, SizeX, SizeY);
        }
    }
}
