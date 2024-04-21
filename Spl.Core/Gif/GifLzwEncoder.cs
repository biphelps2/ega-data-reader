using System;
using System.Collections.Generic;

namespace Spl.Core.Gif
{
    public class GifLzwEncoder
    {
        private const int MaxCodeTableEntries = 4096;
        private const int MinCodeSize = 2;

        private readonly int _numPaletteColours;
        private readonly int _initialCodeSize;

        public GifLzwEncoder(int numPaletteColours)
        {
            var codeSize = (int)Math.Ceiling(Math.Log2(numPaletteColours));

            _numPaletteColours = numPaletteColours;
            _initialCodeSize = Math.Max(MinCodeSize, codeSize);
        }

        public IEnumerable<byte> Encode(int[] indexStream)
        {
            var codeSize = _initialCodeSize + 1;

            var clearCodeIdx = _numPaletteColours;
            var eoiCodeIdx = _numPaletteColours + 1;

            // First byte is the initial code size.
            var output = new List<byte> { (byte)_initialCodeSize };

            Dictionary<int, int> GetInitialHashedCodeTable()
            {
                var table = new Dictionary<int, int>();

                for (var i = 0; i < clearCodeIdx; i++)
                {
                    table.Add(ListHash.GetSequenceHashCode(new[] { i }), i);
                }

                // Clear Code and EOI Code entries.
                table.Add(-1, clearCodeIdx);
                table.Add(-2, eoiCodeIdx);

                return table;
            }

            // 0 = least significant. 7 = most significant.
            // We need to convert a bit stream into bytes. We use a byte-sized
            // buffer and push complete bytes to the output byte array.
            var currentBitIdx = 0;
            byte byteBuffer = 0;

            void AddToCodeStream(int lookupIdx)
            {
                // Convert the new CodeStream code to bits.
                for (var i = 0; i < codeSize; i++)
                {
                    var theBit = (lookupIdx >> i) & 0x1;
                    byteBuffer |= (byte)(theBit << currentBitIdx);

                    currentBitIdx++;
                    if (currentBitIdx > 7)
                    {
                        currentBitIdx = 0;

                        output.Add(byteBuffer);
                        byteBuffer = 0;
                    }
                }
            }

            var hashedCodeTable = GetInitialHashedCodeTable();

            AddToCodeStream(clearCodeIdx);

            // Initial index buffer is just the first index stream element.
            var indexBuffer = new List<int> { indexStream[0] };
            var hc = ListHash.GetSequenceHashCode(indexBuffer);
            var matchingIdxForThis = hashedCodeTable[hc];

            // Loop over the rest of the index stream data.
            for (var i = 1; i < indexStream.Length; i++)
            {
                indexBuffer.Add(indexStream[i]);
                var hc2 = ListHash.GetSequenceHashCodeAddItem(hc, indexStream[i]);

                // Not found.
                if (!hashedCodeTable.ContainsKey(hc2))
                {
                    hashedCodeTable.Add(hc2, hashedCodeTable.Count);
                    AddToCodeStream(matchingIdxForThis);

                    // We've read far enough that the code size is too
                    // small. So, increase it.
                    if ((int)Math.Pow(2, codeSize) < hashedCodeTable.Count)
                    {
                        codeSize++;
                    }

                    // If it's too big, we need to restart. Send clear code and clear data.
                    if (hashedCodeTable.Count == MaxCodeTableEntries)
                    {
                        // Set clear code before, using max code size.
                        AddToCodeStream(clearCodeIdx);

                        // Reset code table.
                        hashedCodeTable = GetInitialHashedCodeTable();

                        codeSize = _initialCodeSize + 1;
                    }

                    // Reset buffer. Just keep last element.
                    // We know this last element will be in the first 16 values... TODO use this info.
                    indexBuffer.RemoveRange(0, indexBuffer.Count - 1);

                    hc = ListHash.GetSequenceHashCode(new List<int> { indexStream[i] });
                    matchingIdxForThis = hashedCodeTable[hc];
                }
                // Found.
                else
                {
                    // Don't reset buffer.
                    hc = hc2;
                    matchingIdxForThis = hashedCodeTable[hc2];
                }
            }

            // Add the final data and EOI marker.
            AddToCodeStream(matchingIdxForThis);
            AddToCodeStream(eoiCodeIdx);

            // Add final partial byte.
            if (currentBitIdx != 0)
            {
                output.Add(byteBuffer);
            }

            var totalCount = output.Count - 1;
            var offset = 1;
            while (totalCount > 255)
            {
                // We need to split up the output into chunks of 255...
                output.Insert(offset, 255);

                totalCount -= 255;
                offset += 256;
            }

            // Final insert for the above logic.
            output.Insert(offset, (byte)totalCount);

            if (totalCount != 0)
            {
                // Finally, set end byte.
                output.Add(0);
            }

            return output;
        }
    }
}
