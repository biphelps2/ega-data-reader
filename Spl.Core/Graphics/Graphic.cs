using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Spl.Core.Graphics
{
    public class Graphic
    {
        // Used for getting textures from file.
        public string? FileName { get; protected set; }
        public int NumInSequence { get; protected set; }

        public Texture2D[] OriginalTextures { get; private set; }
        public Texture2D[] AllTextures { get; private set; }

        public Texture2D Texture => AllTextures[0];
        public Texture2D OriginalTexture => OriginalTextures[0];

        public static List<Graphic> AllGraphics { get; }

        static Graphic()
        {
            AllGraphics = new List<Graphic>();
        }

        public Graphic()
        {
            NumInSequence = 0;

            OriginalTextures = Array.Empty<Texture2D>();
            AllTextures = Array.Empty<Texture2D>();
        }

        public Graphic FromFile(string fileNameIn, int numInSequenceIn = 1)
        {
            FileName = fileNameIn;
            NumInSequence = numInSequenceIn;

            // Add to the "automatically populate from file" list.
            AllGraphics.Add(this);

            return this;
        }

        public virtual void SetTextures(Texture2D[] data)
        {
            OriginalTextures = data;
            AllTextures = new Texture2D[data.Length];

            // Create duplicate of all Texture2Ds.
            for(var i = 0; i < data.Length; i++)
            {

                var clonedData = new Color[OriginalTextures[i].Width * OriginalTextures[i].Height];

                OriginalTextures[i].GetData(clonedData);
                AllTextures[i] = Texture2D.FromData(clonedData, OriginalTextures[i].Width, OriginalTextures[i].Height);
            }
        }

        public virtual void Dispose()
        {
            foreach (var t in OriginalTextures)
            {
                //t.Dispose();
            }
            foreach (var t in AllTextures)
            {
                //t.Dispose();
            }
        }

        [PublicAPI]
        public Texture2D? GetRandomTexture()
        {
            return AllTextures.RandomElement();
        }
    }
}
