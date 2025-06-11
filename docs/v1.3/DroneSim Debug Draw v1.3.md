Document ID: MODSPEC-DEBUG-V1.3  
Date: June 11, 2025  
Title: V1.3 IDebugDrawService Module Specification (Implementing Class: DebugDrawService)  
**1\. Overview**

This document provides the detailed implementation specification for the IDebugDrawService interface, named DebugDrawService. This module provides a centralized system for rendering optional, temporary, 3D debug information within the simulation world.

Its purpose is to decouple the modules that *generate* debug data (like IFlightDynamics or IAutopilot) from the module that *renders* it (IRenderer). This allows any part of the system to request a shape to be drawn without needing a direct reference to the renderer.

**2\. Dependencies**

* **DroneSim.Core:** For the IDebugDrawService interface and data structures like Vector3.  
* **System.Drawing.Common:** For the Color struct.

**3\. V1 Functional Specification**

* **State Management:** The service maintains an internal list of DebugShape objects. Each DebugShape stores its type (line, point, etc.), geometry, color, and a RemainingDuration timer.  
* **Shape Submission:** Any module with a dependency on IDebugDrawService can call its public methods (DrawLine, DrawVector, etc.) to submit a new shape for rendering.  
  * If a shape is submitted with a duration of 0, it is considered a "one-frame" shape and will be removed at the end of the frame.  
  * If a shape has a positive duration, it will persist for that many seconds.  
* **Lifecycle Management (Tick)**:  
  * The service's Tick(deltaTime) method must be called by the Orchestrator once per frame.  
  * This method iterates through all persistent shapes and decrements their RemainingDuration. Any shape whose duration expires is removed from the list.  
* **Data Retrieval (GetShapesToRender):**  
  * The Renderer calls GetShapesToRender() once per frame. This method returns a read-only list of all currently active DebugShape objects.  
* **Cleanup (Clear)**:  
  * At the end of the Tick method, the service automatically cleans up all one-frame shapes.

**4\. Code Skeleton**

This is the proposed source code structure, including the internal helper class for managing shapes.

// In project: DroneSim.DebugDraw  
using DroneSim.Core;  
using System.Numerics;  
using System.Drawing;  
using System.Collections.Generic;

// Internal helper class to represent a single debug primitive  
public abstract class DebugShape  
{  
    public Color Color { get; set; }  
    public float RemainingDuration { get; set; }  
}

public class DebugLine : DebugShape  
{  
    public Vector3 Start { get; set; }  
    public Vector3 End { get; set; }  
}

public class DebugPoint : DebugShape  
{  
    public Vector3 Position { get; set; }  
    public float Size { get; set; }  
}  
// Other shapes like DebugPath could also be added.

/// \<summary\>  
/// V1 implementation of the debug drawing service.  
/// Collects and manages debug shapes to be rendered by the Renderer.  
/// \</summary\>  
public class DebugDrawService : IDebugDrawService  
{  
    private readonly List\<DebugShape\> \_activeShapes \= new();  
    private readonly List\<DebugShape\> \_oneFrameShapes \= new();

    // \--- Consumer Methods \---

    public void DrawLine(Vector3 start, Vector3 end, Color color, float duration \= 0f)  
    {  
        var line \= new DebugLine { Start \= start, End \= end, Color \= color, RemainingDuration \= duration };  
        if (duration \> 0\) \_activeShapes.Add(line);  
        else \_oneFrameShapes.Add(line);  
    }

    public void DrawVector(Vector3 start, Vector3 vector, Color color, float duration \= 0f)  
    {  
        DrawLine(start, start \+ vector, color, duration);  
        // Additional logic could be added here to draw an arrowhead.  
    }

    public void DrawCollisionPoint(Vector3 point, float radius, Color color, float duration \= 0f)  
    {  
        var debugPoint \= new DebugPoint { Position \= point, Size \= radius, Color \= color, RemainingDuration \= duration };  
        if (duration \> 0\) \_activeShapes.Add(debugPoint);  
        else \_oneFrameShapes.Add(debugPoint);  
    }

    public void DrawPath(IReadOnlyList\<Vector3\> pathPoints, Color color, float duration \= 0f)  
    {  
        for (int i \= 0; i \< pathPoints.Count \- 1; i++)  
        {  
            DrawLine(pathPoints\[i\], pathPoints\[i \+ 1\], color, duration);  
        }  
    }

    // \--- Simulation Loop Methods \---

    /// \<summary\>  
    /// Called by the Orchestrator each frame to update timers and clear one-frame shapes.  
    /// \</summary\>  
    public void Tick(float deltaTime)  
    {  
        // Clear shapes from the previous frame  
        \_oneFrameShapes.Clear();

        // Update persistent shapes and remove expired ones  
        \_activeShapes.RemoveAll(shape \=\>  
        {  
            shape.RemainingDuration \-= deltaTime;  
            return shape.RemainingDuration \<= 0;  
        });  
    }

    /// \<summary\>  
    /// Called by the Renderer each frame to get all active shapes to draw.  
    /// \</summary\>  
    public IReadOnlyList\<object\> GetShapesToRender()  
    {  
        var allShapes \= new List\<object\>(\_activeShapes);  
        allShapes.AddRange(\_oneFrameShapes);  
        return allShapes;  
    }

    // This method is deprecated by the new Tick logic but kept for the interface.  
    public void Clear()  
    {  
        \_oneFrameShapes.Clear();  
    }  
}  