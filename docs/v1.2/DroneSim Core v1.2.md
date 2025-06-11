Document ID: MODSPEC-CORE-V1.2

Date: June 10, 2025

Title: V1.2 DroneSim.Core Library Specification

**1. Overview**

This document specifies the contents of the DroneSim.Core C# project. This project is the foundational library for the entire solution. It contains **no implementation logic**. Its sole purpose is to define the public \"contracts\" (interfaces) and shared data models (structs, records, enums) that all other modules will use to communicate. Every other project in the solution will have a reference to DroneSim.Core.

**2. Dependencies**

- **.NET 8**

- **System.Numerics**

**3. Enumerations**

namespace DroneSim.Core;\
\
/// \<summary\>\
/// Defines the operational status of a drone.\
/// For V1, all drones will be \'Active\'.\
/// \</summary\>\
public enum DroneStatus\
{\
Active,\
Crashed\
}\
\
/// \<summary\>\
/// Defines the available camera view modes.\
/// \</summary\>\
public enum CameraViewMode\
{\
FirstPerson,\
OverTheShoulder\
}

**4. Data Structures**

namespace DroneSim.Core;\
\
using System.Numerics;\
\
/// \<summary\>\
/// Placeholder class for renderable mesh data. The concrete implementation\
/// will hold lists of vertices, indices, colors, etc.\
/// \</summary\>\
public class RenderMesh { /\* Placeholder for vertex/index data \*/ }\
\
/// \<summary\>\
/// Placeholder class for physics collision body data. The concrete\
/// implementation will wrap a collider from the chosen physics library.\
/// \</summary\>\
public class PhysicsBody { /\* Placeholder for physics body data \*/ }\
\
/// \<summary\>\
/// Holds all the static data about the generated world.\
/// \</summary\>\
public record WorldData(\
RenderMesh TerrainRenderMesh,\
PhysicsBody TerrainPhysicsBody,\
IReadOnlyList\<PhysicsBody\> ObstaclePhysicsBodies,\
bool\[,\] NavigationGrid\
);\
\
/// \<summary\>\
/// Represents the complete physical state of a single drone at a moment in time.\
/// \</summary\>\
public struct DroneState\
{\
public int Id;\
public Vector3 Position;\
public Quaternion Orientation;\
public DroneStatus Status;\
}\
\
/// \<summary\>\
/// Represents the desired control actions from either the player or an AI.\
/// \</summary\>\
public struct ControlInputs\
{\
public int ThrottleStep; // 0-10\
public float StrafeInput; // -1 to 1\
public float VerticalInput; // -1 to 1\
public float YawInput; // -1 to 1\
}

**5. Agent Class**

namespace DroneSim.Core;\
\
/// \<summary\>\
/// Represents a single, controllable entity in the simulation.\
/// It bundles the drone\'s state with its AI controller.\
/// \</summary\>\
public class DroneAgent\
{\
public DroneState State { get; set; }\
public IAutopilot AutopilotController { get; }\
\
public DroneAgent(DroneState initialState, IAutopilot autopilotController)\
{\
State = initialState;\
AutopilotController = autopilotController;\
}\
}

**6. Module Interfaces**

namespace DroneSim.Core;\
\
using System.Numerics;\
using System.Collections.Generic;\
\
// \-\-- Primary Module Interfaces \-\--\
\
public interface ITerrainGenerator { WorldData Generate(); }\
public interface IAutopilotFactory { IAutopilot Create(); }\
\
public interface IPlayerInput\
{\
void Update(object keyboardState); // keyboardState is a concrete type from input library\
ControlInputs GetFlightControls();\
float GetCameraTiltInput(); // -1 to 1\
bool IsSwitchCameraPressed();\
bool IsSwitchDronePressed();\
bool IsPossessKeyPressed();\
}\
\
/// \<summary\>\
/// REFACTORED: Calculates desired movement without knowledge of collisions.\
/// \</summary\>\
public interface IFlightDynamics\
{\
(Vector3 DesiredDisplacement, Quaternion NewOrientation) CalculateKinematicUpdate(\
DroneState currentState, ControlInputs inputs, float deltaTime);\
}\
\
/// \<summary\>\
/// NEW: Provides an interface for resolving movement against world geometry.\
/// \</summary\>\
public interface IPhysicsService\
{\
Vector3 ResolveEnvironmentCollisions(Vector3 currentPosition, Vector3 desiredDisplacement);\
}\
\
public interface IAutopilot\
{\
void SetTarget(Vector3 targetPosition);\
ControlInputs GetControlUpdate(DroneState currentDroneState);\
}\
\
public interface IAIDroneSpawner\
{\
List\<DroneAgent\> CreateDrones(int count, WorldData worldData);\
}\
\
public interface IRenderer\
{\
/// \<summary\>\
/// Starts the blocking main loop for the application window.\
/// \</summary\>\
void Run();\
}\
\
\
// \-\-- Orchestrator / Renderer Communication Interfaces \-\--\
\
/// \<summary\>\
/// Defines a contract for modules that need to perform setup logic\
/// and be updated every frame by the main loop driver.\
/// \</summary\>\
public interface IFrameTickable\
{\
void Setup();\
void UpdateFrame(float deltaTime);\
}\
\
/// \<summary\>\
/// Defines a contract for a data provider that gives the renderer\
/// all the information it needs to draw the scene.\
/// \</summary\>\
public interface IRenderDataSource\
{\
IReadOnlyList\<DroneState\> GetAllDroneStates();\
int GetPlayerControlledDroneId();\
int GetCameraAttachedToDroneId();\
CameraViewMode GetCameraViewMode();\
float GetCameraTilt();\
string GetHudInfo();\
}
