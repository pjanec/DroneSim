Document ID: REQ-DRNSIM-V1.3

Date: June 11, 2025

Title: V1.3 Complete Requirements Specification for Drone Development Environment

**1. Introduction**

This document specifies the complete requirements for the V1 DroneSim application. The V1 prototype will implement a fully functional application loop with an advanced, extensible architecture ready for future upgrades. It will support both kinematic and dynamic flight models through a unified physics interface.

**2. Guiding Principles**

- **Modularity:** The system shall be composed of independent modules that communicate through well-defined interfaces.

- **Testability:** All modules shall be designed to be testable in isolation.

- **Extensibility:** The architecture must cleanly support both simple kinematic flight models and advanced, force-based dynamic models without requiring changes to the core application loop.

**3. V1 Feature Set**

- **3.1. Core Simulation Environment**

  - The application shall render a real-time 3D scene in a 1280x720 window.

  - The environment shall consist of a single, flat, finite plane.

  - The ground plane shall be rendered with a procedural color pattern (using noise to generate \"biomes\") overlaid with a checkerboard pattern to aid in visual navigation.

  - The simulation shall support a pre-defined number of drone entities (1 player, 9 AI).

- **3.2. Flight & Physics Model**

  - **Unified Physics Service:** A central physics service shall manage all objects in a single simulation world. It must support both **kinematic bodies** (controlled by setting velocity) and **dynamic bodies** (controlled by applying forces).

  - **Flight Dynamics:** The flight model\'s responsibility is to generate a **movement intent** (KinematicIntent or DynamicIntent) for a drone. It is completely decoupled from the physics simulation itself.

  - **Collision Resolution:** All collisions (drone-to-terrain, drone-to-drone) shall be detected and resolved by the unified physics service, providing realistic responses like stopping or bouncing.

- **3.3. Manual Drone Control**

  - The user shall control a single drone at a time via the keyboard.

  - **Throttle:** W and S keys shall control forward movement.

  - **Vertical:** Q and Z keys shall control vertical movement.

  - **Strafing:** A and D keys shall control sideways movement.

  - **Turning (Yaw):** Left Arrow and Right Arrow keys shall control turning.

- **3.4. AI and Entity Control**

  - The simulation shall contain 9 simple AI-controlled drones.

  - The V1 AI shall use \"turn-then-burn\" logic to fly towards random destinations at a fixed altitude.

  - The user shall be able to **possess** any drone using the P key, switching manual control to it.

- **3.5. Camera and View Controls**

  - The T key shall cycle camera attachment through all drones.

  - The C key shall toggle between First-Person and Over-the-Shoulder view modes.

  - Up Arrow and Down Arrow keys shall control camera tilt.

- **3.6. Debug Visualization (Optional)**

  - A dedicated debug view shall be toggled on or off with the **F3 key**.

  - When enabled, the renderer shall display extra visual information overlaid on the 3D scene.

  - **Required Debug Visuals:**

    - **Motion Intentions:** Vectors showing the forces or target velocities being applied to drones.

    - **Planned Paths:** Lines showing the route an AI is attempting to follow.

    - **Collision Points:** Markers (e.g., spheres) that appear at the location of a detected collision.

- **3.7. User Interface (HUD)**

  - A text-based Heads-Up Display shall be rendered on screen, showing critical flight information (speed, altitude, throttle, camera state, etc.) and keybind hints.
