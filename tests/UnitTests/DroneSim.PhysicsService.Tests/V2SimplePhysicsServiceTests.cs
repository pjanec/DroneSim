using DroneSim.Core;
using DroneSim.Physics;
using Microsoft.Extensions.Options;
using Moq;
using System.Numerics;
using Xunit;

namespace DroneSim.Physics.Tests;

public class V2SimplePhysicsServiceTests
{
    private readonly V2SimplePhysicsService _physicsService;
    private readonly Mock<IOptions<PhysicsOptions>> _mockOptions;
    private readonly PhysicsOptions _physicsOptions;

    public V2SimplePhysicsServiceTests()
    {
        _physicsOptions = new PhysicsOptions { WorldBoundary = 100f };
        _mockOptions = new Mock<IOptions<PhysicsOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_physicsOptions);
        _physicsService = new V2SimplePhysicsService(_mockOptions.Object);
    }

    [Fact]
    public void AddKinematicBody_And_AddDynamicBody_ReturnUniqueHandles()
    {
        // Act
        var handle1 = _physicsService.AddKinematicBody(new object());
        var handle2 = _physicsService.AddDynamicBody(new object());
        var handle3 = _physicsService.AddKinematicBody(new object());

        // Assert
        Assert.NotEqual(handle1, handle2);
        Assert.NotEqual(handle2, handle3);
    }

    [Fact]
    public void SubmitMoveIntent_WithKinematicIntent_SetsVelocityDirectly()
    {
        // Arrange
        var bodyHandle = _physicsService.AddKinematicBody(new object());
        var targetVelocity = new Vector3(1, 2, 3);
        var intent = new KinematicIntent(targetVelocity, Quaternion.Identity);

        // Act
        _physicsService.SubmitMoveIntent(bodyHandle, intent);
        _physicsService.Step(1.0f); // Step to apply velocity
        var state = _physicsService.GetState(bodyHandle);

        // Assert
        // Position should be initial (0,0,0) + velocity * deltaTime
        Assert.Equal(targetVelocity, state.Position);
    }

    [Fact]
    public void SubmitMoveIntent_WithDynamicIntent_IntegratesVelocityFromForce()
    {
        // Arrange
        var bodyHandle = _physicsService.AddDynamicBody(new object());
        var force = new Vector3(10, 0, 0); // mass is 1.0f, so acceleration is 10 m/s^2
        var intent = new DynamicIntent(force, Vector3.Zero);
        var deltaTime = 0.5f;

        // Act
        _physicsService.SubmitMoveIntent(bodyHandle, intent);
        _physicsService.Step(deltaTime);
        var state = _physicsService.GetState(bodyHandle);

        // Assert
        // v = a * t = (10 / 1) * 0.5 = 5
        // p = v * t = 5 * 0.5 = 2.5
        var expectedPosition = new Vector3(2.5f, 0, 0);
        Assert.Equal(expectedPosition.X, state.Position.X, 0.001f);
    }

    [Fact]
    public void Step_WhenPositionExceedsBoundary_ClampsPosition()
    {
        // Arrange
        var bodyHandle = _physicsService.AddKinematicBody(new object());
        // Set a very high velocity that will definitely exceed the boundary in one step
        var targetVelocity = new Vector3(_physicsOptions.WorldBoundary + 50f, 0, 0);
        var intent = new KinematicIntent(targetVelocity, Quaternion.Identity);

        // Act
        _physicsService.SubmitMoveIntent(bodyHandle, intent);
        _physicsService.Step(1.0f);
        var state = _physicsService.GetState(bodyHandle);

        // Assert
        Assert.Equal(_physicsOptions.WorldBoundary, state.Position.X);
    }

    [Fact]
    public void Step_WhenPositionIsNegative_ClampsToGround()
    {
        // Arrange
        var bodyHandle = _physicsService.AddKinematicBody(new object());
        var targetVelocity = new Vector3(0, -50f, 0);
        var intent = new KinematicIntent(targetVelocity, Quaternion.Identity);

        // Act
        _physicsService.SubmitMoveIntent(bodyHandle, intent);
        _physicsService.Step(1.0f);
        var state = _physicsService.GetState(bodyHandle);

        // Assert
        Assert.Equal(0, state.Position.Y);
    }
} 