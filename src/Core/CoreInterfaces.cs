// Doc: MODSPEC-CORE-V1.3
// This file contains all the public "contracts" (interfaces) and shared data
// models (structs, records, enums) for the DroneSim solution. It is the
// foundational library that all other modules depend on.

using System.Numerics;
using System.Drawing;
using System.Collections.Generic;
using System;

namespace DroneSim.Core
{
    // --- Enums ---
    public enum DroneStatus { Active, Crashed }
    public enum CameraViewMode { FirstPerson, OverTheShoulder }

    // --- Physics & World Data ---

    /// <summary>
    /// Placeholder for mesh data (vertices, indices, colors) used for rendering.
    /// </summary>
    public class RenderMesh { /* Placeholder for vertex/index/color data */ }

    /// <summary>
    /// Placeholder for the data required to construct a physics body in the physics engine.
    /// </summary>
    public class PhysicsBody { /* Placeholder for physics body data */ }

    /// <summary>
    /// Represents the static game world data.
    /// </summary>
    /// <param name="TerrainRenderMesh">The mesh for rendering the terrain.</param>
    /// <param name="TerrainPhysicsBody">The physics body for terrain collision.</param>
    /// <param name="ObstaclePhysicsBodies">A list of physics bodies for static obstacles.</param>
    /// <param name="NavigationGrid">A grid for AI pathfinding.</param>
    public record WorldData(
        RenderMesh TerrainRenderMesh,
        PhysicsBody TerrainPhysicsBody,
        IReadOnlyList<PhysicsBody> ObstaclePhysicsBodies,
        bool[,] NavigationGrid
    );

    /// <summary>
    /// Represents the physical state of a drone at a point in time.
    /// </summary>
    public struct DroneState
    {
        public int Id;
        public Vector3 Position;
        public Quaternion Orientation;
        public DroneStatus Status;
    }

    // --- Movement Intent Abstraction ---

    /// <summary>
    /// A marker interface for movement intent records.
    /// Represents a drone's desired movement for the current frame.
    /// </summary>
    public interface IMoveIntent { }

    /// <summary>
    /// An intent to move with a specific velocity and orientation.
    /// Used for kinematic bodies.
    /// </summary>
    public record KinematicIntent(Vector3 TargetVelocity, Quaternion TargetOrientation) : IMoveIntent;

    /// <summary>
    /// An intent to move by applying physical forces and torques.
    /// Used for dynamic bodies.
    /// </summary>
    public record DynamicIntent(Vector3 Force, Vector3 Torque) : IMoveIntent;

    // --- Event & Control Data ---

    /// <summary>
    /// Data for a collision event detected by the physics service.
    /// </summary>
    public struct CollisionEventData
    {
        public int BodyAHandle;
        public int BodyBHandle;
        public Vector3 Position;
    }

    /// <summary>
    /// Structured data representing player/AI control inputs for a single frame.
    /// </summary>
    public struct ControlInputs
    {
        /// <summary>
        /// The desired throttle level, typically from 0 to 10.
        /// </summary>
        public int ThrottleStep;
        /// <summary>
        /// Desired sideways movement (-1.0 for left, 1.0 for right).
        /// </summary>
        public float StrafeInput;
        /// <summary>
        /// Desired vertical movement (-1.0 for down, 1.0 for up).
        /// </summary>
        public float VerticalInput;
        /// <summary>
        /// Desired turning speed (-1.0 for left, 1.0 for right).
        /// </summary>
        public float YawInput;
    }

    // --- Agent Class ---

    /// <summary>
    /// Represents a single drone entity in the simulation.
    /// It holds the drone's state and a reference to its AI controller.
    /// </summary>
    public class DroneAgent
    {
        public int PhysicsBodyHandle { get; init; }
        public DroneState State { get; set; }
        public IAutopilot AutopilotController { get; }

        public DroneAgent(int handle, DroneState initialState, IAutopilot autopilot)
        {
            PhysicsBodyHandle = handle;
            State = initialState;
            AutopilotController = autopilot;
        }
    }

    // --- Primary Module Interfaces ---

    public interface ITerrainGenerator { WorldData Generate(); }
    public interface IAutopilotFactory { IAutopilot Create(); }
    public interface IAIDroneSpawner { List<DroneAgent> CreateDrones(int count, WorldData worldData); }
    public interface IRenderer {
        void Run();
    }

    public interface IPlayerInput
    {
        void Update(IDroneSimInput input);
        ControlInputs GetFlightControls();
        float GetCameraTiltInput();
        bool IsSwitchCameraPressed();
        bool IsSwitchDronePressed();
        bool IsPossessKeyPressed();
        bool IsDebugTogglePressed();
    }

    public interface IFlightDynamics
    {
        IMoveIntent GenerateMoveIntent(DroneState currentState, ControlInputs inputs, float deltaTime);
    }

    public interface IPhysicsService
    {
        // Setup
        void AddStaticBody(PhysicsBody bodyData);
        int AddDynamicBody(object description);
        int AddKinematicBody(object description);
        // Simulation
        void SubmitMoveIntent(int bodyHandle, IMoveIntent intent);
        void Step(float deltaTime);
        // State & Events
        DroneState GetState(int bodyHandle);
        event Action<CollisionEventData> CollisionDetected;
    }

    public interface IAutopilot
    {
        void SetTarget(Vector3 targetPosition);
        ControlInputs GetControlUpdate(DroneState currentDroneState);
    }

    public interface IDebugDrawService
    {
        // Consumer methods
        void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f);
        void DrawVector(Vector3 start, Vector3 vector, Color color, float duration = 0f);
        void DrawPath(IReadOnlyList<Vector3> pathPoints, Color color, float duration = 0f);
        void DrawCollisionPoint(Vector3 point, float radius, Color color, float duration = 0f);

        // Methods for the simulation loop
        void Tick(float deltaTime);
        IReadOnlyList<object> GetShapesToRender();
        void Clear();
    }

    // --- Orchestrator / Renderer Communication Interfaces ---

    public interface IFrameTickable
    {
        void Setup();
        void UpdateFrame(float deltaTime, IDroneSimInput? input);
    }

    public interface IRenderDataSource
    {
        IReadOnlyList<DroneState> GetAllDroneStates();
        int GetPlayerControlledDroneId();
        int GetCameraAttachedToDroneId();
        CameraViewMode GetCameraViewMode();
        float GetCameraTilt();
        string GetHudInfo();
        bool IsDebugDrawingEnabled();
    }

    /// <summary>
    /// An extended data source interface for components that need access
    /// to the static world data after it has been generated.
    /// Provides read-only access to the <see cref="WorldData"/> that is
    /// created during <see cref="IFrameTickable.Setup"/>. The orchestrator
    /// implements this interface so that modules like the renderer can
    /// obtain terrain and navigation information once initialization is
    /// complete.
    /// </summary>
    public interface IWorldDataSource
    {
        /// <summary>
        /// Returns the immutable world data generated at startup.
        /// </summary>
        WorldData GetWorldData();
    }

    public interface IDroneSimInput
    {
        bool Forward { get; }
        bool Backward { get; }
        bool Left { get; }
        bool Right { get; }
        bool Up { get; }
        bool Down { get; }
        bool YawLeft { get; }
        bool YawRight { get; }
        // Add more as needed
        bool StrafeLeft { get; }
        bool StrafeRight { get; }
    }
} 