// In project: DroneSim.App
using DroneSim.Core;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Numerics;
using Silk.NET.Input;

namespace DroneSim.App;

/// <summary>
/// Configuration values for the <see cref="Orchestrator"/>. These
/// options are populated from <c>appsettings.json</c> and control
/// how many AI drones are spawned as well as camera behaviour.
/// </summary>
public class OrchestratorOptions
{
    public int AIDroneCount { get; set; } = 9;
    public float CameraTiltSpeed { get; set; } = 1.5708f; // rad/s
    public float MinCameraTilt { get; set; } = -0.7854f; // -45 deg
    public float MaxCameraTilt { get; set; } = 0.3490f;  // +20 deg
}

/// <summary>
/// Central coordination class that drives the simulation loop.
/// Implements <see cref="IFrameTickable"/> so the renderer can
/// invoke <c>Setup</c> and <c>UpdateFrame</c>. It also exposes
/// simulation state to the renderer through <see cref="IRenderDataSource"/>
/// and provides access to generated world data via
/// <see cref="IWorldDataSource"/>. The orchestrator wires together player
/// input, AI logic, physics and terrain generation.
/// </summary>
public class Orchestrator : IFrameTickable, IRenderDataSource, IWorldDataSource
{
    private readonly IPlayerInput _playerInput;
    private readonly IFlightDynamics _flightModel;
    private readonly IPhysicsService _physicsService;
    private readonly IDebugDrawService _debugDraw;
    private readonly ITerrainGenerator _terrainGenerator;
    private readonly IAIDroneSpawner _aiSpawner;
    private readonly OrchestratorOptions _options;
    private readonly Random _random = new();
    private float _aiWorldBoundary = 128.0f;
    private float _aiFlightAltitude = 20.0f;
    private float _aiArrivalRadius = 5.0f;

    private List<DroneAgent> _allDrones = new();
    private WorldData? _worldData;
    private bool _isDebugDrawingEnabled = false;

    // State for IRenderDataSource
    private int _playerControlledDroneId = 0;
    private int _cameraAttachedToDroneId = 0;
    private CameraViewMode _cameraViewMode = CameraViewMode.OverTheShoulder;
    private float _cameraTilt = 0.0f;

    public Orchestrator(
        IPlayerInput playerInput,
        IFlightDynamics flightModel,
        IPhysicsService physicsService,
        IDebugDrawService debugDraw,
        ITerrainGenerator terrainGenerator,
        IAIDroneSpawner aiSpawner,
        IOptions<OrchestratorOptions> options)
    {
        _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
        _flightModel = flightModel ?? throw new ArgumentNullException(nameof(flightModel));
        _physicsService = physicsService ?? throw new ArgumentNullException(nameof(physicsService));
        _debugDraw = debugDraw ?? throw new ArgumentNullException(nameof(debugDraw));
        _terrainGenerator = terrainGenerator ?? throw new ArgumentNullException(nameof(terrainGenerator));
        _aiSpawner = aiSpawner ?? throw new ArgumentNullException(nameof(aiSpawner));
        _options = options.Value;

        _physicsService.CollisionDetected += OnCollision;
    }

    /// <summary>
    /// Initializes the world by generating terrain, registering the terrain
    /// collider with the physics service and spawning both the player and AI
    /// drone agents. This method is invoked once by the renderer at startup.
    /// </summary>
    public void Setup()
    {
        _worldData = _terrainGenerator.Generate();
        _physicsService.AddStaticBody(_worldData.TerrainPhysicsBody);

        // Create player drone
        var playerState = new DroneState { Id = 0, Position = new Vector3(0, 5, 0), Orientation=Quaternion.Identity, Status = DroneStatus.Active };
        int playerBodyHandle = _physicsService.AddKinematicBody(new { Shape = "Drone" });
        var playerAgent = new DroneAgent(playerBodyHandle, playerState, null!); // No autopilot for player
        _allDrones.Add(playerAgent);

        // Create AI drones
        var aiAgents = _aiSpawner.CreateDrones(_options.AIDroneCount, _worldData);
        _allDrones.AddRange(aiAgents);

        // Get AI config for randomization
        if (_aiSpawner is DroneSim.AISpawner.V1AIDroneSpawner spawner)
        {
            var optsObj = spawner.GetType().GetProperty("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(spawner);
            if (optsObj is DroneSim.AISpawner.SpawnerOptions opts)
            {
                _aiWorldBoundary = opts.WorldBoundary;
                _aiFlightAltitude = opts.InitialFlightAltitude;
            }
            else
            {
                _aiWorldBoundary = 128.0f;
                _aiFlightAltitude = 20.0f;
            }
        }
        // Try to get arrival radius from any AI agent's autopilot
        var aiAutopilot = aiAgents.FirstOrDefault()?.AutopilotController;
        if (aiAutopilot != null)
        {
            var autopilotType = aiAutopilot.GetType();
            var optionsField = autopilotType.GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (optionsField?.GetValue(aiAutopilot) is DroneSim.Autopilot.AIBehaviorOptions aiOpts)
                _aiArrivalRadius = aiOpts.ArrivalRadius;
        }
    }

    /// <summary>
    /// Executes one simulation tick. Handles user input, updates drone
    /// control for both player and AI agents, steps the physics world and
    /// refreshes debug drawing state. Called every frame by the renderer.
    /// </summary>
    public void UpdateFrame(float deltaTime, IDroneSimInput? input)
    {
        _playerInput.Update(input);
        HandleInput(deltaTime);

        // 2. Update AI and Player Drone Controls
        foreach (var agent in _allDrones)
        {
            if (agent.State.Status == DroneStatus.Crashed) continue;

            if (agent.State.Id != _playerControlledDroneId && agent.AutopilotController != null)
            {
                // Check if AI drone has reached its target
                var autopilotType = agent.AutopilotController.GetType();
                var targetField = autopilotType.GetField("_targetPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (targetField != null)
                {
                    var target = (Vector3)targetField.GetValue(agent.AutopilotController)!;
                    var pos = agent.State.Position;
                    var horizontalDistSq = (target.X - pos.X) * (target.X - pos.X) + (target.Z - pos.Z) * (target.Z - pos.Z);
                    if (horizontalDistSq < _aiArrivalRadius * _aiArrivalRadius)
                    {
                        var newTarget = GetRandomPositionOnPlane();
                        agent.AutopilotController.SetTarget(newTarget);
                    }
                }
            }

            ControlInputs inputs;
            if (agent.State.Id == _playerControlledDroneId)
            {
                inputs = _playerInput.GetFlightControls();
            }
            else
            {
                inputs = agent.AutopilotController.GetControlUpdate(agent.State);
            }

            var moveIntent = _flightModel.GenerateMoveIntent(agent.State, inputs, deltaTime);
            _physicsService.SubmitMoveIntent(agent.PhysicsBodyHandle, moveIntent);
        }

        // 3. Step Physics
        _physicsService.Step(deltaTime);

        // 4. Update Drone States from Physics
        foreach (var agent in _allDrones)
        {
            if (agent.State.Status == DroneStatus.Crashed) continue;
            var newState = _physicsService.GetState(agent.PhysicsBodyHandle);
            // Preserve Id and Status from the orchestrator's agent object
            newState.Id = agent.State.Id;
            newState.Status = agent.State.Status;
            agent.State = newState;
        }

        // 5. Update Debug Drawing
        _debugDraw.Tick(deltaTime);
    }

    /// <summary>
    /// Processes player keyboard input and updates camera control state.
    /// Called from <see cref="UpdateFrame"/> once per frame.
    /// </summary>
    private void HandleInput(float deltaTime)
    {
        if (_playerInput.IsDebugTogglePressed()) _isDebugDrawingEnabled = !_isDebugDrawingEnabled;
        if (_playerInput.IsSwitchCameraPressed())
        {
            _cameraViewMode = _cameraViewMode == CameraViewMode.OverTheShoulder ?
                CameraViewMode.FirstPerson : CameraViewMode.OverTheShoulder;
        }

        _cameraTilt += _playerInput.GetCameraTiltInput() * _options.CameraTiltSpeed * deltaTime;
        _cameraTilt = Math.Clamp(_cameraTilt, _options.MinCameraTilt, _options.MaxCameraTilt);

        if (_playerInput.IsSwitchDronePressed())
        {
            // This logic allows switching camera view to any active drone, including the player's
            var activeDrones = _allDrones.Where(d => d.State.Status == DroneStatus.Active).ToList();
            var currentIndex = activeDrones.FindIndex(d => d.State.Id == _cameraAttachedToDroneId);
            var nextIndex = (currentIndex + 1) % activeDrones.Count;
            _cameraAttachedToDroneId = activeDrones[nextIndex].State.Id;
        }

        // The "Possess" key is not implemented in V1
    }

    /// <summary>
    /// Responds to collision events reported by the physics service.
    /// In the V1 implementation any collision marks the involved drone
    /// as crashed and triggers a debug draw indicator.
    /// </summary>
    private void OnCollision(CollisionEventData eventData)
    {
        // V1: Any collision is fatal. Find the agent and update its status.
        var agent = _allDrones.FirstOrDefault(a => a.PhysicsBodyHandle == eventData.BodyAHandle || a.PhysicsBodyHandle == eventData.BodyBHandle);
        if (agent != null)
        {
            agent.State = agent.State with { Status = DroneStatus.Crashed };
            _debugDraw.DrawCollisionPoint(eventData.Position, 1f, System.Drawing.Color.Red, 5f);
        }
    }

    private Vector3 GetRandomPositionOnPlane()
    {
        float x = (float)(_random.NextDouble() * 2 - 1) * _aiWorldBoundary;
        float z = (float)(_random.NextDouble() * 2 - 1) * _aiWorldBoundary;
        return new Vector3(x, _aiFlightAltitude, z);
    }

    // --- IRenderDataSource & IWorldDataSource Implementation ---
    public IReadOnlyList<DroneState> GetAllDroneStates() => _allDrones.Select(a => a.State).ToList();
    public int GetPlayerControlledDroneId() => _playerControlledDroneId;
    public int GetCameraAttachedToDroneId() => _cameraAttachedToDroneId;
    public CameraViewMode GetCameraViewMode() => _cameraViewMode;
    public float GetCameraTilt() => _cameraTilt;
    public string GetHudInfo()
    {
        var playerDrone = _allDrones.FirstOrDefault(d => d.State.Id == _playerControlledDroneId);
        if (playerDrone == null) return "No Player Drone";

        return $"Player Drone | Pos: {playerDrone.State.Position:F2} | Speed: 0.0 m/s"; // Speed not tracked in V1
    }
    public bool IsDebugDrawingEnabled() => _isDebugDrawingEnabled;
    public WorldData GetWorldData() => _worldData!;
} 