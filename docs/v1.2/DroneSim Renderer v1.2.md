Document ID: MODSPEC-RENDER3D-V1.2

Date: June 10, 2025

Title: V1.2 Detailed IRenderer 3D Module Specification (Implementing Class: V1SilkNetRenderer)

**1. Overview**

This document provides the detailed implementation specification for the V1 3D renderer, V1SilkNetRenderer. It expands upon the high-level architecture to include concrete details on geometry, coloring, rendering techniques, and user interface content, providing a complete guide for implementation.

**2. Terrain Rendering: Checkerboard & Biomes**

The terrain is a flat plane but will be rendered with visual detail to provide a sense of speed, altitude, and location.

- **Geometry:** The TerrainGenerator will create a single RenderMesh for the terrain. It will be a grid of vertices with dimensions (worldSize + 1) x (worldSize + 1). For each quad in the grid, two triangles will be defined.

- **Color Generation (The \"Biome\" Effect):**

  - This logic resides in the **TerrainGenerator**. For each vertex it creates, it will also calculate a color.

  - **Algorithm:** A 2D Simplex or Perlin noise function will be used. The (x, z) position of each vertex is used as input to the noise function (noise(x \* noiseScale, z \* noiseScale)).

  - **Color Mapping:** The returned noise value (typically between -1 and 1, normalized to 0-1) will be used to interpolate between biome colors. For example:

    - 0.0 - 0.4: Maps to a shade of Green (RGB(0.2, 0.4, 0.2)).

    - 0.4 - 0.7: Maps to a shade of Brown (RGB(0.4, 0.3, 0.2)).

    - 0.7 - 1.0: Maps to a shade of Grey/Blue for water (RGB(0.3, 0.3, 0.5)).

  - The resulting color is stored as a per-vertex attribute in the RenderMesh. The renderer simply displays these vertex colors, which will be smoothly interpolated across the triangles by the GPU, creating large, organic patches of color.

- **Checkerboard Pattern:**

  - To enhance the sense of motion, a subtle checkerboard pattern is overlaid on the biome colors.

  - This logic resides in the **Renderer\'s fragment shader**.

  - **Algorithm:** After receiving the interpolated biome color, the shader will check the vertex\'s world position. If (floor(worldPos.x) + floor(worldPos.z)) % 2 == 0, it will multiply the biome color by a slight darkening factor (e.g., 0.95). This creates the checkerboard effect without requiring extra geometry.

**3. Drone Representation (Set of Boxes)**

Each drone will be rendered as a simple, multi-part model to give it a clear shape and orientation.

- **Geometry:** The renderer will have a single VAO/VBO for a unit cube (1x1x1). It will draw this cube multiple times per drone, applying different transformations and colors for each part.

- **Composition:**

  1.  **Central Body:** A single cube scaled to **1.0m wide (X), 0.5m tall (Y), 1.0m long (Z)**.

  2.  **Four Arms:** Four cuboids scaled to **0.2m wide, 0.2m tall, 1.5m long**.

- **Assembly:** The parts are positioned relative to the drone\'s center (drone.Position).

  - The Body is centered at the drone\'s position.

  - The arms are translated from the center and then rotated. For example:

    - Front-Right Arm: Translate (0.75, 0, 0.75), then rotate 45 degrees.

    - Front-Left Arm: Translate (-0.75, 0, 0.75), then rotate -45 degrees.

    - (And similarly for the back arms).

- **Coloring:**

  - **Player Drone:** The body will be rendered in a bright Red (1.0, 0.2, 0.2).

  - **AI Drones:** The bodies will be rendered in a distinct Blue (0.2, 0.5, 1.0).

  - **Arms:** All arms on all drones will be a neutral Grey (0.5, 0.5, 0.5).

**4. Heads-Up Display (HUD) Content**

The Orchestrator will be responsible for creating a multi-line string with the following content, which the Renderer will then display. Placeholders {\...} denote dynamic data.

DroneSim V1\
\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\-\--\
Speed: {speed:F1} m/s \| Altitude: {altitude:F1} m\
Throttle: {throttle_step}/10\
\
View: {camera_mode} \| Tilt: {tilt_angle:F0}°\
Attached To: Drone {camera_drone_id}\
CONTROLLING: Drone {player_drone_id} \<\-- (This line only appears if player_id == camera_id)\
\
\[P\] Possess \| \[T\] Cycle Drone \| \[C\] Cycle View

**5. Shader Program (GLSL Code)**

This is the minimal shader code required to implement the specified rendering.

**Vertex Shader (shader.vert)**

#version 330 core\
layout (location = 0) in vec3 aPosition;\
layout (location = 1) in vec3 aColor;\
\
// Uniforms set from C# code\
uniform mat4 uModel;\
uniform mat4 uView;\
uniform mat4 uProjection;\
\
// Pass-through to fragment shader\
out vec3 fColor;\
out vec3 fWorldPos;\
\
void main()\
{\
gl_Position = uProjection \* uView \* uModel \* vec4(aPosition, 1.0);\
fColor = aColor;\
fWorldPos = (uModel \* vec4(aPosition, 1.0)).xyz;\
}

**Fragment Shader (shader.frag)**

#version 330 core\
out vec4 FragColor;\
\
in vec3 fColor;\
in vec3 fWorldPos; // World position from vertex shader\
\
// Uniform for drone part color (body vs arms)\
uniform vec3 uObjectColor;\
// Uniform to tell the shader if it should use vertex color (terrain)\
// or object color (drone).\
uniform bool uUseObjectColor;\
\
void main()\
{\
vec3 color = uUseObjectColor ? uObjectColor : fColor;\
\
// Apply checkerboard darkening for terrain\
if (!uUseObjectColor) {\
if (mod(floor(fWorldPos.x) + floor(fWorldPos.z), 2.0) == 0.0) {\
color \*= 0.95;\
}\
}\
\
FragColor = vec4(color, 1.0);\
}
