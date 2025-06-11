Document ID: MODSPEC-ORCH-V1.3

Date: June 11, 2025

Title: V1.3 Detailed Orchestrator Module Specification

**1. Overview**

This document provides the detailed implementation specification for the Orchestrator class. This class is the central nervous system of the simulation, connecting all independent modules. It owns the primary application state and implements the core simulation logic.

Its execution is driven by the Renderer through the IFrameTickable interface, ensuring a clean separation between the simulation update loop and the rendering loop. It also serves as the single source of truth for the renderer via the IRenderDataSource interface.

**2. Dependencies**

The Orchestrator depends on all core module interfaces and services:

- IPlayerInput

- IFlightDynamics

- IPhysicsService

- ITerrainGenerator

- IAIDroneSpawner

- IDebugDrawService

- It also depends on injected IOptions\<T\> for its own configuration.

**3. V1 Functional Specification**

- **Interface Implementation:** The Orchestrator class shall implement both IFrameTickable and IRenderDataSource.

- **Setup() method (IFrameTickable):** This method is called once by the renderer at startup. It performs the following setup:

  1.  Calls ITerrainGenerator.Generate() to get the WorldData.

  2.  Calls IPhysicsService.AddStaticBody() to add the terrain collider to the physics world.

  3.  Creates the initial player DroneAgent (ID 0) and adds it to the physics world via IPhysicsService.AddKinematicBody(), storing the returned handle. The player drone gets a null autopilot controller.

  4.  Calls IAIDroneSpawner.CreateDrones() to get a list of all AI-controlled agents. For each AI agent, it adds it to the physics world and stores its handle.

  5.  Initializes all state variables: \_playerControlledDroneId = 0, \_cameraAttachedToDroneId = 0, etc.

  6.  Subscribes its own OnCollision method to the IPhysicsService.CollisionDetected event.

- **UpdateFrame(deltaTime) method (IFrameTickable):** This is the core logic loop, called every frame by the renderer.

  1.  **Poll Input:** Calls \_playerInput.Update() with the current keyboard state.

  2.  **Process State Inputs:** Checks the flags from the input module (IsPossessKeyPressed, IsDebugTogglePressed, etc.) and updates the orchestrator\'s state variables.

  3.  **Submit Move Intents:** Iterates through every active DroneAgent.

      - Determines the source of ControlInputs.

      - Calls agent.FlightModel.GenerateMoveIntent() to get the movement intention.

      - Submits this intent to the physics service via \_physicsService.SubmitMoveIntent().

      - Calls IDebugDrawService to visualize flight model data if debug drawing is enabled.

  4.  **Step Physics World:** Makes a single call to \_physicsService.Step(deltaTime).

  5.  **Synchronize State:** Iterates through all agents and updates their DroneState by querying the physics service via \_physicsService.GetState().

  6.  **Update Debug Service:** Calls \_debugDrawService.Tick(deltaTime) to update the duration of persistent debug shapes.

  7.  **Update AI Targets:** Checks if any AI drones have arrived at their destination and assigns them new targets.

- **IRenderDataSource Implementation:**

  - Implements all getter methods (GetAllDroneStates(), etc.) to return the current value of the private state fields.

  - The GetHudInfo() method builds the formatted, multi-line HUD string.

- **Collision Handling:**

  - The OnCollision method, when triggered by the physics service event, will find the drone(s) involved and set their Status to Crashed. It will also call the IDebugDrawService to draw a marker at the collision point.

**4. Code Skeleton**

// In project: DroneSim.App\
using DroneSim.Core;\
using Microsoft.Extensions.Options;\
using System.Numerics;\
using System.Text;\
\
public class Orchestrator : IFrameTickable, IRenderDataSource\
{\
// Injected Modules & Services\
private readonly IPlayerInput \_playerInput;\
private readonly IFlightDynamics \_flightModel; // This could be a factory if drones have different models\
private readonly IPhysicsService \_physicsService;\
private readonly IDebugDrawService \_debugDraw;\
// \... other dependencies\
\
// Simulation State\
private List\<DroneAgent\> \_allDrones;\
private bool \_isDebugDrawingEnabled = false;\
// \... other state variables\
\
public Orchestrator(/\*\...all injected dependencies\...\*/)\
{\
// Subscribe to collision events\
\_physicsService.CollisionDetected += OnCollision;\
}\
\
public void Setup()\
{\
// \... generate world, add terrain static body to physics \...\
// \... create player drone, add to physics, store handle in agent \...\
// \... create AI drones, add to physics, store handles in agents \...\
}\
\
public void UpdateFrame(float deltaTime)\
{\
// 1. Poll input & Process state changes (camera, possession, debug toggle)\
ProcessStateInputs(deltaTime);\
\
// 2. Submit intents\
foreach (var agent in \_allDrones.Where(a =\> a.State.Status == DroneStatus.Active))\
{\
var inputs = GetControlInputsForAgent(agent);\
var intent = \_flightModel.GenerateMoveIntent(agent.State, inputs, deltaTime);\
\_physicsService.SubmitMoveIntent(agent.PhysicsBodyHandle, intent);\
\
if(\_isDebugDrawingEnabled)\
{\
// Example debug drawing\
// \_debugDraw.DrawVector(agent.State.Position, intent.Force, Color.Red);\
}\
}\
\
// 3. Step Physics World\
\_physicsService.Step(deltaTime);\
\
// 4. Synchronize State\
foreach (var agent in \_allDrones)\
{\
// Only update active drones from physics. Crashed drones are frozen.\
if(agent.State.Status == DroneStatus.Active)\
{\
agent.State = \_physicsService.GetState(agent.PhysicsBodyHandle);\
}\
}\
\
// 5. Update Debug Service Timers\
\_debugDraw.Tick(deltaTime);\
\
// 6. Update AI Logic\
UpdateAIState();\
}\
\
private void OnCollision(CollisionEventData eventData)\
{\
// Find agents, set status to Crashed, draw debug sphere\
}\
\
// \-\-- IRenderDataSource Implementation \-\--\
public bool IsDebugDrawingEnabled() =\> \_isDebugDrawingEnabled;\
// \... all other getter methods \...\
}

**5. Configuration & Parameters**

The orchestrator is configured via an `OrchestratorOptions` class bound from `appsettings.json`.

| Parameter | Default | Description |
| :--- | ---: | :--- |
| AIDroneCount | 9 | Number of AI-controlled drones spawned at startup. |
| CameraTiltSpeed | 1.5708f | Camera tilt speed in radians per second. |
| MinCameraTilt | -0.7854f | Minimum tilt angle in radians (-45°). |
| MaxCameraTilt | 0.3490f | Maximum tilt angle in radians (+20°). |

The `Orchestrator` implements both `IRenderDataSource` and `IWorldDataSource`. Modules like the renderer cast it to `IWorldDataSource` after `Setup()` completes to obtain terrain data for GPU initialization.
