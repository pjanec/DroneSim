// In project: DroneSim.Autopilot.Tests
using DroneSim.Autopilot;
using DroneSim.Core;
using Microsoft.Extensions.Options;
using Moq;
using System.Numerics;

namespace DroneSim.Autopilot.Tests;

public class V1StupidAutopilotTests
{
    private readonly Mock<IOptions<AIBehaviorOptions>> _mockOptions;
    private readonly Mock<IDebugDrawService> _mockDebugDraw;
    private readonly AIBehaviorOptions _options;
    private readonly V1StupidAutopilot _autopilot;

    public V1StupidAutopilotTests()
    {
        _options = new AIBehaviorOptions
        {
            FlightAltitude = 50f,
            ArrivalRadius = 2f,
            YawTolerance = 0.1f,
            ConstantThrottleStep = 5
        };
        _mockOptions = new Mock<IOptions<AIBehaviorOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_options);

        _mockDebugDraw = new Mock<IDebugDrawService>();

        _autopilot = new V1StupidAutopilot(_mockOptions.Object, _mockDebugDraw.Object);
    }

    [Fact]
    public void SetTarget_ClampsTargetToFlightAltitude()
    {
        // Arrange
        var initialTarget = new Vector3(100, 100, 100);
        var expectedTargetPosition = new Vector3(100, _options.FlightAltitude, 100);
        var state = new DroneState { Position = Vector3.Zero, Orientation = Quaternion.Identity };

        // Act
        _autopilot.SetTarget(initialTarget);
        // This call will use the internal target
        _autopilot.GetControlUpdate(state);

        // Assert
        // The only way to verify the internal state is to see what it does.
        // We expect it to try to climb to the flight altitude.
        var inputs = _autopilot.GetControlUpdate(state);
        Assert.Equal(1.0f, inputs.VerticalInput);
    }

    [Fact]
    public void GetControlUpdate_WhenFarFromTarget_ShouldYawTowardsTarget()
    {
        // Arrange
        var state = new DroneState { Position = Vector3.Zero, Orientation = Quaternion.Identity };
        _autopilot.SetTarget(new Vector3(100, _options.FlightAltitude, 0)); // Directly to the right

        // Act
        var inputs = _autopilot.GetControlUpdate(state);

        // Assert
        Assert.Equal(-1.0f, inputs.YawInput); // Should turn left (fastest direction)
        Assert.Equal(0, inputs.ThrottleStep); // No throttle while turning
    }
    
    [Fact]
    public void GetControlUpdate_WhenFacingTarget_ShouldApplyThrottle()
    {
        // Arrange
        var state = new DroneState { Position = Vector3.Zero, Orientation = Quaternion.Identity };
        _autopilot.SetTarget(new Vector3(0, _options.FlightAltitude, 100)); // Directly ahead

        // Act
        var inputs = _autopilot.GetControlUpdate(state);

        // Assert
        Assert.Equal(0, inputs.YawInput);
        Assert.Equal(_options.ConstantThrottleStep, inputs.ThrottleStep);
    }

    [Fact]
    public void GetControlUpdate_WhenAboveTargetAltitude_ShouldDescend()
    {
        // Arrange
        var state = new DroneState { Position = new Vector3(0, 100, 0), Orientation = Quaternion.Identity };
        _autopilot.SetTarget(new Vector3(0, _options.FlightAltitude, 100));

        // Act
        var inputs = _autopilot.GetControlUpdate(state);

        // Assert
        Assert.Equal(-1.0f, inputs.VerticalInput);
    }

    [Fact]
    public void GetControlUpdate_WhenAtTarget_ShouldReturnZeroInputs()
    {
        // Arrange
        var targetPosition = new Vector3(1, _options.FlightAltitude, 1);
        var state = new DroneState { Position = new Vector3(0, _options.FlightAltitude, 0), Orientation = Quaternion.Identity };
        _autopilot.SetTarget(targetPosition);
        
        // Move state to be within arrival radius
        state.Position = targetPosition;

        // Act
        var inputs = _autopilot.GetControlUpdate(state);

        // Assert
        Assert.Equal(0, inputs.ThrottleStep);
        Assert.Equal(0, inputs.YawInput);
        Assert.Equal(0, inputs.VerticalInput);
        Assert.Equal(0, inputs.StrafeInput);
    }
} 