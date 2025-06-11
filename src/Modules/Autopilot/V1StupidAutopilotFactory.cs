// In project: DroneSim.Autopilot
using DroneSim.Core;
using Microsoft.Extensions.Options;

namespace DroneSim.Autopilot;

/// <summary>
/// Factory for creating instances of the V1StupidAutopilot.
/// </summary>
public class V1StupidAutopilotFactory : IAutopilotFactory
{
    private readonly IOptions<AIBehaviorOptions> _options;
    private readonly IDebugDrawService _debugDrawService;

    public V1StupidAutopilotFactory(IOptions<AIBehaviorOptions> options, IDebugDrawService debugDrawService)
    {
        _options = options;
        _debugDrawService = debugDrawService;
    }

    /// <summary>
    /// Creates a new instance of the V1StupidAutopilot.
    /// </summary>
    /// <returns>A new IAutopilot instance.</returns>
    public IAutopilot Create()
    {
        return new V1StupidAutopilot(_options, _debugDrawService);
    }
} 