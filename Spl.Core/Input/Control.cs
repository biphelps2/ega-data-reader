using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SDL2;

namespace Spl.Core.Input
{
    [PublicAPI]
    public enum MouseButton { Left, Middle, Right }

    // Defines a key / button the player can press that is recognisable by the game.
    [PublicAPI]
    public sealed class Control
    {
        private bool _prevDownState;

        // Set when InputController is created.
        public static InputController? InputController;

        public static readonly List<Control> PlayerControls = new();

        // The keys / buttons that can be pressed to activate this control.
        public int[] KeyCodes;
        public SDL.SDL_GameControllerButton[] ButtonCodes;
        public (SDL.SDL_GameControllerAxis Axis, int Dir)[] ButtonAxes;
        public MouseButton[] MouseButtonCodes { get; set; }

        // True when the control is currently pressed.
        public bool IsDown { get; private set; }

        // True for the first frame that the control is pressed for.
        public bool IsJustPressed { get; set; }

        private const float DeadZone = 0.1f;
        private const float DeadZoneVert = 0.5f;

        public Control(params int[] defaultKeys)
        {
            KeyCodes = defaultKeys;
            ButtonCodes = Array.Empty<SDL.SDL_GameControllerButton>();
            ButtonAxes = Array.Empty<(SDL.SDL_GameControllerAxis Axis, int Dir)>();
            MouseButtonCodes = Array.Empty<MouseButton>();

            PlayerControls.Add(this);
        }

        // Called each frame.
        public void UpdateControl(bool isPressed)
        {
            IsDown = isPressed;
            IsJustPressed = isPressed && !_prevDownState;

            _prevDownState = IsDown;
        }

        public Control WithButtons(params SDL.SDL_GameControllerButton[] defaultKeys)
        {
            ButtonCodes = defaultKeys;
            return this;
        }

        public Control WithAxes(params (SDL.SDL_GameControllerAxis axis, int dir)[] defaultAxes)
        {
            ButtonAxes = defaultAxes;
            return this;
        }

        public Control WithMouse(params MouseButton[] defaultKeys)
        {
            MouseButtonCodes = defaultKeys;
            return this;
        }
    }
}
