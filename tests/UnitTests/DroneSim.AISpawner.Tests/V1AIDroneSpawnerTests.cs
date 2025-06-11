// In project: DroneSim.AISpawner.Tests
using DroneSim.AISpawner;
using DroneSim.Core;
using Microsoft.Extensions.Options;
using Moq;

namespace DroneSim.AISpawner.Tests;

public class V1AIDroneSpawnerTests
{
    private readonly Mock<IAutopilotFactory> _mockAutopilotFactory;
    private readonly Mock<IPhysicsService> _mockPhysicsService;
    private readonly Mock<IOptions<SpawnerOptions>> _mockOptions;
    private V1AIDroneSpawner _spawner;
    private readonly SpawnerOptions _options;

    public V1AIDroneSpawnerTests()
    {
        _mockAutopilotFactory = new Mock<IAutopilotFactory>();
        _mockPhysicsService = new Mock<IPhysicsService>();
        _mockOptions = new Mock<IOptions<SpawnerOptions>>();
        _options = new SpawnerOptions { InitialFlightAltitude = 50f, WorldBoundary = 100f };

        _mockOptions.Setup(o => o.Value).Returns(_options);
        _mockAutopilotFactory.Setup(f => f.Create()).Returns(new Mock<IAutopilot>().Object);
        _mockPhysicsService.Setup(p => p.AddKinematicBody(It.IsAny<object>())).Returns((object desc) => 1); // Return a dummy handle

        _spawner = new V1AIDroneSpawner(_mockAutopilotFactory.Object, _mockPhysicsService.Object, _mockOptions.Object);
    }

    [Fact]
    public void CreateDrones_WithZeroCount_ReturnsEmptyList()
    {
        // Arrange
        var worldData = new WorldData(new RenderMesh(), new PhysicsBody(), new List<PhysicsBody>(), new bool[0,0]);

        // Act
        var result = _spawner.CreateDrones(0, worldData);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void CreateDrones_CreatesCorrectNumberOfDrones()
    {
        // Arrange
        var count = 5;
        var worldData = new WorldData(new RenderMesh(), new PhysicsBody(), new List<PhysicsBody>(), new bool[0,0]);
        _spawner = new V1AIDroneSpawner(_mockAutopilotFactory.Object, _mockPhysicsService.Object, _mockOptions.Object);

        // Act
        var result = _spawner.CreateDrones(count, worldData);

        // Assert
        Assert.Equal(count, result.Count);
        _mockAutopilotFactory.Verify(f => f.Create(), Times.Exactly(count));
        _mockPhysicsService.Verify(p => p.AddKinematicBody(It.IsAny<object>()), Times.Exactly(count));
    }

    [Fact]
    public void CreateDrones_AssignsUniqueIdsStartingFromOne()
    {
        // Arrange
        var count = 3;
        var worldData = new WorldData(new RenderMesh(), new PhysicsBody(), new List<PhysicsBody>(), new bool[0,0]);
        _spawner = new V1AIDroneSpawner(_mockAutopilotFactory.Object, _mockPhysicsService.Object, _mockOptions.Object);

        // Act
        var result = _spawner.CreateDrones(count, worldData);
        var ids = result.Select(d => d.State.Id).ToList();

        // Assert
        Assert.Contains(1, ids);
        Assert.Contains(2, ids);
        Assert.Contains(3, ids);
        Assert.DoesNotContain(0, ids); // ID 0 is for the player
    }

    [Fact]
    public void CreateDrones_SetsInitialStateCorrectly()
    {
        // Arrange
        var count = 1;
        var worldData = new WorldData(new RenderMesh(), new PhysicsBody(), new List<PhysicsBody>(), new bool[0,0]);
        _spawner = new V1AIDroneSpawner(_mockAutopilotFactory.Object, _mockPhysicsService.Object, _mockOptions.Object);

        // Act
        var result = _spawner.CreateDrones(count, worldData);
        var drone = result.Single();

        // Assert
        Assert.Equal(_options.InitialFlightAltitude, drone.State.Position.Y);
        Assert.True(drone.State.Position.X >= -_options.WorldBoundary && drone.State.Position.X <= _options.WorldBoundary);
        Assert.True(drone.State.Position.Z >= -_options.WorldBoundary && drone.State.Position.Z <= _options.WorldBoundary);
        Assert.Equal(DroneStatus.Active, drone.State.Status);
        Assert.NotNull(drone.AutopilotController);
    }
} 