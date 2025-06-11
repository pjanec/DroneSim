Document ID: SETUP-DRNSIM-V1.3  
Date: June 11, 2025  
Title: V1.3 Solution and Project Setup Specification  
**1\. Overview**

This document specifies the complete solution and project structure for the DroneSim application. It establishes a modular foundation and is updated to include all V2 architectural components, including the unified physics service and the new debug drawing service.

**2\. Top-Level Folder Structure**

The structure is updated to include the new DebugDrawService module.

/DroneSim/  
|  
|-- DroneSim.sln                  \# The Visual Studio Solution File  
|  
|-- /src/                         \# All source code for the application  
|   |  
|   |-- Core/  
|   |   |-- DroneSim.Core.csproj  
|   |  
|   |-- Modules/                  \# Folder for all independent feature modules  
|   |   |-- TerrainGenerator/  
|   |   |   |-- DroneSim.TerrainGenerator.csproj  
|   |   |-- PlayerInput/  
|   |   |   |-- DroneSim.PlayerInput.csproj  
|   |   |-- FlightDynamics/  
|   |   |   |-- DroneSim.FlightDynamics.csproj  
|   |   |-- PhysicsService/  
|   |   |   |-- DroneSim.PhysicsService.csproj  
|   |   |-- Autopilot/  
|   |   |   |-- DroneSim.Autopilot.csproj  
|   |   |-- AIDroneSpawner/  
|   |   |   |-- DroneSim.AISpawner.csproj  
|   |   |-- Renderer/  
|   |   |   |-- DroneSim.Renderer.csproj  
|   |   |-- DebugDrawService/  
|   |   |   |-- DroneSim.DebugDraw.csproj  \# New Module  
|   |  
|   |-- App/                      \# The main executable application  
|       |-- DroneSim.App.csproj  
|  
|-- /tests/                       \# All test projects  
    |  
    |-- UnitTests/  
    |   |-- \# ... one test project per source project  
    |  
    |-- IntegrationTests/  
        |-- DroneSim.Integration.Tests.csproj

**3\. Project Definitions**

| Project Name | Type | Key Contents / Purpose | Dependencies |
| :---- | :---- | :---- | :---- |
| **DroneSim.Core** | .NET 8 Class Library | The central "contract." Contains all public interfaces and shared data models. | System.Numerics |
| **DroneSim.TerrainGenerator** | .NET 8 Class Library | Contains V1TerrainGenerator. | DroneSim.Core |
| **DroneSim.PlayerInput** | .NET 8 Class Library | Contains V1KeyboardInput. | DroneSim.Core, Silk.NET.Input |
| **DroneSim.FlightDynamics** | .NET 8 Class Library | Contains V1KinematicFlightModel and V2PhysicsFlightModel. | DroneSim.Core |
| **DroneSim.PhysicsService** | .NET 8 Class Library | Contains V2BepuPhysicsService, which handles both kinematic and dynamic bodies. | DroneSim.Core, BepuPhysics |
| **DroneSim.Autopilot** | .NET 8 Class Library | Contains V1StupidAutopilot and its factory. | DroneSim.Core, IDebugDrawService |
| **DroneSim.AISpawner** | .NET 8 Class Library | Contains V1AIDroneSpawner. | DroneSim.Core |
| **DroneSim.Renderer** | .NET 8 Class Library | Contains V1SilkNetRenderer. | DroneSim.Core, IDebugDrawService |
| **DroneSim.DebugDraw** | .NET 8 Class Library | **New.** Contains DebugDrawService. | DroneSim.Core |
| **DroneSim.App** | .NET 8 Console App | Contains the Orchestrator, Program.cs, and DI setup. | All module projects. |
| **DroneSim.\*.Tests** | .NET 8 xUnit Test Project | Unit tests for each source project. | The project it tests, xunit, Moq |
