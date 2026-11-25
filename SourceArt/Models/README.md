# Model sources

The player, snowman and snowball are built by Python scripts run in Blender (5.3) without opening it.
Only the scripts are kept here; everything they generate (`.blend`, `.fbx`, `.png`) is ignored by git.
The files the game uses live in `Assets/_Project/Art/`.

## Rebuild

From this folder, textures first because the two models load them:

```bash
blender -b --factory-startup -P textures/build_textures.py
blender -b --factory-startup -P player/build_player.py
blender -b --factory-startup -P snowman/build_snowman.py
```

Each model script writes its `.blend`, its `.fbx` and a few `preview-*.png` renders next to itself.
Then copy the results over the ones in the project (same names, so the `.meta` files and import settings stay):

| Generated file | Goes to |
|---|---|
| `player/Player.fbx` | `Assets/_Project/Art/Models/Player/` |
| `snowman/Snowman.fbx`, `snowman/Snowball.fbx` | `Assets/_Project/Art/Models/Snowman/` |
| `textures/*.png` | `Assets/_Project/Art/Textures/` |

## Conventions

- Blender: metres, Z up, models face -Y, stand on Z = 0 and are centred in X/Y. In Unity they come in upright and facing +Z.
- Built sizes: player 1.0 tall, snowman about 1.27 tall, snowball exactly 1.0 across.
- In Unity, `Player.fbx` and `Snowman.fbx` are imported with **Scale Factor 1.6** (player 1.6 units tall, snowman about 2.0),
  so prefabs stay at scale 1. `Snowball.fbx` is imported at 1: the game scales the ball in code. A full ball is meant to be 1.28 across.
- Pieces are separate objects so the game can hide, tint or detach them. Keep these names:
  - Player: `Body`, `Hat`, `ScarfWrap`, `ScarfTail`, and the empty `ScarfAnchor` (where the tail leaves the neck).
  - Snowman: `BodyBall`, `HeadBall`, `Nose`, `Eyes`, `Buttons`, `ArmLeft`, `ArmRight`, `ScarfWrap`, `ScarfTail`.
- Material names, matched by name to the materials in `Assets/_Project/Art/Materials/`:
  `Wood`, `PlayerColour`, `Snow`, `FaceDark`, `Carrot`, `Stick`.
- `PlayerColour` is the only material on hats and scarves (player and snowman): changing its colour recolours them.

## Unity materials

All are URP Lit, smoothness about 0.2, tiling 1,1 (the repeats are baked into the UVs).

| Material | Base map | Base colour | Normal map |
|---|---|---|---|
| `Wood` | `wood-albedo.png` | white | none |
| `PlayerColour` | `knit-albedo.png` (greyscale) | the player colour, default blue `#3E5BA9` | `knit-normal.png`, imported as a normal map, strength 1 |
| `Snow` | none | `#F4F7FA` | none |
| `FaceDark` | none | `#4A3A33` | none |
| `Carrot` | none | `#F08A2C` | none |
| `Stick` | none | `#7A4E2D` | none |

Textures: wrap mode Repeat, mipmaps on. Player colours: red `#C9463D`, blue `#3E5BA9`, green `#4E9A5B`, yellow `#E8B63A`.
