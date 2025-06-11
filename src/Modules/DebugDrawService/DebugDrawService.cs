// In project: DroneSim.DebugDraw
using DroneSim.Core;
using System.Numerics;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace DroneSim.DebugDraw;

/// <summary>
/// V1 implementation of the debug drawing service.
/// Collects and manages debug shapes to be rendered by the Renderer.
/// </summary>
public class DebugDrawService : IDebugDrawService
{
    private readonly List<DebugShape> _persistentShapes = new();
    private readonly List<DebugShape> _oneFrameShapes = new();

    /// <inheritdoc />
    public void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f)
    {
        var line = new DebugLine { Start = start, End = end, Color = color, RemainingDuration = duration };
        if (duration > 0)
        {
            _persistentShapes.Add(line);
        }
        else
        {
            _oneFrameShapes.Add(line);
        }
    }

    /// <inheritdoc />
    public void DrawVector(Vector3 start, Vector3 vector, Color color, float duration = 0f)
    {
        DrawLine(start, start + vector, color, duration);
        // In a real implementation, we might add an arrowhead shape here.
    }

    /// <inheritdoc />
    public void DrawCollisionPoint(Vector3 point, float radius, Color color, float duration = 0f)
    {
        var debugPoint = new DebugPoint { Position = point, Size = radius, Color = color, RemainingDuration = duration };
        if (duration > 0)
        {
            _persistentShapes.Add(debugPoint);
        }
        else
        {
            _oneFrameShapes.Add(debugPoint);
        }
    }

    /// <inheritdoc />
    public void DrawPath(IReadOnlyList<Vector3> pathPoints, Color color, float duration = 0f)
    {
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            DrawLine(pathPoints[i], pathPoints[i + 1], color, duration);
        }
    }

    /// <summary>
    /// Called by the Orchestrator each frame to update timers and clear one-frame shapes.
    /// </summary>
    public void Tick(float deltaTime)
    {
        // Clear one-frame shapes from the previous frame.
        _oneFrameShapes.Clear();

        // Update persistent shapes, removing any that have expired.
        // We iterate backwards because we're removing items from the list.
        for (int i = _persistentShapes.Count - 1; i >= 0; i--)
        {
            var shape = _persistentShapes[i];
            shape.RemainingDuration -= deltaTime;
            if (shape.RemainingDuration <= 0)
            {
                _persistentShapes.RemoveAt(i);
            }
        }
    }

    /// <summary>
    
    /// Called by the Renderer each frame to get all active shapes to draw.
    /// </summary>
    public IReadOnlyList<object> GetShapesToRender()
    {
        // The spec in Core mentions IReadOnlyList<object>
        // but our renderer will be happier with a concrete type.
        // For now, we combine the lists and return them as objects.
        var allShapes = new List<object>(_persistentShapes);
        allShapes.AddRange(_oneFrameShapes);
        return allShapes;
    }

    /// <summary>
    /// Clears all one-frame shapes. This is now handled by Tick,
    /// but is kept for interface compatibility.
    /// </summary>
    public void Clear()
    {
        _oneFrameShapes.Clear();
    }
} 