Document ID: MODSPEC-SPAWNER-V1.3  
Date: June 11, 2025  
Title: V1.3 Detailed IAIDroneSpawner Module Specification (Implementing Class: V1AIDroneSpawner)  
**1\. Overview**

This document provides the detailed implementation specification for the V1 IAIDroneSpawner interface, V1AIDroneSpawner. Its purpose is to run once at startup to create all AI-controlled DroneAgent objects.

**This module's responsibility has been updated.** It must now interact with the IPhysicsService to register each new drone in the physics simulation and retrieve its unique PhysicsBodyHandle. This handle is then used to create the complete DroneAgent object.

**2\. Dependencies**

* **DroneSim.Core:** For all interfaces and data structures.  
* **IPhysicsService:** To add new drone bodies to the simulation world.  
* **IAutopilotFactory:** To create autopilot instances for each drone.  
* **Microsoft.Extensions.Options:** To receive configuration parameters.

**3\. V1 Functional Specification**

* The V1AIDroneSpawner class shall implement the IAIDroneSpawner interface.  
* The CreateDrones method will be the single entry point.  
* For each agent to be created, it will perform the following sequence:  
  1. Invoke the IAutopilotFactory.Create() to get a new IAutopilot instance.  
  2. Generate a random start position and a random target position.  
  3. Assign the target to the new autopilot via SetTarget().  
  4. Create the initial DroneState with a unique ID (starting from 1).  
  5. **NEW:** Call \_physicsService.AddKinematicBody() (for V1 drones) with a description of the drone's collision shape. This method returns the physicsBodyHandle.  
  6. **NEW:** Instantiate a DroneAgent, providing the physicsBodyHandle, the initial DroneState, and the IAutopilot instance to its constructor.  
* The method will return a List\<DroneAgent\> containing all the created agents.

**4\. Configuration & Parameters**

The module will be configured via an options class.

| Parameter | V1 Value | Description |
| :---- | :---- | :---- |
| InitialFlightAltitude | 20.0f | The Y-coordinate at which all AI drones will be spawned. |
| WorldBoundary | 128.0f | The maximum absolute coordinate for random position generation. |

**5\. Code Skeleton**

// In project: DroneSim.AISpawner  
using DroneSim.Core;  
using Microsoft.Extensions.Options;  
using System.Numerics;

/// \<summary\>  
/// V1 implementation of the AI drone spawner.  
/// Creates DroneAgents and registers them with the physics service.  
/// \</summary\>  
public class V1AIDroneSpawner : IAIDroneSpawner  
{  
    private readonly IAutopilotFactory \_autopilotFactory;  
    private readonly IPhysicsService \_physicsService; // New Dependency  
    private readonly SpawnerOptions \_options;  
    private readonly Random \_random \= new();

    public V1AIDroneSpawner(  
        IAutopilotFactory autopilotFactory,   
        IPhysicsService physicsService,   
        IOptions\<SpawnerOptions\> options)  
    {  
        \_autopilotFactory \= autopilotFactory ?? throw new ArgumentNullException(nameof(autopilotFactory));  
        \_physicsService \= physicsService ?? throw new ArgumentNullException(nameof(physicsService));  
        \_options \= options.Value;  
    }

    public List\<DroneAgent\> CreateDrones(int count, WorldData worldData)  
    {  
        var agents \= new List\<DroneAgent\>();  
        if (count \<= 0\) return agents;

        for (int i \= 0; i \< count; i++)  
        {  
            var autopilot \= \_autopilotFactory.Create();

            var startPosition \= GetRandomPositionOnPlane();  
            var targetPosition \= GetRandomPositionOnPlane();  
            autopilot.SetTarget(targetPosition);

            var initialState \= new DroneState  
            {  
                Id \= i \+ 1, // ID 0 is reserved for the player  
                Position \= startPosition,  
                Orientation \= Quaternion.Identity,  
                Status \= DroneStatus.Active  
            };  
              
            // NEW: Add the drone to the physics world to get its handle  
            // The 'description' would be a simple object defining the drone's collision shape.  
            var physicsBodyHandle \= \_physicsService.AddKinematicBody(new { Shape \= "Drone" });

            // NEW: Create the agent with the physics handle  
            agents.Add(new DroneAgent(physicsBodyHandle, initialState, autopilot));  
        }

        return agents;  
    }

    private Vector3 GetRandomPositionOnPlane()  
    {  
        float x \= (float)(\_random.NextDouble() \* 2 \- 1\) \* \_options.WorldBoundary;  
        float z \= (float)(\_random.NextDouble() \* 2 \- 1\) \* \_options.WorldBoundary;  
        return new Vector3(x, \_options.InitialFlightAltitude, z);  
    }  
}  