# Animal Rescue

Animal Rescue is a timed 2D game built in C# with .NET 10 and SDL, without
using a game engine. The player explores a map and tries to catch as many
animals as possible before time runs out.

Cats, dogs, bunnies, and foxes move independently in random directions. Each
species has a different movement speed and score value. Apples, coins, and gems
also appear on the map and provide immediate bonuses.

<!-- Add a gameplay screenshot here before submission. -->

## Objective

The player has 15 seconds to catch as many animals as possible and obtain the
highest possible score.

An animal can be caught when the player is close enough and presses the left
mouse button. A caught animal disappears for the remainder of the round.

The game ends when the timer reaches zero. The Game Over screen displays:

- the final score;
- the saved high score;
- the total number of animals caught;
- the number caught from each species;
- the accumulated money.

## Animals

| Animal | Behavior | Points |
| --- | --- | ---: |
| Cat | Fast movement | 15 |
| Dog | Medium movement speed | 10 |
| Bunny | Fastest movement speed | 20 |
| Fox | Rare and fast | 30 |

All animals use the same base logic for movement and obstacle avoidance, but
each species has a different speed. They periodically change direction and
choose a new one when they encounter an obstacle or the edge of the map.

At the beginning of each round, the game randomly generates:

- between 0 and 2 foxes;
- between 1 and 3 dogs;
- between 1 and 3 cats;
- between 1 and 4 bunnies.

## Collectibles

### Apples

Apples increase the player's movement speed by 1.5 times for 5 seconds.
Collecting another apple while the boost is active resets its duration.

### Coins

Each coin increases the player's money by one. The total amount of money is
displayed in the HUD and persisted between game sessions.

### Gems

Gems add 10 seconds to the remaining time, allowing the player to chase and
catch more animals.

At the beginning of each round, coins, apples, and gems are distributed
randomly across free map tiles. They cannot appear over obstacles, other
collectibles, animals, or immediately next to the player.

The number of collectibles also varies between rounds:

- between 6 and 12 coins;
- between 2 and 5 apples;
- between 1 and 3 gems.

## Controls

| Action | Control |
| --- | --- |
| Move | `W`, `A`, `S`, `D` |
| Catch a nearby animal | left mouse button |
| Collect a nearby object | left mouse button |
| Close and save the game | window close button |

## Scoring

The score is calculated from the points awarded for caught animals. Foxes and
bunnies are worth more points because they are faster or less common.

## Saving Progress

The game saves the following data in JSON format:

- the highest score;
- the total amount of money collected.

The saved data is loaded automatically the next time the game starts.

## Features

- main menu with a Play button;
- timed 15-second rounds;
- HUD displaying time, score, money, and the active speed boost;
- Game Over screen with score and high score;
- Retry button for immediately starting a new round;
- final statistics for the total number of animals and each species;
- persistent high score and money;
- input, update, and rendering game loop;
- tile-based 2D map;
- animated player and animals;
- camera that follows the player;
- collision detection with map elements;
- collectibles with immediate effects;
- random collectible positions and counts for each round;
- random animal counts for each species;
- independently moving foxes, dogs, cats, and bunnies;
- random direction changes and obstacle avoidance;
- mouse interaction for catching animals and collecting objects.

## Requirements

- Windows;
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

NuGet dependencies are restored automatically by the .NET CLI.

## Build and Run

From the project directory:

```powershell
dotnet restore
dotnet run
```

To build the project separately:

```powershell
dotnet build
```

## Technologies and C# Features

- C# and .NET 10;
- FontStashSharp for text rendering;
- object-oriented programming;
- generic collections such as `List<T>` and `Dictionary<TKey, TValue>`;
- inheritance for game object and animal types;
- interfaces for entity behavior;
- LINQ for searching and counting entities;
- records for spawn configuration;
- pattern matching for object states;
- `IDisposable` for releasing SDL resources;
- JSON serialization with `System.Text.Json`;
- rendering and input through Silk.NET SDL;
- image loading through ImageSharp.

## Project Structure

- `Program.cs` - application initialization and the main game loop;
- `Engine.cs` - main game state and screen transitions;
- `Engine.World.cs` - map loading, collisions, and terrain rendering;
- `Engine.Animals.cs` - animal spawning, movement, and catching;
- `Engine.Collectibles.cs` - collectible spawning and effects;
- `Engine.Rendering.cs` - game entity rendering;
- `Input.cs` - keyboard and mouse input processing;
- `GameRenderer.cs` - world, camera, text, and UI rendering;
- `Models/` - game entities and map data models;
- `Assets/` - map, sprites, animations, fonts, and collectible images.

## Sprites and assets
- Grass from https://ninjikin.itch.io/grass
- Text font, play button and houses from https://cupnooble.itch.io/sprout-lands-asset-pack 
- Dogs from https://megamicrobats.itch.io/dogpack?download#google_vignette
- Cats https://last-tick.itch.io/animated-pixel-kittens-cats-32x32
- Bunnies https://last-tick.itch.io/32x32-pixel-bunnies-animated-npc
- Foxes https://elthen.itch.io/2d-pixel-art-fox-sprites

## AI Usage
AI was used as a development assistant for following the laboratory documentation, 
explaining C# and Silk.NET concepts, 
troubleshooting compiler and rendering issues, organizing the Engine class into smaller components and assisting with random spawning logic.
Details about the AI tools used and any fully generated regions are documented
in `AI_USAGE.md`.
