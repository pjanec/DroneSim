Document ID: MODSPEC-AI-V1.2

Date: June 10, 2025

Title: V1.2 Detailed IAutopilot Module Specification (Implementing Class: V1StupidAutopilot)

**1. Overview**

This document provides the detailed implementation specification for the V1 IAutopilot interface, named V1StupidAutopilot. Each instance of this class is responsible for the autonomous control of a single drone.

The V1 implementation is intentionally simplistic, often referred to as a \"stupid\" AI. It does not use the navigation grid and has no collision avoidance capabilities. Its behavior follows a simple \"turn-then-burn\" logic: it will first turn to face its target, then fly forward in a straight line at a constant altitude until it arrives. This provides predictable behavior for initial simulation testing.

**2. Dependencies**

- **DroneSim.Core:** For the IAutopilot interface and all related data structures.

- **Microsoft.Extensions.Options:** To receive the AI behavior tuning parameters from the DI container.

**3. V1 Functional Specification**

- The V1StupidAutopilot class shall implement the IAutopilot interface.

- Each instance of the class maintains its own state, specifically the 3D position of its current target.

- The SetTarget(Vector3 targetPosition) method updates this internal target, but it normalizes the target to the configured flight altitude (\_targetPosition.Y = \_flightAltitude).

- The GetControlUpdate(DroneState currentDroneState) method contains the core AI logic and returns a ControlInputs struct based on the following rules:

  1.  **Arrival Check:** It first checks the horizontal distance to the target. If the distance is within the ArrivalRadius, it returns empty ControlInputs (all zero), effectively stopping the drone.

  2.  **Altitude Control:** It independently checks the drone\'s current altitude against the desired FlightAltitude. It generates a vertical input to ascend or descend towards this altitude. A small dead zone (e.g., +/- 1m) prevents jittering.

  3.  **Yaw Control:** It calculates the angle between the drone\'s current forward direction and the direction to the target on the horizontal (XZ) plane. If this angle is outside the YawTolerance, it generates a yaw input to turn the drone towards the target. During this turning phase, forward throttle is zero.

  4.  **Forward Control:** Only when the drone is facing the target (within the YawTolerance) will it apply a constant forward throttle (ConstantThrottleStep) to move towards it.

**4. Configuration & Parameters**

The module will be configured via an AIBehaviorOptions class.

  ----------------------------------------------------------------------------------------------------------------------------------------
  **Parameter**           **V1 Value**            **Description**
  ----------------------- ----------------------- ----------------------------------------------------------------------------------------
  FlightAltitude          20.0f                   The target altitude (in meters) the AI will try to maintain.

  ConstantThrottleStep    4                       The fixed throttle level (0-10) used when flying forward.

  ArrivalRadius           5.0f                    The distance (in meters) from the target at which the drone is considered \"arrived\".

  YawTolerance            0.1f                    The angle in radians (\~5.7 degrees) within which the drone is \"facing\" its target.
  ----------------------------------------------------------------------------------------------------------------------------------------

**Code for Options Class:**

public class AIBehaviorOptions\
{\
public float FlightAltitude { get; set; } = 20.0f;\
public int ConstantThrottleStep { get; set; } = 4;\
public float ArrivalRadius { get; set; } = 5.0f;\
public float YawTolerance { get; set; } = 0.1f;\
}

**5. Code Skeleton**

// In project: DroneSim.Autopilot\
using DroneSim.Core;\
using Microsoft.Extensions.Options;\
using System.Numerics;\
\
/// \<summary\>\
/// V1 implementation of the autopilot AI.\
/// Provides simple \"turn-and-burn\" logic to fly to a target.\
/// \</summary\>\
public class V1StupidAutopilot : IAutopilot\
{\
private Vector3 \_targetPosition;\
private readonly AIBehaviorOptions \_options;\
\
// Use squared distance for efficiency in checks\
private readonly float \_arrivalRadiusSq;\
\
public V1StupidAutopilot(IOptions\<AIBehaviorOptions\> options)\
{\
\_options = options.Value;\
\_arrivalRadiusSq = \_options.ArrivalRadius \* \_options.ArrivalRadius;\
}\
\
/// \<summary\>\
/// Assigns a new destination for the autopilot to navigate to.\
/// \</summary\>\
public void SetTarget(Vector3 targetPosition)\
{\
\_targetPosition = new Vector3(targetPosition.X, \_options.FlightAltitude, targetPosition.Z);\
}\
\
/// \<summary\>\
/// Calculates the control inputs needed to move towards the target.\
/// \</summary\>\
public ControlInputs GetControlUpdate(DroneState currentDroneState)\
{\
var inputs = new ControlInputs();\
var currentPosition = currentDroneState.Position;\
\
// \-\-- Step 1: Check for arrival \-\--\
var horizontalVectorToTarget = new Vector2(\_targetPosition.X - currentPosition.X, \_targetPosition.Z - currentPosition.Z);\
if (horizontalVectorToTarget.LengthSquared() \< \_arrivalRadiusSq)\
{\
return inputs; // Arrived, return empty inputs.\
}\
\
// \-\-- Step 2: Altitude Control \-\--\
float altitudeError = \_targetPosition.Y - currentPosition.Y;\
if (altitudeError \> 1.0f) inputs.VerticalInput = 1.0f;\
else if (altitudeError \< -1.0f) inputs.VerticalInput = -1.0f;\
\
// \-\-- Step 3: Yaw Control \-\--\
var forwardVector = Vector3.Transform(Vector3.UnitZ, currentDroneState.Orientation);\
var forwardVector2D = Vector2.Normalize(new Vector2(forwardVector.X, forwardVector.Z));\
var targetDirection2D = Vector2.Normalize(horizontalVectorToTarget);\
\
// Dot product gives cosine of the angle. Acos gives the angle.\
float angle = (float)Math.Acos(Vector2.Dot(forwardVector2D, targetDirection2D));\
\
if (angle \> \_options.YawTolerance)\
{\
// Use 2D cross product to determine turn direction\
var crossZ = forwardVector2D.X \* targetDirection2D.Y - forwardVector2D.Y \* targetDirection2D.X;\
inputs.YawInput = Math.Sign(crossZ);\
}\
else\
{\
// \-\-- Step 4: Throttle Control (only if facing target) \-\--\
inputs.ThrottleStep = \_options.ConstantThrottleStep;\
}\
\
return inputs;\
}\
}
