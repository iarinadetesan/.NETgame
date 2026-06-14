# AI Usage

## Tools Used

- OpenAI Codex (GPT-5)

## How AI Was Used

AI was used as a development assistant throughout the project. Its main uses
were:

- explaining C# concepts, syntax, nullable reference 
types, LINQ, records, and `IDisposable`;
- explaining Silk.NET and SDL concepts, including input processing, texture
  rendering and resource cleanup;
- providing additional explanations to the laboratory documentation 
(labs 1 through 9) which were followed step by step when creating this game.
- help with creating and editing the tile map with Tiled;
- discussing game mechanics, collectible effects, scoring, timers, and the
  overall structure of the game;
- suggesting ways to separate the original `Engine` class into smaller partial
  class files with clearer responsibilities;
- helping diagnose compiler errors, warnings, coordinate problems, and
  resource-management issues;
- implementing random spawn logic for animals and collectibles, including spawn ranges and overlap prevention;
- helping use FontStashSharp for text rendering, therefore helping implement the main menu and HUD text;
- reviewing the project against the assignment requirements;
- helping write and update project documentation.

All AI-assisted code was reviewed, tested, and adjusted to fit the existing
project.

## Fully AI-Generated Regions

The following regions were generated with AI assistance and then
reviewed and integrated into the project:

- the random animal spawn configuration and spawn-range logic in
  `Engine.Animals.cs`;
- the random spawn configuration of collectibles in `Engine.Collectibles.cs`;
- the FontStashSharp SDL renderer integration in `SdlFontRenderer.cs`;
- parts of the menu, HUD and texture cleanup logic in
  `GameRenderer.cs`;
- parts of the game state logic in `Engine.cs`;

These regions are highlighted with comments:  `// AI-generated` at the beginning, and ` // end AI-generated` respectively.


