using DroneSim.Core;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Numerics;
using System;

namespace DroneSim.FlightDynamics;

/// <summary>
/// V1 implementation of the flight dynamics module.
/// Translates control inputs into a kinematic movement intent.
/// This model is stateful to track individual drone speeds for smooth acceleration.
/// </summary>
public class V1KinematicFlightModel : IFlightDynamics
{
    private readonly Dictionary<int, float> _droneForwardSpeeds = new();
    private readonly FlightModelOptions _options;

    public V1KinematicFlightModel(IOptions<FlightModelOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Calculates the desired kinematic movement intent for a drone based on its current state and control inputs.
    /// </summary>
    /// <param name="currentState">The current state of the drone (position, orientation, etc.).</param>
    /// <param name="inputs">The player or AI control inputs for this frame.</param>
    /// <param name="deltaTime">The time elapsed since the last frame.</param>
    /// <returns>A KinematicIntent describing the desired velocity and orientation.</returns>
    public IMoveIntent GenerateMoveIntent(DroneState currentState, ControlInputs inputs, float deltaTime)
    {
        // Step 1: Calculate Current Forward Speed (Smooth Throttle)
        _droneForwardSpeeds.TryAdd(currentState.Id, 0.0f);
        float targetForwardSpeed = (inputs.ThrottleStep / 10.0f) * _options.MaxForwardSpeed;
        float currentForwardSpeed = _droneForwardSpeeds[currentState.Id];

        // Use linear interpolation (Lerp) to smoothly approach the target speed
        float smoothedForwardSpeed = Math.Abs(targetForwardSpeed - currentForwardSpeed) > 0.01f
            ? float.Lerp(currentForwardSpeed, targetForwardSpeed, deltaTime * _options.AccelerationFactor)
            : targetForwardSpeed;
        _droneForwardSpeeds[currentState.Id] = smoothedForwardSpeed;

        // Step 2: Calculate World-Space Target Velocity Vector
        var forwardDirection = Vector3.Transform(Vector3.UnitZ, currentState.Orientation);
        var rightDirection = Vector3.Transform(Vector3.UnitX, currentState.Orientation);

        var forwardVelocity = forwardDirection * smoothedForwardSpeed;
        var strafeVelocity = rightDirection * inputs.StrafeInput * _options.MaxStrafeSpeed;
        var verticalVelocity = Vector3.UnitY * inputs.VerticalInput * _options.MaxVerticalSpeed;
        var targetVelocity = forwardVelocity + strafeVelocity + verticalVelocity;

        // Step 3: Calculate new Target Orientation (Yaw)
        var yawChange = inputs.YawInput * _options.YawSpeed * deltaTime;
        var yawRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, -yawChange);
        var targetOrientation = currentState.Orientation * yawRotation;

        // Step 4: Return the final intent object
        return new KinematicIntent(targetVelocity, targetOrientation);
    }
} 