// In project: DroneSim.Autopilot
namespace DroneSim.Autopilot;

/// <summary>
/// Configuration parameters for the autopilot AI.
/// Values are bound from configuration and injected into
/// <see cref="V1StupidAutopilot"/>.
/// <list type="bullet">
/// <item><description><c>FlightAltitude</c> – target altitude in meters.</description></item>
/// <item><description><c>ConstantThrottleStep</c> – throttle level (0-10) when flying forward.</description></item>
/// <item><description><c>ArrivalRadius</c> – distance from the target at which it is considered reached.</description></item>
/// <item><description><c>YawTolerance</c> – angle in radians within which the drone is facing its target.</description></item>
/// </list>
/// </summary>
public class AIBehaviorOptions
{
    public float FlightAltitude { get; set; } = 20.0f;
    public int ConstantThrottleStep { get; set; } = 4;
    public float ArrivalRadius { get; set; } = 5.0f;
    public float YawTolerance { get; set; } = 0.1f;
} 