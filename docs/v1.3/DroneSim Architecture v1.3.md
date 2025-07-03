Document ID: ARCH-DRNSIM-V1.3

Date: June 11, 2025

Title: V1.3 Software Architecture Specification

**1. Core Principles**

- **Interface-Based Design:** All modules communicate through C# interfaces defined in the DroneSim.Core library.

- **Dependency Injection (DI):** The main application host is responsible for creating concrete instances of all modules and "injecting" their interfaces into the constructors of modules that depend on them.

- **Renderer-Driven Loop:** The application's main loop is owned and driven by the Renderer module.

- **Centralized Simulation Logic:** The Orchestrator class centralizes all simulation state and logic. It is "ticked" every frame by the Renderer to progress the simulation state.

- **Unified Physics World:** A single, authoritative IPhysicsService manages all physical interactions for both kinematic and dynamic objects, ensuring consistent and realistic behavior.

**2. System Modules**

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **Module**                  **Interface(s)**                    **Responsibilities**
--------------------------- ----------------------------------- --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **Main Application Host**   *(Executable Host)*                 Program.cs. Configures and runs the .NET Generic Host, sets up DI, and starts the Renderer.

  **Renderer**                IRenderer                           Creates/manages the application window. **Drives the main loop.** Renders the 3D scene, HUD, and debug visuals by querying its data sources.

  **Orchestrator**            IFrameTickable, IRenderDataSource   Owns all simulation state. Implements the core UpdateFrame logic. Manages all DroneAgents. Acts as the data source for the Renderer.

  **Player Input**            IPlayerInput                        Reads raw keyboard state and translates it into structured control commands and toggle actions.

  **Flight Dynamics**         IFlightDynamics                     Translates control inputs into a high-level **IMoveIntent** (KinematicIntent or DynamicIntent). Has no knowledge of the physics simulation.

  **Physics Service**         IPhysicsService                     Manages the unified physics world. Accepts IMoveIntents from the Orchestrator, runs the simulation step, resolves all collisions, and acts as the source of truth for all physical states.

  **Debug Draw Service**      IDebugDrawService                   Collects requests to draw debug shapes (lines, vectors, etc.) from any module and provides them to the Renderer.

  **Autopilot**               IAutopilot                          Calculates ControlInputs for an AI drone to reach a target. Can use the IDebugDrawService to visualize its plan.

  **AI Drone Spawner**        IAIDroneSpawner                     Runs once at setup to create all AI-controlled DroneAgent objects.

  **Terrain Generator**       ITerrainGenerator                   Runs once at setup to generate the WorldData object.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**3. High-Level Architecture Diagram**

This diagram shows the main relationships

```
+--------------------------+
| Main Application Host    |
| (Program.cs, DI Setup)   |
+-----------+--------------+
             |
             v
+-----------+--------------+       +-------------------+
| IRenderer                |<----> | IDebugDrawService |
| (V1SilkNetRenderer)      |       +-------------------+
|                          |           ^
| *Owns the Main Loop*     |           | (Is used by...)
+-----------+--------------+           |
            |                          |
            | IFrameTickable           |
            | IRenderDataSource        |
+-----------+--------------------------+---------------------------+
               ^
               | (Orchestrator)
               v
+--------------------------------------------------------------------------+
| IPlayerInput | IFlightDynamics | IPhysicsService | IAutopilot | IAIDroneSpawner | ITerrainGenerator |
+--------------------------------------------------------------------------+
```
**4. Data Flow & Sequence Diagram (V2 Unified Loop)**

This sequence describes the final, advanced flow of control and data.

1.  **Main() -> Host:** The Program.cs builds and runs the .NET Generic Host.

2.  **Host -> IRenderer:** The Host resolves the IRenderer and calls its blocking Run() method.

3.  **Renderer Starts Loop:** The IRenderer.Run() method starts the Silk.NET window loop.

    - **On Load Event (Once):**

      - The Renderer calls _tickable.Setup() (the Orchestrator.Setup() method).

      - The Orchestrator initializes all other modules (TerrainGenerator, AISpawner, PhysicsService, etc.) to build the initial world state.

    - **On UpdateFrame Event (Every frame):**
  - The Renderer calls _tickable.UpdateFrame(deltaTime) (the Orchestrator.UpdateFrame() method).
    
  - Inside Orchestrator.UpdateFrame():
        a. Orchestrator calls _playerInput.Update() to poll the keyboard.
        b. Orchestrator processes inputs to update its internal state (camera, possession, debug toggle).
        c. Orchestrator loops through all DroneAgents:
        i. Gets ControlInputs (from player or AI).
        ii. Calls the drone's _flightModel.GenerateMoveIntent() to get its intention for the frame.
        iii. Submits the intent to the physics service: _physicsService.SubmitMoveIntent(...).
        d. Orchestrator calls _debugDrawService.Tick(deltaTime) to update timers on persistent shapes.
        e. Orchestrator makes a single call to _physicsService.Step(deltaTime). The physics engine calculates all movement and collisions for the entire world.
        f. Orchestrator loops through all DroneAgents again and synchronizes their state by querying the physics service: agent.State = _physicsService.GetState(...).
        g. Orchestrator checks AI agents for arrival and assigns new targets if needed.
      
- **On RenderFrame Event (Every frame):**
  
  - The Renderer calls its _dataSource dependency (the Orchestrator) to get the latest DroneState list, HUD info, etc.
    
  - The Renderer draws the main scene.
    
  - If debug drawing is enabled, it calls _debugDrawService.GetShapesToRender() and draws all debug geometry.
    
  - The Renderer draws the HUD and swaps the graphics buffers.

This cycle repeats until the window is closed.
