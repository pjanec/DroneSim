using DroneSim.Core;
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

    /// <summary>
    /// This method should be called once per frame to read the raw input
    /// and update the internal state.
    /// </summary>
    /// <param name="input">The input object from the input provider.</param>
    public void Update(IDroneSimInput input)
    {
        if (input == null) return;
        _currentControls = new ControlInputs
        {
            StrafeInput = input.Right ? 1.0f : input.Left ? -1.0f : 0.0f,
            VerticalInput = input.Up ? 1.0f : input.Down ? -1.0f : 0.0f,
            YawInput = input.YawRight ? 1.0f : input.YawLeft ? -1.0f : 0.0f,
            ThrottleStep = _throttleStep // You may want to increment/decrement this based on input
        };
        // Example: Camera tilt and discrete actions can be mapped to additional properties if needed
        // _cameraTiltInput = ...
        // _switchCameraPressed = ...
        // _switchDronePressed = ...
        // _possessKeyPressed = ...
        // _isDebugTogglePressed = ...
    }

    // --- Interface Method Implementations ---
    public ControlInputs GetFlightControls() => _currentControls;
    public float GetCameraTiltInput() => _cameraTiltInput;
    public bool IsSwitchCameraPressed() => _switchCameraPressed;
    public bool IsSwitchDronePressed() => _switchDronePressed;
    public bool IsPossessKeyPressed() => _possessKeyPressed;
    public bool IsDebugTogglePressed() => _isDebugTogglePressed;
} 