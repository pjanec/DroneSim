using Xunit;
using DroneSim.TerrainGenerator;

namespace DroneSim.TerrainGenerator.Tests;

public class V1TerrainGeneratorTests
{
    [Fact]
    public void Generate_WithDefaultParameters_ReturnsValidWorldData()
    {
        // Arrange
        var worldSize = 16; // Use a smaller size for testing
        var cellSize = 1.0f;
        var generator = new V1TerrainGenerator(worldSize, cellSize);

        // Act
        var worldData = generator.Generate();

        // Assert
        Assert.NotNull(worldData);
        Assert.NotNull(worldData.TerrainRenderMesh);
        Assert.NotNull(worldData.TerrainPhysicsBody);
        Assert.NotNull(worldData.NavigationGrid);
        Assert.Empty(worldData.ObstaclePhysicsBodies);

        // Check dimensions
        Assert.Equal(worldSize, worldData.NavigationGrid.GetLength(0));
        Assert.Equal(worldSize, worldData.NavigationGrid.GetLength(1));

        // Check if the render mesh is the correct V1 type and has data
        var renderMesh = worldData.TerrainRenderMesh as V1RenderMesh;
        Assert.NotNull(renderMesh);
        
        // Vertices = (size + 1) * (size + 1)
        var expectedVertexCount = (worldSize + 1) * (worldSize + 1);
        Assert.Equal(expectedVertexCount, renderMesh.Vertices.Count);
        Assert.Equal(expectedVertexCount, renderMesh.Colors.Count);

        // Indices = size * size * 6
        var expectedIndexCount = worldSize * worldSize * 6;
        Assert.Equal(expectedIndexCount, renderMesh.Indices.Count);
    }
} 