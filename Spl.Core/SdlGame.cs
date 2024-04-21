using System;
using SDL2;
using Spl.Core.Font;
using Spl.Core.Graphics;
using Spl.Core.Input;

#if CODEGEN_WASM
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#endif

namespace Spl.Core
{
    public abstract class SdlGame
    {
        protected IntPtr Window; // Reference to the game window.
        protected IntPtr Renderer; // Used for rendering textures (pngs).

        protected Boxer Boxer;
        protected IntPtr GameRenderTarget;

        public string WindowTitle;
        public int BaseWidth;
        public int BaseHeight;
        public int InitialScale;

        public bool IsRunning;

        protected InputController InputController;

        public static readonly string BasePath;

#if CODEGEN_WASM
        public static SdlGame? This;
#endif

        static SdlGame()
        {
            BasePath ??= SDL.SDL_GetBasePath();
            BasicLogger.LogInfo("Base path set: " + BasePath);
        }

        public SdlGame(string windowTitle, int baseWidth, int baseHeight, int initialScale)
        {
            WindowTitle = windowTitle;
            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
            InitialScale = initialScale;

            Initialise();

            // Set up boxer.
            Boxer = new Boxer(baseWidth, baseHeight);
            Boxer.OnWindowSizeChanged(new Rectangle(0, 0, baseWidth * initialScale, baseHeight * initialScale));

            InputController = new InputController();

#if CODEGEN_WASM
            This = this;
#endif
        }

        public abstract void LoadContent();
        public abstract void Update(TimeSpan elapsedTime);
        public abstract void Draw();
        public void Initialise()
        {
            // Also see: https://discourse.libsdl.org/t/difference-between-joysticks-and-game-controllers/24028
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_AUDIO) < 0)
            {
                BasicLogger.LogError($"There was an issue initializing SDL. {SDL.SDL_GetError()}");
            }
            else
            {
                // SDL_Init may set a value here, which we don't care about.
                // No need to clear here, but doing it anyway.
                SDL.SDL_ClearError();
            }

            // Create a new window given a title, size, and passes it a flag indicating it should be shown.
            Window = SDL.SDL_CreateWindow(
                WindowTitle,
                SDL.SDL_WINDOWPOS_UNDEFINED,
                SDL.SDL_WINDOWPOS_UNDEFINED,
                BaseWidth * InitialScale,
                BaseHeight * InitialScale,
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);

            if (Window == IntPtr.Zero)
            {
                BasicLogger.LogError($"There was an issue creating the window. {SDL.SDL_GetError()}");
            }
            
            // Set window icon, if image is available.
            var icon = SDL_image.IMG_Load("icon.png");
            if (icon != IntPtr.Zero)
            {
                SDL.SDL_SetWindowIcon(Window, icon);
            }
            else
            {
                BasicLogger.LogWarning(
                    "Could not find icon.png to set as window icon." +
                    $" Error: {SDL.SDL_GetError()}");
            }

            // Creates a new SDL hardware renderer using the default graphics device with VSYNC enabled.
            Renderer = SDL.SDL_CreateRenderer(
                Window,
                -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            if (Renderer == IntPtr.Zero)
            {
                BasicLogger.LogError($"There was an issue creating the renderer. {SDL.SDL_GetError()}");
            }
            if (SDL.SDL_GetRendererOutputSize(Renderer, out var w, out var h) < 0)
            {
                BasicLogger.LogError("Get Renderer Output Size error: " + SDL.SDL_GetError());
            }

            BasicLogger.LogInfo($"Renderer created. Requested {BaseWidth * InitialScale} {BaseHeight * InitialScale}, got {w} {h}");

            Texture2D.Renderer = Renderer;

            // Initialise SDL_image (for loading PNGs).
            var flags = SDL_image.IMG_InitFlags.IMG_INIT_PNG;
            if ((SDL_image.IMG_Init(flags) & (int)flags) != (int)flags)
            {
                BasicLogger.LogError($"SDL_image could not initialise! SDL_image Error: {SDL_image.IMG_GetError()}\n");
            }

            // Create a texture with "target" access to create a RenderTarget.
            GameRenderTarget = SDL.SDL_CreateTexture(
                Renderer, SDL.SDL_PIXELFORMAT_RGBA8888,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET,
                BaseWidth, BaseHeight);

            // Initialise SDL_mixer (for playing audio).
            // NOTE: Continue even if fails - PC may not have speakers plugged in.
            if (SDL_mixer.Mix_OpenAudio(44100, SDL_mixer.MIX_DEFAULT_FORMAT, 2, 2048) < 0)
            {
                BasicLogger.LogError($"SDL_mixer could not initialise! SDL_mixer Error: {SDL_mixer.Mix_GetError()}");
            }
        }

        // NOTE: If JetBrains.Annotations cannot be found, check for rogue ".NET Host" processes which
        // need to be killed. These seem to result in a silent "package resolution" failure on build.

#if CODEGEN_WASM
        public unsafe void Play()
#else
        public void Play()
#endif
        {
            IsRunning = true;

            // A while loop does not work in the browser. See:
            // https://emscripten.org/docs/porting/emscripten-runtime-environment.html#browser-main-loop
            // So when wasm is the target runtime, we use the emscripten_set_main_loop function instead.
#if CODEGEN_WASM
            emscripten_set_main_loop(&WasmFrame, 0, 0);
#else
            while (IsRunning)
            {
                // Respond to events / input.
                InputController.KeysDownThisFrame = "";
                PollEvents();
                InputController.UpdateControlStates();

                Tick();
            }
#endif
        }

#if CODEGEN_WASM
        [DllImport("*")]
        public static extern unsafe void emscripten_set_main_loop(
            delegate* unmanaged[Cdecl]<void> f, int fps, int simulate_infinite_loop);

        [UnmanagedCallersOnly(EntryPoint = "_render", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe void WasmFrame()
        {
            This!.InputController.KeysDownThisFrame.Clear();
            This!.PollEvents();
            This!.InputController.UpdateControlStates();

            // Apply update(s) and render.
            This!.Tick();
        }
#endif

        private ulong _accumulatedElapsedTime;
        private ulong _previousTicks;
        public const ulong TargetElapsedTime = 16;
        private ulong _maxElapsedTime = 60; // milliseconds.

        public unsafe void Tick()
        {
        RetryTick:
            var currentTicks = SDL.SDL_GetTicks64();
            _accumulatedElapsedTime += currentTicks - _previousTicks;
            _previousTicks = currentTicks;

            // Wait until we need a new update and render.
            if (_accumulatedElapsedTime < TargetElapsedTime)
            {
                // Sleep for as long as possible without overshooting the update time
                var sleepTime = (TargetElapsedTime - _accumulatedElapsedTime);
                if (sleepTime >= 2.0)
                {
                    SDL.SDL_Delay(1);

                    // TODO or this?
                    //System.Threading.Thread.Sleep(1);
                }
                // Keep looping until it's time to perform the next update
                goto RetryTick;
            }

            // Do not allow any update to take longer than our maximum.
            if (_accumulatedElapsedTime > _maxElapsedTime)
            {
                _accumulatedElapsedTime = _maxElapsedTime;
            }

            var stepCount = 0;

            // Perform as many full fixed length time steps as we can
            // within a single "render" frame.
            while (_accumulatedElapsedTime >= TargetElapsedTime)
            {
                _accumulatedElapsedTime -= TargetElapsedTime;
                ++stepCount;

                Update(TimeSpan.FromMilliseconds(TargetElapsedTime));
            }

            // One draw call per frame.
            Render();
        }

        public void Render()
        {
            SDL.SDL_SetRenderTarget(Renderer, GameRenderTarget);

            Draw();

            // Swap to back buffer.
            SDL.SDL_SetRenderTarget(Renderer, IntPtr.Zero);
            Clear(Color.Black);

            // Draw entire game to scale.
            DrawTexture(
                GameRenderTarget, Boxer.XOffset, Boxer.YOffset,
                (int)(BaseWidth * Boxer.Scale), (int)(BaseHeight * Boxer.Scale));

            // Switches out the currently presented render surface with the one we just did work on.
            SDL.SDL_RenderPresent(Renderer);
        }

        private void PollEvents()
        {
            // Check to see if there are any events and continue to do so until the queue is empty.
            while (SDL.SDL_PollEvent(out SDL.SDL_Event e) == 1)
            {
                // Input controller might want to do something.
                InputController.HandleEvent(e);

                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        IsRunning = false;
                        break;
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        switch (e.window.windowEvent)
                        {
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                            case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                                var mWidth = e.window.data1;
                                var mHeight = e.window.data2;

                                Boxer.OnWindowSizeChanged(new Rectangle(0, 0, mWidth, mHeight));
                                break;
                        }
                        break;
                }
            }
        }

        protected void Clear(Color c)
        {
            // Sets the color that the screen will be cleared with.
            SDL.SDL_SetRenderDrawColor(Renderer, c.R, c.G, c.B, c.A);

            // Clears the current render surface.
            SDL.SDL_RenderClear(Renderer);
        }

        protected void DrawPixel(int x1, int y1, Color c)
        {
            // Set the color to red before drawing our shape
            SDL.SDL_SetRenderDrawColor(Renderer, c.R, c.G, c.B, c.A);

            // Draw a line from top left to bottom right
            SDL.SDL_RenderDrawPoint(Renderer, x1, y1);
        }

        protected void DrawLine(int x1, int y1, int x2, int y2, Color c)
        {
            // Set the color to red before drawing our shape
            SDL.SDL_SetRenderDrawColor(Renderer, c.R, c.G, c.B, c.A);

            // Draw a line from top left to bottom right
            SDL.SDL_RenderDrawLine(Renderer, x1, y1, x2, y2);
        }

        protected void DrawRect(int x, int y, int width, int height, Color c)
        {
            // Set the color to red before drawing our shape
            SDL.SDL_SetRenderDrawColor(Renderer, c.R, c.G, c.B, c.A);

            var rect = new SDL.SDL_Rect
            {
                x = x,
                y = y,
                w = width,
                h = height
            };

            // Draw a filled in rectangle.
            SDL.SDL_RenderFillRect(Renderer, ref rect);
        }

        protected void DrawTexture(Texture2D t, int x, int y)
        {
            DrawTexture(t, x, y, t.Width, t.Height);
        }

        protected void DrawTexture(Texture2D t, int x, int y, Color c)
        {
            SDL.SDL_SetTextureColorMod(t.TexturePtr, c.R, c.G, c.B);
            SDL.SDL_SetTextureAlphaMod(t.TexturePtr, c.A);
            DrawTexture(t, x, y, t.Width, t.Height);
            SDL.SDL_SetTextureColorMod(t.TexturePtr, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        }

        protected void DrawTexture(Texture2D t, int x, int y, int width, int height, Color c)
        {
            SDL.SDL_SetTextureColorMod(t.TexturePtr, c.R, c.G, c.B);
            SDL.SDL_SetTextureAlphaMod(t.TexturePtr, c.A);
            DrawTexture(t, x, y, width, height);
            SDL.SDL_SetTextureColorMod(t.TexturePtr, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        }

        protected void DrawTexture(Texture2D t, int x, int y, double angle, Point origin, FlipMode flipMode, Color c, float scale = 1f)
        {
            DrawTexture(t, x, y, t.Width, t.Height, angle, origin, flipMode, c, scale);
        }

        protected void DrawTexture(Texture2D t, int x, int y, int width, int height, double angle, Point origin, FlipMode flipMode, Color c, float scale = 1f)
        {
            DrawTexture(t.TexturePtr, x, y, width, height, angle, origin, flipMode, c, scale);
        }

        protected void DrawTexture(IntPtr t, int x, int y, int width, int height, double angle, Point origin, FlipMode flipMode, Color c, float scale = 1f)
        {
            var widthIncreasedBy = (int)Math.Round((width * scale) - width);
            var heightIncreasedBy = (int)Math.Round((height * scale) - height);

            var actualOffsetX = origin.X + widthIncreasedBy / 2;
            var actualOffsetY = origin.Y + heightIncreasedBy / 2;

            var rect = new SDL.SDL_Rect
            {
                x = x - actualOffsetX,
                y = y - actualOffsetY,
                w = width + widthIncreasedBy,
                h = height + heightIncreasedBy
            };

            var sdlOrigin = new SDL.SDL_Point
            {
                x = actualOffsetX,
                y = actualOffsetY
            };

            var sdlFlip = (SDL.SDL_RendererFlip)flipMode;

            SDL.SDL_SetTextureColorMod(t, c.R, c.G, c.B);
            SDL.SDL_SetTextureAlphaMod(t, c.A);
            SDL.SDL_RenderCopyEx(Renderer, t, IntPtr.Zero, ref rect, angle, ref sdlOrigin, sdlFlip);
            SDL.SDL_SetTextureColorMod(t, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        }

        private void DrawTexture(Texture2D t, int x, int y, int width, int height)
        {
            DrawTexture(t.TexturePtr, x, y, width, height);
        }

        public void DrawTexture(IntPtr texture, int x, int y, int width, int height)
        {
            var rect = new SDL.SDL_Rect
            {
                x = x,
                y = y,
                w = width,
                h = height
            };

            SDL.SDL_RenderCopy(Renderer, texture, IntPtr.Zero, ref rect);
        }

        private void DrawTexture(Texture2D t, int x, int y, int width, int height, Rectangle sourceRect, Color colour)
        {
            var sdlDestRect = new SDL.SDL_Rect
            {
                x = x,
                y = y,
                w = width,
                h = height
            };
            var sdlSourceRect = new SDL.SDL_Rect
            {
                x = sourceRect.X,
                y = sourceRect.Y,
                w = sourceRect.Width,
                h = sourceRect.Height
            };

            SDL.SDL_SetTextureColorMod(t.TexturePtr, colour.R, colour.G, colour.B);
            SDL.SDL_SetTextureAlphaMod(t.TexturePtr, colour.A);
            SDL.SDL_RenderCopy(Renderer, t.TexturePtr, ref sdlSourceRect, ref sdlDestRect);
            SDL.SDL_SetTextureColorMod(t.TexturePtr, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        }

        public void DrawTextureFont(TextureFont textureFont, string text, int x, int y, Color colour, float scale = 1f)
        {
            var xOffset = 0;
            var yOffset = 0;

            foreach (var c in text)
            {
                if (c == '\n')
                {
                    // Move to next line and continue.
                    xOffset = 0;
                    yOffset += (int)(textureFont.LineHeight * scale);
                    continue;
                }

                // Look up glyph, or default to a space.
                var glyph = textureFont.Glyphs.ContainsKey(c) ? textureFont.Glyphs[c] : textureFont.Glyphs[' '];

                var location = new Rectangle(
                    x + xOffset, y + yOffset - (int)((glyph.SizeY - glyph.BaselineOffsetY) * scale),
                    (int)(glyph.SizeX * scale), (int)(glyph.SizeY * scale));

                var sourceRect = glyph.GetSourceRect();
                DrawTexture(textureFont.BaseTexture!, location.X, location.Y, (int)(sourceRect.Width * scale), (int)(sourceRect.Height * scale), glyph.GetSourceRect(), colour);

                xOffset += (int)((glyph.SizeX + 1) * scale);
            }
        }
    }
}
