using Spl.Core.Graphics;
using Spl.Core.Monogame;

namespace Spl.Core
{
    /// <summary>
    /// Used for letterbox / pillarbox effect.
    /// The game should define a "native" resolution. The <see cref="Boxer"/> will
    /// create a transformation matrix used by the GraphicsController to
    /// provide resizing functionality.
    /// </summary>
    public class Boxer
    {
        private readonly float _nativeAspectRatio;
        private readonly Rectangle _nativeBounds;

        public int XOffset;
        public int YOffset;
        public float Scale { get; private set; }
        public Matrix TransformMatrix { get; private set; }

        public Boxer(int x, int y)
        {
            _nativeBounds = new Rectangle(0, 0, x, y);
            _nativeAspectRatio = x / (float)y;
        }

        public void OnWindowSizeChanged(Rectangle wClientBounds)
        {

            BasicLogger.LogDetail($"Window resized: {wClientBounds.Width}, {wClientBounds.Height}");
            var currentScreenRatio = wClientBounds.Width / (float)wClientBounds.Height;

            if (currentScreenRatio < _nativeAspectRatio)
            {
                // Pillarbox.
                var scale = wClientBounds.Width / (float)_nativeBounds.Width;
                var remainingHeight = wClientBounds.Height - _nativeBounds.Height * scale;
                UpdateMatrix(0, (int)(remainingHeight / 2), scale);
            }
            else
            {
                // Letterbox.
                var scale = wClientBounds.Height / (float)_nativeBounds.Height;
                var remainingWidth = wClientBounds.Width - _nativeBounds.Width * scale;
                UpdateMatrix((int)(remainingWidth / 2), 0, scale);
            }
        }

        private void UpdateMatrix(int xTranslate, int yTranslate, float scale)
        {
            XOffset = xTranslate;
            YOffset = yTranslate;
            Scale = scale;
            TransformMatrix = Matrix.CreateScale(scale) * Matrix.CreateTranslation(xTranslate, yTranslate, 0);
        }
    }
}
