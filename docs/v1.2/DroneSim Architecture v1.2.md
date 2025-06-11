Document ID: ARCH-DRNSIM-V1.2

Date: June 10, 2025

Title: V1.2 Software Architecture Specification

**1. Core Principles**

- **Interface-Based Design:** All modules communicate through C# interfaces defined in the DroneSim.Core library, not concrete classes. This enforces separation of concerns and allows for interchangeable implementations.

- **Dependency Injection (DI):** The main application host is responsible for creating concrete instances of all modules and \"injecting\" their interfaces into the constructors of modules that depend on them. This decouples the modules and simplifies testing.

- **Renderer-Driven Loop:** The application\'s main loop is owned and driven by the Renderer module (specifically, the Silk.NET window loop). This is the standard pattern for real-time graphical applications.

- **Centralized Simulation Logic:** The Orchestrator class centralizes all simulation state and logic. It is \"ticked\" every frame by the Renderer to progress the simulation state, acting as the brain of the application.

**2. System Modules**

The system is composed of the following modules, each defined by an interface.

  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **Module**                  **Interface(s)**                    **Responsibilities**
  --------------------------- ----------------------------------- ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **Main Application Host**   *(Executable Host)*                 Program.cs. Configures and runs the .NET Generic Host, sets up Dependency Injection, and starts the Renderer.

  **Renderer**                IRenderer                           Creates and manages the application window and graphics context. **Drives the main loop.** Renders the 3D scene and HUD by querying the IRenderDataSource.

  **Orchestrator**            IFrameTickable, IRenderDataSource   Owns all simulation state. Implements the core logic loop (UpdateFrame). Manages all DroneAgents. Acts as the data source for the Renderer.

  **Player Input**            IPlayerInput                        Reads raw keyboard state and translates it into structured control commands and single-press actions.

  **Flight Dynamics**         IFlightDynamics                     **Purely kinematic.** Calculates a drone\'s desired, unobstructed displacement and orientation change based on control inputs. **Has no knowledge of collisions.**

  **Physics Service**         IPhysicsService                     **Collision resolver**. Resolves a desired displacement against the environment\'s static geometry (ground plane, world boundaries) to determine a final, valid position.

  **Autopilot**               IAutopilot                          Contains the logic for a single AI drone to **navigate**. Calculates ControlInputs to reach a target. Its creation is handled by an IAutopilotFactory.

  **AI Drone Spawner**        IAIDroneSpawner                     Runs once at setup to create all AI-controlled DroneAgent objects.

  **Terrain Generator**       ITerrainGenerator                   Runs once at setup to generate the **WorldData** object, including render mesh with procedural colors, physics colliders, and navigation data.
  -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**3. High-Level Architecture Diagram**

This diagram shows the main relationships. The Renderer drives the loop, which \"ticks\" the Orchestrator. The Orchestrator in turn coordinates all other simulation modules.

+\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\--+\
\| Main Application Host \|\
\| (Program.cs, DI Setup) \|\
+\-\-\-\-\-\-\-\-\-\--+\-\-\-\-\-\-\-\-\-\-\-\-\--+\
\|\
v\
+\-\-\-\-\-\-\-\-\-\--+\-\-\-\-\-\-\-\-\-\-\-\-\--+ +\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\--+\
\| IRenderer \|\-\-\-\--\>\| IFrameTickable \| (Implemented by Orchestrator)\
\| (V1SilkNetRenderer) \| \| IRenderDataSource \|\
\| \| +\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\--+\
\| \*Owns the Main Loop\* \|\
+\-\-\-\-\-\-\-\-\-\--+\-\-\-\-\-\-\-\-\-\-\-\-\--+\
\|\
\| (Orchestrator, via interfaces, uses\...)\
v\
+\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\--+\
\| IPlayerInput \| IFlightDynamics \| IPhysicsService \| IAutopilot \| IAIDroneSpawner \| ITerrainGenerator \|\
+\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\--+

**4. Data Flow & Sequence Diagram (Main Loop)**

This sequence describes the flow of control and data for the entire application, starting from the entry point.

1.  **Main() -\> Host:** The Program.cs Main() method builds and runs the .NET Generic Host.

2.  **Host -\> IRenderer:** The Host\'s IHostedService resolves the IRenderer instance from the DI container and calls its blocking Run() method.

3.  **Renderer Starts Loop:** The IRenderer.Run() method starts the Silk.NET window loop. The following events now drive the application:

    - **On Load Event (Once):**

      - The Renderer calls \_tickable.Setup() (which is the Orchestrator.Setup() method).

      - The Orchestrator calls ITerrainGenerator.Generate() and IAIDroneSpawner.CreateDrones() to initialize the world and all agents.

    - **On UpdateFrame Event (Every frame):**

      - The Renderer calls \_tickable.UpdateFrame(deltaTime) (which is the Orchestrator.UpdateFrame() method).

      - Inside Orchestrator.UpdateFrame():\
        a. Orchestrator calls \_playerInput.Update() to poll the keyboard.\
        b. Orchestrator processes inputs to update its internal state (camera, possession, etc.).\
        c. Orchestrator loops through all DroneAgents:\
        i. Gets ControlInputs from either \_playerInput or the agent\'s \_autopilot.\
        ii. Calls \_flightModel.CalculateKinematicUpdate() to get the desired displacement vector.\
        iii. Calls \_physicsService.ResolveEnvironmentCollisions() with the desired displacement to get the final, valid position.\
        iv. Updates the DroneAgent\'s DroneState with the final position and new orientation.\
        d. Orchestrator checks AI agents for arrival and assigns new targets if needed.

    - **On RenderFrame Event (Every frame):**

      - The Renderer calls the methods on its \_dataSource dependency (which is the Orchestrator).

      - It calls \_dataSource.GetAllDroneStates(), \_dataSource.GetHudInfo(), etc., to get all the data it needs to draw.

      - The Renderer performs all OpenGL draw calls for the terrain, drones, and HUD.

      - The Renderer swaps the graphics buffers.

This cycle repeats until the window is closed, which ends the Renderer.Run() method and allows the application to shut down gracefully.
