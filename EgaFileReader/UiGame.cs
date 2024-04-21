using SDL2;
using Spl.Core;
using Spl.Core.Graphics;
using Spl.Core.Input;

namespace Spl.EgaFileReader
{
    public class UiGame : SdlGame
    {
        private int _currentFileIdx = -1;
        private string _currentFileName = "";

        private readonly Control _nextFile;
        private readonly Control _toggleBitPlaneCount;
        private readonly Control _increaseTileWidth;
        private readonly Control _decreaseTileWidth;
        private readonly Control _increaseTileHeight;
        private readonly Control _decreaseTileHeight;
        private readonly Control _increaseOutputNumTilesWide;
        private readonly Control _decreaseOutputNumTilesWide;
        private readonly Control _saveCurrentTextureContents;

        private readonly TimeSpan _holdBeforeFast = TimeSpan.FromSeconds(0.4f);
        private TimeSpan _timer = TimeSpan.Zero;

        private readonly FileConverter _fileConverter;
        private Color[]? _data;
        private int _generatedDataWidth;
        private int _generatedDataHeight;
        private int _numTilesWide;
        private Texture2D? _dataVisualised;

        private int _tileWidth;
        private int _tileHeight;
        private int _numBitPlanes;

        public UiGame()
            : base("EGA File Reader", 400, 250, 3)
        {
            _nextFile = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_N);
            _toggleBitPlaneCount = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_P);
            _increaseTileWidth = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_RIGHT);
            _decreaseTileWidth = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_LEFT);
            _increaseTileHeight = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_DOWN);
            _decreaseTileHeight = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_UP);
            _increaseOutputNumTilesWide = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_E);
            _decreaseOutputNumTilesWide = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_Q);
            _saveCurrentTextureContents = new Control(
                (int)SDL.SDL_Scancode.SDL_SCANCODE_S);

            _tileWidth = 32;
            _tileHeight = 32;
            _numBitPlanes = 4;
            _numTilesWide = 10;

            _fileConverter = new FileConverter();
        }

        public override void LoadContent()
        {
            // Set up bitmap font.
            TextureFonts.M3x6.BaseTexture = Texture2D.FromFile("font-3x6.png");
        }

        public override void Update(TimeSpan elapsedTime)
        {
            if (_nextFile.IsJustPressed)
            {
                var allFilesInDir = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.EGA");

                if (allFilesInDir.Length > 0)
                {
                    _currentFileIdx++;
                    _currentFileName = allFilesInDir[_currentFileIdx % allFilesInDir.Length];

                    _fileConverter.LoadFileData(_currentFileName);

                    UpdateEgaTexture();
                }
            }

            if (_toggleBitPlaneCount.IsJustPressed)
            {
                _numBitPlanes = _numBitPlanes switch
                {
                    4 => 2,
                    2 => 1,
                    _ => 4
                };

                UpdateEgaTexture();
            }

            if (_increaseTileWidth.IsDown)
            {
                if (_timer == TimeSpan.Zero || _timer > _holdBeforeFast)
                {
                    _tileWidth++;
                    if (_tileWidth > 2000)
                    {
                        _tileWidth = 2000;
                    }
                    UpdateEgaTexture();
                }

                _timer += elapsedTime;
            }
            else if (_decreaseTileWidth.IsDown)
            {
                if (_timer == TimeSpan.Zero || _timer > _holdBeforeFast)
                {
                    _tileWidth--;
                    if (_tileWidth < 1)
                    {
                        _tileWidth = 1;
                    }
                    UpdateEgaTexture();
                }

                _timer += elapsedTime;
            }
            else if (_increaseTileHeight.IsDown)
            {
                if (_timer == TimeSpan.Zero || _timer > _holdBeforeFast)
                {
                    _tileHeight++;
                    if (_tileHeight > 2000)
                    {
                        _tileHeight = 2000;
                    }
                    UpdateEgaTexture();
                }

                _timer += elapsedTime;
            }
            else if (_decreaseTileHeight.IsDown)
            {
                if (_timer == TimeSpan.Zero || _timer > _holdBeforeFast)
                {
                    _tileHeight--;
                    if (_tileHeight < 1)
                    {
                        _tileHeight = 1;
                    }
                    UpdateEgaTexture();
                }

                _timer += elapsedTime;
            }
            else
            {
                _timer = TimeSpan.Zero;
            }

            if (_increaseOutputNumTilesWide.IsJustPressed)
            {
                _numTilesWide++;
                if (_numTilesWide > 32)
                {
                    _numTilesWide = 32;
                }
                UpdateEgaTexture();
            }
            else if (_decreaseOutputNumTilesWide.IsJustPressed)
            {
                _numTilesWide--;
                if (_numTilesWide < 1)
                {
                    _numTilesWide = 1;
                }
                UpdateEgaTexture();
            }

            if (_saveCurrentTextureContents.IsJustPressed)
            {
                if (_data is not null)
                {
                    FileConverter.ToPng("output.png", _data, _generatedDataWidth, _generatedDataHeight);
                }
            }
        }

        private void UpdateEgaTexture()
        {
            if (!_fileConverter.FileIsLoaded)
            {
                BasicLogger.LogError("Tried to update texture, but no file loaded.");
                return;
            }

            var numTilesInFile = _fileConverter.NumTilesInFile(_tileWidth, _tileHeight, _numBitPlanes);
            var requiredFileHeight = (numTilesInFile / _numTilesWide) * _tileHeight;
            if (numTilesInFile % _numTilesWide != 0)
            {
                requiredFileHeight += _tileHeight;
            }

            (_data, _generatedDataWidth, _generatedDataHeight) = _fileConverter.ConvertToRgba(
                _tileWidth, _tileHeight, _numBitPlanes, _numTilesWide * _tileWidth, requiredFileHeight);

            if (_dataVisualised is not null)
            {
                SDL.SDL_ClearError();
                SDL.SDL_DestroyTexture(_dataVisualised.TexturePtr);

                if(!string.IsNullOrWhiteSpace(SDL.SDL_GetError()))
                {
                    BasicLogger.LogError("Could not destroy texture: " + SDL.SDL_GetError());
                }
            }

            _dataVisualised = Texture2D.FromData(_data,
                _numTilesWide * _tileWidth,
                requiredFileHeight);
        }

        public override void Draw()
        {
            Clear(Color.Black);

            if (_dataVisualised is null)
            {
                DrawTextureFont(TextureFonts.M3x6,
                    "Welcome! Press N to cycle through .EGA files in current directory" +
                    "\n  (" + Directory.GetCurrentDirectory() + ").",
                    2,
                    TextureFonts.M3x6.LineHeight + 2, Color.White);
            }
            else
            {
                DrawTextureFont(TextureFonts.M3x6,
                    "UP/DOWN/LEFT/RIGHT: Change tile size. Q/E: output width in tiles." +
                    "\n[N]ext file. Current file: " + _currentFileName +
                    $"\nTile size: {_tileWidth}x{_tileHeight}. num [P]lanes: {_numBitPlanes}. output width in tiles: {_numTilesWide}. [S]ave as PNG.",
                    2,
                    TextureFonts.M3x6.LineHeight + 2, Color.White);

                DrawTexture(_dataVisualised, 0, TextureFonts.M3x6.LineHeight * 3 + 2 + 2);
            }
        }
    }
}
