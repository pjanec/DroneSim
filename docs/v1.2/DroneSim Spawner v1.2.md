Document ID: MODSPEC-SPAWNER-V1.2

Date: June 10, 2025

Title: V1.2 Detailed IAIDroneSpawner Module Specification (Implementing Class: V1AIDroneSpawner)

**1. Overview**

This document provides the detailed implementation specification for the V1 IAIDroneSpawner interface, named V1AIDroneSpawner. The sole purpose of this module is to run once at the beginning of the simulation to create a specified number of AI-controlled drones.

It acts as a factory for DroneAgent objects. For each drone, it is responsible for instantiating its initial DroneState, creating its IAutopilot controller, assigning it an initial random target, and bundling them into a DroneAgent.

To maintain modularity, this spawner does not create a specific type of autopilot directly. Instead, it uses an injected IAutopilotFactory to create autopilot instances, making it independent of the AI\'s implementation details.

**2. Dependencies**

- **DroneSim.Core:** For the IAIDroneSpawner interface, DroneAgent class, and the IAutopilotFactory interface.

- **Microsoft.Extensions.Options:** To receive configuration parameters from the DI container.

**3. Architectural Note: The Autopilot Factory**

The DI container will be configured to inject a concrete V1StupidAutopilotFactory (which implements IAutopilotFactory) into the spawner. This factory\'s Create() method will return a new V1StupidAutopilot(\...). This ensures the spawner itself remains decoupled.

**4. V1 Functional Specification**

- The V1AIDroneSpawner class shall implement the IAIDroneSpawner interface.

- The CreateDrones method will be the single entry point.

- Upon being called, it will loop count times to create the requested number of agents.

- For each agent, it will:

  1.  Invoke the IAutopilotFactory.Create() method to get a new IAutopilot instance.

  2.  Generate a random starting Position within the defined world boundaries and at the InitialFlightAltitude.

  3.  Generate a random target Position using the same logic.

  4.  Assign the random target to the new autopilot instance via its SetTarget method.

  5.  Instantiate a DroneState with a unique ID (starting from 1, as 0 is reserved for the player), the random starting position, and a default Quaternion.Identity orientation.

  6.  Instantiate a DroneAgent, packaging the DroneState and the IAutopilot instance.

- The method will return a List\<DroneAgent\> containing all the created agents.

**5. Configuration & Parameters**

The module will be configured via an options class.

  -----------------------------------------------------------------------------------------------------------------
  **Parameter**           **V1 Value**            **Description**
  ----------------------- ----------------------- -----------------------------------------------------------------
  InitialFlightAltitude   20.0f                   The Y-coordinate at which all AI drones will be spawned.

  WorldBoundary           128.0f                  The maximum absolute coordinate for random position generation.
  -----------------------------------------------------------------------------------------------------------------

**Code for Options Class:**

public class SpawnerOptions\
{\
public float InitialFlightAltitude { get; set; } = 20.0f;\
public float WorldBoundary { get; set; } = 128.0f;\
}

**6. Code Skeleton**

// In project: DroneSim.AISpawner\
using DroneSim.Core;\
using Microsoft.Extensions.Options;\
using System.Numerics;\
\
/// \<summary\>\
/// V1 implementation of the AI drone spawner.\
/// Creates a list of AI-controlled DroneAgents with random starting positions and targets.\
/// \</summary\>\
public class V1AIDroneSpawner : IAIDroneSpawner\
{\
private readonly IAutopilotFactory \_autopilotFactory;\
private readonly SpawnerOptions \_options;\
private readonly Random \_random = new();\
\
public V1AIDroneSpawner(IAutopilotFactory autopilotFactory, IOptions\<SpawnerOptions\> options)\
{\
\_autopilotFactory = autopilotFactory ?? throw new ArgumentNullException(nameof(autopilotFactory));\
\_options = options.Value;\
}\
\
/// \<summary\>\
/// Creates a specified number of AI-controlled drone agents.\
/// \</summary\>\
/// \<param name=\"count\"\>The number of AI drones to create.\</param\>\
/// \<param name=\"worldData\"\>The world data (unused in V1 but part of the interface for future use).\</param\>\
/// \<returns\>A list of newly created DroneAgents.\</returns\>\
public List\<DroneAgent\> CreateDrones(int count, WorldData worldData)\
{\
var agents = new List\<DroneAgent\>();\
if (count \<= 0) return agents;\
\
for (int i = 0; i \< count; i++)\
{\
var autopilot = \_autopilotFactory.Create();\
\
var startPosition = GetRandomPositionOnPlane();\
var targetPosition = GetRandomPositionOnPlane();\
autopilot.SetTarget(targetPosition);\
\
// Create the initial drone state. We use i + 1 for the ID, as ID 0 is reserved for the player\'s initial drone.\
var initialState = new DroneState\
{\
Id = i + 1,\
Position = startPosition,\
Orientation = Quaternion.Identity,\
Status = DroneStatus.Active\
};\
\
agents.Add(new DroneAgent(initialState, autopilot));\
}\
\
return agents;\
}\
\
private Vector3 GetRandomPositionOnPlane()\
{\
float x = (float)(\_random.NextDouble() \* 2 - 1) \* \_options.WorldBoundary;\
float z = (float)(\_random.NextDouble() \* 2 - 1) \* \_options.WorldBoundary;\
return new Vector3(x, \_options.InitialFlightAltitude, z);\
}\
}
