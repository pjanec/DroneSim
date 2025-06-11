namespace DroneSim.Physics;

/// <summary>
/// Configuration options for the Physics Service.
/// </summary>
public class PhysicsOptions
{
    /// <summary>
    /// The half-size of the cubic world boundary. A value of 100 means the world
    /// extends from -100 to +100 on the X and Z axes.
    /// </summary>
    public float WorldBoundary { get; set; } = 100.0f;
} 