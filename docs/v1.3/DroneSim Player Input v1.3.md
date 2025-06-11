Document ID: MODSPEC-INPUT-V1.3  
Date: June 11, 2025  
Title: V1.3 Detailed IPlayerInput Module Specification (Implementing Class: V1KeyboardInput)  
**1\. Overview**

This document provides the detailed implementation specification for the V1 IPlayerInput interface, named V1KeyboardInput. This module is responsible for capturing all raw keyboard input from the user and translating it into the structured, game-logic-friendly formats defined in DroneSim.Core.

This module is stateful. Its primary challenge is to correctly differentiate between a key that is being held down (for continuous actions like strafing) and a key that was just pressed in the current frame (for discrete actions like toggling a feature).

**2\. Dependencies**

* **DroneSim.Core:** For the IPlayerInput interface and ControlInputs data structure.  
* **A Windowing/Input Library (Silk.NET.Input):** The module is designed to be given a reference to the IKeyboard object managed by the Renderer.

**3\. V1 Functional Specification**

* **Stateful Update:** The module's public Update(IKeyboard keyboard) method must be called by the Orchestrator exactly once per frame. This method reads the raw input and updates all internal states. The public interface methods (GetFlightControls, etc.) simply return the values calculated during the most recent Update call.  
* **Single-Press vs. Held Logic:** To detect single presses, the module must maintain a set of keys that were down in the *previous* frame. A key registers as "just pressed" if it is down in the current frame but was *not* down in the previous frame.  
* **Key Mappings & Logic:**  
  * **Continuous (Held) Keys:**  
    * A / D: Control StrafeInput (-1.0f / 1.0f).  
    * Q / Z: Control VerticalInput (1.0f / \-1.0f).  
    * Left/Right Arrows: Control YawInput (-1.0f / 1.0f).  
    * Up/Down Arrows: Control camera tilt input.  
  * **Discrete (Single Press) Keys:**  
    * W / S: Increment/Decrement throttle step (clamped 0-10).  
    * C: Toggles camera view (IsSwitchCameraPressed()).  
    * T: Cycles attached drone (IsSwitchDronePressed()).  
    * P: Possesses attached drone (IsPossessKeyPressed()).  
    * **F3**: Toggles debug visualization (IsDebugTogglePressed()).

**4\. Code Skeleton**

// In project: DroneSim.PlayerInput  
using DroneSim.Core;  
using Silk.NET.Input;  
using System.Collections.Generic;

/// \<summary\>  
/// V1 implementation of the player input module using a keyboard.  
/// \</summary\>  
public class V1KeyboardInput : IPlayerInput  
{  
    private ControlInputs \_currentControls;  
    private float \_cameraTiltInput;  
    private int \_throttleStep \= 0;

    // Flags for single-press actions  
    private bool \_switchCameraPressed;  
    private bool \_switchDronePressed;  
    private bool \_possessKeyPressed;  
    private bool \_isDebugTogglePressed; // Newly Added

    // Set to store keys that were down in the previous frame for press detection  
    private readonly HashSet\<Key\> \_previousFrameKeys \= new();

    public void Update(IKeyboard keyboard)  
    {  
        // \--- Handle held keys for continuous input \---  
        \_currentControls.StrafeInput \= GetAxisInput(keyboard, Key.A, Key.D);  
        \_currentControls.VerticalInput \= GetAxisInput(keyboard, Key.Z, Key.Q);  
        \_currentControls.YawInput \= GetAxisInput(keyboard, Key.Left, Key.Right);  
        \_cameraTiltInput \= GetAxisInput(keyboard, Key.Down, Key.Up);

        // \--- Handle single-press keys \---  
        if (WasJustPressed(keyboard, Key.W)) \_throttleStep \= Math.Min(10, \_throttleStep \+ 1);  
        if (WasJustPressed(keyboard, Key.S)) \_throttleStep \= Math.Max(0, \_throttleStep \- 1);  
        \_currentControls.ThrottleStep \= \_throttleStep;

        \_switchCameraPressed \= WasJustPressed(keyboard, Key.C);  
        \_switchDronePressed \= WasJustPressed(keyboard, Key.T);  
        \_possessKeyPressed \= WasJustPressed(keyboard, Key.P);  
        \_isDebugTogglePressed \= WasJustPressed(keyboard, Key.F3); // Newly Added

        // \--- Update previous keys state for the next frame \---  
        \_previousFrameKeys.Clear();  
        foreach (var key in keyboard.PressedKeys)  
        {  
            \_previousFrameKeys.Add(key);  
        }  
    }

    private bool WasJustPressed(IKeyboard keyboard, Key key)  
    {  
        return keyboard.IsKeyPressed(key) && \!\_previousFrameKeys.Contains(key);  
    }  
      
    private float GetAxisInput(IKeyboard keyboard, Key negativeKey, Key positiveKey)  
    {  
        float axis \= 0;  
        if (keyboard.IsKeyPressed(positiveKey)) axis \+= 1.0f;  
        if (keyboard.IsKeyPressed(negativeKey)) axis \-= 1.0f;  
        return axis;  
    }

    // \--- Interface Method Implementations \---  
    public ControlInputs GetFlightControls() \=\> \_currentControls;  
    public float GetCameraTiltInput() \=\> \_cameraTiltInput;  
    public bool IsSwitchCameraPressed() \=\> \_switchCameraPressed;  
    public bool IsSwitchDronePressed() \=\> \_switchDronePressed;  
    public bool IsPossessKeyPressed() \=\> \_possessKeyPressed;  
    public bool IsDebugTogglePressed() \=\> \_isDebugTogglePressed; // Newly Added

    void IPlayerInput.Update(object keyboardState)  
    {  
        if (keyboardState is IKeyboard keyboard)  
        {  
            Update(keyboard);  
        }  
    }  
}  