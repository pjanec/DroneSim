Document ID: SETUP-DRNSIM-V1.1

Date: June 10, 2025

Title: V1.1 Solution and Project Setup Specification

**1. Overview**

This document specifies the complete solution and project structure for the DroneSim application. Its purpose is to establish a clean, modular, and consistent foundation for all development work. Adhering to this structure is mandatory to ensure the architectural principles of separation of concerns and testability are maintained.

The structure separates the core contracts, individual module implementations, the main application, and all test projects into a logical folder hierarchy.

**2. Top-Level Folder Structure**

The solution will be organized within a root DroneSim folder as follows:

/DroneSim/\
\|\
\|\-- DroneSim.sln \# The Visual Studio Solution File\
\|\
\|\-- /src/ \# All source code for the application\
\| \|\
\| \|\-- Core/\
\| \| \|\-- DroneSim.Core.csproj \# Foundational interfaces and data models\
\| \|\
\| \|\-- Modules/ \# Folder for all independent feature modules\
\| \| \|\-- TerrainGenerator/\
\| \| \| \|\-- DroneSim.TerrainGenerator.csproj\
\| \| \|\-- PlayerInput/\
\| \| \| \|\-- DroneSim.PlayerInput.csproj\
\| \| \|\-- FlightDynamics/\
\| \| \| \|\-- DroneSim.FlightDynamics.csproj\
\| \| \|\-- PhysicsService/\
\| \| \| \|\-- DroneSim.PhysicsService.csproj \# Newly Added Module\
\| \| \|\-- Autopilot/\
\| \| \| \|\-- DroneSim.Autopilot.csproj\
\| \| \|\-- AIDroneSpawner/\
\| \| \| \|\-- DroneSim.AISpawner.csproj\
\| \| \|\-- Renderer/\
\| \| \| \|\-- DroneSim.Renderer.csproj\
\| \|\
\| \|\-- App/ \# The main executable application\
\| \|\-- DroneSim.App.csproj\
\|\
\|\-- /tests/ \# All test projects\
\|\
\|\-- UnitTests/\
\| \|\-- DroneSim.Core.Tests.csproj\
\| \|\-- DroneSim.TerrainGenerator.Tests.csproj\
\| \|\-- \# \... one test project per source project\
\|\
\|\-- IntegrationTests/\
\|\-- DroneSim.Integration.Tests.csproj

**3. Project Definitions**

  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
  **Project Name**                 **Type**                    **Key Contents / Purpose**                                                                     **Dependencies**
  -------------------------------- --------------------------- ---------------------------------------------------------------------------------------------- ------------------------------------------------
  **DroneSim.Core**                .NET 8 Class Library        Contains all public interfaces, shared data structures, and enums. The central \"contract\".   System.Numerics

  **DroneSim.TerrainGenerator**    .NET 8 Class Library        Contains the V1TerrainGenerator implementation.                                                DroneSim.Core

  **DroneSim.PlayerInput**         .NET 8 Class Library        Contains the V1KeyboardInput implementation.                                                   DroneSim.Core, Silk.NET.Input

  **DroneSim.FlightDynamics**      .NET 8 Class Library        Contains the V1KinematicFlightModel. **Does not handle collisions.**                           DroneSim.Core

  **DroneSim.PhysicsService**      .NET 8 Class Library        **New.** Contains V1BoundaryPhysicsService for collision/boundary checks.                      DroneSim.Core

  **DroneSim.Autopilot**           .NET 8 Class Library        Contains the V1StupidAutopilot and V1StupidAutopilotFactory.                                   DroneSim.Core

  **DroneSim.AISpawner**           .NET 8 Class Library        Contains the V1AIDroneSpawner implementation.                                                  DroneSim.Core

  **DroneSim.Renderer**            .NET 8 Class Library        Contains the V1SilkNetRenderer implementation.                                                 DroneSim.Core, Silk.NET.\*, ImageSharp.Drawing

  **DroneSim.App**                 .NET 8 Console App          Contains the Orchestrator, Program.cs, and DI setup.                                           All module projects.

  **DroneSim.\*.Tests**            .NET 8 xUnit Test Project   One project per corresponding source project for unit tests.                                   The project it tests, xunit, Moq

  **DroneSim.Integration.Tests**   .NET 8 xUnit Test Project   Contains tests verifying interactions between multiple modules.                                DroneSim.App, xunit, Moq
  ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

**4. Test Project Setup (XUnit)**

All test projects will use the xUnit framework.

- **Required NuGet Packages:**

  - xunit

  - xunit.runner.visualstudio

  - Microsoft.NET.Test.Sdk

  - Moq (for creating mock objects of dependencies)

**5. Initial Setup Steps (Using dotnet CLI)**

A developer can create this entire structure using the following commands from the root /DroneSim/ directory.

\# Create solution and directories\
dotnet new sln -n DroneSim\
mkdir src tests\
\
\# Create Core project\
dotnet new classlib -n DroneSim.Core -o src/Core\
dotnet sln add src/Core/DroneSim.Core.csproj\
\
\# Create Module projects (example for two, repeat for all)\
dotnet new classlib -n DroneSim.FlightDynamics -o src/Modules/FlightDynamics\
dotnet sln add src/Modules/FlightDynamics/DroneSim.FlightDynamics.csproj\
dotnet add src/Modules/FlightDynamics/DroneSim.FlightDynamics.csproj reference src/Core/DroneSim.Core.csproj\
\
dotnet new classlib -n DroneSim.PhysicsService -o src/Modules/PhysicsService\
dotnet sln add src/Modules/PhysicsService/DroneSim.PhysicsService.csproj\
dotnet add src/Modules/PhysicsService/DroneSim.PhysicsService.csproj reference src/Core/DroneSim.Core.csproj\
\
\# Create the App project\
dotnet new console -n DroneSim.App -o src/App\
dotnet sln add src/App/DroneSim.App.csproj\
\# (Add references to all module projects here)\
\
\# Create Test projects (example for one, repeat for all)\
mkdir tests/UnitTests\
dotnet new xunit -n DroneSim.FlightDynamics.Tests -o tests/UnitTests/\
dotnet sln add tests/UnitTests/DroneSim.FlightDynamics.Tests.csproj\
dotnet add tests/UnitTests/DroneSim.FlightDynamics.Tests.csproj reference src/Modules/FlightDynamics/DroneSim.FlightDynamics.csproj
