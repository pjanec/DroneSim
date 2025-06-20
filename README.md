**Principles:**

# Design first

- **Prepare a thorough design using a full-fledged (web-based) AI model.**
  - **Go to details**
    - Clarify everything down to the smallest detail. Every later change is difficult.
    - Ask what else the model needs clarified.
    - Think about what we might be missing, as if we were implementing it ourselves.
  - **Stay in control**
    - Have the model **explain how stuff work**, for example how the individual parts work together.
      - *How XXX works?*
      - *How do we  get the value of YYYY?*
      - *Why ZZZZ is using AAA?*
      - *What if we use AAA instead of BBB?*
    - **Understand**, assess suitability, and discuss the solution until we are fully satisfied.
    - **Ask for criticism of your ideas** and their suitability for the overall solution. Don't fall for model's flattery.
      - *Be objective. Take a critical look and provide pros and cons of the ideas.*
    - **Keep attention**, the model may push its own solutions which might not suit you.
- **Have the model generate specifications.**
  - **Requirements** – what we are trying to achieve.
  - **Architecture** – how the solution will work in principle.
    - Modules, responsibilities
    - Dependencies, relationships, main operation sequences
    - etc.
  - **Detailed description of individual modules** with well-commented code skeletons and unit test designs.
  - **Proposal of integration tests**.
  - **Solution structure** – what projects will be included in solution

# Implement with Cursor

Let the Cursor to implement according to your design

- **Use Markdown docs **
  - Add specs to project folder where you can easily drag&drop them to cursor's chat window
  - The better the guides and specifications,
    - the more chance you will get what you needed,
    - the fewer prompts will be needed and less “requests” will be spent.
- **Proceed in stages**
  - **Break work to smaller parts.**
    - Do not overwhelm the model with too much work at once, or in one long chat!
    - The model has a short memory. After a while, it loses context and starts making mistakes.
  - **Keep model informed.**
    - For the next stage, open a new chat and feed the necessary docs to the model again.
    - Always feed the basic specification, architecture etc., so model has the necessary context.
- **Keep watching what the model produces**
  - Even if code compiles, it might not be exactly what you wanted.
  - The later we discover a problem, the harder it is to fix.

# Useful prompts

* *Comment the code thoroughly. Do not change existing comments unless they are wrong.*
  * Without this, you might not receive any code comments.
  * Model likes to rephrase comments, which might remove important info you added by hand.
* *Do just minimal necessary changes.*
  * Model likes to refactor stuff that is not directly related to your task.
  * Extremely important if adding features to an already existing codebase.
* *Implement the feature in full, leave no unfinished parts.*
  * The model is lazy, it tends to postpone more difficult implementation to later time, adding just some TODO comments instead.



# Prompts used



```
------------------------------------------------------
pls read the docs in @docs/v1.3 folder. Make a clear idea what we are building. Summarize it, as well as the implementation plan. Put the sources to the @/src folder. Start with project structure as described in @DroneSim Solution Setup v1.3.md Create the solution and projects skeletons and make sure it builds. Stop once finished with the project structure. Don't forget xunit test projects for each library.
------------------------------------------------------
*** starting  a NEW CHAT to keep the AI model's state clear
------------------------------------------------------
pls read the @DroneSim Architecture v1.3.md @DroneSim Requirements v1.3.md @DroneSim Core v1.3.md  @DroneSim Player Input v1.3.md @DroneSim Physics v1.3.md @DroneSim Flight Dynamic v1.3.md @DroneSim Testing v1.3.md 

implement the Core library, PlayerInput, Physics, Flight dynamics, including their unit tests, as suggested in the docs. 

document the code well.

make sure it builds.

make sure the the unit tests are passing - run them and fix the troubles.
------------------------------------------------------
*** starting  a NEW CHAT to keep the AI model's state clear
------------------------------------------------------
pls read the @DroneSim Architecture v1.3.md @DroneSim Requirements v1.3.md @DroneSim Core v1.3.md @DroneSim Debug Draw v1.3.md  @DroneSim Testing v1.3.md @DroneSim Terrain Gen v1.2.md @DroneSim Renderer v1.3.md 

implement the debug draw, terrain gen and the renderer, including their unit tests where applicable. 

Do not change anything else. Keep existing comments.

document the code well.


make sure it builds.

make sure the the unit tests are passing - run them and fix the troubles.
------------------------------------------------------
** Model was lazy, rendere left unfinished, need to try again
------------------------------------------------------
pls read the @DroneSim Architecture v1.3.md @DroneSim Requirements v1.3.md @DroneSim Core v1.3.md @DroneSim Debug Draw v1.3.md  @DroneSim Testing v1.3.md @DroneSim Terrain Gen v1.2.md @DroneSim Renderer v1.3.md 

finish the detailed implementation of the renderer. it seems to be just a draft. Implement in full, render the terrain, the drones, the debug view, HUDs, handle camera placement/attachment etc.

Do not change anything else. Keep existing comments.

document the code well.


make sure it builds.
------------------------------------------------------------
*** again unfinished, blovked by some stupid error. Sign of fatique. Model needs a new chat window.
------------------------------------------------------------
pls read the @DroneSim Architecture v1.3.md @DroneSim Requirements v1.3.md @DroneSim Core v1.3.md @DroneSim Debug Draw v1.3.md  @DroneSim Testing v1.3.md @DroneSim Terrain Gen v1.2.md @DroneSim Renderer v1.3.md 

finish the detailed implementation of the renderer. it seems to be missing some parts and can not be compiled. Implement in full, render the terrain, the drones, the debug view, HUDs, handle camera placement/attachment etc.

Do not change anything else. Keep existing comments.

document the code well.

make sure it builds.
------------------------------------------------------------
*** blovcked on wrong code genertaed
     fixed (float* viewPtr = &view.M11)
ChatGPT adviced
      void* viewPtr = Unsafe.AsPointer(ref view);
Same solution adviced to Curson, helped...
------------------------------------------------------
*** starting  a NEW CHAT to keep the AI model's state clear
------------------------------------------------------
pls read the @DroneSim Architecture v1.3.md @DroneSim Requirements v1.3.md @DroneSim Core v1.3.md @DroneSim Debug Draw v1.3.md  @DroneSim Testing v1.3.md @DroneSim Autopilot v1.2.md @DroneSim Spawner v1.3.md @DroneSim Orchestrator v1.3.md @DroneSim Main Application v1.3.md 

implement the spawner, autopilot, orchestrator, main app and other remaining parts , including their unit tests where applicable. 

implemetn each module fully, leave no unfinished parts

Do not change anything else. Keep existing comments.

document the code well.


make sure it builds.

make sure the the unit tests are passing - run them and fix the troubles.

------------------------------------------------------
*** starting  a NEW CHAT to keep the AI model's state clear
Model used ConsoleRenderer draft without keyboard handling.
We wanted Silk.NET renderer its IKeyboard.
------------------------------------------------------
@DroneSim Architecture v1.3.md @DroneSim Requirements v1.3.md 

pls instead of ConsoleRenderer use V1SilkNetRenderer.

use proper IKeyboard instance from the Silk.Net in void IPlayerInput.Update(object keyboardState) - fix how this method gets called

you migh need to extend the interface to get the IKeyboard object from the renderer.

---------------------------------
*** App can be compiled and run.
*** Drone flying, just turning left/right not functional
*** Needed to fix missing quaternion init to Identity (was all zeroes)
------------------------------------
```

