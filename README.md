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



# Using Gemini with Cursor

**Gemini** is great for **strategic** stuff of large code bases

* Discussing new features or large refactors.
* Creating implementation guides that matches you source code.

**Cursor** is great for **tactical** stuff within a smaller context

* Implementing ideas sketched by gemini
* Fixing little bugs and inconsistencies that might be left in Gemini's instructions

**Process**

* **Give web Gemini relevant part of you code**
  * Using a script, concatenate all your source code files to one big TXT file and drag it to the chat prompt.
  * Add clear separators in between individual files, specifying the file path and name

```
-----------------------------------------------------------------------------
-------------------- File: ConfigEditor\Dom\ArrayNode.cs --------------------
-----------------------------------------------------------------------------
```

* **Ask Gemini to analyze given source** code and explain what you want to do

  * Find a bug - explain the bug, provide logs
  * Describe new feature/modification request
  * Ask about how it works etc.

* **Discuss/tailor the problem** with Gemini until you like the solution

* **Ask Gemini to write an implementation guide**
  * "Pls write a detailed implementation guide for feature XXX. Use well commented code."
* **Put suggested changes to your project**
  * Export the implementation guide to google docs
  * Copy to clipboard as markdown
  * Pass it to Cursor to implement.
    * *Please follow the guide below. Do not change anything else."*



# Gemini as a code generator

* Ask Gemini to **generate files one by one**, each to a separate canvas, letting **you review each one** before it generates next. 
  * Tell Gemini to make the files:
    * Well documented
    * Complete, leaving no placeholders, back-references or unimplemented parts
    * Perfectly matching your original code (if you are doing a modification  of already existing sources)
  * Before accepting, check each file if it contains what you expect, ask questions until you are happy
* Ask Gemini for a **gap analysis** between the generated code and the design documents generated earlier. 
  * Or if you are modifying existing code, the feature gap analysis between original code and the newly generated one.
* Ask Gemini for **pre-mortem bug analysis**
  * Often reveals logical, performance or user-unfriendliness bugs
* Let Gemini to **update the generated files (in its canvas)** with bug fixes. Repeat until you are happy with th eresults.
* Place the generated file to your project
  * Either copy/paste files manually
  * Or **Export** each file as **google document**, convert it to **markdown** and pass it to Cursor to implement it.



# Bugfixing complex code with Gemini

Gemini is excellent at analyzing big existing code base and helping you to pinpoint the cause of the bug

* Gemini can
  * Follow the execution path to the deepest levels.
  * Find subtle bugs/race conditions
  * Provide exact solution.

* The more info you provide (bug description, logs, console prints etc.), the more precise results you will get.
  * Without the extra info the model just guesses what could be happening. Logs tell the model what is actually happening in your code.




# Known Issues

* AI struggles with changing very long monolithic code (1000+ lines)
  * Solution: **Refactor first to smaller modules** with clear separation of concerns
* AI likes to refactor other stuff you didn't tell it to do
  * Changing/removing comments
  * Changing variable names
  * etc.
  * Solution: Add **"Do not change anything else."** to your prompt.



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

