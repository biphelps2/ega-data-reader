using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Xml;
using SDL2;
using Spl.Core.Graphics;

namespace Spl.Core.Kra
{
    // Reads out layer data from a .kra (Krita) file.
    public class KraLayerReader : IDisposable
    {
        private class KraLayerInfo
        {
            public string LayerName;
            public int XOffset;
            public int YOffset;
            public byte Opacity;
        }

        private IntPtr _renderer;
        private IntPtr _scratchSurface;
        private Dictionary<string, KraLayerInfo> _fileNameToLayerName;

        public KraLayerReader(IntPtr renderer)
        {
            _renderer = renderer;
            _scratchSurface = SDL.SDL_CreateRGBSurface(
                0, 200, 200, 32,
                0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000);
            if (_scratchSurface == IntPtr.Zero)
            {
                BasicLogger.LogError("Could not create scratch surface: " + SDL_image.IMG_GetError());
            }

            _fileNameToLayerName = new Dictionary<string, KraLayerInfo>();
        }

        public List<(string Id, Texture2D Texture)> CreateTexturesFromKra(string path)
        {
            var result = new List<(string Id, Texture2D Texture)>();

            // A .kra file is a zip, so we unzip it.
            Stream s = File.OpenRead(path);
            using (ZipArchive archive = new ZipArchive(s))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // First, read XML file describing each file layer (including group layers).
                    // We assume we will find this file before the others.
                    if (entry.Name == "maindoc.xml")
                    {
                        // Read the XML.
                        using (var data = entry.Open())
                        {
                            ReadXmlLayerData(data);
                        }
                    }
                    else
                    {
                        // Filter out files we don't care about.
                        if (!entry.FullName.Contains("layers"))
                        {
                            continue;
                        }
                        if (entry.FullName.Contains(".defaultpixel"))
                        {
                            continue;
                        }
                        if (entry.FullName.Contains(".icc"))
                        {
                            continue;
                        }

                        // Try to read the data from this compressed (we assume) file.
                        using (var data = entry.Open())
                        {
                            var info = _fileNameToLayerName[entry.Name];
                            BasicLogger.LogDetail("File found: " + entry.FullName);

                            SDL.SDL_FillRect(_scratchSurface, IntPtr.Zero, 0);

                            LayerFileToScratch(data, info);

                            BasicLogger.LogDetail("Creating texture.");
                            var texture = CreateTextureFromScratchSurface(_renderer);

                            result.Add((_fileNameToLayerName[entry.Name].LayerName, texture));
                            BasicLogger.LogDetail("Added to result.");
                        }
                    }
                }
            }

            BasicLogger.LogDetail($"Completed. Loaded {result.Count} entries.");
            return result;
        }

        private Texture2D CreateTextureFromScratchSurface(IntPtr renderer)
        {
            //Directory.CreateDirectory("temptest");
            //SDL_image.IMG_SavePNG(_scratchSurface, "temptest/" + SplRandom.Random.Next(0, 100000));

            return Texture2D.FromSurface(renderer, _scratchSurface);
        }

        private void LayerFileToScratch(Stream zippedLayerFile, KraLayerInfo info)
        {
            var expectedAtStart = "VERSION 2\nTILEWIDTH 64\nTILEHEIGHT 64\nPIXELSIZE 4\nDATA ";
            for (var j = 0; j < expectedAtStart.Length; j++)
            {
                zippedLayerFile.ReadByte();
            }

            var currentChar = (char)zippedLayerFile.ReadByte();
            string numTilesStr = "";
            while (currentChar != '\n')
            {
                numTilesStr += currentChar;
                currentChar = (char)zippedLayerFile.ReadByte();
            }

            var numTiles = int.Parse(numTilesStr);

            BasicLogger.LogDetail("numTiles: " + numTiles);

            for (var i = 0; i < numTiles; i++)
            {
                // Read each compressed tile.
                currentChar = (char)zippedLayerFile.ReadByte();
                string headerStr = "";
                while (currentChar != '\n')
                {
                    headerStr += currentChar;
                    currentChar = (char)zippedLayerFile.ReadByte();
                }

                var headerValues = headerStr.Split(",");
                var tileLeft = int.Parse(headerValues[0]);
                var tileTop = int.Parse(headerValues[1]);
                var compressionType = headerValues[2];
                var compressedSize = int.Parse(headerValues[3]);
                var isCompressed = zippedLayerFile.ReadByte() == 1;

                // tile width * tile height * pixel size.
                var uncompressedSize = 64 * 64 * 4;
                var uncompressedResult = new byte[uncompressedSize];

                var compressedData = new byte[compressedSize - 1];
                zippedLayerFile.ReadExactly(compressedData);

                Decompress(compressedData, compressedSize - 1, uncompressedResult, uncompressedSize);

                var pixels = new uint[uncompressedSize / 4];
                for (var j = 0; j < uncompressedSize / 4; j++)
                {
                    pixels[j] = new Color(
                        uncompressedResult[pixels.Length * 2 + j],
                        uncompressedResult[pixels.Length * 1 + j],
                        uncompressedResult[pixels.Length * 0 + j],
                        uncompressedResult[pixels.Length * 3 + j]).PackedValue;
                }

                BasicLogger.LogDetail($"Adding tile to surface at {tileLeft + info.XOffset} {tileTop + info.YOffset}");
                AddTileToSurface(tileLeft + info.XOffset, tileTop + info.YOffset, pixels, info.Opacity);
            }

            BasicLogger.LogDetail("File complete.");
        }

        private void ReadXmlLayerData(Stream zippedXmlFile)
        {
            var reader = XmlReader.Create(zippedXmlFile, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse
            });

            // Since we expect all layers to appear in exactly one group, we
            // can do this simple "which group am I in" check.
            var mostRecentLayer = "";
            string currentGroupLayer = "";
            while (reader.Read())
            {
                if (reader.IsStartElement() && reader.Name == "layers")
                {
                    if(mostRecentLayer != "")
                    {
                        currentGroupLayer = mostRecentLayer + "-";
                    }
                }

                if (reader.IsStartElement() && reader.Name == "layer")
                {
                    // Get layer info.
                    var fileName = reader.GetAttribute("filename")!;

                    _fileNameToLayerName.Add(fileName, new KraLayerInfo
                    {
                        LayerName = currentGroupLayer + reader.GetAttribute("name")!,
                        XOffset = int.Parse(reader.GetAttribute("x")!),
                        YOffset = int.Parse(reader.GetAttribute("y")!),
                        Opacity = byte.Parse(reader.GetAttribute("opacity")!)
                    });

                    mostRecentLayer = reader.GetAttribute("name")!;
                }
            }
        }

        private void AddTileToSurface(int x, int y, uint[] data, byte alphaMod)
        {
            // https://stackoverflow.com/a/537722/11678918
            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            const int TileWidth = 64;
            const int TileHeight = 64;

            var s = SDL.SDL_CreateRGBSurfaceFrom(pointer,
                TileWidth, TileHeight, 32, TileWidth * 4,
                0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000);
            if (s == IntPtr.Zero)
            {
                BasicLogger.LogError(SDL_image.IMG_GetError());
            }

            // Apply layer opacity.
            SDL.SDL_SetSurfaceBlendMode(s, SDL.SDL_BlendMode.SDL_BLENDMODE_NONE);
            SDL.SDL_SetSurfaceAlphaMod(s, alphaMod);

            var rect = new SDL.SDL_Rect
            {
                x = 0, y = 0, w = 64, h = 64
            };

            var to = new SDL.SDL_Rect
            {
                x = x, y = y, w = 64, h = 64
            };

            SDL.SDL_BlitSurface(s, ref rect, _scratchSurface, ref to);

        }

        // https://github.com/2shady4u/godot-kra-psd-importer/blob/master/docs/KRA_FORMAT.md
        // https://invent.kde.org/graphics/krita/-/blob/krita/4.4.8/libs/image/tiles3/swap/kis_lzf_compression.cpp?ref_type=heads
        private static void Decompress(byte[] input, int compressedLength, byte[] output, int outputLength)
        {
            int ip = 0;
            int ipLimit = compressedLength - 1; // -1 for an unknown reason
            int op = 0;
            int opLimit = outputLength;

            // Process entire input.
            while (ip < ipLimit)
            {
                int ctrl = input[ip] + 1;
                int offset = (input[ip] & 31) << 8;
                int len = input[ip++] >> 5;

                if (ctrl < 33)
                {
                    Array.Copy(input, ip, output, op, ctrl);
                    ip += ctrl;
                    op += ctrl;
                }
                else
                {
                    len--;
                    var reef = op - offset;
                    reef--;

                    if (len == 7 - 1)
                    {
                        len += input[ip++];
                    }

                    reef -= input[ip++];

                    if (op + len + 3 > opLimit)
                    {
                        throw new Exception();
                    }

                    if (reef < 0)
                    {
                        throw new Exception();
                    }

                    // Copy in 3 bytes.
                    output[op++] = output[reef++];
                    output[op++] = output[reef++];
                    output[op++] = output[reef++];

                    if (len > 0)
                    {
                        for (; len > 0; --len)
                        {
                            output[op++] = output[reef++];
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            SDL.SDL_FreeSurface(_scratchSurface);
        }
    }
}
