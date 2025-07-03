using DroneSim.Core;
using DroneSim.DebugDraw;
using DroneSim.TerrainGenerator;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using OpenTK.Mathematics;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace DroneSim.Renderer;

/// <summary>
/// OpenTK based renderer (minimal stub). Opens a window and clears the screen.
/// </summary>
public class V1OpenTKRenderer : IRenderer, IDisposable
{
    private readonly IFrameTickable _tickable;
    private readonly IRenderDataSource _dataSource;
    private readonly IDebugDrawService _debugDraw;
    private readonly IWorldDataSource? _worldDataSource;
    private GameWindow? _window;
    private bool _disposed = false;

    // --- Ported fields from V1SilkNetRenderer ---
    private int _sceneShaderProgram;
    private int _sceneModelLocation;
    private int _sceneViewLocation;
    private int _sceneProjectionLocation;

    private int _terrainVao;
    private int _terrainVbo;
    private int _terrainEbo;
    private int _terrainColorVbo;
    private int _terrainIndexCount;

    private int _droneVao;
    private int _droneVbo;
    private int _droneEbo;
    private int _droneIndexCount;

    private int _debugShaderProgram;
    private int _debugViewLocation;
    private int _debugProjectionLocation;
    private int _debugVao;
    private int _debugVbo;
    private readonly List<float> _debugBuffer = new();

    private int _hudShaderProgram;
    private int _hudVao;
    private int _hudVbo;
    private int _hudTexture;
    private Image<Rgba32>? _hudImage;
    private Font _hudFont;
    // --- End ported fields ---

    #region Shader Sources
    private const string SceneVertexShader = @"#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

out vec3 vColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    vColor = aColor;
}
";
    private const string SceneFragmentShader = @"#version 330 core
in vec3 vColor;
out vec4 FragColor;

uniform vec3 uOverrideColor;

void main()
{
    vec3 finalColor = (uOverrideColor.r == 0.0 && uOverrideColor.g == 0.0 && uOverrideColor.b == 0.0) ? vColor : uOverrideColor;
    
    float checker = mod(floor(gl_FragCoord.x / 20.0) + floor(gl_FragCoord.y / 20.0), 2.0);
    finalColor *= (1.0 - checker * 0.1);

    FragColor = vec4(finalColor, 1.0);
}
";
    private const string DebugVertexShader = @"#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;

out vec3 vColor;

uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uView * vec4(aPosition, 1.0);
    vColor = aColor;
}
";
    private const string DebugFragmentShader = @"#version 330 core
in vec3 vColor;
out vec4 FragColor;

void main()
{
    FragColor = vec4(vColor, 1.0);
}
";
    private const string HudVertexShader = @"#version 330 core
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoords;

out vec2 vTexCoords;

void main()
{
    vTexCoords = aTexCoords;
    gl_Position = vec4(aPosition.x, aPosition.y, 0.0, 1.0);
}
";
    private const string HudFragmentShader = @"#version 330 core
in vec2 vTexCoords;
out vec4 FragColor;

uniform sampler2D uHudTexture;

void main()
{
    vec4 texColor = texture(uHudTexture, vTexCoords);
    if(texColor.a < 0.1)
        discard;
    FragColor = texColor;
}
";
    #endregion

    private class OpenTKDroneSimInput : IDroneSimInput
    {
        private readonly HashSet<OpenTK.Windowing.GraphicsLibraryFramework.Keys> _pressed = new();
        public void SetKey(OpenTK.Windowing.GraphicsLibraryFramework.Keys key, bool pressed)
        {
            if (pressed) _pressed.Add(key);
            else _pressed.Remove(key);
        }
        public bool Forward => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W) || _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up);
        public bool Backward => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S) || _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down);
        public bool Left => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A) || _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left);
        public bool Right => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D) || _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right);
        public bool Up => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space);
        public bool Down => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift) || _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightShift);
        public bool YawLeft => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Q);
        public bool YawRight => _pressed.Contains(OpenTK.Windowing.GraphicsLibraryFramework.Keys.E);
    }
    private OpenTKDroneSimInput _input = new();

    public V1OpenTKRenderer(IFrameTickable tickable, IRenderDataSource dataSource, IDebugDrawService debugDraw)
    {
        _tickable = tickable ?? throw new ArgumentNullException(nameof(tickable));
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _debugDraw = debugDraw ?? throw new ArgumentNullException(nameof(debugDraw));
        _worldDataSource = dataSource as IWorldDataSource;

        var fontCollection = new FontCollection();
        if (!fontCollection.TryGet("Arial", out var fontFamily))
            if (!fontCollection.TryGet("Verdana", out fontFamily))
                fontFamily = SystemFonts.Families.First(x => x.Name == "Arial");
        if (fontFamily == null) throw new InvalidOperationException("No suitable font found.");
        _hudFont = fontFamily.CreateFont(16, FontStyle.Regular);
    }

    public void Run()
    {
        var nativeSettings = new NativeWindowSettings()
        {
            Size = new OpenTK.Mathematics.Vector2i(1280, 720),
            Title = "DroneSim V1 (OpenTK)",
        };
        _window = new GameWindow(GameWindowSettings.Default, nativeSettings);
        _window.Load += OnLoad;
        _window.RenderFrame += OnRenderFrame;
        _window.UpdateFrame += OnUpdateFrame;
        _window.Closing += OnClose;
        _window.KeyDown += e => _input.SetKey(e.Key, true);
        _window.KeyUp += e => _input.SetKey(e.Key, false);
        _window.Run();
    }

    private void OnLoad()
    {
        _sceneShaderProgram = CompileShaders(SceneVertexShader, SceneFragmentShader);
        _debugShaderProgram = CompileShaders(DebugVertexShader, DebugFragmentShader);
        _hudShaderProgram = CompileShaders(HudVertexShader, HudFragmentShader);

        _sceneModelLocation = GL.GetUniformLocation(_sceneShaderProgram, "uModel");
        _sceneViewLocation = GL.GetUniformLocation(_sceneShaderProgram, "uView");
        _sceneProjectionLocation = GL.GetUniformLocation(_sceneShaderProgram, "uProjection");
        _debugViewLocation = GL.GetUniformLocation(_debugShaderProgram, "uView");
        _debugProjectionLocation = GL.GetUniformLocation(_debugShaderProgram, "uProjection");

        if (_worldDataSource?.GetWorldData()?.TerrainRenderMesh is not null)
        {
            SetupTerrain(_worldDataSource.GetWorldData().TerrainRenderMesh);
        }
        SetupDroneModel();
        SetupDebug();
        SetupHud();

        GL.ClearColor(0.1f, 0.1f, 0.2f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _tickable.Setup();
    }

    private void OnUpdateFrame(FrameEventArgs args)
    {
        _tickable.UpdateFrame((float)args.Time, _input);
    }

    private void OnRenderFrame(FrameEventArgs args)
    {
        if (_disposed) return;
        GL.Clear((ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        var droneStates = _dataSource.GetAllDroneStates();
        if (!droneStates.Any())
        {
            _window?.SwapBuffers();
            return;
        }

        var playerDroneId = _dataSource.GetPlayerControlledDroneId();
        var cameraDroneId = _dataSource.GetCameraAttachedToDroneId();
        var cameraDrone = droneStates.FirstOrDefault(d => d.Id == cameraDroneId);
        if (cameraDrone.Equals(default(DroneSim.Core.DroneState)))
            cameraDrone = droneStates.FirstOrDefault(d => d.Id == playerDroneId);
        if (cameraDrone.Equals(default(DroneSim.Core.DroneState)))
            cameraDrone = droneStates.First();

        var (viewMatrix, projectionMatrix) = CalculateCameraMatrices(cameraDrone, _dataSource.GetCameraViewMode(), _dataSource.GetCameraTilt());

        RenderTerrain(viewMatrix, projectionMatrix);
        RenderDrones(droneStates, playerDroneId, viewMatrix, projectionMatrix);
        if (_dataSource.IsDebugDrawingEnabled())
        {
            RenderDebugShapes(viewMatrix, projectionMatrix);
        }
        RenderHud();

        _window?.SwapBuffers();
    }

    private void OnClose(System.ComponentModel.CancelEventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _window?.Close();
        _window?.Dispose();
        _disposed = true;
    }

    private int CompileShaders(string vertexSource, string fragmentSource)
    {
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);
        GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vStatus);
        if (vStatus != (int)All.True)
            throw new Exception("Vertex shader failed to compile: " + GL.GetShaderInfoLog(vertexShader));

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);
        GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fStatus);
        if (fStatus != (int)All.True)
            throw new Exception("Fragment shader failed to compile: " + GL.GetShaderInfoLog(fragmentShader));

        int shaderProgram = GL.CreateProgram();
        GL.AttachShader(shaderProgram, vertexShader);
        GL.AttachShader(shaderProgram, fragmentShader);
        GL.LinkProgram(shaderProgram);
        GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int pStatus);
        if (pStatus != (int)All.True)
            throw new Exception("Shader program failed to link: " + GL.GetProgramInfoLog(shaderProgram));

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        return shaderProgram;
    }

    private void SetupTerrain(object mesh)
    {
        if (mesh is not DroneSim.TerrainGenerator.V1RenderMesh v1Mesh || v1Mesh.Vertices.Count == 0) return;
        _terrainIndexCount = v1Mesh.Indices.Count;
        _terrainVao = GL.GenVertexArray();
        GL.BindVertexArray(_terrainVao);

        _terrainVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _terrainVbo);
        var terrainVerts = v1Mesh.Vertices.SelectMany(v => new[] { v.X, v.Y, v.Z }).ToArray();
        GL.BufferData(BufferTarget.ArrayBuffer, terrainVerts.Length * sizeof(float), terrainVerts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        _terrainColorVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _terrainColorVbo);
        var terrainColors = v1Mesh.Colors.SelectMany(c => new[] { c.X, c.Y, c.Z }).ToArray();
        GL.BufferData(BufferTarget.ArrayBuffer, terrainColors.Length * sizeof(float), terrainColors, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);

        _terrainEbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _terrainEbo);
        var indices = v1Mesh.Indices.ToArray();
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.StaticDraw);

        GL.BindVertexArray(0);
    }

    private void SetupDroneModel()
    {
        float[] vertices = { -0.5f,-0.5f,-0.5f, 0.8f,0.8f,0.1f,  0.5f,-0.5f,-0.5f, 0.8f,0.8f,0.1f,
                             0.5f, 0.5f,-0.5f, 0.8f,0.8f,0.1f, -0.5f, 0.5f,-0.5f, 0.8f,0.8f,0.1f,
                            -0.5f,-0.5f, 0.5f, 0.8f,0.8f,0.1f,  0.5f,-0.5f, 0.5f, 0.8f,0.8f,0.1f,
                             0.5f, 0.5f, 0.5f, 0.8f,0.8f,0.1f, -0.5f, 0.5f, 0.5f, 0.8f,0.8f,0.1f };
        uint[] indices = { 0,1,2, 2,3,0, 4,5,1, 1,0,4, 7,6,5, 5,4,7, 3,2,6, 6,7,3, 1,5,6, 6,2,1, 4,0,3, 3,7,4 };
        _droneIndexCount = indices.Length;

        _droneVao = GL.GenVertexArray();
        GL.BindVertexArray(_droneVao);

        _droneVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _droneVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        _droneEbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _droneEbo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);

        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        GL.BindVertexArray(0);
    }

    private void SetupDebug()
    {
        _debugVao = GL.GenVertexArray();
        GL.BindVertexArray(_debugVao);
        _debugVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _debugVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 0, IntPtr.Zero, BufferUsageHint.StreamDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);
    }

    private void SetupHud()
    {
        if (_window == null) return;
        _hudVao = GL.GenVertexArray();
        GL.BindVertexArray(_hudVao);
        _hudVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _hudVbo);

        float[] quadVertices = { -1f,-1f,0f,1f,  // Bottom-left vertex, tex coord (0,1)
                                  1f,-1f,1f,1f,  // Bottom-right vertex, tex coord (1,1)
                                  1f,1f,1f,0f,  // Top-right vertex, tex coord (1,0)
                                 -1f,1f,0f,0f }; // Top-left vertex, tex coord (0,0)
        GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);
        GL.BindVertexArray(0);

        var (width, height) = (_window.Size.X, _window.Size.Y);
        _hudImage = new Image<Rgba32>(width, height);
        _hudTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _hudTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
    }

    private (System.Numerics.Matrix4x4 view, System.Numerics.Matrix4x4 projection) CalculateCameraMatrices(DroneSim.Core.DroneState targetDrone, CameraViewMode mode, float tilt)
    {
        if (_window == null) return (System.Numerics.Matrix4x4.Identity, System.Numerics.Matrix4x4.Identity);
        var projectionMatrix = System.Numerics.Matrix4x4.CreatePerspectiveFieldOfView((float)(Math.PI / 180f * 75f), (float)_window.Size.X / _window.Size.Y, 0.1f, 1000f);
        var dronePosition = targetDrone.Position;
        var droneOrientation = targetDrone.Orientation;
        var forward = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, 0, 1), droneOrientation);
        var up = System.Numerics.Vector3.Transform(new System.Numerics.Vector3(0, 1, 0), droneOrientation);
        System.Numerics.Vector3 cameraPosition, cameraTarget;
        if (mode == CameraViewMode.FirstPerson)
        {
            cameraPosition = dronePosition + forward * 0.5f + up * 0.3f;
            cameraTarget = cameraPosition + forward;
        }
        else
        {
            cameraPosition = dronePosition - forward * 8f + up * 3f;
            cameraTarget = dronePosition;
        }
        var tiltRotation = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitX, droneOrientation), -tilt);
        var finalUp = System.Numerics.Vector3.Transform(up, tiltRotation);
        var viewMatrix = System.Numerics.Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, finalUp);
        return (viewMatrix, projectionMatrix);
    }

    private static OpenTK.Mathematics.Matrix4 ToOpenTKMatrix4(System.Numerics.Matrix4x4 m)
    {
        return new OpenTK.Mathematics.Matrix4(
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44
        );
    }

    private void RenderTerrain(System.Numerics.Matrix4x4 view, System.Numerics.Matrix4x4 projection)
    {
        if (_terrainVao == 0) return;
        GL.UseProgram(_sceneShaderProgram);
        var viewMat = ToOpenTKMatrix4(view);
        var projMat = ToOpenTKMatrix4(projection);
        var modelMat = OpenTK.Mathematics.Matrix4.Identity;
        GL.UniformMatrix4(_sceneViewLocation, false, ref viewMat);
        GL.UniformMatrix4(_sceneProjectionLocation, false, ref projMat);
        GL.UniformMatrix4(_sceneModelLocation, false, ref modelMat);
        GL.BindVertexArray(_terrainVao);
        GL.DrawElements(PrimitiveType.Triangles, _terrainIndexCount, DrawElementsType.UnsignedInt, 0);
        GL.BindVertexArray(0);
    }

    private void RenderDrones(IReadOnlyList<DroneSim.Core.DroneState> droneStates, int playerDroneId, System.Numerics.Matrix4x4 view, System.Numerics.Matrix4x4 projection)
    {
        if (_droneVao == 0) return;
        GL.UseProgram(_sceneShaderProgram);
        var viewMat = ToOpenTKMatrix4(view);
        var projMat = ToOpenTKMatrix4(projection);
        GL.UniformMatrix4(_sceneViewLocation, false, ref viewMat);
        GL.UniformMatrix4(_sceneProjectionLocation, false, ref projMat);
        GL.BindVertexArray(_droneVao);
        var body = (scale: new System.Numerics.Vector3(0.4f, 0.2f, 1.0f), offset: System.Numerics.Vector3.Zero, color: new System.Numerics.Vector3(0.8f, 0.8f, 0.1f));
        var arm1 = (scale: new System.Numerics.Vector3(1.2f, 0.1f, 0.2f), offset: System.Numerics.Vector3.Zero, color: new System.Numerics.Vector3(0.6f, 0.6f, 0.6f));
        var arm2 = (scale: new System.Numerics.Vector3(0.2f, 0.1f, 1.2f), offset: System.Numerics.Vector3.Zero, color: new System.Numerics.Vector3(0.6f, 0.6f, 0.6f));
        var playerColor = new System.Numerics.Vector3(0.1f, 0.9f, 0.2f);
        int colorLocation = GL.GetUniformLocation(_sceneShaderProgram, "uOverrideColor");
        foreach (var drone in droneStates)
        {
            var droneMatrix = System.Numerics.Matrix4x4.CreateFromQuaternion(drone.Orientation) * System.Numerics.Matrix4x4.CreateTranslation(drone.Position);
            var bodyModelMatrix = System.Numerics.Matrix4x4.CreateScale(body.scale) * System.Numerics.Matrix4x4.CreateTranslation(body.offset) * droneMatrix;
            var bodyMat = ToOpenTKMatrix4(bodyModelMatrix);
            GL.UniformMatrix4(_sceneModelLocation, false, ref bodyMat);
            var color = drone.Id == playerDroneId ? playerColor : body.color;
            GL.Uniform3(colorLocation, color.X, color.Y, color.Z);
            GL.DrawElements(PrimitiveType.Triangles, _droneIndexCount, DrawElementsType.UnsignedInt, 0);
            var arm1ModelMatrix = System.Numerics.Matrix4x4.CreateScale(arm1.scale) * System.Numerics.Matrix4x4.CreateTranslation(arm1.offset) * droneMatrix;
            var arm1Mat = ToOpenTKMatrix4(arm1ModelMatrix);
            GL.UniformMatrix4(_sceneModelLocation, false, ref arm1Mat);
            GL.Uniform3(colorLocation, arm1.color.X, arm1.color.Y, arm1.color.Z);
            GL.DrawElements(PrimitiveType.Triangles, _droneIndexCount, DrawElementsType.UnsignedInt, 0);
            var arm2ModelMatrix = System.Numerics.Matrix4x4.CreateScale(arm2.scale) * System.Numerics.Matrix4x4.CreateTranslation(arm2.offset) * droneMatrix;
            var arm2Mat = ToOpenTKMatrix4(arm2ModelMatrix);
            GL.UniformMatrix4(_sceneModelLocation, false, ref arm2Mat);
            GL.Uniform3(colorLocation, arm2.color.X, arm2.color.Y, arm2.color.Z);
            GL.DrawElements(PrimitiveType.Triangles, _droneIndexCount, DrawElementsType.UnsignedInt, 0);
        }
        GL.BindVertexArray(0);
    }

    private void RenderDebugShapes(System.Numerics.Matrix4x4 view, System.Numerics.Matrix4x4 projection)
    {
        var shapes = _debugDraw.GetShapesToRender();
        if (!shapes.Any()) return;
        _debugBuffer.Clear();
        int lineVertexCount = 0;
        int pointVertexCount = 0;
        foreach (var shape in shapes)
        {
            if (shape is DroneSim.DebugDraw.DebugLine line)
            {
                _debugBuffer.AddRange(new[] { line.Start.X, line.Start.Y, line.Start.Z, line.Color.R / 255f, line.Color.G / 255f, line.Color.B / 255f });
                _debugBuffer.AddRange(new[] { line.End.X, line.End.Y, line.End.Z, line.Color.R / 255f, line.Color.G / 255f, line.Color.B / 255f });
                lineVertexCount += 2;
            }
            else if (shape is DroneSim.DebugDraw.DebugPoint point)
            {
                _debugBuffer.AddRange(new[] { point.Position.X, point.Position.Y, point.Position.Z, point.Color.R / 255f, point.Color.G / 255f, point.Color.B / 255f });
                pointVertexCount += 1;
            }
        }
        if (!_debugBuffer.Any()) return;
        GL.UseProgram(_debugShaderProgram);
        var viewMat = ToOpenTKMatrix4(view);
        var projMat = ToOpenTKMatrix4(projection);
        GL.UniformMatrix4(_debugViewLocation, false, ref viewMat);
        GL.UniformMatrix4(_debugProjectionLocation, false, ref projMat);
        GL.BindVertexArray(_debugVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _debugVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, _debugBuffer.Count * sizeof(float), _debugBuffer.ToArray(), BufferUsageHint.StreamDraw);
        if (lineVertexCount > 0)
        {
            GL.DrawArrays(PrimitiveType.Lines, 0, lineVertexCount);
        }
        if (pointVertexCount > 0)
        {
            GL.PointSize(10f);
            GL.DrawArrays(PrimitiveType.Points, lineVertexCount, pointVertexCount);
        }
        GL.BindVertexArray(0);
    }

    private void RenderHud()
    {
        if (_hudImage == null || _window == null) return;
        using (var hudImage = new Image<Rgba32>(_window.Size.X, _window.Size.Y))
        {
            string hudText = _dataSource.GetHudInfo();
            hudImage.Mutate(ctx =>
            {
                ctx.Fill(SixLabors.ImageSharp.Color.Transparent);
                ctx.DrawText(hudText, _hudFont, SixLabors.ImageSharp.Color.White, new SixLabors.ImageSharp.PointF(10, 10));
            });
            hudImage.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    unsafe
                    {
                        fixed (void* p = row)
                        {
                            GL.BindTexture(TextureTarget.Texture2D, _hudTexture);
                            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y, accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)p);
                        }
                    }
                }
            });
        }
        GL.Disable(EnableCap.DepthTest);
        GL.UseProgram(_hudShaderProgram);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _hudTexture);
        GL.BindVertexArray(_hudVao);
        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        GL.BindVertexArray(0);
        GL.Enable(EnableCap.DepthTest);
    }
} 