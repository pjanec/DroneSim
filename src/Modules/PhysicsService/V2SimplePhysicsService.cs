using DroneSim.Core;
using DroneSim.Physics;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DroneSim.Physics;

/// <summary>
/// A simplified, "toy" implementation of the V2 physics service.
/// Uses basic Euler integration and boundary clamping. Does not use a real physics engine.
/// This is intended as a stepping stone for testing other modules.
/// </summary>
public class V2SimplePhysicsService : IPhysicsService
{
    private class SimplePhysicsBody
    {
        public int Handle;
        public bool IsKinematic;
        public float Mass = 1.0f;
        public Vector3 Position;
        public Quaternion Orientation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity = Vector3.Zero;
        public List<Vector3> ForcesThisFrame = new();
        public List<Vector3> TorquesThisFrame = new();
    }

    private readonly List<SimplePhysicsBody> _bodies = new();
    private readonly float _worldBoundary;
    private int _handleCounter = 0;

    /// <summary>
    /// This event will never be invoked in this simplified implementation,
    /// as no actual collision detection is performed.
    /// </summary>
#pragma warning disable 0067
    public event Action<CollisionEventData>? CollisionDetected;
#pragma warning restore 0067

    public V2SimplePhysicsService(IOptions<PhysicsOptions> options)
    {
        _worldBoundary = options.Value.WorldBoundary;
    }

    public int AddDynamicBody(object description)
    {
        var handle = ++_handleCounter;
        _bodies.Add(new SimplePhysicsBody { Handle = handle, IsKinematic = false });
        return handle;
    }

    public int AddKinematicBody(object description)
    {
        var handle = ++_handleCounter;
        _bodies.Add(new SimplePhysicsBody { Handle = handle, IsKinematic = true });
        return handle;
    }

    public void AddStaticBody(PhysicsBody bodyData)
    {
        // Does nothing in this simplified implementation.
    }

    public void SubmitMoveIntent(int bodyHandle, IMoveIntent intent)
    {
        var body = _bodies.Find(b => b.Handle == bodyHandle);
        if (body == null) return;

        switch (intent)
        {
            case DynamicIntent di:
                if (!body.IsKinematic)
                {
                    body.ForcesThisFrame.Add(di.Force);
                    body.TorquesThisFrame.Add(di.Torque);
                }
                break;
            case KinematicIntent ki:
                if (body.IsKinematic)
                {
                    body.LinearVelocity = ki.TargetVelocity;
                    body.Orientation = ki.TargetOrientation;
                }
                break;
        }
    }

    public void Step(float deltaTime)
    {
        foreach (var body in _bodies)
        {
            // --- Integration for Dynamic Bodies ---
            if (!body.IsKinematic && body.ForcesThisFrame.Any())
            {
                var totalForce = Vector3.Zero;
                foreach (var force in body.ForcesThisFrame) totalForce += force;

                var acceleration = totalForce / body.Mass;
                body.LinearVelocity += acceleration * deltaTime;

                // Note: Torque/angular velocity integration is omitted for simplicity
            }

            // --- Position Update for All Bodies ---
            var newPosition = body.Position + body.LinearVelocity * deltaTime;

            // --- Collision Resolution (Clamping) ---
            newPosition.X = Math.Clamp(newPosition.X, -_worldBoundary, _worldBoundary);
            newPosition.Z = Math.Clamp(newPosition.Z, -_worldBoundary, _worldBoundary);
            newPosition.Y = Math.Max(0.0f, newPosition.Y); // Clamp to ground
            body.Position = newPosition;

            // Clear forces for next frame
            body.ForcesThisFrame.Clear();
            body.TorquesThisFrame.Clear();
        }
    }

    public DroneState GetState(int bodyHandle)
    {
        var body = _bodies.Find(b => b.Handle == bodyHandle);
        if (body != null)
        {
            return new DroneState
            {
                Id = body.Handle,
                Position = body.Position,
                Orientation = body.Orientation,
                Status = DroneStatus.Active // Simplified: always active
            };
        }
        return default;
    }
} 