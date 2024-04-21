using System;
using System.IO;

namespace Spl.Core
{
    public static class WavReader
    {
        // https://stackoverflow.com/a/11162668/11678918
        // Helper to skip past header data and return only sample data.
        public static byte[] OpenWav(string filename)
        {
            byte[] wav = File.ReadAllBytes(filename);

            // Get past all the other sub chunks to get to the data subchunk:
            // First Subchunk ID from 12 to 16
            int pos = 12;

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 in hex, 100 97 116 97 in decimal).
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            // Allocate memory.
            var result = new byte[wav.Length - pos];
            Array.Copy(wav, pos, result, 0, result.Length);

            return result;
        }
    }
}
