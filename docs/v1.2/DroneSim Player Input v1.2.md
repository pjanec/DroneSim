Document ID: MODSPEC-INPUT-V1.2

Date: June 10, 2025

Title: V1.2 Detailed IPlayerInput Module Specification (Implementing Class: V1KeyboardInput)

**1. Overview**

This document provides the detailed implementation specification for the V1 IPlayerInput interface, named V1KeyboardInput. This module is responsible for capturing all raw keyboard input from the user and translating it into the structured, game-logic-friendly formats defined in DroneSim.Core.

This module is stateful. Its primary challenge is to correctly differentiate between a key that is being held down (for continuous actions like strafing) and a key that was just pressed in the current frame (for discrete actions like toggling a camera or changing the throttle step).

**2. Dependencies**

- **DroneSim.Core:** For the IPlayerInput interface and ControlInputs data structure.

- **A Windowing/Input Library (Silk.NET.Input):** The module is designed to be given a reference to the IKeyboard object managed by the Renderer.

**3. V1 Functional Specification**

- **Stateful Update:** The module\'s public Update(IKeyboard keyboard) method must be called by the Orchestrator exactly once per frame. This method reads the raw input and updates all internal states. The public interface methods (GetFlightControls, IsPossessKeyPressed, etc.) simply return the values calculated during the most recent Update call.

- **Single-Press vs. Held Logic:** To detect single presses, the module must maintain a set or list of keys that were down in the *previous* frame. For a key to register as \"just pressed,\" it must be down in the current frame but *not* have been down in the previous frame.

- **Key Mappings & Logic:**

  - **Continuous (Held) Keys:**

    - A / D: Set StrafeInput to -1.0f / 1.0f respectively. If both or neither are pressed, it is 0.0f.

    - Q / Z: Set VerticalInput to 1.0f / -1.0f respectively.

    - Left/Right Arrows: Set YawInput to -1.0f / 1.0f respectively.

    - Up/Down Arrows: Set GetCameraTiltInput() to 1.0f / -1.0f respectively.

  - **Discrete (Single Press) Keys:**

    - W: On press, increment internal throttleStep by 1 (clamped at a max of 10).

    - S: On press, decrement internal throttleStep by 1 (clamped at a min of 0).

    - C: On press, IsSwitchCameraPressed() returns true for one frame only.

    - T: On press, IsSwitchDronePressed() returns true for one frame only.

    - P: On press, IsPossessKeyPressed() returns true for one frame only.

**4. Configuration & Parameters**

There are no external configuration parameters for the V1 implementation of this module. The key mappings are hard-coded as specified.

**5. Code Skeleton**

// In project: DroneSim.PlayerInput\
using DroneSim.Core;\
using Silk.NET.Input; // Assumed input library namespace\
using System.Collections.Generic;\
\
/// \<summary\>\
/// V1 implementation of the player input module using a keyboard.\
/// This class is stateful and must be updated once per frame.\
/// \</summary\>\
public class V1KeyboardInput : IPlayerInput\
{\
// Internal state for the current control commands\
private ControlInputs \_currentControls;\
private float \_cameraTiltInput;\
private int \_throttleStep = 0;\
\
// State for handling single-press toggle keys\
private bool \_switchCameraPressed;\
private bool \_switchDronePressed;\
private bool \_possessKeyPressed;\
\
// Set to store keys that were down in the previous frame\
private readonly HashSet\<Key\> \_previousFrameKeys = new();\
\
/// \<summary\>\
/// Updates the internal state of the input module based on the current keyboard state.\
/// This method MUST be called once per frame by the Orchestrator.\
/// \</summary\>\
/// \<param name=\"keyboard\"\>The keyboard device object from the input library.\</param\>\
public void Update(IKeyboard keyboard)\
{\
// \-\-- Handle held keys for continuous input \-\--\
\_currentControls.StrafeInput = GetAxisInput(keyboard, Key.A, Key.D);\
\_currentControls.VerticalInput = GetAxisInput(keyboard, Key.Z, Key.Q);\
\_currentControls.YawInput = GetAxisInput(keyboard, Key.Left, Key.Right);\
\_cameraTiltInput = GetAxisInput(keyboard, Key.Down, Key.Up);\
\
// \-\-- Handle single-press keys for stepped or toggled actions \-\--\
if (WasJustPressed(keyboard, Key.W)) \_throttleStep = Math.Min(10, \_throttleStep + 1);\
if (WasJustPressed(keyboard, Key.S)) \_throttleStep = Math.Max(0, \_throttleStep - 1);\
\_currentControls.ThrottleStep = \_throttleStep;\
\
\_switchCameraPressed = WasJustPressed(keyboard, Key.C);\
\_switchDronePressed = WasJustPressed(keyboard, Key.T);\
\_possessKeyPressed = WasJustPressed(keyboard, Key.P);\
\
// \-\-- Update previous keys state for the next frame \-\--\
\_previousFrameKeys.Clear();\
foreach (var key in keyboard.PressedKeys)\
{\
\_previousFrameKeys.Add(key);\
}\
}\
\
// Helper to check for a single press\
private bool WasJustPressed(IKeyboard keyboard, Key key)\
{\
return keyboard.IsKeyPressed(key) && !\_previousFrameKeys.Contains(key);\
}\
\
// Helper to get -1, 0, or 1 axis input\
private float GetAxisInput(IKeyboard keyboard, Key negativeKey, Key positiveKey)\
{\
float axis = 0;\
if (keyboard.IsKeyPressed(positiveKey)) axis += 1.0f;\
if (keyboard.IsKeyPressed(negativeKey)) axis -= 1.0f;\
return axis;\
}\
\
// \-\-- Interface Method Implementations \-\--\
public ControlInputs GetFlightControls() =\> \_currentControls;\
public float GetCameraTiltInput() =\> \_cameraTiltInput;\
public bool IsSwitchCameraPressed() =\> \_switchCameraPressed;\
public bool IsSwitchDronePressed() =\> \_switchDronePressed;\
public bool IsPossessKeyPressed() =\> \_possessKeyPressed;\
\
// This method is part of the interface but its parameter is generic.\
// The Orchestrator will cast the object to the correct IKeyboard type.\
void IPlayerInput.Update(object keyboardState)\
{\
if (keyboardState is IKeyboard keyboard)\
{\
Update(keyboard);\
}\
}\
}
