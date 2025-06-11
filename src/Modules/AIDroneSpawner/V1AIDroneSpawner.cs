// In project: DroneSim.AISpawner
using DroneSim.Core;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DroneSim.AISpawner;

/// <summary>
/// V1 implementation of the AI drone spawner.
/// Creates DroneAgents and registers them with the physics service.
/// </summary>
public class V1AIDroneSpawner : IAIDroneSpawner
{
    private readonly IAutopilotFactory _autopilotFactory;
    private readonly IPhysicsService _physicsService;
    private readonly SpawnerOptions _options;
    private readonly Random _random = new();

    public V1AIDroneSpawner(
        IAutopilotFactory autopilotFactory,
        IPhysicsService physicsService,
        IOptions<SpawnerOptions> options)
    {
        _autopilotFactory = autopilotFactory ?? throw new ArgumentNullException(nameof(autopilotFactory));
        _physicsService = physicsService ?? throw new ArgumentNullException(nameof(physicsService));
        _options = options.Value;
    }

    /// <summary>
    /// Creates a specified number of AI-controlled DroneAgents.
    /// </summary>
    /// <param name="count">The number of drones to create.</param>
    /// <param name="worldData">The world data (not used in V1 spawner).</param>
    /// <returns>A list of the created DroneAgents.</returns>
    public List<DroneAgent> CreateDrones(int count, WorldData worldData)
    {
        var agents = new List<DroneAgent>();
        if (count <= 0) return agents;

        for (int i = 0; i < count; i++)
        {
            var autopilot = _autopilotFactory.Create();

            var startPosition = GetRandomPositionOnPlane();
            var targetPosition = GetRandomPositionOnPlane();
            autopilot.SetTarget(targetPosition);

            var initialState = new DroneState
            {
                Id = i + 1, // ID 0 is reserved for the player
                Position = startPosition,
                Orientation = Quaternion.Identity,
                Status = DroneStatus.Active
            };

            // NEW: Add the drone to the physics world to get its handle
            // The 'description' would be a simple object defining the drone's collision shape.
            // For V1, we assume kinematic bodies.
            var physicsBodyHandle = _physicsService.AddKinematicBody(new { Shape = "Drone" });

            // NEW: Create the agent with the physics handle
            agents.Add(new DroneAgent(physicsBodyHandle, initialState, autopilot));
        }

        return agents;
    }

    private Vector3 GetRandomPositionOnPlane()
    {
        float x = (float)(_random.NextDouble() * 2 - 1) * _options.WorldBoundary;
        float z = (float)(_random.NextDouble() * 2 - 1) * _options.WorldBoundary;
        return new Vector3(x, _options.InitialFlightAltitude, z);
    }
} 