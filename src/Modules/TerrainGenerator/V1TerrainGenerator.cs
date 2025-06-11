// In project: DroneSim.TerrainGenerator
using DroneSim.Core;
using System.Numerics;
using System.Collections.Generic;

namespace DroneSim.TerrainGenerator;

/// <summary>
/// A concrete implementation of RenderMesh for the V1 renderer.
/// It inherits from the core placeholder class and adds the data fields
/// required for the renderer to be able to draw the mesh.
/// </summary>
public class V1RenderMesh : RenderMesh
{
    public IReadOnlyList<Vector3> Vertices { get; }
    public IReadOnlyList<Vector3> Colors { get; }
    public IReadOnlyList<int> Indices { get; }

    public V1RenderMesh(IReadOnlyList<Vector3> vertices, IReadOnlyList<Vector3> colors, IReadOnlyList<int> indices)
    {
        Vertices = vertices;
        Colors = colors;
        Indices = indices;
    }
}


/// <summary>
/// V1 implementation of the terrain generator.
/// Creates a flat plane with procedural vertex coloring for biomes.
/// </summary>
public class V1TerrainGenerator : ITerrainGenerator
{
    private readonly int _worldSize;
    private readonly float _cellSize;
    private readonly float _noiseScale;
    // private readonly FastNoiseLite _noise; // Noise library dependency is commented out as per spec

    public V1TerrainGenerator(int worldSize = 256, float cellSize = 1.0f, float noiseScale = 0.05f)
    {
        _worldSize = worldSize;
        _cellSize = cellSize;
        _noiseScale = noiseScale;
        // _noise = new FastNoiseLite();
        // _noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
    }

    /// <inheritdoc />
    public WorldData Generate()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Vector3>();
        var indices = new List<int>();

        // Step 1 & 2: Generate Vertices and Colors
        for (int z = 0; z <= _worldSize; z++)
        {
            for (int x = 0; x <= _worldSize; x++)
            {
                // Add vertex position
                vertices.Add(new Vector3(x * _cellSize, 0.0f, z * _cellSize));

                // Add vertex color based on Perlin noise
                // Since we don't have the noise library, we'll use a default color for now.
                // float noiseValue = (_noise.GetNoise(x * _noiseScale, z * _noiseScale) + 1) / 2; // Normalize to 0-1
                // colors.Add(GetColorFromNoise(noiseValue));
                colors.Add(GetColorFromNoise(0.5f)); // Default to "Brown"
            }
        }

        // Step 3: Generate Triangle Indices for a quad grid
        for (int z = 0; z < _worldSize; z++)
        {
            for (int x = 0; x < _worldSize; x++)
            {
                int row1 = z * (_worldSize + 1);
                int row2 = (z + 1) * (_worldSize + 1);

                // Quad vertices
                int v0 = row1 + x;
                int v1 = v0 + 1;
                int v2 = row2 + x;
                int v3 = v2 + 1;

                // First triangle
                indices.Add(v0);
                indices.Add(v2);
                indices.Add(v1);

                // Second triangle
                indices.Add(v1);
                indices.Add(v2);
                indices.Add(v3);
            }
        }

        // Step 4: Package Data
        var terrainRenderMesh = new V1RenderMesh(vertices, colors, indices);
        
        // The PhysicsBody is a placeholder in Core, so we just new it up.
        // The physics service implementation will know what to do with it.
        var terrainPhysicsBody = new PhysicsBody(); 
        
        var navigationGrid = new bool[_worldSize, _worldSize];
        // Initialize the navigation grid to all true (all terrain is navigable)
        for(int x = 0; x < _worldSize; x++)
        {
            for(int z = 0; z < _worldSize; z++)
            {
                navigationGrid[x,z] = true;
            }
        }

        return new WorldData(
            TerrainRenderMesh: terrainRenderMesh,
            TerrainPhysicsBody: terrainPhysicsBody,
            ObstaclePhysicsBodies: new List<PhysicsBody>(), // No obstacles in V1
            NavigationGrid: navigationGrid
        );
    }

    private Vector3 GetColorFromNoise(float noiseValue)
    {
        if (noiseValue < 0.4f) return new Vector3(0.2f, 0.4f, 0.2f); // Green
        if (noiseValue < 0.7f) return new Vector3(0.4f, 0.3f, 0.2f); // Brown
        return new Vector3(0.3f, 0.3f, 0.5f); // Water
    }
} 