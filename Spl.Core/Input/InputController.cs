using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using SDL2;

namespace Spl.Core.Input
{
    [PublicAPI]
    public class InputController
    {
        // Joysticks can report values from -32768 to 32767. (int16).
        private const int JoystickDeadZone = 1000;
        private const int MaxRecentControls = 20;
        public const int AxisX = 0;
        public const int AxisY = 0;

        // Keep track of mouse position.
        public int CurrentMousePositionX;
        public int CurrentMousePositionY;
        public bool CurrentMouseLeftIsDown;
        public bool CurrentMouseMiddleIsDown;
        public bool CurrentMouseRightIsDown;

        public Dictionary<int, JoystickState> Controllers;

        public string KeysDownThisFrame;

        public readonly List<Control> RecentlyPressedControls;

        public InputController()
        {
            Control.InputController = this;
            RecentlyPressedControls = new List<Control>();

            Controllers = new Dictionary<int, JoystickState>();
            KeysDownThisFrame = "";
        }

        public void CheckForControllers()
        {
            BasicLogger.LogInfo("Checking for controllers...");

            // Close existing joysticks.
            foreach (var c in Controllers)
            {
                SDL.SDL_GameControllerClose(c.Value.Controller);
            }

            Controllers.Clear();

            var numControllers = SDL.SDL_NumJoysticks();
            for (var i = 0; i < numControllers; i++)
            {
                var gGameController = SDL.SDL_GameControllerOpen(i);

                if (gGameController == IntPtr.Zero )
                {
                    BasicLogger.LogError($"Warning: Unable to open game controller! SDL Error: {SDL.SDL_GetError()}");
                }
                else
                {
                    var joystick = SDL.SDL_GameControllerGetJoystick(gGameController);
                    if(joystick == IntPtr.Zero)
                    {
                        BasicLogger.LogError($"Error getting joystick from controller: {SDL.SDL_GetError()}");
                    }
                    else
                    {
                        var joystickId = SDL.SDL_JoystickInstanceID(joystick);
                        if (joystickId == -1)
                        {
                            BasicLogger.LogError($"Error reading instance id: {SDL.SDL_GetError()}");
                        }
                        else
                        {
                            BasicLogger.LogInfo($"Detected joystick {joystickId} - adding.");
                            Controllers.Add(joystickId, new JoystickState(gGameController, joystick));
                        }
                    }
                }
            }

            BasicLogger.LogInfo($"{Controllers.Count} found. SDL Error text: " + SDL.SDL_GetError());
        }

        public void HandleEvent(SDL.SDL_Event e)
        {
            // There is no GetJoystickState helper so we need to handle the actual events.
            if (e.type == SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION)
            {
                var controllerIdx = e.jaxis.which;

                // NOTE: Since this is a Controller event we have the correct key here.....
                var key = e.jaxis.axis;
                var val = e.jaxis.axisValue;

                var controllerAxis = (SDL.SDL_GameControllerAxis)key;

                (int Value, float ValueNormalised) result = val switch
                {
                    < -JoystickDeadZone => (-1, (val + JoystickDeadZone) / (32768f - JoystickDeadZone)),
                    > JoystickDeadZone => (1, (val - JoystickDeadZone) / (32767f - JoystickDeadZone)),
                    _ => (0, 0)
                };

                Controllers[controllerIdx].AxisStates[controllerAxis] = result;
            }

            // NOTE: Since we're looking for SDL_CONTROLLERBUTTONDOWN event,
            // we get a controller binding key, not a joystick button.
            else if (e.type == SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN)
            {
                var controllerIdx = e.jbutton.which;
                Controllers[controllerIdx].ButtonsDownThisFrame.Add((SDL.SDL_GameControllerButton)e.jbutton.button);
            }
            else if (e.type == SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP)
            {
                var controllerIdx = e.jbutton.which;
                Controllers[controllerIdx].ButtonsDownThisFrame.Remove((SDL.SDL_GameControllerButton)e.jbutton.button);
            }
            else if (e.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED)
            {
                CheckForControllers();
            }
            else if (e.type == SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED)
            {
                CheckForControllers();
            }
            else if (e.type == SDL.SDL_EventType.SDL_TEXTINPUT)
            {
                unsafe
                {
                    // Inefficient but eh
                    byte c = e.text.text[0];
                    int length = 0;
                    while (c != '\0')
                    {
                        c = e.text.text[length++];
                    }

                    var arr = new byte[length];
                    Marshal.Copy((IntPtr)e.text.text, arr, 0, length);

                    var text = Encoding.UTF8.GetString(arr);
                    KeysDownThisFrame += text;
                }
            }
            else if (e.type == SDL.SDL_EventType.SDL_TEXTEDITING)
            {
                unsafe
                {
                    // var text = Encoding.UTF8.GetString(e.text.text);
                    // byte[] arr = new byte[len];
                    // Marshal.Copy((IntPtr)ptr, arr, 0, len);
                    // e.text.
                    // Console.WriteLine("EVENT INPUT: " + e.text.text);
                    Console.WriteLine("EVENT INPUT 1");
                }
            }
        }

        // Update input states.
        public void UpdateControlStates()
        {
            // We use GetKeyboardState and GetMouseState instead of handling events.
            var keyboardState = SDL.SDL_GetKeyboardState(out var arrayLength);
            var mouseState = SDL.SDL_GetMouseState(out var x, out var y);

            // Update mouse position.
            CurrentMousePositionX = x;
            CurrentMousePositionY = y;

            CurrentMouseLeftIsDown = (mouseState >> ((int)SDL.SDL_BUTTON_LEFT - 1) & 1) == 1;
            CurrentMouseMiddleIsDown = (mouseState >> ((int)SDL.SDL_BUTTON_MIDDLE - 1) & 1) == 1;
            CurrentMouseRightIsDown = (mouseState >> ((int)SDL.SDL_BUTTON_RIGHT - 1) & 1) == 1;

            var managedArray = new byte[arrayLength];
            Marshal.Copy(keyboardState, managedArray, 0, arrayLength);

            // Axes.
            foreach (var c in Controllers)
            {
                c.Value.AxesDownThisFrame.Clear();

                foreach(var a in c.Value.AxisStates)
                {
                    if (a.Value.Value != 0)
                    {
                        // It's pressed.
                        c.Value.AxesDownThisFrame.Add(a.Key);
                    }
                }
            }
            var controllerStates = Controllers;

            // Buttons.
            foreach (var control in Control.PlayerControls)
            {
                var isPressed = false;
                foreach (var k in control.KeyCodes)
                {
                    if (managedArray[k] != 0)
                    {
                        isPressed = true;
                    }
                }

                if (controllerStates.Count > 0)
                {
                    foreach (var c in Controllers)
                    {
                        foreach (var k in control.ButtonCodes)
                        {
                            if (controllerStates[c.Key].ButtonsDownThisFrame.Contains(k))
                            {
                                isPressed = true;
                            }
                        }

                        foreach (var (Axis, Dir) in control.ButtonAxes)
                        {
                            if (controllerStates[c.Key].AxesDownThisFrame.Contains(Axis))
                            {
                                if (Dir == controllerStates[c.Key].AxisStates[Axis].Value)
                                    isPressed = true;
                            }
                        }
                    }
                }

                foreach (var k in control.MouseButtonCodes)
                {
                    if (k == MouseButton.Left && ((mouseState >> 0) & 1) == 1)
                    {
                        isPressed = true;
                    }
                    else if (k == MouseButton.Middle && ((mouseState >> 1) & 1) == 1)
                    {
                        isPressed = true;
                    }
                    else if (k == MouseButton.Right && ((mouseState >> 2) & 1) == 1)
                    {
                        isPressed = true;
                    }
                }

                control.UpdateControl(isPressed);

                if (control.IsJustPressed)
                {
                    RecentlyPressedControls.Add(control);

                    if (RecentlyPressedControls.Count > MaxRecentControls)
                    {
                        RecentlyPressedControls.RemoveAt(0);
                    }
                }
            }
        }
    }
}
