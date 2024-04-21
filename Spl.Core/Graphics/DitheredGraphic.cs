using System;

namespace Spl.Core.Graphics
{
    public class DitheredGraphic : Graphic
    {
        public Texture2D[] DitheredVersions { get; private set; }

        public DitheredGraphic()
        {
            DitheredVersions = Array.Empty<Texture2D>();
        }

        public new DitheredGraphic FromFile(string fileNameIn, int numInSequenceIn = 1)
        {
            FileName = fileNameIn;
            NumInSequence = numInSequenceIn;

            // Add to the "automatically populate from file" list.
            AllGraphics.Add(this);

            return this;
        }

        // Choose the correct dithering alignment.
        public Texture2D DitherTextureForAlignment(int textureIdx, int x, int y)
        {
            var chosenTextureIdx = (x + y % 2) % 2 == 0 ? textureIdx * 2 : textureIdx * 2 + 1;
            return DitheredVersions[chosenTextureIdx];
        }

        public override void SetTextures(Texture2D[] data)
        {
            base.SetTextures(data);

            // Now set dithered.
            DitheredVersions = new Texture2D[data.Length * 2];

            for (var i = 0; i < data.Length; i++)
            {
                // Get current data.
                var width = AllTextures[i].Width;
                var height = AllTextures[i].Height;
                var data1 = new Color[width * height];
                var data2 = new Color[width * height];
                AllTextures[i].GetData(data1);
                AllTextures[i].GetData(data2);

                // Set every second pixel to transparent.
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if ((x + y % 2) % 2 == 0)
                        {
                            data1[x % width + y * width] = Color.Transparent;
                        }
                        else
                        {
                            data2[x % width + y * width] = Color.Transparent;
                        }
                    }
                }

                // Create dither alignment 1.
                DitheredVersions[2 * i] = Texture2D.FromData(data1, width, height);

                // Create dither alignment 2.
                DitheredVersions[2 * i + 1] = Texture2D.FromData(data2, width, height);
            }
        }

        public override void Dispose()
        {
            foreach (var t in DitheredVersions)
            {
                //t.Dispose();
            }

            base.Dispose();
        }
    }
}
