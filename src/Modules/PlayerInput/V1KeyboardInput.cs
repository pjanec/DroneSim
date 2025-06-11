using DroneSim.Core;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;

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
        if (keyboard == null) return;

        var currentFrameKeys = new HashSet<Key>(keyboard.SupportedKeys.Where(k => keyboard.IsKeyPressed(k)));

        // --- Handle held keys for continuous input ---
        _currentControls.StrafeInput = GetAxisInput(currentFrameKeys, Key.A, Key.D);
        _currentControls.VerticalInput = GetAxisInput(currentFrameKeys, Key.Z, Key.Q);
        _currentControls.YawInput = GetAxisInput(currentFrameKeys, Key.Left, Key.Right);
        _cameraTiltInput = GetAxisInput(currentFrameKeys, Key.Down, Key.Up);

        // --- Handle single-press keys for discrete actions ---
        if (WasJustPressed(currentFrameKeys, Key.W)) _throttleStep = Math.Min(10, _throttleStep + 1);
        if (WasJustPressed(currentFrameKeys, Key.S)) _throttleStep = Math.Max(0, _throttleStep - 1);
        _currentControls.ThrottleStep = _throttleStep;

        _switchCameraPressed = WasJustPressed(currentFrameKeys, Key.C);
        _switchDronePressed = WasJustPressed(currentFrameKeys, Key.T);
        _possessKeyPressed = WasJustPressed(currentFrameKeys, Key.P);
        _isDebugTogglePressed = WasJustPressed(currentFrameKeys, Key.F3);

        // --- Update previous keys state for the next frame ---
        _previousFrameKeys.Clear();
        foreach (var key in currentFrameKeys)
        {
            _previousFrameKeys.Add(key);
        }
    }

    /// <summary>
    /// Checks if a key was pressed in the current frame but not in the previous one.
    /// </summary>
    private bool WasJustPressed(HashSet<Key> currentKeys, Key key)
    {
        return currentKeys.Contains(key) && !_previousFrameKeys.Contains(key);
    }

    /// <summary>
    /// Helper to get an axis value from two keys (e.g., -1.0 for left, 1.0 for right).
    /// </summary>
    private float GetAxisInput(HashSet<Key> currentKeys, Key negativeKey, Key positiveKey)
    {
        float axis = 0;
        if (currentKeys.Contains(positiveKey)) axis += 1.0f;
        if (currentKeys.Contains(negativeKey)) axis -= 1.0f;
        return axis;
    }

    // --- Interface Method Implementations ---
    public ControlInputs GetFlightControls() => _currentControls;
    public float GetCameraTiltInput() => _cameraTiltInput;
    public bool IsSwitchCameraPressed() => _switchCameraPressed;
    public bool IsSwitchDronePressed() => _switchDronePressed;
    public bool IsPossessKeyPressed() => _possessKeyPressed;
    public bool IsDebugTogglePressed() => _isDebugTogglePressed;
} 