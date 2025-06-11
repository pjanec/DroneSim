// In project: DroneSim.DebugDraw
using DroneSim.Core;
using System.Numerics;
using System.Drawing;

namespace DroneSim.DebugDraw;

/// <summary>
/// Base class for all debug primitives.
/// </summary>
public abstract class DebugShape
{
    /// <summary>
    /// The color of the shape.
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// The remaining duration in seconds for the shape to be visible.
    /// A value of 0 means it will be rendered for one frame.
    /// </summary>
    public float RemainingDuration { get; set; }
}

/// <summary>
/// Represents a line segment in 3D space for debug rendering.
/// </summary>
public class DebugLine : DebugShape
{
    /// <summary>
    /// The start point of the line.
    /// </summary>
    public Vector3 Start { get; set; }

    /// <summary>
    /// The end point of the line.
    /// </summary>
    public Vector3 End { get; set; }
}

/// <summary>
/// Represents a point in 3D space for debug rendering.
/// </summary>
public class DebugPoint : DebugShape
{
    /// <summary>
    /// The position of the point.
    /// </summary>
    public Vector3 Position { get; set; }
    
    /// <summary>
    /// The size or radius of the point.
    /// </summary>
    public float Size { get; set; }
} 