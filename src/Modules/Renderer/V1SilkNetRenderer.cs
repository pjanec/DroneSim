// In project: DroneSim.Renderer
using DroneSim.Core;
using DroneSim.TerrainGenerator; // To access V1RenderMesh
using DroneSim.DebugDraw; // To access DebugShape types
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System;
using System.Numerics;
using System.Drawing;
using System.Collections.Generic;

namespace DroneSim.Renderer;

/// <summary>
/// V1 implementation of the IRenderer interface using Silk.NET and OpenGL.
/// This class is responsible for creating the window, driving the main application loop,
/// and rendering the entire simulation state.
/// </summary>
public class V1SilkNetRenderer : IRenderer
{
    private readonly IFrameTickable _tickable;
    private readonly IRenderDataSource _dataSource;
    private readonly IDebugDrawService _debugDraw;
    private readonly IWorldDataSource? _worldDataSource; // Can be null if not provided

    private IWindow? _window;
    private GL? _gl;

    // Placeholder fields for OpenGL objects
    private uint _terrainVao;
    private uint _terrainVbo;
    private uint _terrainEbo;
    private uint _terrainColorVbo;
    private uint _shaderProgram;
    private int _viewLocation;
    private int _projectionLocation;
    
    private int _terrainIndexCount;


    public V1SilkNetRenderer(IFrameTickable tickable, IRenderDataSource dataSource, IDebugDrawService debugDraw)
    {
        _tickable = tickable ?? throw new ArgumentNullException(nameof(tickable));
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _debugDraw = debugDraw ?? throw new ArgumentNullException(nameof(debugDraw));

        // The dataSource might also be a world data source.
        _worldDataSource = dataSource as IWorldDataSource;
    }

    /// <inheritdoc />
    public void Run()
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(1280, 720);
        options.Title = "DroneSim V1";
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3));

        _window = Window.Create(options);

        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;

        _window.Run();
    }

    private void OnLoad()
    {
        _gl = _window?.CreateOpenGL() ?? throw new InvalidOperationException("Could not create OpenGL context.");

        // Inform the orchestrator to set up the world
        _tickable.Setup();

        // Now that the world is set up, get the terrain data
        if (_worldDataSource != null)
        {
            var worldData = _worldDataSource.GetWorldData();
            if (worldData?.TerrainRenderMesh is V1RenderMesh terrainMesh)
            {
                SetupTerrain(terrainMesh);
            }
        }
        else
        {
            // Handle case where the data source doesn't provide world data.
            // In a real app, this might log a warning.
        }
        
        // TODO: Compile shaders, setup drone models, HUD textures, etc.
        // For now, just set a clear color.
        _gl.ClearColor(Color.CornflowerBlue);
    }
    
    private void SetupTerrain(V1RenderMesh mesh)
    {
        if (_gl == null) return;

        _terrainIndexCount = mesh.Indices.Count;
        
        // Placeholder for setting up VAO, VBOs for terrain
        // In a real implementation:
        // 1. gl.GenVertexArrays, gl.BindVertexArray
        // 2. gl.GenBuffers, gl.BindBuffer (for vertices)
        // 3. gl.BufferData (with mesh.Vertices)
        // 4. gl.VertexAttribPointer, gl.EnableVertexAttribArray
        // 5. Repeat for colors
        // 6. Repeat for indices (EBO)
        // 7. Unbind VAO
    }


    private void OnUpdate(double deltaTime)
    {
        // Update the simulation state via the orchestrator
        _tickable.UpdateFrame((float)deltaTime);
    }

    private void OnRender(double deltaTime)
    {
        if (_gl == null) return;
        
        _gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

        // 1. Get all simulation data from _dataSource.
        var droneStates = _dataSource.GetAllDroneStates();
        var isDebugEnabled = _dataSource.IsDebugDrawingEnabled();

        // 2. Calculate camera matrices based on data from _dataSource
        // Matrix4x4 view = ...
        // Matrix4x4 projection = ...

        // 3. Draw main scene (terrain, drones).
        // gl.UseProgram(_shaderProgram);
        // gl.UniformMatrix4(_viewLocation, 1, false, ref view);
        // gl.UniformMatrix4(_projectionLocation, 1, false, ref projection);
        
        // Draw Terrain
        // gl.BindVertexArray(_terrainVao);
        // gl.DrawElements(PrimitiveType.Triangles, (uint)_terrainIndexCount, DrawElementsType.UnsignedInt, null);

        // Draw Drones
        foreach(var drone in droneStates)
        {
            // Calculate model matrix for each drone
            // Draw drone model
        }

        // 4. If debug drawing is enabled, draw debug shapes from _debugDraw service.
        if (isDebugEnabled)
        {
            var shapes = _debugDraw.GetShapesToRender();
            // Loop through shapes, cast them to their concrete types (DebugLine, etc.)
            // and use a simple shader to draw them with GL_LINES, GL_POINTS
        }

        // 5. Update and draw HUD texture.

        // 6. Swap buffers is handled by Silk.NET's Run loop
    }
    
    private void OnClose()
    {
        // Dispose of OpenGL resources
        // gl.DeleteBuffers, gl.DeleteVertexArrays, gl.DeleteProgram, etc.
    }
} 