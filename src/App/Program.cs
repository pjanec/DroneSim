// In project: DroneSim.App
using DroneSim.AISpawner;
using DroneSim.Autopilot;
using DroneSim.Core;
using DroneSim.DebugDraw;
using DroneSim.FlightDynamics;
using DroneSim.Physics;
using DroneSim.PlayerInput;
using DroneSim.TerrainGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace DroneSim.App;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("DroneSim Application Starting...");

        var host = CreateHostBuilder(args).Build();
        var renderer = host.Services.GetRequiredService<IRenderer>();
        renderer.Run();

        Console.WriteLine("DroneSim Application Exiting.");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false);
            })
            .ConfigureServices((hostContext, services) =>
            {
                // Register options
                services.Configure<OrchestratorOptions>(hostContext.Configuration.GetSection("OrchestratorOptions"));
                services.Configure<SpawnerOptions>(hostContext.Configuration.GetSection("SpawnerOptions"));
                services.Configure<AIBehaviorOptions>(hostContext.Configuration.GetSection("AIBehaviorOptions"));
                
                // V1 Implementations:
                services.Configure<FlightModelOptions>(hostContext.Configuration.GetSection("FlightModelOptions"));
                services.AddSingleton<IFlightDynamics, V1KinematicFlightModel>();
                
                // V2 Physics (as per spec, but using the correct name)
                services.Configure<PhysicsOptions>(hostContext.Configuration.GetSection("PhysicsOptionsV2"));
                services.AddSingleton<IPhysicsService, V2SimplePhysicsService>();


                // Register core services & factories
                services.AddSingleton<IPlayerInput, V1KeyboardInput>();
                services.AddSingleton<ITerrainGenerator, V1TerrainGenerator>();
                services.AddSingleton<IDebugDrawService, DebugDrawService>();
                services.AddSingleton<IAutopilotFactory, V1StupidAutopilotFactory>();
                services.AddSingleton<IAIDroneSpawner, V1AIDroneSpawner>();

                // The Orchestrator is the heart of the simulation logic
                services.AddSingleton<Orchestrator>();
                // The Renderer needs access to the Orchestrator for data
                // We resolve it once and pass it to the renderer constructor
                services.AddSingleton<IRenderer>(sp =>
                {
                    var orchestrator = sp.GetRequiredService<Orchestrator>();
                    // This is a placeholder for a real renderer
                    return new ConsoleRenderer(orchestrator, orchestrator, orchestrator);
                });
            });
}

// A minimal renderer to allow the application to run without a graphics engine.
public class ConsoleRenderer : IRenderer
{
    private readonly IFrameTickable _tickable;
    private readonly IRenderDataSource _dataSource;
    private readonly IWorldDataSource _worldDataSource;

    public ConsoleRenderer(IFrameTickable tickable, IRenderDataSource dataSource, IWorldDataSource worldDataSource)
    {
        _tickable = tickable;
        _dataSource = dataSource;
        _worldDataSource = worldDataSource;
    }

    public void Run()
    {
        _tickable.Setup();

        // Placeholder for a real game loop
        for (int i = 0; i < 100; i++)
        {
            _tickable.UpdateFrame(0.016f); // Simulate a 60 FPS frame rate
            Console.WriteLine($"Frame {i}: HUD: {_dataSource.GetHudInfo()}");
        }
    }
}
