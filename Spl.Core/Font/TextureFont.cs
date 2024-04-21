using System.Collections.Generic;
using Spl.Core.Graphics;

namespace Spl.Core.Font
{
    public class TextureFont
    {
        public Texture2D? BaseTexture { get; set; }

        public Dictionary<char, Glyph> Glyphs { get; }
        public int LineHeight { get; }

        public TextureFont(int lineHeight, Dictionary<char, Glyph> glyphs)
        {
            LineHeight = lineHeight;
            Glyphs = glyphs;
        }

        public static int LengthOf(TextureFont textureFont, string str)
        {
            var length = 0;

            foreach (var c in str)
            {
                // Look up glyph, or default to a space.
                var glyph = textureFont.Glyphs.ContainsKey(c) ? textureFont.Glyphs[c] : textureFont.Glyphs[' '];

                length += glyph.SizeX + 1;
            }

            if (str.Length > 0)
            {
                // Take off the last pixel.
                length--;
            }

            return length;
        }
    }
}
