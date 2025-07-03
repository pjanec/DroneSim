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

    // Key Bindings:
    //   W: Full forward (ThrottleStep=10)
    //   S: Stop forward (ThrottleStep=0)
    //   Q: Strafe left
    //   E: Strafe right
    //   A / Left Arrow: Yaw left
    //   D / Right Arrow: Yaw right
    //   Up Arrow: Up
    //   Down Arrow: Down
    //
    // To move forward, hold W. To stop, hold S.
    // To strafe, hold Q/E. To turn, hold A/D or Left/Right Arrow.
    // To go up/down, hold Up/Down Arrow.

    /// <summary>
    /// This method should be called once per frame to read the raw input
    /// and update the internal state.
    /// </summary>
    /// <param name="input">The input object from the input provider.</param>
    public void Update(IDroneSimInput input)
    {
        if (input == null) return;
        // Throttle logic: W = full, S = stop, else keep previous
        if (input.Forward)
            _throttleStep = 10;
        else if (input.Backward)
            _throttleStep = 0;
        // else, keep previous throttleStep
        _currentControls = new ControlInputs
        {
            StrafeInput = input.StrafeRight ? 1.0f : input.StrafeLeft ? -1.0f : 0.0f,
            VerticalInput = input.Up ? 1.0f : input.Down ? -1.0f : 0.0f,
            YawInput = input.YawRight ? 1.0f : input.YawLeft ? -1.0f : 0.0f,
            ThrottleStep = _throttleStep
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