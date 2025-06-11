Document ID: MODSPEC-ORCH-V1.2

Date: June 10, 2025

Title: V1.2 Detailed Orchestrator Module Specification

**1. Overview**

This document provides the detailed implementation specification for the Orchestrator class. This class is the central nervous system of the simulation, connecting all independent modules. It owns the primary application state and implements the core simulation logic.

Its execution is driven by the Renderer through the IFrameTickable interface, ensuring a clean separation between the simulation update loop and the rendering loop. It also serves as the single source of truth for the renderer via the IRenderDataSource interface.

**2. Dependencies**

The Orchestrator depends on all core module interfaces from DroneSim.Core:

- IPlayerInput

- IFlightDynamics

- IPhysicsService

- ITerrainGenerator

- IAIDroneSpawner

- It also depends on injected IOptions\<T\> for its own configuration.

**3. V1 Functional Specification**

- **Interface Implementation:** The Orchestrator class shall implement both IFrameTickable and IRenderDataSource.

- **Setup() method (IFrameTickable):** This method is called once by the renderer at startup. It performs the following setup:

  1.  Calls ITerrainGenerator.Generate() to get the WorldData.

  2.  Creates the initial player DroneAgent (ID 0) at a default position (e.g., (0, 5, 0)). The player drone gets a null autopilot controller.

  3.  Calls IAIDroneSpawner.CreateDrones() to get a list of all AI-controlled agents.

  4.  Combines the player and AI drones into the master \_allDrones list.

  5.  Initializes all state variables: \_playerControlledDroneId = 0, \_cameraAttachedToDroneId = 0, etc.

- **UpdateFrame(deltaTime) method (IFrameTickable):** This is the core logic loop, called every frame by the renderer.

  1.  **Poll Input:** Calls \_playerInput.Update() with the current keyboard state.

  2.  **Process State Inputs:** Checks the flags from the input module (IsPossessKeyPressed, etc.) and updates the orchestrator\'s state variables (\_playerControlledDroneId, \_cameraViewMode, etc.). The camera tilt is updated by applying GetCameraTiltInput() over deltaTime and clamping the result.

  3.  **Update Agent Dynamics (Main Loop):** Iterates through every DroneAgent.

      - Determines the source of ControlInputs (from IPlayerInput if the agent is the player, or from the agent\'s IAutopilot if it\'s an AI).

      - Calls \_flightModel.CalculateKinematicUpdate to get the desired displacement and new orientation.

      - Calls \_physicsService.ResolveEnvironmentCollisions with the drone\'s current position and desired displacement to get the final, valid position.

      - Updates the agent\'s DroneState with the new position and orientation.

  4.  **Update AI Targets:** After all drones have moved, it iterates through the AI agents. If an AI drone has reached its destination, it is assigned a new random target.

- **IRenderDataSource Implementation:**

  - The getter methods (GetAllDroneStates(), GetPlayerControlledDroneId(), etc.) simply return the current value of the corresponding private state fields.

  - The GetHudInfo() method builds the formatted, multi-line HUD string using the current state data.

**4. Configuration & Parameters**

  -----------------------------------------------------------------------------------------------------------
  **Parameter**           **V1 Value**            **Description**
  ----------------------- ----------------------- -----------------------------------------------------------
  PlayerDroneCount        1                       The number of player-controllable drones to create.

  AIDroneCount            9                       The number of AI drones to create.

  CameraTiltSpeed         1.5708f                 The speed in radians/sec for camera tilting (90 deg/sec).

  MinCameraTilt           -0.7854f                The minimum camera tilt angle (-45 degrees).

  MaxCameraTilt           0.3490f                 The maximum camera tilt angle (20 degrees).
  -----------------------------------------------------------------------------------------------------------

**5. Code Skeleton**

// In project: DroneSim.App\
using DroneSim.Core;\
using Microsoft.Extensions.Options;\
using System.Numerics;\
using System.Text;\
\
public class Orchestrator : IFrameTickable, IRenderDataSource\
{\
// Injected Modules\
private readonly IPlayerInput \_playerInput;\
private readonly IFlightDynamics \_flightModel;\
private readonly IPhysicsService \_physicsService;\
private readonly ITerrainGenerator \_terrainGenerator;\
private readonly IAIDroneSpawner \_aiSpawner;\
// \... other dependencies like options\
\
// Simulation State\
private List\<DroneAgent\> \_allDrones;\
private WorldData \_worldData;\
private int \_playerControlledDroneId = 0;\
private int \_cameraAttachedToDroneId = 0;\
private CameraViewMode \_cameraViewMode = CameraViewMode.OverTheShoulder;\
private float \_cameraTilt = 0.0f;\
// \... other state and options fields\
\
public Orchestrator(/\*\...all injected dependencies\...\*/) { /\* \... \*/ }\
\
// \-\-- IFrameTickable Implementation \-\--\
\
public void Setup()\
{\
\_worldData = \_terrainGenerator.Generate();\
\
var playerState = new DroneState { Id = 0, Position = new Vector3(0, 5, 0), /\*\...\*/ };\
var playerAgent = new DroneAgent(playerState, null); // Player has no autopilot\
\
var aiAgents = \_aiSpawner.CreateDrones(9, \_worldData);\
\
\_allDrones = new List\<DroneAgent\> { playerAgent };\
\_allDrones.AddRange(aiAgents);\
}\
\
public void UpdateFrame(float deltaTime)\
{\
// 1. Poll input. Assume host passes keyboard state.\
// \_playerInput.Update(keyboard);\
\
// 2. Process state changes from input\
ProcessStateInputs(deltaTime);\
\
// 3. Update all drone positions\
foreach (var agent in \_allDrones)\
{\
var inputs = (agent.State.Id == \_playerControlledDroneId)\
? \_playerInput.GetFlightControls()\
: agent.AutopilotController.GetControlUpdate(agent.State);\
\
var (disp, orient) = \_flightModel.CalculateKinematicUpdate(agent.State, inputs, deltaTime);\
var finalPos = \_physicsService.ResolveEnvironmentCollisions(agent.State.Position, disp);\
\
agent.State.Position = finalPos;\
agent.State.Orientation = orient;\
}\
\
// 4. Update AI logic (check for arrival, set new targets)\
UpdateAIState();\
}\
\
private void ProcessStateInputs(float deltaTime) { /\* logic for possession, camera switching, tilt, etc. \*/ }\
private void UpdateAIState() { /\* logic for checking AI arrival and assigning new targets \*/ }\
\
// \-\-- IRenderDataSource Implementation \-\--\
\
public IReadOnlyList\<DroneState\> GetAllDroneStates() =\> \_allDrones;\
public int GetPlayerControlledDroneId() =\> \_playerControlledDroneId;\
public int GetCameraAttachedToDroneId() =\> \_cameraAttachedToDroneId;\
public CameraViewMode GetCameraViewMode() =\> \_cameraViewMode;\
public float GetCameraTilt() =\> \_cameraTilt;\
\
public string GetHudInfo()\
{\
var sb = new StringBuilder();\
// \... build the multi-line HUD string using the current state \...\
return sb.ToString();\
}\
}
