using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SDL2;

namespace Spl.Core.Input
{
    [PublicAPI]
    public class JoystickState
    {
        public IntPtr Joystick;
        public IntPtr Controller;

        // Axes.
        public Dictionary<SDL.SDL_GameControllerAxis, (int Value, float ValuePrecise)> AxisStates;

        public readonly List<SDL.SDL_GameControllerAxis> AxesDownThisFrame;
        public readonly List<SDL.SDL_GameControllerButton> ButtonsDownThisFrame;

        public JoystickState(IntPtr controller, IntPtr joystick)
        {
            Controller = controller;
            Joystick = joystick;

            AxisStates = new Dictionary<SDL.SDL_GameControllerAxis, (int, float)>
            {
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, (0, 0) },
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, (0, 0) },
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, (0, 0) },
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY, (0, 0) },
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT, (0, 0) },
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT, (0, 0) },
            };

            AxesDownThisFrame = new List<SDL.SDL_GameControllerAxis>();
            ButtonsDownThisFrame = new List<SDL.SDL_GameControllerButton>();
        }
    }
}
