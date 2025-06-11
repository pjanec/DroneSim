Document ID: MODSPEC-TERRAIN-V1.2

Date: June 10, 2025

Title: V1.2 Detailed ITerrainGenerator Module Specification (Implementing Class: V1TerrainGenerator)

**1. Overview**

This document provides the detailed implementation specification for the V1 ITerrainGenerator interface, V1TerrainGenerator. Its purpose is to create the complete world environment, which for V1 includes the **geometry** (a flat plane) and the **vertex color data** used to render the \"biome\" and checkerboard patterns.

**2. Dependencies**

- **DroneSim.Core:** For the ITerrainGenerator interface and WorldData structure.

- **A Noise Library:** A 2D Simplex or Perlin noise generation library is required. A lightweight, modern C# library like **FastNoiseLite** is recommended for this purpose.

**3. V1 Functional Specification**

The Generate() method will execute a multi-step process to build the final WorldData object.

- **3.1. Vertex and Color Generation**

  - The method will create two lists: List\<Vector3\> vertices and List\<Vector3\> colors.

  - It will iterate through a grid from x = 0 to worldSize and z = 0 to worldSize.

  - For each (x, z) coordinate pair:

    1.  A vertex position is created at new Vector3(x \* cellSize, 0.0f, z \* cellSize) and added to the vertices list.

    2.  A noise value is calculated: noiseValue = noise.GetNoise(x \* noiseScale, z \* noiseScale). This value is normalized to the \[0, 1\] range.

    3.  The noiseValue is mapped to a biome color using a gradient logic and added to the colors list:

        - If noiseValue \< 0.4f, use Green (RGB(0.2, 0.4, 0.2)).

        - If noiseValue \< 0.7f, use Brown (RGB(0.4, 0.3, 0.2)).

        - Otherwise, use Blue/Grey for water (RGB(0.3, 0.3, 0.5)).

- **3.2. Index Generation (Triangulation)**

  - The method will create a List\<int\> indices.

  - It will iterate through the grid of quads, from x = 0 to worldSize - 1 and z = 0 to worldSize - 1.

  - For each quad, it will calculate the indices of its four corner vertices (v0, v1, v2, v3) and add six integers to the indices list to define the two triangles that form the quad (e.g., v0, v1, v2 and v2, v1, v3).

- **3.3. Data Packaging**

  - The generated vertices, colors, and indices lists are used to create the RenderMesh object. The concrete structure of RenderMesh will be a class containing these lists, which the Renderer will know how to process.

  - A simple plane PhysicsBody is created.

  - An all-true NavigationGrid of size worldSize x worldSize is created.

  - These components are assembled into the final WorldData object and returned.

**4. Configuration & Parameters**

  ----------------------------------------------------------------------------------------------------------------------------------
  **Parameter**           **V1 Value**            **Description**
  ----------------------- ----------------------- ----------------------------------------------------------------------------------
  worldSize               256                     The number of cells along the X and Z axes.

  cellSize                1.0f                    The size of each cell in world units (meters).

  noiseScale              0.05f                   Scale factor for the noise function. Smaller values create larger biome patches.
  ----------------------------------------------------------------------------------------------------------------------------------

**5. Code Skeleton**

// In project: DroneSim.TerrainGenerator\
using DroneSim.Core;\
using System.Numerics;\
// using FastNoiseLite; // Example noise library\
\
/// \<summary\>\
/// V1 implementation of the terrain generator.\
/// Creates a flat plane with procedural vertex coloring for biomes.\
/// \</summary\>\
public class V1TerrainGenerator : ITerrainGenerator\
{\
private readonly int \_worldSize;\
private readonly float \_cellSize;\
private readonly float \_noiseScale;\
// private readonly FastNoiseLite \_noise;\
\
public V1TerrainGenerator(int worldSize = 256, float cellSize = 1.0f, float noiseScale = 0.05f)\
{\
\_worldSize = worldSize;\
\_cellSize = cellSize;\
\_noiseScale = noiseScale;\
// \_noise = new FastNoiseLite();\
// \_noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);\
}\
\
public WorldData Generate()\
{\
var vertices = new List\<Vector3\>();\
var colors = new List\<Vector3\>();\
var indices = new List\<int\>();\
\
// Step 1 & 2: Generate Vertices and Colors\
for (int z = 0; z \<= \_worldSize; z++)\
{\
for (int x = 0; x \<= \_worldSize; x++)\
{\
// Add vertex position\
vertices.Add(new Vector3(x \* \_cellSize, 0.0f, z \* \_cellSize));\
\
// Add vertex color based on Perlin noise\
// float noiseValue = (\_noise.GetNoise(x \* \_noiseScale, z \* \_noiseScale) + 1) / 2; // Normalize to 0-1\
// colors.Add(GetColorFromNoise(noiseValue));\
}\
}\
\
// Step 3: Generate Triangle Indices\
for (int z = 0; z \< \_worldSize; z++)\
{\
for (int x = 0; x \< \_worldSize; x++)\
{\
int v0 = (z \* (\_worldSize + 1)) + x;\
int v1 = v0 + 1;\
int v2 = v0 + (\_worldSize + 1);\
int v3 = v2 + 1;\
\
indices.AddRange(new int\[\] { v2, v1, v0, v2, v3, v1 });\
}\
}\
\
// Step 4: Package Data\
// The concrete RenderMesh class will be defined to hold these lists.\
var terrainRenderMesh = new RenderMesh(/\* vertices, colors, indices \*/);\
var terrainPhysicsBody = new PhysicsBody(/\* Simple plane collider data \*/);\
var navigationGrid = new bool\[\_worldSize, \_worldSize\]; // Assumes default init to false, needs to be set to true\
// \... (code to set nav grid to all true) \...\
\
return new WorldData(\
TerrainRenderMesh: terrainRenderMesh,\
TerrainPhysicsBody: terrainPhysicsBody,\
ObstaclePhysicsBodies: new List\<PhysicsBody\>(),\
NavigationGrid: navigationGrid\
);\
}\
\
private Vector3 GetColorFromNoise(float noiseValue)\
{\
if (noiseValue \< 0.4f) return new Vector3(0.2f, 0.4f, 0.2f); // Green\
if (noiseValue \< 0.7f) return new Vector3(0.4f, 0.3f, 0.2f); // Brown\
return new Vector3(0.3f, 0.3f, 0.5f); // Water\
}\
}
