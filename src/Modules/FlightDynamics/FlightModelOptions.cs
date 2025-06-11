namespace DroneSim.FlightDynamics;

/// <summary>
/// Configuration options for the flight models.
/// </summary>
public class FlightModelOptions
{
    /// <summary>
    /// Speed in m/s at ThrottleStep = 10.
    /// </summary>
    public float MaxForwardSpeed { get; set; } = 20.0f;

    /// <summary>
    /// Top speed for sideways movement.
    /// </summary>
    public float MaxStrafeSpeed { get; set; } = 10.0f;

    /// <summary>
    /// Top speed for vertical movement.
    /// </summary>
    public float MaxVerticalSpeed { get; set; } = 5.0f;

    /// <summary>
    /// Turn rate in radians per second (90 deg/sec).
    /// </summary>
    public float YawSpeed { get; set; } = 1.5708f;

    /// <summary>
    /// A multiplier for the Lerp smoothing for acceleration.
    /// </summary>
    public float AccelerationFactor { get; set; } = 5.0f;
} 