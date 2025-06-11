// In project: DroneSim.Autopilot
namespace DroneSim.Autopilot;

public class AIBehaviorOptions
{
    public float FlightAltitude { get; set; } = 20.0f;
    public int ConstantThrottleStep { get; set; } = 4;
    public float ArrivalRadius { get; set; } = 5.0f;
    public float YawTolerance { get; set; } = 0.1f;
} 