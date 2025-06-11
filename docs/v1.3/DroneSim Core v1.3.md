Document ID: MODSPEC-CORE-V1.3

Date: June 11, 2025

Title: V1.3 DroneSim.Core Library Specification

**1. Overview**

This document specifies the contents of the DroneSim.Core C# project. This project is the foundational library for the entire solution. It contains **no implementation logic**. Its sole purpose is to define the public \"contracts\" (interfaces) and shared data models (structs, records, enums) that all other modules will use to communicate. This version reflects the advanced V2 architecture, including a unified physics service and a debug drawing service.

**2. Dependencies**

- **.NET 8**

- **System.Numerics**

- **System.Drawing.Common** (for the Color struct, or a custom one can be used)

**3. Data Structures & Enums**

namespace DroneSim.Core;\
\
using System.Numerics;\
using System.Drawing;\
using System.Collections.Generic;\
\
// \-\-- Enums \-\--\
public enum DroneStatus { Active, Crashed }\
public enum CameraViewMode { FirstPerson, OverTheShoulder }\
\
// \-\-- Physics & World Data \-\--\
public class RenderMesh { /\* Placeholder for vertex/index/color data \*/ }\
public class PhysicsBody { /\* Placeholder for physics body data \*/ }\
\
public record WorldData(\
RenderMesh TerrainRenderMesh,\
PhysicsBody TerrainPhysicsBody,\
IReadOnlyList\<PhysicsBody\> ObstaclePhysicsBodies,\
bool\[,\] NavigationGrid\
);\
\
public struct DroneState\
{\
public int Id;\
public Vector3 Position;\
public Quaternion Orientation;\
public DroneStatus Status;\
}\
\
// \-\-- Movement Intent Abstraction \-\--\
public interface IMoveIntent { }\
public record KinematicIntent(Vector3 TargetVelocity, Quaternion TargetOrientation) : IMoveIntent;\
public record DynamicIntent(Vector3 Force, Vector3 Torque) : IMoveIntent;\
\
// \-\-- Event & Control Data \-\--\
public struct CollisionEventData\
{\
public int BodyAHandle;\
public int BodyBHandle;\
public Vector3 Position;\
}\
\
public struct ControlInputs\
{\
public int ThrottleStep;\
public float StrafeInput;\
public float VerticalInput;\
public float YawInput;\
}\
\
// \-\-- Agent Class \-\--\
public class DroneAgent\
{\
public int PhysicsBodyHandle { get; init; }\
public DroneState State { get; set; }\
public IAutopilot AutopilotController { get; }\
// A drone could also hold a reference to its specific flight model\
// public IFlightDynamics FlightModel { get; }\
public DroneAgent(int handle, DroneState initialState, IAutopilot autopilot) { /\* \... \*/ }\
}

**4. Module Interfaces**

namespace DroneSim.Core;\
\
// \-\-- Primary Module Interfaces \-\--\
public interface ITerrainGenerator { WorldData Generate(); }\
public interface IAutopilotFactory { IAutopilot Create(); }\
public interface IAIDroneSpawner { List\<DroneAgent\> CreateDrones(int count, WorldData worldData); }\
public interface IRenderer { void Run(); }\
\
public interface IPlayerInput\
{\
void Update(object keyboardState);\
ControlInputs GetFlightControls();\
float GetCameraTiltInput();\
bool IsSwitchCameraPressed();\
bool IsSwitchDronePressed();\
bool IsPossessKeyPressed();\
bool IsDebugTogglePressed(); // NEW\
}\
\
public interface IFlightDynamics\
{\
IMoveIntent GenerateMoveIntent(DroneState currentState, ControlInputs inputs, float deltaTime);\
}\
\
public interface IPhysicsService\
{\
// Setup\
void AddStaticBody(PhysicsBody bodyData);\
int AddDynamicBody(object description);\
int AddKinematicBody(object description);\
// Simulation\
void SubmitMoveIntent(int bodyHandle, IMoveIntent intent);\
void Step(float deltaTime);\
// State & Events\
DroneState GetState(int bodyHandle);\
event Action\<CollisionEventData\> CollisionDetected;\
}\
\
public interface IAutopilot\
{\
void SetTarget(Vector3 targetPosition);\
ControlInputs GetControlUpdate(DroneState currentDroneState);\
}\
\
public interface IDebugDrawService\
{\
// Consumer methods (called by modules like Autopilot, FlightDynamics, etc.)\
void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f);\
void DrawVector(Vector3 start, Vector3 vector, Color color, float duration = 0f);\
void DrawPath(IReadOnlyList\<Vector3\> pathPoints, Color color, float duration = 0f);\
void DrawCollisionPoint(Vector3 point, float radius, Color color, float duration = 0f);\
\
// Methods for the simulation loop\
void Tick(float deltaTime); // Called by Orchestrator\
IReadOnlyList\<object\> GetShapesToRender(); // Called by Renderer\
void Clear(); // Clears all one-frame shapes\
}\
\
\
// \-\-- Orchestrator / Renderer Communication Interfaces \-\--\
public interface IFrameTickable\
{\
void Setup();\
void UpdateFrame(float deltaTime);\
}\
\
public interface IRenderDataSource\
{\
IReadOnlyList\<DroneState\> GetAllDroneStates();\
int GetPlayerControlledDroneId();\
int GetCameraAttachedToDroneId();\
CameraViewMode GetCameraViewMode();\
float GetCameraTilt();\
string GetHudInfo();\
bool IsDebugDrawingEnabled(); // NEW\
}

/// <summary>
/// Provides access to the immutable <c>WorldData</c> generated during
/// <see cref="IFrameTickable.Setup"/>. Implemented by the orchestrator so the
/// renderer can obtain terrain information when initializing graphics resources.
/// </summary>
public interface IWorldDataSource
{
    WorldData GetWorldData();
}
