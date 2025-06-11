Document ID: MODSPEC-FLIGHT-V1.3

Date: June 11, 2025

Title: V1.3 Detailed IFlightDynamics Module Specification (Implementing Class: V1KinematicFlightModel)

**1. Overview**

This document provides the detailed implementation specification for a V1-style flight model, V1KinematicFlightModel. This module implements the IFlightDynamics interface.

Its sole responsibility is to translate player/AI control inputs into a high-level **KinematicIntent**. This intent describes the desired velocity and orientation of the drone, which is then passed to the IPhysicsService for execution and collision resolution. The flight model itself has no knowledge of physics, forces, or world boundaries.

The module remains stateful to track individual drone speeds for smooth acceleration and deceleration.

**2. Dependencies**

- **DroneSim.Core:** For the IFlightDynamics interface, the IMoveIntent and KinematicIntent types, and other data structures.

- **Microsoft.Extensions.Options:** To receive flight model tuning parameters from the DI container.

**3. V1 Functional Specification**

- The V1KinematicFlightModel class shall implement the IFlightDynamics interface.

- Its GenerateMoveIntent method is the single entry point. It returns a KinematicIntent record containing the drone\'s TargetVelocity and TargetOrientation.

- **State Management:** The class maintains a dictionary to store the current forward speed for each drone, keyed by the drone\'s ID, to enable smooth throttle changes.

- **Throttle Logic:** The integer ThrottleStep (0-10) is mapped to a targetForwardSpeed. The module uses linear interpolation (Lerp) to smoothly adjust the drone\'s currentForwardSpeed towards the target speed.

- **Velocity Calculation:** A final world-space TargetVelocity vector is computed by combining the drone\'s forward velocity (from its smoothed speed), its lateral (strafe) velocity, and its vertical velocity.

- **Rotation Calculation:** The TargetOrientation is calculated by applying a rotation around the drone\'s local Y-axis, based on the YawInput.

**4. Configuration & Parameters**

The module will be configured via a FlightModelOptions class.

  -----------------------------------------------------------------------------------------------
  **Parameter**           **V1 Value**            **Description**
  ----------------------- ----------------------- -----------------------------------------------
  MaxForwardSpeed         20.0f                   Speed in m/s at ThrottleStep = 10.

  MaxStrafeSpeed          10.0f                   Top speed for sideways movement.

  MaxVerticalSpeed        5.0f                    Top speed for vertical movement.

  YawSpeed                1.5708f                 Turn rate in radians per second (90 deg/sec).

  AccelerationFactor      5.0f                    A multiplier for the Lerp smoothing.
  -----------------------------------------------------------------------------------------------

**5. Code Skeleton**

// In project: DroneSim.FlightDynamics\
using DroneSim.Core;\
using Microsoft.Extensions.Options;\
using System.Numerics;\
\
/// \<summary\>\
/// V1 implementation of the flight dynamics module.\
/// Translates control inputs into a kinematic movement intent.\
/// \</summary\>\
public class V1KinematicFlightModel : IFlightDynamics\
{\
private readonly Dictionary\<int, float\> \_droneForwardSpeeds = new();\
private readonly FlightModelOptions \_options;\
\
public V1KinematicFlightModel(IOptions\<FlightModelOptions\> options)\
{\
\_options = options.Value;\
}\
\
/// \<summary\>\
/// Calculates the desired kinematic movement intent for a drone.\
/// \</summary\>\
public IMoveIntent GenerateMoveIntent(DroneState currentState, ControlInputs inputs, float deltaTime)\
{\
// Step 1: Calculate Current Forward Speed (Smooth Throttle)\
\_droneForwardSpeeds.TryAdd(currentState.Id, 0.0f);\
float targetForwardSpeed = (inputs.ThrottleStep / 10.0f) \* \_options.MaxForwardSpeed;\
float currentForwardSpeed = \_droneForwardSpeeds\[currentState.Id\];\
\_droneForwardSpeeds\[currentState.Id\] = Math.Abs(targetForwardSpeed - currentForwardSpeed) \> 0.01f\
? float.Lerp(currentForwardSpeed, targetForwardSpeed, deltaTime \* \_options.AccelerationFactor)\
: targetForwardSpeed;\
\
// Step 2: Calculate World-Space Target Velocity Vector\
var forwardDirection = Vector3.Transform(Vector3.UnitZ, currentState.Orientation);\
var rightDirection = Vector3.Transform(Vector3.UnitX, currentState.Orientation);\
\
var forwardVelocity = forwardDirection \* \_droneForwardSpeeds\[currentState.Id\];\
var strafeVelocity = rightDirection \* inputs.StrafeInput \* \_options.MaxStrafeSpeed;\
var verticalVelocity = Vector3.UnitY \* inputs.VerticalInput \* \_options.MaxVerticalSpeed;\
var targetVelocity = forwardVelocity + strafeVelocity + verticalVelocity;\
\
// Step 3: Calculate new Target Orientation (Yaw)\
var yawChange = inputs.YawInput \* \_options.YawSpeed \* deltaTime;\
var yawRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -yawChange);\
var targetOrientation = currentState.Orientation \* yawRotation;\
\
// Step 4: Return the final intent object\
return new KinematicIntent(targetVelocity, targetOrientation);\
}\
}
