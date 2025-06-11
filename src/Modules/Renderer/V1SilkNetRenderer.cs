using DroneSim.Core;
using DroneSim.TerrainGenerator;
using DroneSim.DebugDraw;
using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.CompilerServices;

namespace DroneSim.Renderer;

public class V1SilkNetRenderer : IRenderer, IDisposable
{
    private readonly IFrameTickable _tickable;
    private readonly IRenderDataSource _dataSource;
    private readonly IDebugDrawService _debugDraw;
    private readonly IWorldDataSource? _worldDataSource;

    private IWindow? _window;
    private GL? _gl;

    private uint _sceneShaderProgram;
    private int _sceneModelLocation;
    private int _sceneViewLocation;
    private int _sceneProjectionLocation;

    private uint _terrainVao;
    private uint _terrainVbo;
    private uint _terrainEbo;
    private uint _terrainColorVbo;
    private int _terrainIndexCount;

    private uint _droneVao;
    private uint _droneVbo;
    private uint _droneEbo;
    private int _droneIndexCount;

    private uint _debugShaderProgram;
    private int _debugViewLocation;
    private int _debugProjectionLocation;
    private uint _debugVao;
    private uint _debugVbo;
    private readonly List<float> _debugBuffer = new();

    private uint _hudShaderProgram;
    private uint _hudVao;
    private uint _hudVbo;
    private uint _hudTexture;
    private Image<Rgba32>? _hudImage;
    private Font _hudFont;

    private bool _disposed = false;

    public V1SilkNetRenderer(IFrameTickable tickable, IRenderDataSource dataSource, IDebugDrawService debugDraw)
    {
        _tickable = tickable ?? throw new ArgumentNullException(nameof(tickable));
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _debugDraw = debugDraw ?? throw new ArgumentNullException(nameof(debugDraw));
        _worldDataSource = dataSource as IWorldDataSource;

        var fontCollection = new FontCollection();
        if (!fontCollection.TryGet("Arial", out var fontFamily))
            if(!fontCollection.TryGet("Verdana", out fontFamily))
                fontFamily = SystemFonts.Families.FirstOrDefault();

        if (fontFamily == null) throw new InvalidOperationException("No suitable font found.");
        _hudFont = fontFamily.CreateFont(16, FontStyle.Regular);
    }

    public void Run()
    {
        var options = WindowOptions.Default;
        options.Size = new Silk.NET.Maths.Vector2D<int>(1280, 720);
        options.Title = "DroneSim V1";
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3));

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Closing += OnClose;
        _window.Resize += OnResize;
        _window.Run();
    }

    private unsafe void OnLoad()
    {
        _gl = _window?.CreateOpenGL() ?? throw new InvalidOperationException("Could not create OpenGL context.");
        _tickable.Setup();

        _sceneShaderProgram = CompileShaders(SceneVertexShader, SceneFragmentShader);
        _debugShaderProgram = CompileShaders(DebugVertexShader, DebugFragmentShader);
        _hudShaderProgram = CompileShaders(HudVertexShader, HudFragmentShader);
        
        _sceneModelLocation = _gl.GetUniformLocation(_sceneShaderProgram, "uModel");
        _sceneViewLocation = _gl.GetUniformLocation(_sceneShaderProgram, "uView");
        _sceneProjectionLocation = _gl.GetUniformLocation(_sceneShaderProgram, "uProjection");
        _debugViewLocation = _gl.GetUniformLocation(_debugShaderProgram, "uView");
        _debugProjectionLocation = _gl.GetUniformLocation(_debugShaderProgram, "uProjection");

        if (_worldDataSource?.GetWorldData()?.TerrainRenderMesh is V1RenderMesh terrainMesh)
        {
            SetupTerrain(terrainMesh);
        }

        SetupDroneModel();
        SetupDebug();
        SetupHud();
        
        _gl.ClearColor(0.1f, 0.1f, 0.2f, 1.0f);
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    private unsafe void SetupTerrain(V1RenderMesh mesh)
    {
        if (_gl == null || !mesh.Vertices.Any()) return;
        _terrainIndexCount = mesh.Indices.Count;
        _terrainVao = _gl.GenVertexArray();
        _gl.BindVertexArray(_terrainVao);

        _terrainVbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _terrainVbo);
        var terrainVerts = mesh.Vertices.SelectMany(v => new[] { v.X, v.Y, v.Z }).ToArray();
        _gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(terrainVerts), BufferUsageARB.StaticDraw);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);

        _terrainColorVbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _terrainColorVbo);
        var terrainColors = mesh.Colors.SelectMany(c => new[] { c.X, c.Y, c.Z }).ToArray();
        _gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(terrainColors), BufferUsageARB.StaticDraw);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(1);

        _terrainEbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _terrainEbo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, new ReadOnlySpan<int>(mesh.Indices.ToArray()), BufferUsageARB.StaticDraw);
        
        _gl.BindVertexArray(0);
    }
    
    private unsafe void SetupDroneModel()
    {
        if (_gl == null) return;
        
        float[] vertices = { -0.5f,-0.5f,-0.5f, 0.8f,0.8f,0.1f,  0.5f,-0.5f,-0.5f, 0.8f,0.8f,0.1f,
                             0.5f, 0.5f,-0.5f, 0.8f,0.8f,0.1f, -0.5f, 0.5f,-0.5f, 0.8f,0.8f,0.1f,
                            -0.5f,-0.5f, 0.5f, 0.8f,0.8f,0.1f,  0.5f,-0.5f, 0.5f, 0.8f,0.8f,0.1f,
                             0.5f, 0.5f, 0.5f, 0.8f,0.8f,0.1f, -0.5f, 0.5f, 0.5f, 0.8f,0.8f,0.1f };
        uint[] indices = { 0,1,2, 2,3,0, 4,5,1, 1,0,4, 7,6,5, 5,4,7, 3,2,6, 6,7,3, 1,5,6, 6,2,1, 4,0,3, 3,7,4 };
        _droneIndexCount = indices.Length;
        
        _droneVao = _gl.GenVertexArray();
        _gl.BindVertexArray(_droneVao);

        _droneVbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _droneVbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(vertices), BufferUsageARB.StaticDraw);

        _droneEbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _droneEbo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, new ReadOnlySpan<uint>(indices), BufferUsageARB.StaticDraw);
        
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        _gl.BindVertexArray(0);
    }

    private unsafe void SetupDebug()
    {
        if (_gl == null) return;
        _debugVao = _gl.GenVertexArray();
        _gl.BindVertexArray(_debugVao);
        _debugVbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _debugVbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, 0, (ReadOnlySpan<byte>)null, BufferUsageARB.StreamDraw);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        _gl.BindVertexArray(0);
    }

    private unsafe void SetupHud()
    {
        if (_gl == null || _window == null) return;
        _hudVao = _gl.GenVertexArray();
        _gl.BindVertexArray(_hudVao);
        _hudVbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _hudVbo);
        float[] quadVertices = { -1f,-1f,0f,0f,  1f,-1f,1f,0f,  1f,1f,1f,1f, -1f,1f,0f,1f };
        _gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(quadVertices), BufferUsageARB.StaticDraw);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        _gl.BindVertexArray(0);
        
        var (width, height) = (_window.Size.X, _window.Size.Y);
        _hudImage = new Image<Rgba32>(width, height);
        _hudTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _hudTexture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
    }
    
    private void OnUpdate(double deltaTime) => _tickable.UpdateFrame((float)deltaTime);

    private unsafe void OnRender(double deltaTime)
    {
        if (_gl == null || _disposed) return;
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        var droneStates = _dataSource.GetAllDroneStates();
        if (!droneStates.Any()) 
        {
            _window?.SwapBuffers();
            return;
        }

        var playerDroneId = _dataSource.GetPlayerControlledDroneId();
        var cameraDroneId = _dataSource.GetCameraAttachedToDroneId();
        
        var cameraDrone = droneStates.FirstOrDefault(d => d.Id == cameraDroneId);
        if (cameraDrone.Equals(default(DroneState)))
            cameraDrone = droneStates.FirstOrDefault(d => d.Id == playerDroneId);
        if (cameraDrone.Equals(default(DroneState)))
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

    /// <summary>
    /// Renders the static terrain mesh.
    /// </summary>
    private unsafe void RenderTerrain(Matrix4x4 view, Matrix4x4 projection)
    {
        if (_gl == null || _terrainVao == 0) return;

        _gl.UseProgram(_sceneShaderProgram);
        _gl.UniformMatrix4(_sceneViewLocation, 1, false, (float*)Unsafe.AsPointer(ref view));
        _gl.UniformMatrix4(_sceneProjectionLocation, 1, false, (float*)Unsafe.AsPointer(ref projection));

        var modelMatrix = Matrix4x4.Identity;
        _gl.UniformMatrix4(_sceneModelLocation, 1, false, (float*)Unsafe.AsPointer(ref modelMatrix));

        _gl.BindVertexArray(_terrainVao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_terrainIndexCount, DrawElementsType.UnsignedInt, null);
        _gl.BindVertexArray(0);
    }

    /// <summary>
    /// Renders all drone agents in the simulation.
    /// </summary>
    private unsafe void RenderDrones(IReadOnlyList<DroneState> droneStates, int playerDroneId, Matrix4x4 view, Matrix4x4 projection)
    {
        if (_gl == null || _droneVao == 0) return;

        _gl.UseProgram(_sceneShaderProgram);
        _gl.UniformMatrix4(_sceneViewLocation, 1, false, (float*)Unsafe.AsPointer(ref view));
        _gl.UniformMatrix4(_sceneProjectionLocation, 1, false, (float*)Unsafe.AsPointer(ref projection));
        
        _gl.BindVertexArray(_droneVao);

        var body = (scale: new Vector3(0.4f, 0.2f, 1.0f), offset: Vector3.Zero, color: new Vector3(0.8f, 0.8f, 0.1f));
        var arm1 = (scale: new Vector3(1.2f, 0.1f, 0.2f), offset: Vector3.Zero, color: new Vector3(0.6f, 0.6f, 0.6f));
        var arm2 = (scale: new Vector3(0.2f, 0.1f, 1.2f), offset: Vector3.Zero, color: new Vector3(0.6f, 0.6f, 0.6f));
        var playerColor = new Vector3(0.1f, 0.9f, 0.2f);
        
        int colorLocation = _gl.GetUniformLocation(_sceneShaderProgram, "uOverrideColor");

        foreach (var drone in droneStates)
        {
            var droneMatrix = Matrix4x4.CreateFromQuaternion(drone.Orientation) * Matrix4x4.CreateTranslation(drone.Position);

            var bodyModelMatrix = Matrix4x4.CreateScale(body.scale) * Matrix4x4.CreateTranslation(body.offset) * droneMatrix;
            _gl.UniformMatrix4(_sceneModelLocation, 1, false, (float*)Unsafe.AsPointer(ref bodyModelMatrix));
            _gl.Uniform3(colorLocation, drone.Id == playerDroneId ? playerColor : body.color);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_droneIndexCount, DrawElementsType.UnsignedInt, null);
            
            var arm1ModelMatrix = Matrix4x4.CreateScale(arm1.scale) * Matrix4x4.CreateTranslation(arm1.offset) * droneMatrix;
            _gl.UniformMatrix4(_sceneModelLocation, 1, false, (float*)Unsafe.AsPointer(ref arm1ModelMatrix));
            _gl.Uniform3(colorLocation, arm1.color);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_droneIndexCount, DrawElementsType.UnsignedInt, null);

            var arm2ModelMatrix = Matrix4x4.CreateScale(arm2.scale) * Matrix4x4.CreateTranslation(arm2.offset) * droneMatrix;
            _gl.UniformMatrix4(_sceneModelLocation, 1, false, (float*)Unsafe.AsPointer(ref arm2ModelMatrix));
            _gl.DrawElements(PrimitiveType.Triangles, (uint)_droneIndexCount, DrawElementsType.UnsignedInt, null);
        }

        _gl.BindVertexArray(0);
    }

    private (Matrix4x4, Matrix4x4) CalculateCameraMatrices(DroneState targetDrone, CameraViewMode mode, float tilt)
    {
        if (_window == null) return (Matrix4x4.Identity, Matrix4x4.Identity);
        
        var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 180f * 75f, (float)_window.Size.X / _window.Size.Y, 0.1f, 1000f);
        
        var dronePosition = targetDrone.Position;
        var droneOrientation = targetDrone.Orientation;
        
        Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), droneOrientation);
        Vector3 up = Vector3.Transform(new Vector3(0, 1, 0), droneOrientation);

        Vector3 cameraPosition;
        Vector3 cameraTarget;

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

        var tiltRotation = Quaternion.CreateFromAxisAngle(Vector3.Transform(Vector3.UnitX, droneOrientation), -tilt * (MathF.PI / 180f));
        var finalUp = Vector3.Transform(up, tiltRotation);

        var viewMatrix = Matrix4x4.CreateLookAt(cameraPosition, cameraTarget, finalUp);
        return (viewMatrix, projectionMatrix);
    }
    
    /// <summary>
    /// Renders debug shapes provided by the DebugDraw service.
    /// </summary>
    private unsafe void RenderDebugShapes(Matrix4x4 view, Matrix4x4 projection)
    {
        if (_gl == null) return;
        
        var shapes = _debugDraw.GetShapesToRender();
        if (!shapes.Any()) return;

        _debugBuffer.Clear();
        int lineVertexCount = 0;
        int pointVertexCount = 0;

        foreach (var shape in shapes)
        {
            if (shape is DebugLine line)
            {
                 _debugBuffer.AddRange(new[] { line.Start.X, line.Start.Y, line.Start.Z, line.Color.R / 255f, line.Color.G / 255f, line.Color.B / 255f });
                 _debugBuffer.AddRange(new[] { line.End.X, line.End.Y, line.End.Z, line.Color.R / 255f, line.Color.G / 255f, line.Color.B / 255f });
                 lineVertexCount += 2;
            }
            else if (shape is DebugPoint point)
            {
                _debugBuffer.AddRange(new[] { point.Position.X, point.Position.Y, point.Position.Z, point.Color.R / 255f, point.Color.G / 255f, point.Color.B / 255f });
                pointVertexCount += 1;
            }
        }
        
        if (!_debugBuffer.Any()) return;

        _gl.UseProgram(_debugShaderProgram);
        _gl.UniformMatrix4(_debugViewLocation, 1, false, (float*)Unsafe.AsPointer(ref view));
        _gl.UniformMatrix4(_debugProjectionLocation, 1, false, (float*)Unsafe.AsPointer(ref projection));

        _gl.BindVertexArray(_debugVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _debugVbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, new ReadOnlySpan<float>(_debugBuffer.ToArray()), BufferUsageARB.StreamDraw);

        if (lineVertexCount > 0)
        {
            _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)lineVertexCount);
        }
        if (pointVertexCount > 0)
        {
            _gl.PointSize(10f);
            _gl.DrawArrays(PrimitiveType.Points, lineVertexCount, (uint)pointVertexCount);
        }

        _gl.BindVertexArray(0);
    }
    
    private unsafe void RenderHud()
    {
        if (_gl == null || _hudImage == null) return;

        string hudText = _dataSource.GetHudInfo();

        _hudImage.Mutate(ctx => 
        {
            ctx.Fill(Color.Transparent);
            ctx.DrawText(hudText, _hudFont, Color.White, new PointF(10, 10));
        });

        _hudImage.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                fixed (void* p = row)
                {
                    _gl.BindTexture(TextureTarget.Texture2D, _hudTexture);
                    _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y, (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, p);
                }
            }
        });
        
        _gl.Disable(EnableCap.DepthTest);
        _gl.UseProgram(_hudShaderProgram);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _hudTexture);

        _gl.BindVertexArray(_hudVao);
        _gl.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        _gl.BindVertexArray(0);
        _gl.Enable(EnableCap.DepthTest);
    }

    private void OnResize(Silk.NET.Maths.Vector2D<int> size)
    {
        if (_gl == null) return;
        _gl.Viewport(size);

        _hudImage?.Dispose();
        _hudImage = new Image<Rgba32>(size.X, size.Y);
        _gl.BindTexture(TextureTarget.Texture2D, _hudTexture);
        unsafe {
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, (uint)size.X, (uint)size.Y, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
        }
    }

    private void OnClose() => Dispose();
    
    private unsafe uint CompileShaders(string vertexSource, string fragmentSource)
    {
        if (_gl == null) return 0;
        var vertexShader = _gl.CreateShader(ShaderType.VertexShader);
        _gl.ShaderSource(vertexShader, vertexSource);
        _gl.CompileShader(vertexShader);
        _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out var vStatus);
        if (vStatus != (int)GLEnum.True)
            throw new Exception("Vertex shader failed to compile: " + _gl.GetShaderInfoLog(vertexShader));
        
        var fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
        _gl.ShaderSource(fragmentShader, fragmentSource);
        _gl.CompileShader(fragmentShader);
        _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out var fStatus);
        if (fStatus != (int)GLEnum.True)
            throw new Exception("Fragment shader failed to compile: " + _gl.GetShaderInfoLog(fragmentShader));

        var shaderProgram = _gl.CreateProgram();
        _gl.AttachShader(shaderProgram, vertexShader);
        _gl.AttachShader(shaderProgram, fragmentShader);
        _gl.LinkProgram(shaderProgram);
        
        _gl.GetProgram(shaderProgram, GLEnum.LinkStatus, out var pStatus);
        if (pStatus != (int)GLEnum.True)
            throw new Exception("Shader program failed to link: " + _gl.GetProgramInfoLog(shaderProgram));

        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
        return shaderProgram;
    }

    public void Dispose()
    {
        if (_disposed) return;

        GC.SuppressFinalize(this);

        _gl?.BindVertexArray(0);
        _gl?.UseProgram(0);

        _gl?.DeleteBuffer(_terrainVbo);
        _gl?.DeleteBuffer(_terrainColorVbo);
        _gl?.DeleteBuffer(_terrainEbo);
        _gl?.DeleteVertexArray(_terrainVao);

        _gl?.DeleteBuffer(_droneVbo);
        _gl?.DeleteBuffer(_droneEbo);
        _gl?.DeleteVertexArray(_droneVao);
        
        _gl?.DeleteBuffer(_debugVbo);
        _gl?.DeleteVertexArray(_debugVao);
        
        _gl?.DeleteBuffer(_hudVbo);
        _gl?.DeleteVertexArray(_hudVao);
        _gl?.DeleteTexture(_hudTexture);

        _gl?.DeleteProgram(_sceneShaderProgram);
        _gl?.DeleteProgram(_debugShaderProgram);
        _gl?.DeleteProgram(_hudShaderProgram);
        
        _hudImage?.Dispose();
        _gl?.Dispose();

        _disposed = true;
    }
    
    ~V1SilkNetRenderer()
    {
        Dispose();
    }

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
}