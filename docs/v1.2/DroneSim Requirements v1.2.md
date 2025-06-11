Document ID: REQ-DRNSIM-V1.2

Date: June 10, 2025

Title: V1.2 Complete Requirements Specification for Drone Development Environment

**1. Introduction**

This document specifies the complete requirements for the first functional iteration (V1) of a real-time 3D drone development environment. The purpose of this environment is to provide a platform for developing and testing drone flight models and control systems.

The V1 prototype will prioritize a fully functional application loop with simplified underlying systems but a complete feature set from the user\'s perspective. This allows for rapid development and validation while providing a stable foundation for future enhancements.

**2. Guiding Principles**

- **Modularity:** The system shall be composed of independent modules that communicate through well-defined interfaces.

- **Testability:** All modules shall be designed to be testable in isolation.

- **Extensibility:** The V1 design shall directly support future upgrades to more complex systems (e.g., a physics-based flight model) without requiring a major architectural rewrite.

**3. V1 Feature Set**

- **3.1. Core Simulation Environment**

  - The application shall render a real-time 3D scene in a 1280x720 window.

  - The environment shall consist of a single, flat, finite plane.

  - The ground plane shall be rendered with a procedural color pattern (using Perlin noise to generate \"biomes\") overlaid with a checkerboard pattern to provide a sense of motion and aid in visual navigation.

  - The simulation shall support a pre-defined number of drone entities (1 player, 9 AI).

- **3.2. Flight & Physics Model**

  - **Flight Dynamics:** The flight model shall be **purely kinematic**. Its responsibility is to translate user or AI controls into an unobstructed displacement vector and a new orientation for a given frame. It shall have no knowledge of the world environment.

  - **Smooth Control:** The flight model shall provide smooth acceleration and deceleration when throttle is applied or released.

  - **Physics Service:** A dedicated service shall be responsible for all environment collision checks. It will take the desired displacement from the flight model and return a final, valid position after clamping it against the ground plane (Y=0) and the world boundaries.

- **3.3. Manual Drone Control**

  - The user shall be able to control a single drone at a time using the keyboard.

  - **Throttle:** W and S keys shall increase/decrease the drone\'s forward throttle in 10 discrete steps (on a single key press).

  - **Vertical Movement:** Q and Z keys shall make the drone move directly up and down (while held).

  - **Strafing:** A and D keys shall make the drone move directly left and right relative to its current orientation, without turning (while held).

  - **Turning (Yaw):** Left Arrow and Right Arrow keys shall turn the drone left and right (while held).

- **3.4. AI and Entity Control**

  - The simulation shall contain 9 simple AI-controlled drones.

  - The V1 AI shall use \"turn-then-burn\" logic:

    1.  The AI will be assigned a random destination on the map at a fixed altitude.

    2.  It will first turn on the spot to face the destination.

    3.  Once facing the destination, it will apply constant forward throttle to fly towards it in a straight line.

    4.  Upon reaching the destination\'s vicinity, it will be assigned a new random target.

  - The user shall be able to **possess** any drone in the simulation using the P key. Possessing a drone switches manual control to it. The previously possessed drone reverts to AI control and is assigned a new random target.

- **3.5. Camera and View Controls**

  - The camera shall be attachable to any drone. The T key shall cycle the camera attachment through all available drones.

  - The camera shall support two view modes, toggled with the C key:

    1.  **First-Person View:** From the drone\'s perspective.

    2.  **Over-the-Shoulder View:** A third-person view positioned behind and above the drone.

  - Up Arrow and Down Arrow keys shall control the vertical tilt of the attached camera (while held).

- **3.6. User Interface (UI)**

  - The simulation shall display a simple, text-based Heads-Up Display (HUD) overlaid on the 3D scene.

  - The HUD shall contain the following information:

    - Speed and Altitude

    - Throttle Level (e.g., \"4/10\")

    - Current Camera Mode and Tilt Angle

    - The ID of the drone the camera is attached to

    - A special indicator for the drone currently being controlled by the player

    - A static hint for key controls (\[P\] Possess, etc.)

**4. Future Upgrades (Post-V1 Vision)**

This V1 prototype lays the groundwork for the following planned enhancements, which are enabled by the modular architecture:

- **Physics-Based Flight Model:** Replacing the kinematic model and boundary service with a full rigid-body physics simulation using a library like BepuPhysics.

- **Advanced Terrain Generation:** Replacing the flat plane with procedurally generated terrain including hills, valleys, and obstacles that interact with the physics system.

- **Advanced AI:** Implementing intelligent pathfinding (e.g., A\* on the nav-mesh) and collision avoidance for AI drones.

- **Collision System:** Introducing damage or Crashed states when drones collide with obstacles or the ground, handled by events from the physics engine.
