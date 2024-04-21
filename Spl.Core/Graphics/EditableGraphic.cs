using System;

namespace Spl.Core.Graphics
{
    public class EditableGraphic : DitheredGraphic
    {
        public Texture2D[] FullColourVersions { get; private set; }

        public EditableGraphic()
        {
            FullColourVersions = Array.Empty<Texture2D>();
        }

        public new EditableGraphic FromFile(string fileNameIn, int numInSequenceIn = 1)
        {
            FileName = fileNameIn;
            NumInSequence = numInSequenceIn;

            // Add to the "automatically populate from file" list.
            AllGraphics.Add(this);

            return this;
        }

        public override void SetTextures(Texture2D[] data)
        {
            base.SetTextures(data);

            FullColourVersions = new Texture2D[data.Length];

            for (var i = 0; i < AllTextures.Length; i++)
            {
                // Get current data.
                var width = AllTextures[i].Width;
                var height = AllTextures[i].Height;
                var dataFull = new Color[width * height];
                var data1 = new Color[width * height];
                var data2 = new Color[width * height];
                AllTextures[i].GetData(dataFull);
                AllTextures[i].GetData(data1);
                AllTextures[i].GetData(data2);

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        // Set every second pixel to transparent.
                        if ((x + y % 2) % 2 == 0)
                        {
                            data1[x % width + y * width] = Color.Transparent;

                            if (data2[x % width + y * width] != Color.Transparent)
                            {
                                data2[x % width + y * width] = Color.White;
                            }
                        }
                        // Set every (other) second pixel to transparent.
                        else
                        {
                            if (data1[x % width + y * width] != Color.Transparent)
                            {
                                data1[x % width + y * width] = Color.White;
                            }

                            data2[x % width + y * width] = Color.Transparent;
                        }

                        // Just set full white for full white texture.
                        if (dataFull[x % width + y * width] != Color.Transparent)
                        {
                            dataFull[x % width + y * width] = Color.White;
                        }
                    }
                }

                FullColourVersions[i] = Texture2D.FromData(dataFull, width, height);
            }
        }

        public override void Dispose()
        {
            foreach (var t in FullColourVersions)
            {
                //t.Dispose();
            }

            base.Dispose();
        }
    }
}
