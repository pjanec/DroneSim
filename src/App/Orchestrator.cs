// In project: DroneSim.App
using DroneSim.Core;
using System.Collections.Generic;
using System.Linq;
using System;

namespace DroneSim.App;

// THIS IS A SKELETON IMPLEMENTATION BASED ON DOCUMENTATION
// It is NOT the full orchestrator logic.

public class Orchestrator : IFrameTickable, IRenderDataSource, IWorldDataSource
{
    private readonly IPlayerInput _playerInput;
    private readonly IFlightDynamics _flightModel;
    private readonly IPhysicsService _physicsService;
    private readonly IDebugDrawService _debugDraw;
    private readonly ITerrainGenerator _terrainGenerator;
    private readonly IAIDroneSpawner _aiSpawner;

    private List<DroneAgent> _allDrones = new();
    private WorldData? _worldData;
    private bool _isDebugDrawingEnabled = false;

    // --- State for IRenderDataSource ---
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
        IAIDroneSpawner aiSpawner)
    {
        _playerInput = playerInput;
        _flightModel = flightModel;
        _physicsService = physicsService;
        _debugDraw = debugDraw;
        _terrainGenerator = terrainGenerator;
        _aiSpawner = aiSpawner;

        // _physicsService.CollisionDetected += OnCollision;
    }

    public void Setup()
    {
        _worldData = _terrainGenerator.Generate();
        // _physicsService.AddStaticBody(_worldData.TerrainPhysicsBody);
        
        // Create player drone
        var playerState = new DroneState { Id = 0, Position = new System.Numerics.Vector3(0, 5, 0), Status = DroneStatus.Active };
        var playerAgent = new DroneAgent(0, playerState, null!); // Null autopilot for player
        _allDrones.Add(playerAgent);

        // Create AI drones
        // var aiAgents = _aiSpawner.CreateDrones(9, _worldData);
        // _allDrones.AddRange(aiAgents);
    }

    public void UpdateFrame(float deltaTime)
    {
        // This is a minimal implementation to allow the program to run.
        // A full implementation would poll input, update AI, step physics, etc.
        _debugDraw.Tick(deltaTime);
    }

    private void OnCollision(CollisionEventData eventData)
    {
        // Find agents, set status to Crashed, draw debug sphere
    }

    // --- IRenderDataSource & IWorldDataSource Implementation ---
    public IReadOnlyList<DroneState> GetAllDroneStates() => _allDrones.Select(a => a.State).ToList();
    public int GetPlayerControlledDroneId() => _playerControlledDroneId;
    public int GetCameraAttachedToDroneId() => _cameraAttachedToDroneId;
    public CameraViewMode GetCameraViewMode() => _cameraViewMode;
    public float GetCameraTilt() => _cameraTilt;
    public string GetHudInfo() => "HUD INFO PLACEHOLDER";
    public bool IsDebugDrawingEnabled() => _isDebugDrawingEnabled;
    public WorldData GetWorldData() => _worldData!;
} 