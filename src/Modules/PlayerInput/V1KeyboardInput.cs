using DroneSim.Core;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace DroneSim.PlayerInput;

/// <summary>
/// V1 implementation of the player input module using a keyboard.
/// This module is stateful. It differentiates between a key being held down
/// (continuous action) and a key that was just pressed (discrete action).
/// </summary>
public class V1KeyboardInput : IPlayerInput
{
    private ControlInputs _currentControls;
    private float _cameraTiltInput;
    private int _throttleStep = 0;

    // Flags for single-press actions, reset each frame.
    private bool _switchCameraPressed;
    private bool _switchDronePressed;
    private bool _possessKeyPressed;
    private bool _isDebugTogglePressed;

    // Set to store keys that were down in the previous frame for press detection.
    private readonly HashSet<Key> _previousFrameKeys = new();

    /// <summary>
    /// This method should be called once per frame to read the raw input
    /// and update the internal state.
    /// </summary>
    /// <param name="keyboard">The keyboard object from the input provider.</param>
    public void Update(IKeyboard keyboard)
    {
        // --- Handle held keys for continuous input ---
        _currentControls.StrafeInput = GetAxisInput(keyboard, Key.A, Key.D);
        _currentControls.VerticalInput = GetAxisInput(keyboard, Key.Z, Key.Q);
        _currentControls.YawInput = GetAxisInput(keyboard, Key.Left, Key.Right);
        _cameraTiltInput = GetAxisInput(keyboard, Key.Down, Key.Up);

        // --- Handle single-press keys for discrete actions ---
        if (WasJustPressed(keyboard, Key.W)) _throttleStep = Math.Min(10, _throttleStep + 1);
        if (WasJustPressed(keyboard, Key.S)) _throttleStep = Math.Max(0, _throttleStep - 1);
        _currentControls.ThrottleStep = _throttleStep;

        _switchCameraPressed = WasJustPressed(keyboard, Key.C);
        _switchDronePressed = WasJustPressed(keyboard, Key.T);
        _possessKeyPressed = WasJustPressed(keyboard, Key.P);
        _isDebugTogglePressed = WasJustPressed(keyboard, Key.F3);

        // --- Update previous keys state for the next frame ---
        _previousFrameKeys.Clear();
        var allKeys = (Key[])Enum.GetValues(typeof(Key));
        foreach (var key in allKeys)
        {
            if (keyboard.IsKeyPressed(key))
            {
                _previousFrameKeys.Add(key);
            }
        }
    }

    /// <summary>
    /// Checks if a key was pressed in the current frame but not in the previous one.
    /// </summary>
    private bool WasJustPressed(IKeyboard keyboard, Key key)
    {
        return keyboard.IsKeyPressed(key) && !_previousFrameKeys.Contains(key);
    }

    /// <summary>
    /// Helper to get an axis value from two keys (e.g., -1.0 for left, 1.0 for right).
    /// </summary>
    private float GetAxisInput(IKeyboard keyboard, Key negativeKey, Key positiveKey)
    {
        float axis = 0;
        if (keyboard.IsKeyPressed(positiveKey)) axis += 1.0f;
        if (keyboard.IsKeyPressed(negativeKey)) axis -= 1.0f;
        return axis;
    }

    // --- Interface Method Implementations ---
    public ControlInputs GetFlightControls() => _currentControls;
    public float GetCameraTiltInput() => _cameraTiltInput;
    public bool IsSwitchCameraPressed() => _switchCameraPressed;
    public bool IsSwitchDronePressed() => _switchDronePressed;
    public bool IsPossessKeyPressed() => _possessKeyPressed;
    public bool IsDebugTogglePressed() => _isDebugTogglePressed;

    /// <summary>
    /// Explicit interface implementation to handle the generic object parameter from the core interface.
    /// </summary>
    void IPlayerInput.Update(object keyboardState)
    {
        if (keyboardState is IKeyboard keyboard)
        {
            Update(keyboard);
        }
    }
} 