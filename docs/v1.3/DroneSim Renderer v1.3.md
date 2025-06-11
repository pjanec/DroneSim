Document ID: MODSPEC-RENDER3D-V1.3

Date: June 11, 2025

Title: V1.3 Detailed IRenderer Module Specification (Implementing Class: V1SilkNetRenderer)

**1. Overview**

This document provides the detailed implementation specification for the V1 IRenderer interface, named V1SilkNetRenderer. This module provides a full, real-time 3D visual representation of the simulation using Silk.NET and OpenGL.

Crucially, it **drives the main application loop**. It is decoupled from the core simulation logic via the IFrameTickable and IRenderDataSource interfaces, which it uses to update and draw the world state each frame.

**2. Dependencies**

- **DroneSim.Core:** For the IRenderer, IFrameTickable, IRenderDataSource, and IDebugDrawService interfaces.

- **Silk.NET** (specifically OpenGL, Windowing, Input): For window creation, input handling, and graphics API calls.

- **ImageSharp.Drawing** (or similar): For rendering HUD text to a bitmap in memory.

**3. V1 Functional Specification**

- **3.1. Window and Graphics Setup**

  - The class constructor will initialize a Silk.NET window with a resolution of **1280x720** and the title \"DroneSim V1\".

  - It will initialize an **OpenGL 3.3 Core Profile** graphics context.

  - During its OnLoad event, it will set up the rendering pipeline: compile shader programs (one for main scene, one for debug drawing), create Vertex Array Objects (VAOs), and configure global OpenGL states (e.g., depth testing, blending).

- **3.2. Main Loop Driver**

  - The public Run() method starts the blocking Silk.NET window loop.

  - The OnLoad event handler calls \_tickable.Setup() once.

  - The OnUpdate event handler calls \_tickable.UpdateFrame(deltaTime) every frame to progress the simulation logic.

  - The OnRender event handler calls methods on \_dataSource to get the latest state and draws the entire scene.

- **3.3. Terrain Rendering**

  - The renderer receives the terrain\'s RenderMesh during setup. It uploads the vertex position and procedural color data to the GPU.

  - It uses a shader that displays the interpolated vertex colors (the \"biomes\") and applies a subtle darkening effect in a checkerboard pattern based on world position to enhance the sense of motion.

- **3.4. Drone Rendering**

  - The renderer will have a pre-defined VAO for a unit cube.

  - For each drone, it will draw the cube multiple times with different transformations (scale, position) and colors to form a composite model (a central body and four arms).

  - The player drone\'s body will be colored distinctly from AI drones.

- **3.5. Camera**

  - The renderer maintains the View and Projection matrices.

  - The **Projection** matrix is a perspective matrix with a 75-degree FOV.

  - The **View** matrix is calculated each frame based on the camera\'s attachment ID, position, orientation, and the current camera mode and tilt, all queried from the IRenderDataSource.

- **3.6. Debug Drawing (Optional)**

  - The renderer checks the \_dataSource.IsDebugDrawingEnabled() flag each frame.

  - If true, it enters a debug drawing pass after rendering the main scene.

  - It calls \_debugDrawService.GetShapesToRender() to get a list of all lines, vectors, and points for the current frame.

  - It uses a separate, simple shader to draw these primitives using GL_LINES and GL_POINTS.

- **3.7. HUD Rendering**

  - The renderer receives the hudInfo string from the IRenderDataSource. If the string has changed, it re-renders the text to an in-memory bitmap and uploads it to an OpenGL texture.

  - At the end of the frame, it switches to an orthographic projection and draws a textured quad in a screen corner to display the HUD.

**4. Code Skeleton**

// In project: DroneSim.Renderer\
using DroneSim.Core;\
using Silk.NET.Windowing;\
using Silk.NET.OpenGL;\
// \... other usings \...\
\
public class V1SilkNetRenderer : IRenderer\
{\
private readonly IFrameTickable \_tickable;\
private readonly IRenderDataSource \_dataSource;\
private readonly IDebugDrawService \_debugDraw;\
private IWindow \_window;\
private GL \_gl;\
// \... private fields for OpenGL handles, shaders, etc. \...\
\
public V1SilkNetRenderer(\
IFrameTickable tickable,\
IRenderDataSource dataSource,\
IDebugDrawService debugDraw)\
{\
\_tickable = tickable;\
\_dataSource = dataSource;\
\_debugDraw = debugDraw;\
// Window creation and event subscription (OnLoad, OnUpdate, OnRender, OnClose)\
}\
\
private void OnLoad()\
{\
// \... get keyboard state and pass to IPlayerInput \...\
// \... GL and resource setup (compile shaders, create VAOs/VBOs) \...\
\_tickable.Setup();\
}\
\
private void OnUpdate(double deltaTime)\
{\
// Update the simulation state via the orchestrator\
\_tickable.UpdateFrame((float)deltaTime);\
}\
\
private void OnRender(double deltaTime)\
{\
// 1. Get all simulation data from \_dataSource.\
// 2. Clear screen, calculate camera matrices.\
// 3. Draw main scene (terrain, drones).\
// 4. If debug drawing is enabled, draw debug shapes from \_debugDraw service.\
// 5. Update and draw HUD texture.\
// 6. Swap buffers.\
}\
\
// This is the main entry point called by the application host\
public void Run() =\> \_window.Run();\
}
