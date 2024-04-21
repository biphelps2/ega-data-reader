using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SDL2;

namespace Spl.Core.Graphics
{
    public class Texture2D
    {
        public int Width;
        public int Height;

        public IntPtr TexturePtr;
        public uint[] PixelData; // Could be a Color...

        public static IntPtr? Renderer;

        public static Texture2D Empty => FromData(Array.Empty<Color>(), 0, 0);

        public static Texture2D FromFile(string path)
        {
            Debug.Assert(Renderer != null, nameof(Renderer) + " != null");

            var fullPath = Path.Join(SdlGame.BasePath, path);

            BasicLogger.LogInfo("Loading texture from: " + fullPath);

            var readableTexHopefully = SDL_image.IMG_Load(fullPath);
            if (readableTexHopefully == IntPtr.Zero)
            {
                BasicLogger.LogError("Texture2D.FromFile Error: " + SDL_image.IMG_GetError());
            }

            var ver = Marshal.PtrToStructure<SDL.SDL_Surface>(readableTexHopefully);

            var actualPixels = new int[ver.w * ver.h];
            Marshal.Copy(ver.pixels, actualPixels, 0, actualPixels.Length);

            var actualPixelsConverted = actualPixels.Select(p => (uint)p)
                .ToArray();

            SDL.SDL_FreeSurface(readableTexHopefully);

            var tex = SDL_image.IMG_LoadTexture(Renderer.Value, fullPath);
            if (tex == IntPtr.Zero)
            {
                BasicLogger.LogError("FromFile:" + SDL_image.IMG_GetError());
            }

            return new Texture2D(tex, actualPixelsConverted, ver.w, ver.h);
        }

        public static Texture2D FromData(Color[] data, int width, int height)
        {
            var toIntArray = data
                .Select(d => d.PackedValue)
                .ToArray();

            return FromData(toIntArray, width, height);
        }
        public static Texture2D FromData(uint[] data, int width, int height)
        {
            Debug.Assert(Renderer != null, nameof(Renderer) + " != null");

            // https://stackoverflow.com/a/537722/11678918
            var pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            var pointer = pinnedArray.AddrOfPinnedObject();

            var s = SDL.SDL_CreateRGBSurfaceFrom(pointer,
                width, height, 32, width * 4,
                0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000);
            if (s == IntPtr.Zero)
            {
                BasicLogger.LogError("FromData surface:" + SDL_image.IMG_GetError());
            }

            var t = SDL.SDL_CreateTextureFromSurface(Renderer.Value, s);
            if (t == IntPtr.Zero)
            {
                BasicLogger.LogError("FromData texture:" + SDL_image.IMG_GetError());
            }

            SDL.SDL_FreeSurface(s);

            pinnedArray.Free();

            return new Texture2D(t, data, width, height);
        }

        // Surface is external, and should not be disposed of here.
        public static Texture2D FromSurface(IntPtr renderer, IntPtr surface)
        {
            BasicLogger.LogDetail("PtrToStructure... " + surface);
            var ver = Marshal.PtrToStructure<SDL.SDL_Surface>(surface);

            BasicLogger.LogDetail($"Result: w {ver.w} h {ver.h} p {ver.pixels}");

            var actualPixels = new int[ver.w * ver.h];
            Marshal.Copy(ver.pixels, actualPixels, 0, actualPixels.Length);

            var actualPixelsConverted = actualPixels.Select(p => (uint)p)
                .ToArray();

            BasicLogger.LogDetail($"Creating texture from surface. r {renderer} s {surface}");
            var tex = SDL.SDL_CreateTextureFromSurface(renderer, surface);
            if (tex == IntPtr.Zero)
            {
                BasicLogger.LogError("FromSurface:" + SDL_image.IMG_GetError());
            }

            BasicLogger.LogDetail("new Texture2d()");
            return new Texture2D(tex, actualPixelsConverted, ver.w, ver.h);
        }

        public void OverrideData(uint[] data, int width, int height)
        {
            SDL.SDL_ClearError();
            SDL.SDL_DestroyTexture(TexturePtr);

            if(!string.IsNullOrWhiteSpace(SDL.SDL_GetError()))
            {
                BasicLogger.LogError("Could not destroy texture: " + SDL.SDL_GetError());
            }

            var temp = FromData(data, width, height);
            TexturePtr = temp.TexturePtr;
        }

        private Texture2D(IntPtr texturePtr, uint[] pixelData, int width, int height)
        {
            TexturePtr = texturePtr;
            PixelData = pixelData;
            Width = width;
            Height = height;
        }

        public void GetData(Color[] toFill)
        {
            Array.Copy(PixelData, toFill, PixelData.Length);
        }
    }
}
