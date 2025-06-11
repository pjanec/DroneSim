Document ID: MODSPEC-PHYSICS-V1.3-SIMPLE  
Date: June 11, 2025  
Title: V1.3 Simplified IPhysicsService Module Specification (Implementing Class: V2SimplePhysicsService)  
**1\. Overview**

This document specifies a **simplified, interim** V2 implementation of the IPhysicsService interface, named V2SimplePhysicsService.

Its purpose is to fulfill the V2 IPhysicsService contract with the most basic logic possible, **without using a real physics engine**. It simulates movement by using simple numerical integration (Forward Euler) and resolves collisions with a basic boundary clamping method.

This module is intended as a stepping stone. It allows development and testing of the V2PhysicsFlightModel and the V2 Orchestrator loop before the full V2BepuPhysicsService is complete. **It does not provide realistic physical interactions.**

**2\. Dependencies**

* **DroneSim.Core:** For the IPhysicsService interface and all data structures.  
* **Microsoft.Extensions.Options:** To receive configuration parameters.

**3\. V1 Functional Specification**

* **Internal State:** The service will maintain an internal list of SimplePhysicsBody objects. Each object will store its handle, mass, position, orientation, linear velocity, angular velocity, and a list of forces and torques that have been applied during the current frame.  
* **Body Management:**  
  * AddDynamicBody()/AddKinematicBody(): Creates a new SimplePhysicsBody instance and adds it to the internal list, returning its handle.  
* **Intent Submission (SubmitMoveIntent)**:  
  * This method inspects the IMoveIntent.  
  * For a DynamicIntent, it adds the Force and Torque to the list of forces for the specified body.  
  * For a KinematicIntent, it directly sets the Velocity and Orientation fields of the specified body, ignoring any forces.  
* **Simulation Step (Step(deltaTime))**: This is the core of the simplified engine.  
  1. It iterates through every SimplePhysicsBody in its list.  
  2. For **dynamic bodies**, it performs Forward Euler integration:  
     * Sum all forces from the list to get TotalForce.  
     * acceleration \= TotalForce / mass.  
     * velocity \+= acceleration \* deltaTime.  
     * (A simplified version for torque/orientation would also be applied).  
  3. For **all bodies**, it updates their position based on their current velocity: position \+= velocity \* deltaTime.  
  4. **Crucially, it then performs the V1-style boundary check**: The final calculated position is clamped to the ground plane (Y=0) and the world boundaries.  
  5. Finally, it clears the list of applied forces/torques for all bodies, preparing for the next frame.  
* **Collision Events:** The CollisionDetected event will **never be fired**, as this engine does not perform any actual shape-to-shape collision tests.

**4\. Limitations & Trade-offs**

This simplified implementation comes with significant trade-offs that must be understood:

* **Instability:** A flight model based on applying forces (like V2PhysicsFlightModel) will feel very unstable and "floaty" without the advanced solvers and stabilization provided by a real engine. It will not hover realistically without significant effort from the flight model's control logic (i.e., PID controllers).  
* **No Real Collisions:** This service does not detect collisions between drones or provide realistic responses like bouncing, friction, or sliding. Objects will simply stop dead and "stick" to the boundaries. Drones will fly through each other.  
* **Inaccurate Integration:** Forward Euler is the simplest but least stable numerical integration method. At high speeds or with large deltaTime, the simulation can become inaccurate or "explode."

**5\. Code Skeleton**

// In project: DroneSim.PhysicsService  
using DroneSim.Core;  
using Microsoft.Extensions.Options;  
using System.Numerics;

/// \<summary\>  
/// A simplified, "toy" implementation of the V2 physics service.  
/// Uses basic Euler integration and boundary clamping. Does not use a real physics engine.  
/// \</summary\>  
public class V2SimplePhysicsService : IPhysicsService  
{  
    private class SimplePhysicsBody  
    {  
        public int Handle;  
        public float Mass \= 1.0f;  
        public Vector3 Position;  
        public Quaternion Orientation;  
        public Vector3 LinearVelocity;  
        public Vector3 AngularVelocity; // Simplified for V1  
        public List\<Vector3\> ForcesThisFrame \= new();  
        public List\<Vector3\> TorquesThisFrame \= new();  
    }

    private readonly List\<SimplePhysicsBody\> \_bodies \= new();  
    private readonly float \_worldBoundary;

    public event Action\<CollisionEventData\> CollisionDetected; // Will never be invoked

    public V2SimplePhysicsService(IOptions\<PhysicsOptions\> options) { /\*...\*/ }

    public int AddDynamicBody(object description) { /\* Add a new SimplePhysicsBody to the list \*/ return \-1; }  
    public int AddKinematicBody(object description) { /\* Same as dynamic for this simple engine \*/ return \-1; }  
    public void AddStaticBody(PhysicsBody bodyData) { /\* Does nothing in this implementation \*/ }

    public void SubmitMoveIntent(int bodyHandle, IMoveIntent intent)  
    {  
        var body \= \_bodies.Find(b \=\> b.Handle \== bodyHandle);  
        if (body \== null) return;

        switch (intent)  
        {  
            case DynamicIntent di:  
                body.ForcesThisFrame.Add(di.Force);  
                body.TorquesThisFrame.Add(di.Torque);  
                break;  
            case KinematicIntent ki:  
                body.LinearVelocity \= ki.TargetVelocity;  
                body.Orientation \= ki.TargetOrientation;  
                break;  
        }  
    }

    public void Step(float deltaTime)  
    {  
        foreach (var body in \_bodies)  
        {  
            // \--- Integration for Dynamic Bodies \---  
            if (body.ForcesThisFrame.Any())  
            {  
                var totalForce \= Vector3.Zero;  
                foreach (var force in body.ForcesThisFrame) totalForce \+= force;

                var acceleration \= totalForce / body.Mass;  
                body.LinearVelocity \+= acceleration \* deltaTime;  
            }

            // \--- Position Update for All Bodies \---  
            var newPosition \= body.Position \+ body.LinearVelocity \* deltaTime;

            // \--- Collision Resolution (Clamping) \---  
            newPosition.X \= Math.Clamp(newPosition.X, \-\_worldBoundary, \_worldBoundary);  
            newPosition.Z \= Math.Clamp(newPosition.Z, \-\_worldBoundary, \_worldBoundary);  
            newPosition.Y \= Math.Max(0.0f, newPosition.Y);  
            body.Position \= newPosition;

            // Clear forces for next frame  
            body.ForcesThisFrame.Clear();  
            body.TorquesThisFrame.Clear();  
        }  
    }

    public DroneState GetState(int bodyHandle)  
    {  
        var body \= \_bodies.Find(b \=\> b.Handle \== bodyHandle);  
        // ... return state from the body ...  
        return default;  
    }  
}  