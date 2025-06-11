Document ID: SETUP-MAINAPP-V1.2

Date: June 10, 2025

Title: V1.2 Detailed Main Application & Setup Specification (DroneSim.App)

**1. Overview**

This document provides the detailed implementation specification for the DroneSim.App project, the composition root for the entire application. It uses modern .NET practices for configuration, dependency injection, and application lifecycle management.

Its sole responsibility is to configure and connect all the application\'s modules and start the simulation by running the Renderer\'s main loop.

**2. Dependencies**

- **All DroneSim.\* module projects:** It requires references to every module to instantiate them.

- **Microsoft.Extensions.Hosting:** The primary NuGet package for building the application host and managing dependency injection.

**3. V1 Functional Specification**

- **Configuration:**

  - An appsettings.json file will provide all configurable parameters.

  - The application will bind the sections of this JSON file to strongly-typed options classes (e.g., FlightModelOptions, AIBehaviorOptions). These options classes are registered with the DI container and can be injected into any module that needs them.

- **Dependency Injection (Program.cs):**

  - The main entry point will configure a ServiceCollection.

  - All module interfaces (IPlayerInput, IFlightDynamics, etc.) will be registered with their concrete V1 implementations (V1KeyboardInput, V1KinematicFlightModel, etc.) as Singleton services. This ensures that only one instance of each module exists for the application\'s lifetime.

  - The Orchestrator class is registered as a singleton. Then, the IFrameTickable and IRenderDataSource interfaces are mapped to that same singleton instance, so when the Renderer asks for those interfaces, it receives the Orchestrator.

- **Application Host:**

  - The application uses the .NET Generic Host (IHost) to manage its lifecycle.

  - The Host is configured to run a single IHostedService (SimulationHostedService).

  - The SimulationHostedService\'s job is to resolve the IRenderer from the DI container and call its blocking Run() method. This starts the Silk.NET window loop, which in turn drives the entire simulation by calling the Orchestrator\'s UpdateFrame method.

**4. Configuration File (appsettings.json)**

This file must be present in the output directory of the DroneSim.App project.

{\
\"OrchestratorOptions\": {\
\"AIDroneCount\": 9,\
\"CameraTiltSpeed\": 1.5708,\
\"MinCameraTilt\": -0.7854,\
\"MaxCameraTilt\": 0.3490\
},\
\"SpawnerOptions\": {\
\"InitialFlightAltitude\": 20.0,\
\"WorldBoundary\": 128.0\
},\
\"PhysicsOptions\": {\
\"WorldBoundary\": 128.0\
},\
\"FlightModelOptions\": {\
\"MaxForwardSpeed\": 20.0,\
\"MaxStrafeSpeed\": 10.0,\
\"MaxVerticalSpeed\": 5.0,\
\"YawSpeed\": 1.5708,\
\"AccelerationFactor\": 5.0\
},\
\"AIBehaviorOptions\": {\
\"FlightAltitude\": 20.0,\
\"ConstantThrottleStep\": 4,\
\"ArrivalRadius\": 5.0,\
\"YawTolerance\": 0.1\
}\
}

**5. Code Skeleton (Program.cs)**

// In project: DroneSim.App\
using DroneSim.Core;\
using DroneSim.App.Configuration; // Assuming options classes are in this namespace\
using DroneSim.TerrainGenerator;\
using DroneSim.PlayerInput;\
using DroneSim.FlightDynamics;\
using DroneSim.PhysicsService;\
using DroneSim.Autopilot;\
using DroneSim.AISpawner;\
using DroneSim.Renderer;\
using Microsoft.Extensions.Hosting;\
using Microsoft.Extensions.DependencyInjection;\
\
public static class Program\
{\
public static async Task Main(string\[\] args)\
{\
var host = Host.CreateDefaultBuilder(args)\
.ConfigureServices((context, services) =\>\
{\
// Bind configuration sections from appsettings.json to options classes\
services.Configure\<OrchestratorOptions\>(context.Configuration.GetSection(\"OrchestratorOptions\"));\
services.Configure\<SpawnerOptions\>(context.Configuration.GetSection(\"SpawnerOptions\"));\
services.Configure\<PhysicsOptions\>(context.Configuration.GetSection(\"PhysicsOptions\"));\
services.Configure\<FlightModelOptions\>(context.Configuration.GetSection(\"FlightModelOptions\"));\
services.Configure\<AIBehaviorOptions\>(context.Configuration.GetSection(\"AIBehaviorOptions\"));\
\
// Register all module implementations as singletons\
services.AddSingleton\<ITerrainGenerator, V1TerrainGenerator\>();\
services.AddSingleton\<IPlayerInput, V1KeyboardInput\>();\
services.AddSingleton\<IFlightDynamics, V1KinematicFlightModel\>();\
services.AddSingleton\<IPhysicsService, V1BoundaryPhysicsService\>();\
services.AddSingleton\<IAutopilotFactory, V1StupidAutopilotFactory\>();\
services.AddSingleton\<IAIDroneSpawner, V1AIDroneSpawner\>();\
services.AddSingleton\<IRenderer, V1SilkNetRenderer\>();\
\
// Register the Orchestrator and map its tick/data source interfaces\
services.AddSingleton\<Orchestrator\>();\
services.AddSingleton\<IFrameTickable\>(sp =\> sp.GetRequiredService\<Orchestrator\>());\
services.AddSingleton\<IRenderDataSource\>(sp =\> sp.GetRequiredService\<Orchestrator\>());\
\
// Register the main application service that starts the simulation\
services.AddHostedService\<SimulationHostedService\>();\
})\
.Build();\
\
await host.RunAsync();\
}\
}\
\
/// \<summary\>\
/// A simple hosted service responsible for starting the renderer\'s main loop.\
/// \</summary\>\
public class SimulationHostedService : IHostedService\
{\
private readonly IRenderer \_renderer;\
public SimulationHostedService(IRenderer renderer) { \_renderer = renderer; }\
\
public Task StartAsync(CancellationToken cancellationToken)\
{\
// This call is blocking and will run until the window is closed.\
\_renderer.Run();\
return Task.CompletedTask;\
}\
\
public Task StopAsync(CancellationToken cancellationToken) =\> Task.CompletedTask;\
}
