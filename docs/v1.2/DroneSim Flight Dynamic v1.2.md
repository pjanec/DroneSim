Document ID: MODSPEC-FLIGHT-V1.2

Date: June 10, 2025

Title: V1.2 Detailed IFlightDynamics Module Specification (Implementing Class: V1KinematicFlightModel)

**1. Overview**

This document provides the detailed implementation specification for the V1 IFlightDynamics interface, V1KinematicFlightModel. **This module has been refactored** to adhere to the Single Responsibility Principle.

Its sole responsibility is now to calculate a drone\'s desired, **unobstructed** movement. It translates control inputs into a kinematic update (a displacement vector and a new orientation) without any knowledge of world boundaries, the ground, or other obstacles. This pure kinematic calculation is then passed to the IPhysicsService for collision resolution.

The module remains stateful to track individual drone speeds for smooth acceleration and deceleration.

**2. Dependencies**

- **DroneSim.Core:** For the refactored IFlightDynamics interface and data structures.

- **Microsoft.Extensions.Options:** To receive flight model tuning parameters from the DI container.

**3. V1 Functional Specification**

- The V1KinematicFlightModel class shall implement the IFlightDynamics interface.

- The CalculateKinematicUpdate method is the single entry point. It returns a tuple containing the desired displacement Vector3 for the frame and the drone\'s Quaternion new orientation.

- **State Management:** The class maintains a dictionary to store the current forward speed for each drone, keyed by the drone\'s ID.

- **Throttle Logic:** The integer ThrottleStep (0-10) is mapped to a targetForwardSpeed. The module uses linear interpolation (Lerp) to smoothly adjust the drone\'s currentForwardSpeed towards the target speed.

- **Displacement Calculation:** A final world-space velocity vector is computed by combining forward, strafe, and vertical components. This velocity is then multiplied by deltaTime to produce the final desiredDisplacement vector.

- **Rotation Update:** The new Orientation is calculated by applying a rotation around the drone\'s local Y-axis, based on the YawInput.

- **No Collision Logic:** This module performs absolutely no position clamping or boundary checks.

**4. Configuration & Parameters**

The module will be configured via a FlightModelOptions class. The WorldBoundary parameter has been removed.

  -----------------------------------------------------------------------------------------------
  **Parameter**           **V1 Value**            **Description**
  ----------------------- ----------------------- -----------------------------------------------
  MaxForwardSpeed         20.0f                   Speed in m/s at ThrottleStep = 10.

  MaxStrafeSpeed          10.0f                   Top speed for sideways movement.

  MaxVerticalSpeed        5.0f                    Top speed for vertical movement.

  YawSpeed                1.5708f                 Turn rate in radians per second (90 deg/sec).

  AccelerationFactor      5.0f                    A multiplier for the Lerp smoothing.
  -----------------------------------------------------------------------------------------------

**Code for Options Class:**

public class FlightModelOptions\
{\
public float MaxForwardSpeed { get; set; } = 20.0f;\
public float MaxStrafeSpeed { get; set; } = 10.0f;\
public float MaxVerticalSpeed { get; set; } = 5.0f;\
public float YawSpeed { get; set; } = 1.5708f;\
public float AccelerationFactor { get; set; } = 5.0f;\
}

**5. Code Skeleton**

// In project: DroneSim.FlightDynamics\
using DroneSim.Core;\
using Microsoft.Extensions.Options;\
using System.Numerics;\
\
/// \<summary\>\
/// V1 implementation of the flight dynamics module.\
/// Uses a kinematic model to calculate desired displacement and orientation.\
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
/// Calculates the drone\'s desired displacement and orientation change.\
/// Does not perform any collision or boundary checks.\
/// \</summary\>\
public (Vector3 DesiredDisplacement, Quaternion NewOrientation) CalculateKinematicUpdate(\
DroneState currentState, ControlInputs inputs, float deltaTime)\
{\
// Step 1: Calculate Current Forward Speed (Smooth Throttle)\
\_droneForwardSpeeds.TryAdd(currentState.Id, 0.0f);\
float targetForwardSpeed = (inputs.ThrottleStep / 10.0f) \* \_options.MaxForwardSpeed;\
float currentForwardSpeed = \_droneForwardSpeeds\[currentState.Id\];\
\_droneForwardSpeeds\[currentState.Id\] = Math.Abs(targetForwardSpeed - currentForwardSpeed) \> 0.01f\
? float.Lerp(currentForwardSpeed, targetForwardSpeed, deltaTime \* \_options.AccelerationFactor)\
: targetForwardSpeed;\
\
// Step 2: Calculate World-Space Velocity Vector\
var forwardDirection = Vector3.Transform(Vector3.UnitZ, currentState.Orientation);\
var rightDirection = Vector3.Transform(Vector3.UnitX, currentState.Orientation);\
\
var forwardVelocity = forwardDirection \* \_droneForwardSpeeds\[currentState.Id\];\
var strafeVelocity = rightDirection \* inputs.StrafeInput \* \_options.MaxStrafeSpeed;\
var verticalVelocity = Vector3.UnitY \* inputs.VerticalInput \* \_options.MaxVerticalSpeed;\
var totalVelocity = forwardVelocity + strafeVelocity + verticalVelocity;\
\
// Step 3: Calculate final displacement\
var desiredDisplacement = totalVelocity \* deltaTime;\
\
// Step 4: Calculate new Orientation (Yaw)\
var yawChange = inputs.YawInput \* \_options.YawSpeed \* deltaTime;\
var yawRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -yawChange);\
var newOrientation = currentState.Orientation \* yawRotation;\
\
// Step 5: Return the result\
return (desiredDisplacement, newOrientation);\
}\
}
