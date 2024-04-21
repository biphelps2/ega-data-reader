using System;

namespace Spl.Core.Graphics
{
    public struct Color
    {
        public uint PackedValue;
        public byte R => (byte)(PackedValue >> 0);
        public byte G => (byte)(PackedValue >> 8);
        public byte B => (byte)(PackedValue >> 16);
        public byte A => (byte)(PackedValue >> 24);

        public static Color Transparent = new Color(1f, 1f, 1f, 0f);
        public static Color White = new Color(1f, 1f, 1f);
        public static Color Black = new Color(0f, 0f, 0f);
        public static Color Red = new Color(1f, 0f, 0f);
        public static Color Green = new Color(0f, 0.5f, 0f);
        public static Color Blue = new Color(0f, 0f, 1f);
        public static Color Cyan = new Color(0f, 1f, 1f);
        public static Color Magenta = new Color(1f, 0f, 1f);
        public static Color Yellow = new Color(1f, 1f, 0f);

        public Color(uint packed)
        {
            PackedValue = packed;
        }

        public Color(byte r, byte g, byte b, byte a = 255)
            : this((r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24))) { }

        public Color(float r, float g, float b, float a = 1f)
            : this((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), (byte)(a * 255)) { }

        public Color(Color c, float a)
        {
            var aB = (byte)(a * 255);
            PackedValue = (c.PackedValue & 0xffffff) | ((uint)aB << 24);
        }

        public static Color Lerp(Color value1, Color value2, Single amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return new Color(
                (byte)MathHelper.Lerp(value1.R, value2.R, amount),
                (byte)MathHelper.Lerp(value1.G, value2.G, amount),
                (byte)MathHelper.Lerp(value1.B, value2.B, amount),
                (byte)MathHelper.Lerp(value1.A, value2.A, amount));
        }

        public static bool operator ==(Color c1, Color c2)
        {
            return c1.PackedValue == c2.PackedValue;
        }

        public static bool operator !=(Color c1, Color c2)
        {
            return !(c1 == c2);
        }

        public bool Equals(Color other)
        {
            return PackedValue == other.PackedValue;
        }

        public override bool Equals(object? obj)
        {
            return obj is Color other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)PackedValue;
        }

    }
}
