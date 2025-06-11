using Xunit;
using DroneSim.DebugDraw;
using System.Numerics;
using System.Drawing;
using System.Linq;

namespace DroneSim.DebugDraw.Tests;

public class DebugDrawServiceTests
{
    private readonly DebugDrawService _sut; // System Under Test

    public DebugDrawServiceTests()
    {
        _sut = new DebugDrawService();
    }

    [Fact]
    public void DrawLine_WithZeroDuration_IsRemovedAfterOneTick()
    {
        // Arrange
        _sut.DrawLine(Vector3.Zero, Vector3.One, Color.Red, 0f);

        // Act
        // The first call to GetShapesToRender should contain the shape.
        var shapesBeforeTick = _sut.GetShapesToRender();
        _sut.Tick(0.1f); // Simulate a frame passing
        var shapesAfterTick = _sut.GetShapesToRender();

        // Assert
        Assert.Single(shapesBeforeTick);
        Assert.Empty(shapesAfterTick);
    }

    [Fact]
    public void DrawLine_WithPositiveDuration_PersistsAndDecrements()
    {
        // Arrange
        _sut.DrawLine(Vector3.Zero, Vector3.One, Color.Blue, 1.0f);

        // Act
        _sut.Tick(0.1f);
        var shapes = _sut.GetShapesToRender();
        var line = shapes.FirstOrDefault() as DebugLine;

        // Assert
        Assert.Single(shapes);
        Assert.NotNull(line);
        Assert.True(line.RemainingDuration > 0.89f && line.RemainingDuration < 0.91f, $"Duration was {line.RemainingDuration}");
    }

    [Fact]
    public void Tick_WhenShapeDurationExpires_RemovesShape()
    {
        // Arrange
        _sut.DrawLine(Vector3.Zero, Vector3.One, Color.Green, 0.5f);

        // Act
        _sut.Tick(0.6f); // Tick for longer than the duration
        var shapes = _sut.GetShapesToRender();

        // Assert
        Assert.Empty(shapes);
    }
    
    [Fact]
    public void GetShapesToRender_ReturnsAllActiveShapes()
    {
        // Arrange
        // A persistent shape
        _sut.DrawLine(Vector3.Zero, Vector3.One, Color.Green, 1.0f);
        // A one-frame shape
        _sut.DrawVector(Vector3.Zero, Vector3.UnitX, Color.Red, 0f);

        // Act
        var shapes = _sut.GetShapesToRender();

        // Assert
        Assert.Equal(2, shapes.Count);
    }
    
    [Fact]
    public void Clear_RemovesOnlyOneFrameShapes()
    {
        // Arrange
        _sut.DrawLine(Vector3.Zero, Vector3.One, Color.Green, 1.0f); // Persistent
        _sut.DrawVector(Vector3.Zero, Vector3.UnitX, Color.Red, 0f); // One-frame

        // Act
        _sut.Clear();
        var shapes = _sut.GetShapesToRender();
        var line = shapes.FirstOrDefault() as DebugLine;

        // Assert
        Assert.Single(shapes); // Only the persistent one should remain
        Assert.NotNull(line);
        Assert.Equal(Color.Green, line.Color);
    }
} 