**Principles:**

- **Prepare a thorough design using a full-fledged (web-based) AI model.**
  - Clarify everything down to the smallest detail. Every later change is difficult.
  - Ask what else the model needs clarified.
  - Think about what we might be missing, as if we were implementing it ourselves.
  - Have the model explain how the individual parts work together.
  - Understand, assess suitability, and discuss the solution until we are fully satisfied.
    - The model may suggest its own solutions, which may not suit us.
- **Have the model generate specifications.**
  - **Requirements** – what we are trying to achieve.
  - **Architecture** – how the solution will work in principle.
    - Modules, responsibilities
    - Dependencies, relationships, main operation sequences
    - etc.
  - Detailed description of individual modules with well-commented code skeletons and unit test designs.
  - *Write a detailed implementation guide. Include fully documented interfaces and classes skeletons with all important fields and methods. Comment their purpose, responsibility, relations and usage.*
  - Proposal of integration tests.
  - **Solution structure** – what projects will be included
- **Create a project directory, insert documentation as Markdown.**
- **Let Cursor implement the solution in smaller parts (not all at once).**
  - Always feed it the basic specification, architecture, etc., so it has the context.
- **Do not overwhelm the model with too much work at once.** The model has a short memory. After a while, it loses context and starts making mistakes.
- **Proceed in smaller blocks.** For the next block, open a new chat.
- **Prepare descriptions of the desired solution as Markdown documents**, and feed them to the model whenever we start a new chat.
  - The better the guides and specifications, the fewer prompts are needed and the better the cost-efficiency of paid “requests.”
- **Review what the model produces.** Even if it translates, it might not be exactly what we wanted.
  - The model is lazy, it tends to postpone things.
  - The later we discover a problem, the harder it is to fix.



Prompts used:



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
Model použil ConsoleRenderer a nedodal správnou obsluhu klávesnice.
My chceme Silk.NET renderer a IKeyboard objekt z jeho okna.
------------------------------------------------------
@DroneSim Architecture v1.3.md @DroneSim Requirements v1.3.md 

pls instead of ConsoleRenderer use V1SilkNetRenderer.

use proper IKeyboard instance from the Silk.Net in void IPlayerInput.Update(object keyboardState) - fix how this method gets called

you migh need to extend the interface to get the IKeyboard object from the renderer.

---------------------------------
***  apka can be compiled and run
*** drone flying, just turning left/right not functional
*** needed to fix missing quaternion init to Identity (was all zeroes)
------------------------------------
```

