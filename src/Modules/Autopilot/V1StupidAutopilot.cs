// In project: DroneSim.Autopilot
using DroneSim.Core;
using Microsoft.Extensions.Options;
using System;
using System.Numerics;

namespace DroneSim.Autopilot;

/// <summary>
/// V1 implementation of the autopilot AI.
/// Provides simple "turn-and-burn" logic to fly to a target.
/// </summary>
public class V1StupidAutopilot : IAutopilot
{
    private Vector3 _targetPosition;
    private readonly AIBehaviorOptions _options;
    private readonly IDebugDrawService _debugDraw;

    // Use squared distance for efficiency in checks
    private readonly float _arrivalRadiusSq;

    public V1StupidAutopilot(IOptions<AIBehaviorOptions> options, IDebugDrawService debugDrawService)
    {
        _options = options.Value;
        _arrivalRadiusSq = _options.ArrivalRadius * _options.ArrivalRadius;
        _debugDraw = debugDrawService;
    }

    /// <summary>
    /// Assigns a new destination for the autopilot to navigate to.
    /// The target is clamped to the configured flight altitude.
    /// </summary>
    public void SetTarget(Vector3 targetPosition)
    {
        _targetPosition = new Vector3(targetPosition.X, _options.FlightAltitude, targetPosition.Z);
    }

    /// <summary>
    /// Calculates the control inputs needed to move towards the target based on the current drone state.
    /// </summary>
    public ControlInputs GetControlUpdate(DroneState currentDroneState)
    {
        var inputs = new ControlInputs();
        var currentPosition = currentDroneState.Position;

        // Visualize the plan
        if (_debugDraw != null)
        {
            _debugDraw.DrawPath(new[] { currentPosition, _targetPosition }, System.Drawing.Color.Cyan, 0);
        }

        // --- Step 1: Check for arrival ---
        var horizontalVectorToTarget = new Vector2(_targetPosition.X - currentPosition.X, _targetPosition.Z - currentPosition.Z);
        if (horizontalVectorToTarget.LengthSquared() < _arrivalRadiusSq)
        {
            return inputs; // Arrived, return empty inputs.
        }

        // --- Step 2: Altitude Control ---
        float altitudeError = _targetPosition.Y - currentPosition.Y;
        if (altitudeError > 1.0f) inputs.VerticalInput = 1.0f;
        else if (altitudeError < -1.0f) inputs.VerticalInput = -1.0f;

        // --- Step 3: Yaw Control ---
        var forwardVector = Vector3.Transform(Vector3.UnitZ, currentDroneState.Orientation);
        var forwardVector2D = Vector2.Normalize(new Vector2(forwardVector.X, forwardVector.Z));
        var targetDirection2D = Vector2.Normalize(horizontalVectorToTarget);

        // Dot product gives cosine of the angle. Acos gives the angle.
        // Clamp the dot product to avoid Math.Acos domain errors due to floating point inaccuracies
        float dot = Vector2.Dot(forwardVector2D, targetDirection2D);
        float angle = (float)Math.Acos(Math.Clamp(dot, -1.0f, 1.0f));

        if (angle > _options.YawTolerance)
        {
            // Use 2D cross product to determine turn direction
            var crossZ = forwardVector2D.X * targetDirection2D.Y - forwardVector2D.Y * targetDirection2D.X;
            inputs.YawInput = Math.Sign(crossZ);
        }
        else
        {
            // --- Step 4: Throttle Control (only if facing target) ---
            inputs.ThrottleStep = _options.ConstantThrottleStep;
        }

        return inputs;
    }
} 