using DroneSim.Core;
using DroneSim.FlightDynamics;
using Microsoft.Extensions.Options;
using Moq;
using System.Numerics;
using Xunit;

namespace DroneSim.FlightDynamics.Tests;

public class V1KinematicFlightModelTests
{
    private readonly Mock<IOptions<FlightModelOptions>> _mockOptions;
    private readonly FlightModelOptions _flightModelOptions;

    public V1KinematicFlightModelTests()
    {
        _flightModelOptions = new FlightModelOptions();
        _mockOptions = new Mock<IOptions<FlightModelOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_flightModelOptions);
    }

    [Fact]
    public void GenerateMoveIntent_WithFullThrottle_ReturnsKinematicIntentWithMaxForwardSpeed()
    {
        // Arrange
        var flightModel = new V1KinematicFlightModel(_mockOptions.Object);
        var droneState = new DroneState { Id = 1, Orientation = Quaternion.Identity };
        var inputs = new ControlInputs { ThrottleStep = 10 }; // Full throttle
        var deltaTime = 1.0f; // Use 1s for simplicity

        // Act
        var intent = flightModel.GenerateMoveIntent(droneState, inputs, deltaTime) as KinematicIntent;

        // Assert
        Assert.NotNull(intent);
        // After one second, the speed will be smoothed by the acceleration factor.
        var expectedSpeed = float.Lerp(0f, _flightModelOptions.MaxForwardSpeed, deltaTime * _flightModelOptions.AccelerationFactor);
        Assert.Equal(expectedSpeed, intent.TargetVelocity.Z, 3f);
    }

    [Fact]
    public void GenerateMoveIntent_WithStrafeInput_ReturnsKinematicIntentWithCorrectXVelocity()
    {
        // Arrange
        var flightModel = new V1KinematicFlightModel(_mockOptions.Object);
        var droneState = new DroneState { Id = 1, Orientation = Quaternion.Identity };
        var inputs = new ControlInputs { StrafeInput = 1.0f }; // Full right strafe
        var deltaTime = 0.1f;

        // Act
        var intent = flightModel.GenerateMoveIntent(droneState, inputs, deltaTime) as KinematicIntent;

        // Assert
        Assert.NotNull(intent);
        Assert.Equal(_flightModelOptions.MaxStrafeSpeed, intent.TargetVelocity.X, 0.001f);
    }

    [Fact]
    public void GenerateMoveIntent_WithYawInput_ReturnsKinematicIntentWithCorrectRotation()
    {
        // Arrange
        var flightModel = new V1KinematicFlightModel(_mockOptions.Object);
        var droneState = new DroneState { Id = 1, Orientation = Quaternion.Identity };
        var inputs = new ControlInputs { YawInput = 1.0f }; // Full right yaw
        var deltaTime = 1.0f;

        // Act
        var intent = flightModel.GenerateMoveIntent(droneState, inputs, deltaTime) as KinematicIntent;
        
        // Assert
        Assert.NotNull(intent);
        // Expected yaw change is negative because of the coordinate system
        var expectedYawRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -_flightModelOptions.YawSpeed * deltaTime);
        Assert.Equal(expectedYawRotation.Y, intent.TargetOrientation.Y, 0.001f);
        Assert.Equal(expectedYawRotation.W, intent.TargetOrientation.W, 0.001f);
    }
} 