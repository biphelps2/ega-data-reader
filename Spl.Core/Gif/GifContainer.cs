using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Spl.Core.Graphics;

namespace Spl.Core.Gif
{
    // References:
    // https://web.archive.org/web/20070715193731/http://odur.let.rug.nl/~kleiweg/gif/netscape.html
    // http://www.matthewflickinger.com/lab/whatsinagif/
    // https://github.com/Banane9/SharpGif/blob/master/SharpGif/GifLZW.cs
    // Note:
    // - Numbers are stored as little-endian (least significant byte first)
    public class GifContainer
    {
        // 6 byte header for version 89a ("GIF89a").
        private static readonly byte[] GifHeader = { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };

        // Set NETSCAPE2.0 application extension data so we can loop our gif.
        // 0x21 = extension chunk.
        // 0xff = application extension chunk.
        // 0x0B = 11 bytes follow of fixed length data.
        // 0x03 = 3 bytes follow of fixed length data (0x01, then 2 bytes for "times to repeat")
        // 0x00 = end of extension chunk.
        private static readonly byte[] ApplicationExtension =
        {
            0x21, 0xff,
            0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30,
            0x03, 0x01, 0x00, 0x00, 0x00
        };

        private const int PaletteWidth = 4;
        private const int PaletteHeight = 4;
        private const int NumColours = PaletteWidth * PaletteHeight;

        private readonly int _width;
        private readonly int _height;
        private readonly int _scale;
        private readonly byte[] _paletteData;
        private readonly Dictionary<Color, int> _colourLookup;

        private readonly GifLzwEncoder _lzwEncoder;
        private bool _isComplete;

        [PublicAPI]
        public readonly List<int[]> GifFrames;

        [PublicAPI]
        public List<byte> GifFileData;

        // Assumes palette size 4x4 with black in top-left corner.
        // Assumes we want a looping gif.
        public GifContainer(int width, int height, int scale, Texture2D palette)
        {
            _scale = scale;
            _width = width;
            _height = height;

            GifFrames = new List<int[]>();
            GifFileData = new List<byte>();

            var textureData = new Color[NumColours];
            palette.GetData(textureData);

            // Now we set the palette data.
            // This will be [16 colours * 3 bytes per colour] in size.
            _paletteData = new byte[NumColours * 3];
            _colourLookup = new Dictionary<Color, int>();

            for (var i = 0; i < PaletteWidth; i++)
            {
                for (var j = 0; j < PaletteHeight; j++)
                {
                    var idx = i + j * PaletteWidth;

                    _paletteData[idx * 3 + 0] = textureData[idx].R;
                    _paletteData[idx * 3 + 1] = textureData[idx].G;
                    _paletteData[idx * 3 + 2] = textureData[idx].B;

                    if (!_colourLookup.ContainsKey(textureData[idx]))
                    {
                        _colourLookup.Add(textureData[idx], idx);
                    }
                }
            }

            _lzwEncoder = new GifLzwEncoder(NumColours);

            InitGif();
        }

        public void InitGif()
        {
            _isComplete = false;

            // 7 bytes Logical screen descriptor
            // packedField: Reading most-significant bit first:
            // 1 = there's a global palette.
            // 001 = colour resolution.
            // 1 = it's sorted.
            // 000 = num colours in palette. We use NumColours here to avoid hardcoding.
            var packedField = (byte)(0b10011000 | ((int)Math.Ceiling(Math.Log2(NumColours) - 1) & 0b111));
            // 0x00: Our background colour, black, is at index 0 in the palette.
            // 0x00: A bit mysterious but everyone sets it to 0.
            byte[] logicalScreenDescriptor =
            {
                (byte)((_width * _scale) & 0b11111111), (byte)(((_width * _scale) >> 8) & 0b11111111),
                (byte)((_height * _scale) & 0b11111111), (byte)(((_height * _scale) >> 8) & 0b11111111),
                packedField,
                0x00,
                0x00
            };

            GifFileData.Clear();
            GifFileData.AddRange(GifHeader);
            GifFileData.AddRange(logicalScreenDescriptor);
            GifFileData.AddRange(_paletteData);
            GifFileData.AddRange(ApplicationExtension);
        }

        public void AddFrameData(Color[] data)
        {
            // Get the palette data arrays.
            var paletteLookupData = new int[_width * _height];
            for (var i = 0; i < paletteLookupData.Length; i++)
            {
                if (_colourLookup.ContainsKey(data[i]))
                {
                    paletteLookupData[i] = _colourLookup[data[i]];
                }
                else
                {
                    paletteLookupData[i] = 8; // Bright red
                }
            }

            GifFrames.Add(paletteLookupData);
        }

        /// <summary>
        /// Finish off the data.
        /// </summary>
        public void Finish()
        {
            var numWrittenFrames = 0;

            if (GifFrames.Any())
            {
                const int perFrameLength = 0x03;

                // Full-sized frame.
                // 10 bytes Image descriptor.
                // 0x2C: Never changes.
                // 0x00 0x00: x offset from left. We always render a full frame, so this is always 0.
                // 0x00 0x00: y offset from top. We always render a full frame, so this is always 0.
                var fullFrameImageDescriptorChunk = new byte[]
                {
                    0x2C, 0x00, 0x00, 0x00, 0x00,
                    (byte)((_width * _scale) & 0b11111111), (byte)(((_width * _scale) >> 8) & 0b11111111),
                    (byte)((_height * _scale) & 0b11111111), (byte)(((_height * _scale) >> 8) & 0b11111111),
                    0x00
                };

                var thisFrameLength = perFrameLength;

                var lastUniqueFrame = 0;
                var isFirstDrawnFrame = true;

                for (var f = 0; f < GifFrames.Count; f++)
                {
                    var frameNativeRes = GifFrames[f];
                    var frameScaled = new int[_width * _scale * _height * _scale];

                    // If scale = 2, every pixel must be copied twice in a row.
                    for (var y = 0; y < _height; y++)
                    {
                        for (var x = 0; x < _width; x++)
                        {
                            // We're looking at 1 pixel from the source data.
                            var idx = x + y * _width;

                            var xInLarger = x * _scale;
                            var yInLarger = y * _scale;

                            var topLeftIdx = xInLarger + yInLarger * _width * _scale;

                            for (var sY = 0; sY < _scale; sY++)
                            {
                                for (var sX = 0; sX < _scale; sX++)
                                {
                                    frameScaled[topLeftIdx + sX + sY * _width * _scale] = frameNativeRes[idx];
                                }
                            }
                        }
                    }

                    // Check next frame to see if it's identical.
                    var nextFrame = f + 1 == GifFrames.Count ? null : GifFrames[f + 1];
                    if (nextFrame != null)
                    {
                        var nextFrameIdentical = true;

                        // Find the bounding box of all changed data.
                        for (var x = 0; x < _width; x++)
                        {
                            for (var y = 0; y < _height; y++)
                            {
                                var idx = x + y * _width;
                                if (nextFrame[idx] != frameNativeRes[idx])
                                {
                                    nextFrameIdentical = false;
                                    break;
                                }
                            }

                            if (!nextFrameIdentical)
                            {
                                break;
                            }
                        }

                        // We can skip this frame because the next one is the same.
                        // Our lastUniqueFrame will lag behind, as intended.
                        if (nextFrameIdentical)
                        {
                            // This frame should last longer.
                            thisFrameLength += perFrameLength;
                            continue;
                        }
                    }

                    if (isFirstDrawnFrame)
                    {
                        isFirstDrawnFrame = false;

                        // Non transparent, with frame length = thisFrameLength.
                        var variableFrameLengthGce = new byte[]
                        {
                            0x21, 0xF9,
                            0x04, 0x04,
                            (byte)(thisFrameLength & 0b11111111), (byte)((thisFrameLength >> 8) & 0b11111111),
                            0x00,
                            0x00
                        };

                        // First frame, so create full area.
                        var imageData = _lzwEncoder.Encode(frameScaled).ToArray();
                        GifFileData.AddRange(variableFrameLengthGce);
                        GifFileData.AddRange(fullFrameImageDescriptorChunk);
                        GifFileData.AddRange(imageData);

                        lastUniqueFrame = f;
                        thisFrameLength = perFrameLength;

                        numWrittenFrames++;
                    }
                    else
                    {
                        // f != 0 so we can check lastUniqueFrame here.
                        var lastFrameToCompare = GifFrames[lastUniqueFrame];

                        var firstDifferentRow = -1;
                        var lastDifferentRow = -1;
                        var firstDifferentColumn = -1;
                        var lastDifferentColumn = -1;

                        // Find the bounding box of f changed data.
                        for (var y = 0; y < _height; y++)
                        {
                            var rowIdentical = true;

                            for (var x = 0; x < _width; x++)
                            {
                                var idx = x + y * _width;
                                if (lastFrameToCompare[idx] != frameNativeRes[idx])
                                {
                                    rowIdentical = false;
                                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                    // Full if-else statement is used for readability.
                                    if (firstDifferentColumn == -1)
                                    {
                                        firstDifferentColumn = x;
                                    }
                                    else
                                    {
                                        firstDifferentColumn = Math.Min(x, firstDifferentColumn);
                                    }

                                    lastDifferentColumn = Math.Max(x, lastDifferentColumn);
                                }
                            }

                            if (!rowIdentical)
                            {
                                if (firstDifferentRow == -1)
                                {
                                    firstDifferentRow = y;
                                }

                                lastDifferentRow = y;
                            }
                        }

                        // Calculate region size from difference checks.
                        var startY = firstDifferentRow;
                        var endY = lastDifferentRow + 1;
                        var height = lastDifferentRow - firstDifferentRow + 1;
                        var startX = firstDifferentColumn;
                        var endX = lastDifferentColumn + 1;
                        var width = lastDifferentColumn - firstDifferentColumn + 1;

                        // Non transparent, with frame length = thisFrameLength.
                        var variableFrameLengthGce = new byte[]
                        {
                            0x21, 0xF9,
                            0x04, 0x04,
                            (byte)(thisFrameLength & 0b11111111), (byte)((thisFrameLength >> 8) & 0b11111111),
                            0x00,
                            0x00
                        };

                        var imageDescriptorChunk = new byte[]
                        {
                            0x2C,
                            (byte)((startX * _scale) & 0b11111111), (byte)(((startX * _scale) >> 8) & 0b11111111),
                            (byte)((startY * _scale) & 0b11111111), (byte)(((startY * _scale) >> 8) & 0b11111111),
                            (byte)((width * _scale) & 0b11111111), (byte)(((width * _scale) >> 8) & 0b11111111),
                            (byte)((height * _scale) & 0b11111111), (byte)(((height * _scale) >> 8) & 0b11111111),
                            0x00
                        };

                        // Extract a rectangular region from the frame data.
                        var rectData = new int[width * height * _scale * _scale];
                        var rectDataIdx = 0;
                        for (var i = 0; i < _width * _scale * _height * _scale; i++)
                        {
                            var x = i % (_width * _scale);
                            var y = i / (_width * _scale);

                            if (x >= startX * _scale && x < endX * _scale)
                            {
                                if (y >= startY * _scale && y < endY * _scale)
                                {
                                    rectData[rectDataIdx++] = frameScaled[i];
                                }
                            }
                        }

                        var imageData = _lzwEncoder.Encode(rectData).ToArray();

                        GifFileData.AddRange(variableFrameLengthGce);
                        GifFileData.AddRange(imageDescriptorChunk);
                        GifFileData.AddRange(imageData);

                        thisFrameLength = perFrameLength;

                        numWrittenFrames++;

                        // We just drew a unique frame.
                        lastUniqueFrame = f;
                    }
                }
            }

            // 0x3B: EOF marker ";"
            GifFileData.Add(0x3b);

            BasicLogger.LogInfo($"Written {numWrittenFrames} frames to gif.");

            _isComplete = true;
        }

        /// <summary>
        /// Save to file.
        /// </summary>
        /// <param name="saveTo">Path to create file at.</param>
        public void SaveToFile(string saveTo)
        {
            if (_isComplete)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(saveTo)!);
                File.WriteAllBytes(saveTo, GifFileData.ToArray());
            }
        }
    }
}
