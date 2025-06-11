Document ID: TEST-DRNSIM-V1.2

Date: June 10, 2025

Title: V1.2 Testing Strategy & Specification

**1. Overview & Testing Philosophy**

This document specifies the testing strategy for the V1 DroneSim application. Our approach is divided into two primary categories: **Unit Tests** and **Integration Tests**.

- **Unit Tests:** These are fine-grained tests that focus on a single class or module in complete isolation. All external dependencies for the class under test will be replaced with \"mock\" objects (using a library like Moq). This ensures we are testing the logic of the class itself, not its dependencies. Every module in the src/Modules/ directory will have a corresponding unit test project.

- **Integration Tests:** These tests verify that multiple modules work correctly together when connected. They focus on the interactions and data flow between components, primarily testing the Orchestrator\'s logic. These tests will live in a single DroneSim.Integration.Tests project.

All tests will be written using the **xUnit** framework.

**2. Unit Test Specifications**

#### 2.1 DroneSim.TerrainGenerator.Tests

- **Class Under Test:** V1TerrainGenerator

- **Goal:** Verify that the generated world data is correct and consistent.

- **Key Tests:**

  - Generate_ReturnsNonNullWorldData(): Ensures the method returns a valid object with non-null properties.

  - Generate_ObstacleList_IsEmpty(): Confirms that for V1, no obstacles are created.

  - Generate_NavigationGrid_HasCorrectDimensions(): Checks if the bool\[,\] array matches the worldSize parameter passed to the constructor.

  - Generate_NavigationGrid_AllCellsAreFlyable(): Iterates through the entire nav grid and asserts that every value is true.

  - Generate_RenderMesh_ContainsCorrectVertexAndIndexCount(): Validates that the number of vertices and indices generated matches the expected count for the given worldSize. (vertices = (size+1)\*(size+1), indices = size\*size\*6).

  - Constructor_WithInvalidParameters_ThrowsException(): Checks that passing a worldSize of 0 or less throws an ArgumentOutOfRangeException.

#### 2.2 DroneSim.PlayerInput.Tests

- **Class Under Test:** V1KeyboardInput

- **Goal:** Verify that raw keyboard states are correctly translated into structured ControlInputs and single-press events.

- **Dependencies to Mock:** IKeyboard from the Silk.NET.Input library.

- **Key Tests:**

  - Update_WKeyJustPressed_ThrottleIncrementsByOne(): Simulate keyboard state where \'W\' was UP last frame and is DOWN this frame. Assert ThrottleStep is 1.

  - Update_WKeyIsHeld_ThrottleDoesNotIncrement(): Simulate keyboard state where \'W\' was DOWN last frame and is still DOWN. Assert ThrottleStep does not change.

  - Update_ThrottleAtMax_WPressDoesNotIncrementFurther(): Set throttle to 10, simulate a \'W\' press, and assert it remains 10.

  - Update_DKeyIsHeld_ReturnsCorrectStrafeInput(): Simulate \'D\' is held down. Assert StrafeInput is 1.0f.

  - Update_AAndDKeysHeld_ReturnsZeroStrafeInput(): Simulate both \'A\' and \'D\' are held. Assert StrafeInput is 0.0f.

  - IsPossessKeyPressed_PKeyJustPressed_ReturnsTrueForOneFrameOnly(): Test the single-press logic. First, simulate a press and assert true. Then, in a subsequent update where the key is held, assert false.

#### 2.3 DroneSim.PhysicsService.Tests

- **Class Under Test:** V1BoundaryPhysicsService

- **Goal:** Verify that movement resolution correctly clamps positions.

- **Key Tests:**

  - ResolveCollisions_WhenWithinBounds_ReturnsUnchangedPosition(): Provide a position and displacement that result in a final position well within the world boundaries. Assert the returned vector is correct.

  - ResolveCollisions_WhenMovingPastBoundary_ClampsXCoordinate(): Provide a position near the +X boundary and a displacement that would push it past. Assert the returned vector\'s X component is exactly worldBoundary. (Repeat for -X, +Z, -Z).

  - ResolveCollisions_WhenMovingBelowGround_ClampsYCoordinate(): Provide a position just above the ground and a downward displacement. Assert the returned vector\'s Y component is exactly 0.0f.

#### 2.4 DroneSim.FlightDynamics.Tests

- **Class Under Test:** V1KinematicFlightModel

- **Goal:** Verify that control inputs are correctly translated into unobstructed displacement and orientation changes.

- **Key Tests:**

  - CalculateUpdate_WithFullThrottle_GeneratesPositiveZDisplacement(): Provide ThrottleStep = 10 and deltaTime = 1.0f. Assert the returned DesiredDisplacement.Z is positive.

  - CalculateUpdate_WithZeroThrottleFromSpeed_GeneratesNegativeZDisplacement(): Prime the model by running it with throttle, so it has internal speed. Then, call with ThrottleStep = 0. Assert the displacement is still positive but smaller (due to Lerp), showing deceleration.

  - CalculateUpdate_WithStrafeInput_GeneratesCorrectXDisplacement(): Provide StrafeInput = -1.0f. Assert DesiredDisplacement.X is negative.

  - CalculateUpdate_WithYawInput_ReturnsCorrectNewOrientation(): Provide YawInput = 1.0f and deltaTime = 1.0f. Assert the returned NewOrientation is rotated correctly around the Y-axis compared to the initial orientation.

  - CalculateUpdate_DoesNotClampPosition(): Provide inputs that would result in a large displacement. Assert that the returned DesiredDisplacement is NOT clamped, confirming the responsibility is correctly delegated.

#### 2.5 DroneSim.Autopilot.Tests

- **Class Under Test:** V1StupidAutopilot

- **Goal:** Verify the \"turn-then-burn\" AI logic.

- **Key Tests:**

  - GetUpdate_WhenFacingAwayFromTarget_ReturnsYawInputOnly(): Set a target directly north of the drone, but have the drone facing south. Assert YawInput is non-zero and ThrottleStep is zero.

  - GetUpdate_WhenFacingTarget_ReturnsThrottleInputOnly(): Set a target north and have the drone facing north. Assert YawInput is zero and ThrottleStep is non-zero.

  - GetUpdate_WhenBelowFlightAltitude_ReturnsPositiveVerticalInput(): Set the drone\'s position well below the configured flight altitude. Assert VerticalInput is 1.0f.

  - GetUpdate_WhenAtTarget_ReturnsZeroInputs(): Set the drone\'s position within the ArrivalRadius of the target. Assert all returned control inputs are zero.

#### 2.6 DroneSim.AISpawner.Tests

- **Class Under Test:** V1AIDroneSpawner

- **Goal:** Verify that the correct number of agents are created with valid initial states.

- **Dependencies to Mock:** IAutopilotFactory, IAutopilot.

- **Key Tests:**

  - CreateDrones_WithCountOfFive_ReturnsListOfFiveAgents(): Call with count = 5. Assert the returned list\'s Count is 5.

  - CreateDrones_AllAgents_HaveUniqueIdsStartingFromOne(): Call with a count of 3. Get all IDs and assert they are unique and do not contain 0.

  - CreateDrones_CallsAutopilotFactory_ForEachAgent(): Call with count = 5. Verify that \_mockAutopilotFactory.Create() was called exactly 5 times.

  - CreateDrones_SetsTarget_OnEachNewAutopilot(): Verify that the SetTarget method on the mock IAutopilot object was called for each agent created.

**3. Integration Test Specifications**

- **Class Under Test:** Orchestrator

- **Goal:** Verify the interactions between fully implemented modules. This requires a test setup that can run a single \"tick\" of the Orchestrator.UpdateFrame method.

- **Key Tests:**

  - UpdateFrame_PlayerPressesW_PlayerDroneMovesForward(): Arrange by setting up a full orchestrator with all V1 modules. Act by simulating a \'W\' key press via a mock IPlayerInput and running one update tick. Assert that the player drone\'s DroneState.Position has changed correctly.

  - UpdateFrame_PlayerPossessesAIDrone_ControlIsTransferred(): Arrange by having the camera attached to an AI drone. Simulate a \'P\' key press. Act by running a tick. Assert the orchestrator\'s internal \_playerControlledDroneId has changed to the AI drone\'s ID.

  - UpdateFrame_AIDroneReachesTarget_NewTargetIsAssigned(): Arrange by manually setting an AI drone\'s position to be within the arrival radius of its target. Act by running a tick. Assert that the SetTarget method on that drone\'s IAutopilot instance was called again with a *new* target position.

  - GetHudInfo_WhenPlayerIsControlling_GeneratesCorrectHudString(): Run a tick and call the orchestrator\'s GetHudInfo() method. Assert that the returned string contains the expected text, like speed, altitude, and throttle level.
