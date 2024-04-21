using Spl.Core;

namespace Spl.EgaFileReader
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BasicLogger.SetName("efr");

            if (args.Contains("-h") || args.Contains("--help"))
            {
                Console.WriteLine("Usage: efr [-u] -i FILEPATH -o OUTPATH -t 32x32 -p 4");
                Console.WriteLine("  [-h] [--help]: Show this page");
                Console.WriteLine("  [-i] [--input]: Path to EGA data file");
                Console.WriteLine("  [-o] [--output]: Path to PNG output file");
                Console.WriteLine("  [-t] [--tilesize]: Tile dimensions, x and y separated by 'x' e.g. 32x32");
                Console.WriteLine("  [-z] [--outputwidthtiles]: Number of tiles in output image per row");
                Console.WriteLine("  [-p] [--numbitplanes]: Number of bit planes. 1 for 2-colour, 2 for 4-colour, 4 for 8-colour");
                Console.WriteLine("  [-u] [--gui]: Run GUI tool");
            }
            else if (args.Contains("-u") || args.Contains("--gui"))
            {
                var game = new UiGame();
                game.LoadContent();
                game.Play();
            }
            else
            {
                // Comand line mode.
                string inputPath;
                var inputArgPosition = Math.Max(args.ToList().IndexOf("-i"), args.ToList().IndexOf("--input"));
                if (inputArgPosition != -1 && inputArgPosition < args.Length - 1)
                {
                    inputPath = args[inputArgPosition + 1];
                }
                else
                {
                    Console.WriteLine("Must specify input path.");
                    return;
                }

                var outputPath = Path.Join(Directory.GetCurrentDirectory(), "output.png");
                var outputArgPosition = Math.Max(args.ToList().IndexOf("-o"), args.ToList().IndexOf("--output"));
                if (outputArgPosition != -1 && outputArgPosition < args.Length - 1)
                {
                    outputPath = args[outputArgPosition + 1];
                }

                if (!Path.Exists(inputPath))
                {
                    Console.WriteLine("Could not find input file at path: " + inputPath);
                    return;
                }

                var tileSizeX = 32;
                var tileSizeY = 32;
                var tilesizeArgPosition = Math.Max(args.ToList().IndexOf("-t"), args.ToList().IndexOf("--tilesize"));
                if (tilesizeArgPosition != -1 && tilesizeArgPosition < args.Length - 1)
                {
                    var tileSizeStr = args[outputArgPosition + 1];
                    var inParts = tileSizeStr.Split("x");
                    if (inParts.Length != 2
                        || !int.TryParse(inParts[0], out tileSizeX)
                        || !int.TryParse(inParts[1], out tileSizeY))
                    {
                        Console.WriteLine("Invalid tile size argument. Expected form: [width]x[height]");
                        return;
                    }
                }

                var outputNumTilesWide = 10;
                var outputNumTilesArgPosition = Math.Max(args.ToList().IndexOf("-z"), args.ToList().IndexOf("--outputwidthtiles"));
                if (outputNumTilesArgPosition != -1 && outputNumTilesArgPosition < args.Length - 1)
                {
                    var outputNumTilesWideStr = args[outputNumTilesArgPosition + 1];
                    if (!int.TryParse(outputNumTilesWideStr, out outputNumTilesWide))
                    {
                        Console.WriteLine("Invalid output width argument. Expected number, got: " + outputNumTilesWideStr);
                        return;
                    }
                }

                var numBitPlanes = 4;
                var numBitPlanesArgPosition = Math.Max(args.ToList().IndexOf("-p"), args.ToList().IndexOf("--numbitplanes"));
                if (numBitPlanesArgPosition != -1 && numBitPlanesArgPosition < args.Length - 1)
                {
                    if (numBitPlanes is not 1 and not 2 and not 4)
                    {
                        Console.WriteLine("Invalid num bit planes. Expected 1, 2 or 4");
                        return;
                    }
                }

                var converter = new FileConverter();
                converter.LoadFileData(inputPath);

                var numTilesInFile = converter.NumTilesInFile(tileSizeX, tileSizeY, numBitPlanes);
                var requiredFileHeight = (numTilesInFile / outputNumTilesWide) * tileSizeY;
                if (numTilesInFile % outputNumTilesWide != 0)
                {
                    requiredFileHeight += tileSizeY;
                }

                var (data, width, height) = converter.ConvertToRgba(
                    tileSizeX, tileSizeY, numBitPlanes,
                    outputNumTilesWide * tileSizeX, requiredFileHeight);

                // Now save.
                FileConverter.ToPng(outputPath, data, width, height);
                Console.WriteLine($"Saved file to: {outputPath}. Dimensions: {width}x{height}. numoutputtileswidth: {outputNumTilesWide}");
            }
        }
    }
}
