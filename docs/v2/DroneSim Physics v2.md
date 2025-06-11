Document ID: MODSPEC-PHYSICS-V2.0

Date: June 11, 2025

Title: V2 IPhysicsService Module Specification (Implementing Class: V2BepuPhysicsService)

**1. Overview**

This document specifies the V2 implementation of the physics service, named V2BepuPhysicsService. This module is a high-level wrapper around a real-time physics engine (assumed to be **BepuPhysics**). It replaces the simple V1 boundary checker entirely.

Its core responsibility is to manage a single, unified simulation world that can contain both **dynamic** rigid bodies (influenced by forces, for the V2 flight model) and **kinematic** bodies (driven by velocity, for the V1 flight model). This service is the authoritative source for all object positions and orientations and is responsible for detecting and resolving all collisions.

**2. Dependencies**

- **DroneSim.Core:** For the unified IPhysicsService interface and all data structures.

- **BepuPhysics & BepuUtilities:** The underlying physics engine libraries.

- **Microsoft.Extensions.Options:** To receive configuration parameters like gravity.

**3. V2 Functional Specification**

- **Initialization:**

  - The constructor initializes the core BepuPhysics objects: Simulation, BufferPool, and a custom implementation of INarrowPhaseCallbacks for collision event detection.

  - It configures the simulation\'s global settings, such as the gravity vector.

- **Body Management:**

  - AddStaticBody(): Creates a static collider (e.g., from a terrain mesh) and adds it to the simulation.

  - AddDynamicBody(): Creates a dynamic rigid body with properties like mass and inertia. Adds it to the simulation and returns a handle. This body type will be affected by forces and torques.

  - AddKinematicBody(): Creates a kinematic rigid body. Adds it to the simulation and returns a handle. This body type is not affected by forces but will collide with other objects. Its velocity is set directly.

- **Intent-Based Simulation:**

  - SubmitMoveIntent(): This method is called for every drone, every frame, *before* the Step() method. It does not perform any calculations itself. It stores the IMoveIntent for each drone handle in an internal dictionary, ready to be processed.

- **Simulation Step:**

  - The Step(deltaTime) method is the \"engine crank\" called once per frame by the Orchestrator. It performs the following sequence:

    1.  **Pre-Step (Apply Intents):** It iterates through the dictionary of stored intents.

        - If an intent is DynamicIntent, it applies the specified force and torque to the corresponding dynamic body in the Bepu simulation (e.g., body.ApplyForce()).

        - If an intent is KinematicIntent, it sets the velocity of the corresponding kinematic body directly (e.g., body.Velocity.Linear = \...).

    2.  **Simulation Execution:** It calls the main \_simulation.Step(deltaTime) method. BepuPhysics now takes over, calculating all accelerations, velocities, positions, and resolving all collisions and physical responses (bouncing, friction, etc.) for every object in the world.

    3.  **Post-Step (Cleanup):** It clears the dictionary of intents to prepare for the next frame.

- **Collision Events:**

  - The custom INarrowPhaseCallbacks implementation will detect when two bodies begin to collide.

  - When a collision is detected, it will fire the public C# CollisionDetected event, passing information about which two bodies collided. The Orchestrator will subscribe to this to implement game logic like crashing.

- **State Retrieval:**

  - The GetState(bodyHandle) method queries the BepuPhysics simulation for the specified body\'s latest pose (position and orientation) and returns it as a standard DroneState struct.

**4. Configuration & Parameters**

  ---------------------------------------------------------------------------------------------------------------
  **Parameter**           **V2 Value**            **Description**
  ----------------------- ----------------------- ---------------------------------------------------------------
  Gravity                 -9.81f                  The downward acceleration on the Y-axis.

  TimestepCount           4                       Number of sub-steps per frame for higher simulation fidelity.
  ---------------------------------------------------------------------------------------------------------------

**5. Code Skeleton**

// In project: DroneSim.PhysicsService\
using DroneSim.Core;\
using BepuPhysics;\
using BepuUtilities.Memory;\
using System.Numerics;\
using Microsoft.Extensions.Options;\
\
/// \<summary\>\
/// V2 implementation of the physics service using BepuPhysics engine.\
/// Manages a unified simulation for both dynamic and kinematic bodies.\
/// \</summary\>\
public class V2BepuPhysicsService : IPhysicsService // Assuming IPhysicsService is the unified V2 interface\
{\
private readonly Simulation \_simulation;\
private readonly BufferPool \_bufferPool;\
\
// Stores the movement intentions submitted for the current frame.\
private readonly Dictionary\<int, IMoveIntent\> \_moveIntents = new();\
\
public event Action\<CollisionEventData\> CollisionDetected;\
\
public V2BepuPhysicsService(IOptions\<PhysicsOptionsV2\> options)\
{\
\_bufferPool = new BufferPool();\
// A custom narrow phase callback handler is needed to raise collision events.\
// var narrowPhaseCallbacks = new BepuCollisionCallbacks(CollisionDetected);\
// var poseIntegratorCallbacks = new PoseIntegratorCallbacks(new Vector3(0, options.Value.Gravity, 0));\
\
// \_simulation = Simulation.Create(\_bufferPool, narrowPhaseCallbacks, poseIntegratorCallbacks, new SolveDescription(options.Value.TimestepCount, 1));\
}\
\
// \-\-- Setup Methods \-\--\
public void AddStaticBody(PhysicsBody bodyData) { /\* Creates a Bepu StaticDescription and adds to \_simulation \*/ }\
public int AddDynamicBody(object description) { /\* Creates a Bepu BodyDescription, adds to \_simulation, returns handle \*/ return -1;}\
public int AddKinematicBody(object description) { /\* Creates a Bepu BodyDescription with kinematic inertia, adds to \_simulation, returns handle \*/ return -1;}\
\
// \-\-- Per-Frame Simulation \-\--\
public void SubmitMoveIntent(int bodyHandle, IMoveIntent intent)\
{\
\_moveIntents\[bodyHandle\] = intent;\
}\
\
public void Step(float deltaTime)\
{\
// 1. Pre-Step: Apply all stored intents from this frame.\
foreach (var entry in \_moveIntents)\
{\
var bodyHandle = new BodyHandle(entry.Key); // Assuming our handle maps directly\
// var body = \_simulation.Bodies.GetBodyReference(bodyHandle);\
\
switch (entry.Value)\
{\
case DynamicIntent di:\
// Bepu uses impulses, so convert Force -\> Impulse\
// body.ApplyLinearImpulse(di.Force \* deltaTime);\
// body.ApplyAngularImpulse(di.Torque \* deltaTime);\
break;\
case KinematicIntent ki:\
// body.Velocity.Linear = ki.TargetVelocity;\
// body.Velocity.Angular = CalculateAngularVelocityFor(ki.TargetOrientation); // More complex logic here\
break;\
}\
}\
\
// 2. Simulation Execution: The engine does all the hard work.\
\_simulation.Timestep(deltaTime);\
\
// 3. Post-Step: Cleanup\
\_moveIntents.Clear();\
}\
\
// \-\-- State Retrieval \-\--\
public DroneState GetState(int bodyHandle)\
{\
// var body = \_simulation.Bodies.GetBodyReference(new BodyHandle(bodyHandle));\
// return new DroneState { Position = body.Pose.Position, Orientation = body.Pose.Orientation, \... };\
return default;\
}\
}
