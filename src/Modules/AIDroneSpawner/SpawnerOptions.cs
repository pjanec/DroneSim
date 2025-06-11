// In project: DroneSim.AISpawner
namespace DroneSim.AISpawner;

/// <summary>
/// Options controlling how AI drones are spawned.
/// <list type="bullet">
/// <item><description><c>InitialFlightAltitude</c> – starting altitude for newly created drones.</description></item>
/// <item><description><c>WorldBoundary</c> – maximum absolute coordinate used when randomizing positions.</description></item>
/// </list>
/// </summary>
public class SpawnerOptions
{
    public float InitialFlightAltitude { get; set; } = 20.0f;
    public float WorldBoundary { get; set; } = 128.0f;
} 