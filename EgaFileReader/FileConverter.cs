using System.Runtime.InteropServices;
using SDL2;
using Spl.Core;
using Spl.Core.Graphics;

namespace Spl.EgaFileReader
{
    public class FileConverter
    {
        private byte[]? _fileContents;
        private readonly Color[] _palette16Colour;
        private readonly Color[] _palette4Colour;
        private readonly Color[] _palette2Colour;
        private const int NumPixelsPerByte = 8;

        public bool FileIsLoaded => _fileContents is not null;
        public int NumTilesInFile(int tileWidth, int tileHeight, int numPlanes) {
            var x = (_fileContents?.Length ?? 0) * NumPixelsPerByte / tileWidth / tileHeight / numPlanes;
            if(((_fileContents?.Length ?? 0) * NumPixelsPerByte) % (tileWidth* tileHeight * numPlanes) != 0)
            {
                x++;
            }
            return x;
        }

        public FileConverter()
        {
            const byte oneThird = 85;
            const byte twoThirds = 170;
            _palette16Colour = new Color[]
            {
                new(0, 0, 0),
                new(0, 0, twoThirds),
                new(0, twoThirds, 0),
                new(0, twoThirds, twoThirds),

                new(twoThirds, 0, 0),
                new(twoThirds, 0, twoThirds),
                new(twoThirds, oneThird, 0),
                new(twoThirds, twoThirds, twoThirds),

                new(oneThird, oneThird, oneThird),
                new(oneThird, oneThird, 255),
                new(oneThird, 255, oneThird),
                new(oneThird, 255, 255),

                new(255, oneThird, oneThird),
                new(255, oneThird, 255),
                new(255, 255, oneThird),
                new(255, 255, 255),
            };

            _palette4Colour = new Color[]
            {
                new(0, 0, 0),
                new(0, 255, 255),
                new(255, 0, 255),
                new(255, 255, 255)
            };

            _palette2Colour = new Color[]
            {
                new(0, 0, 0),
                new(255, 255, 255)
            };
        }

        public void LoadFileData(string inputPath)
        {
            _fileContents = File.ReadAllBytes(inputPath);
        }

        public (Color[] Data, int Width, int Height) ConvertToRgba(int tileWidth, int tileHeight, int numBitPlanes, int imageWidth, int imageHeight)
        {
            if (_fileContents is null)
            {
                return (Array.Empty<Color>(), 0, 0);
            }

            var planeLengthInBytes = (tileWidth * tileHeight) / NumPixelsPerByte;
            var imageWidthInTiles = imageWidth / tileWidth;
            var imageHeightInTiles = imageHeight / tileHeight;

            var onScreenTextureData = new Color[imageWidthInTiles * tileWidth * imageHeightInTiles * tileHeight];
            for (var i = 0; i < onScreenTextureData.Length; i++)
            {
                onScreenTextureData[i] = Color.Magenta;
            }

            for (var y = 0; y < imageHeightInTiles * tileHeight; y++)
            {
                for (var x = 0; x < imageWidthInTiles * tileWidth; x++)
                {
                    var pixelIdx = (x) + (y * imageWidthInTiles * tileWidth);
                    if (pixelIdx > onScreenTextureData.Length - 1)
                    {
                        onScreenTextureData[pixelIdx] = Color.Cyan;
                        continue;
                    }

                    var tileX = x / tileWidth;
                    var tileY = y / tileHeight;
                    var tileIdx = tileX + tileY * imageWidthInTiles;

                    var numBytesInTile = (tileWidth * tileHeight / NumPixelsPerByte * numBitPlanes);
                    var byteIdx = tileIdx * numBytesInTile
                                  + ((x % tileWidth) / NumPixelsPerByte)
                                  + ((y % tileHeight) * (tileWidth) / NumPixelsPerByte);

                    var palleteIdx = 0;
                    for (var i = 0; i < numBitPlanes; i++)
                    {
                        var lookupAddress = byteIdx + (planeLengthInBytes * i);

                        if (lookupAddress > _fileContents.Length - 1)
                        {
                            onScreenTextureData[pixelIdx] = Color.Magenta;
                            break;
                        }

                        var relevantBitPosition = pixelIdx % NumPixelsPerByte;

                        var val = (_fileContents[lookupAddress] >> (7 - relevantBitPosition)) & 1;
                        palleteIdx = (palleteIdx << 1) | val;
                    }

                    if (pixelIdx > onScreenTextureData.Length - 1)
                    {
                        BasicLogger.LogError("Tried to set pixel out of bounds");
                        break;
                    }

                    var paletteToUse = numBitPlanes == 1 ? _palette2Colour
                        : numBitPlanes == 2 ? _palette4Colour : _palette16Colour;
                    onScreenTextureData[pixelIdx] = paletteToUse[palleteIdx];
                }
            }

            return (onScreenTextureData, imageWidthInTiles * tileWidth, imageHeightInTiles * tileHeight);
        }

        public static void ToPng(string outputPath, Color[] data, int width, int height)
        {
            var pixelData = data.Select(d => d.PackedValue).ToArray();

            // https://stackoverflow.com/a/537722/11678918
            var pinnedArray = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
            var pointer = pinnedArray.AddrOfPinnedObject();

            var s = SDL.SDL_CreateRGBSurfaceFrom(pointer,
                width, height, 32, width * 4,
                0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000);
            if (s == IntPtr.Zero)
            {
                BasicLogger.LogError("ToPng: " + SDL_image.IMG_GetError());
            }

            SDL_image.IMG_SavePNG(s, outputPath);

            SDL.SDL_FreeSurface(s);
        }
    }
}
