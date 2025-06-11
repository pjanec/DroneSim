Document ID: TEST-DRNSIM-V1.3

Date: June 11, 2025

Title: V1.3 Testing Strategy & Specification

**1. Overview & Testing Philosophy**

This document specifies the testing strategy for the DroneSim application, updated to reflect the V2 architecture. Our approach is divided into two primary categories: **Unit Tests** and **Integration Tests**.

- **Unit Tests:** These are fine-grained tests that focus on a single class or module in complete isolation. All external dependencies for the class under test will be replaced with \"mock\" objects (using a library like Moq). This ensures we are testing the logic of the class itself, not its dependencies. Every module in the src/Modules/ directory will have a corresponding unit test project.

- **Integration Tests:** These tests verify that multiple modules work correctly together when connected. They focus on the interactions and data flow between components, primarily testing the Orchestrator\'s logic. These tests will live in a single DroneSim.Integration.Tests project.

All tests will be written using the **xUnit** framework.

**2. Unit Test Specifications**

#### 2.1 DroneSim.FlightDynamics.Tests

- **Class Under Test:** V1KinematicFlightModel and V2PhysicsFlightModel.

- **Goal:** Verify that control inputs are correctly translated into the appropriate IMoveIntent.

- **Key Tests (for V1KinematicFlightModel):**

  - GenerateMoveIntent_WithFullThrottle_ReturnsKinematicIntentWithPositiveZVelocity(): Assert the returned intent is of type KinematicIntent and its TargetVelocity.Z is positive.

  - GenerateMoveIntent_WithStrafeInput_ReturnsKinematicIntentWithCorrectXVelocity(): Assert the TargetVelocity.X component matches the input.

- **Key Tests (for V2PhysicsFlightModel):**

  - GenerateMoveIntent_WithFullThrottle_ReturnsDynamicIntentWithForwardForce(): Assert the intent is DynamicIntent and its Force vector has a component along the drone\'s forward axis.

  - GenerateMoveIntent_WithYawInput_ReturnsDynamicIntentWithCorrectTorque(): Assert the Torque vector has a component around the drone\'s Y-axis.

#### 2.2 DroneSim.PhysicsService.Tests

- **Class Under Test:** V2BepuPhysicsService.

- **Goal:** Verify that the service correctly interacts with the underlying BepuPhysics engine. This requires mocking the BepuPhysics.Simulation object, which is advanced. A more practical approach is to test it via integration tests. However, some unit tests are possible.

- **Key Tests:**

  - Step_WhenCalled_ClearsMoveIntentDictionary(): Submit an intent, call Step(), and then assert that the internal dictionary of intents is empty.

  - SubmitMoveIntent_KinematicIntent_SetsKinematicBodyVelocity(): Mock the Simulation object. Submit a KinematicIntent. Call Step(). Verify that the Velocity property on the mock kinematic body was set correctly.

#### 2.3 DroneSim.Autopilot.Tests

- **Class Under Test:** V1StupidAutopilot.

- **Goal:** Verify the \"turn-then-burn\" AI logic and its use of the debug service.

- **Dependencies to Mock:** IDebugDrawService.

- **Key Tests:**

  - GetUpdate_WhenFacingAwayFromTarget_ReturnsYawInputOnly(): (Same as before).

  - GetUpdate_WhenFacingTarget_ReturnsThrottleInputOnly(): (Same as before).

  - GetUpdate_CallsDebugDrawPath_ToVisualizePlan(): When GetControlUpdate is called, verify that \_mockDebugDraw.DrawPath() was called with the correct start and end points.

#### 2.4 DroneSim.DebugDraw.Tests

- **Class Under Test:** DebugDrawService.

- **Goal:** Verify that the service correctly manages the lifecycle of debug shapes.

- **Key Tests:**

  - DrawLine_WithZeroDuration_IsRemovedAfterOneTick(): Call DrawLine with duration = 0. Assert that GetShapesToRender() returns one shape. Then call Tick(0.1f). Assert that GetShapesToRender() now returns zero shapes.

  - DrawLine_WithPositiveDuration_PersistsAndDecrements(): Call DrawLine with duration = 1.0f. Call Tick(0.1f). Assert that the shape returned by GetShapesToRender() now has a remaining duration of 0.9f.

  - Tick_WhenShapeDurationExpires_RemovesShape(): Call DrawLine with duration = 0.5f. Call Tick(0.6f). Assert that GetShapesToRender() returns an empty list.

*(Unit tests for TerrainGenerator, PlayerInput, and AISpawner remain largely the same as specified in the previous testing document.)*

**3. Integration Test Specifications**

- **Class Under Test:** Orchestrator.

- **Goal:** Verify the interactions between fully implemented modules using the final V2 architecture. This requires a test setup that can run a single \"tick\" of the Orchestrator.UpdateFrame method.

- **Key Tests:**

  - UpdateFrame_UsingKinematicModel_DroneStopsAtBoundary(): Arrange by setting up the orchestrator to use the V1KinematicFlightModel. Give the drone full forward throttle towards a wall. Act by running several ticks. Assert that the drone\'s final position is clamped at the world boundary.

  - UpdateFrame_UsingDynamicModel_CollisionEventIsFiredAtBoundary(): Arrange by using the V2PhysicsFlightModel. Give the drone full forward throttle towards a wall. Act by running ticks until it hits. Assert that the CollisionDetected event was fired and the orchestrator correctly set the drone\'s Status to Crashed.

  - UpdateFrame_PlayerPressesDebugToggle_IsDebugDrawingEnabledFlagFlips(): Simulate an F3 key press via a mock IPlayerInput. Run a tick. Assert that \_dataSource.IsDebugDrawingEnabled() now returns true.

  - UpdateFrame_AIDroneGeneratesPath_DebugDrawServiceIsCalled(): Run a tick and verify that the IDebugDrawService.DrawPath method was called by the AI autopilot module, confirming the dependency was correctly injected and used.
