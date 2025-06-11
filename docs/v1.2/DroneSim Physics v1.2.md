Document ID: MODSPEC-PHYSICS-V1.2

Date: June 10, 2025

Title: V1.2 Detailed IPhysicsService Module Specification (Implementing Class: V1BoundaryPhysicsService)

**1. Overview**

This document provides the detailed implementation specification for the V1 IPhysicsService interface, named V1BoundaryPhysicsService. This module was introduced to decouple the flight dynamics logic from the world\'s geometric rules.

Its sole responsibility in V1 is to act as a simple \"movement resolution\" service. It takes a desired, unobstructed movement vector and returns a final, valid position after checking it against the hard-coded world boundaries and ground plane. It does not simulate forces, momentum, or object-to-object collisions.

**2. Dependencies**

- **DroneSim.Core:** For the IPhysicsService interface.

- **Microsoft.Extensions.Options:** To receive configuration parameters from the DI container in a strongly-typed manner.

**3. V1 Functional Specification**

- The V1BoundaryPhysicsService class shall implement the IPhysicsService interface.

- The class is stateless and its ResolveEnvironmentCollisions method is a pure function (its output depends only on its inputs).

- The ResolveEnvironmentCollisions method accepts the drone\'s currentPosition and its desiredDisplacement for the frame.

- **Logic:**

  1.  It first calculates the potential new position: var proposedPosition = currentPosition + desiredDisplacement;

  2.  It then clamps this proposedPosition vector to the defined boundaries:

      - **Ground Collision:** The Y-coordinate is clamped to a minimum of 0.0f. (Math.Max(0.0f, proposedPosition.Y))

      - **World Boundaries:** The X and Z coordinates are clamped between -worldBoundary and +worldBoundary. (Math.Clamp(\...))

  3.  It returns the final, clamped Vector3 position.

**4. Configuration & Parameters**

The module will be configured via an options class injected by the DI container.

  -------------------------------------------------------------------------------------------------------------
  **Parameter**           **V1 Value**            **Description**
  ----------------------- ----------------------- -------------------------------------------------------------
  WorldBoundary           128.0f                  The maximum absolute coordinate value for the X and Z axes.

  -------------------------------------------------------------------------------------------------------------

**Code for Options Class:**

public class PhysicsOptions\
{\
public float WorldBoundary { get; set; } = 128.0f;\
}

**5. Code Skeleton**

// In project: DroneSim.PhysicsService\
using DroneSim.Core;\
using Microsoft.Extensions.Options; // For strongly-typed configuration\
using System.Numerics;\
\
/// \<summary\>\
/// V1 implementation of the physics service.\
/// Provides simple movement resolution by clamping positions to world boundaries.\
/// \</summary\>\
public class V1BoundaryPhysicsService : IPhysicsService\
{\
private readonly float \_worldBoundary;\
\
/// \<summary\>\
/// Initializes the service with the world boundary configuration.\
/// \</summary\>\
public V1BoundaryPhysicsService(IOptions\<PhysicsOptions\> options)\
{\
// IOptions\<T\> is the standard way to inject configuration in .NET\
\_worldBoundary = options.Value.WorldBoundary;\
}\
\
/// \<summary\>\
/// Resolves desired movement against the environment\'s static boundaries.\
/// \</summary\>\
/// \<param name=\"currentPosition\"\>The object\'s starting position for the frame.\</param\>\
/// \<param name=\"desiredDisplacement\"\>The unobstructed displacement vector for the frame.\</param\>\
/// \<returns\>The final, valid position after clamping to boundaries.\</returns\>\
public Vector3 ResolveEnvironmentCollisions(Vector3 currentPosition, Vector3 desiredDisplacement)\
{\
var finalPosition = currentPosition + desiredDisplacement;\
\
// Clamp to X and Z boundaries\
finalPosition.X = Math.Clamp(finalPosition.X, -\_worldBoundary, \_worldBoundary);\
finalPosition.Z = Math.Clamp(finalPosition.Z, -\_worldBoundary, \_worldBoundary);\
\
// Clamp to ground plane\
finalPosition.Y = Math.Max(0.0f, finalPosition.Y);\
\
return finalPosition;\
}\
}
