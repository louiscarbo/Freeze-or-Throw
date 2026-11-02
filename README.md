# Freeze or Throw

4-player online top-down party game: roll snowballs, stack them into snowmen, sabotage everyone else.
Course project for *Entretenimiento y Videojuegos* (ETSIInf, 2026-27).

| | |
|---|---|
| Engine | **Unity 6.3 LTS** (exact patch: see `ProjectSettings/ProjectVersion.txt`) |
| Template | Universal 3D (URP) |
| Input | Input System package |
| UI | uGUI + TextMeshPro |
| Multiplayer | Photon Fusion 2, Shared Mode (added in December) |
| Team | Louis Carbo Estaque, Daniel Banegas, Elise Bachet, Onur Sen |

---

## 1. First-time setup (everyone)

1. Install **Unity Hub**, then the **exact Unity version** listed in `ProjectSettings/ProjectVersion.txt`
   (Hub → Installs → Install Editor → Archive if it's not in the list). A different patch makes Unity
   rewrite files and creates noisy diffs.
2. Install **Git LFS** and enable it once per machine:
   ```bash
   # macOS
   brew install git-lfs
   # Windows: it comes with Git for Windows / GitHub Desktop
   git lfs install
   ```
   GitHub Desktop users: it handles LFS for you.
3. Clone the repo:
   ```bash
   git clone https://github.com/<owner>/<repo>.git
   ```
4. Unity Hub → **Add → Add project from disk** → pick the cloned folder. The first open takes a few
   minutes (Unity rebuilds `Library/`).
5. Set up Smart Merge (section 4). It takes one minute and saves hours.
6. Create your sandbox scene if it doesn't exist yet: `Assets/_Project/Scenes/Sandbox/<YourName>.unity`.

## 2. Folder structure

The Unity project is the repo root. All of **our** files live under `Assets/_Project/`.
Third-party packages (Photon, Asset Store packs) stay in their own folders outside `_Project`.

```
Assets/
  _Project/
    Art/
      Models/      Player/  Arena/  PowerUps/          (.fbx exported from Blender)
      Materials/
      Textures/
      UI/          Icons/  Buttons/  Panels/  Backgrounds/  Fonts/
    Audio/         Music/  SFX/
    Input/         PlayerControls.inputactions
    Prefabs/       Player/  Snowball/  Snowman/  Arena/  Events/  PowerUps/  UI/  Network/
    Scenes/
      Main/        Menu.unity  Game.unity
      Sandbox/     Louis.unity  Daniel.unity  Elise.unity  Onur.unity
    Scripts/       Core/  Player/  Snow/  Arena/  Events/  PowerUps/  Network/  UI/
    Settings/      URP assets, physics materials
  Photon/          (added in December)
SourceArt/         Optional: .blend / source files. NEVER inside Assets/.
```

The `.gitkeep` files only exist so git keeps empty folders. Leave them; Unity ignores them.

## 3. Git LFS

`.gitattributes` already sends binaries to LFS: images (`.png .jpg .psd ...`), models (`.fbx .obj .blend`),
audio (`.wav .mp3 .ogg`), fonts (`.ttf .otf`), DLLs and archives.
Unity's own text files (`.unity .prefab .mat .asset .meta`) stay in normal git so they can be diffed and merged.

- GitHub's free plan has a small LFS quota (storage **and** download bandwidth). Commit only the files
  the game actually uses. Keep `.blend`, layered `.psd` and uncompressed recordings in Drive, or in `SourceArt/`
  only if they're small.
- Check what went into LFS: `git lfs ls-files`.
- If you see an image or model in a diff as text gibberish, LFS isn't installed on your machine: run `git lfs install`.

## 4. Smart Merge (UnityYAMLMerge)

Scenes and prefabs are big YAML files. `.gitattributes` routes them through Unity's merge tool, but each
person must tell git where that tool is. Run once (replace `<version>` with the one in `ProjectVersion.txt`):

**macOS**
```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "'/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/Tools/UnityYAMLMerge' merge -h -p --force %O %B %A %A"
git config --global merge.unityyamlmerge.recursive binary
```

**Windows** (Git Bash)
```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -h -p --force %O %B %A %A"
git config --global merge.unityyamlmerge.recursive binary
```

Smart Merge is a safety net, not a plan. If a scene merge still breaks, keep one version
(`git checkout --theirs path/to/Scene.unity` or `--ours`) and redo the small change by hand.

## 5. Branch workflow

- `main` **always opens and plays**. Nobody pushes to `main` directly.
- One branch per task, named `<name>/<task>`, e.g. `daniel/snowball-rolling`, `louis/hud-timer`.
  Keep it short-lived (1 to 3 days).
- Pull `main` into your branch often: `git pull origin main`.
- Open a **pull request**; one other person glances at it and merges. Use the PR checklist.
- Before you commit, look at the file list. Every new or moved asset has a matching `.meta` file.

### Rules that prevent 90% of Unity merge pain

1. **One owner per scene.** Only the owner edits and commits it. Everyone else asks, or works in their sandbox.
2. **Everyone has a sandbox scene** (`Scenes/Sandbox/<Name>.unity`). Test freely there.
3. **Build everything as prefabs.** `Game.unity` should be mostly prefab instances; editing a prefab doesn't touch the scene.
4. **Split big prefabs** into nested prefabs with one owner each (e.g. Player contains a Scarf prefab).
5. **Shout before touching shared files** (`ProjectSettings/`, `Packages/manifest.json`, `PlayerControls.inputactions`, `Game.unity`) in the group chat, then commit quickly.
6. **Move and rename assets inside Unity's Project window**, never in Finder/Explorer, so the `.meta` follows.
7. Editor settings stay: Asset Serialization = **Force Text**, Version Control = **Visible Meta Files**.

## 6. Who owns what

Owner = the only person who edits those scripts, prefabs and scenes (not the only one with ideas about them).

| Person | Systems (scripts) | Prefabs & assets | Scenes |
|---|---|---|---|
| **Louis** | UI and menus; match manager (7-min timer, score, winner, respawn rule); later sound | UI prefabs (HUD, countdown, rankings), 2D icons/buttons, audio | `Menu.unity`, `Sandbox/Louis` |
| **Daniel** | Player controls (move, tank controls while rolling, push, grab, drop); snowball (roll, grow, max size, freeze penalty, throw, stun); snowman building | Player, Snowball, Snowman prefabs; player model; Input Actions asset | `Sandbox/Daniel` |
| **Elise** | Arena; snow field (grid, depletion, regen); water/holes ring-out; snowstorm and geyser events | Island model, obstacles, event prefabs | `Game.unity`, `Sandbox/Elise` |
| **Onur** | Multiplayer (Photon Fusion), lobby connection, voice chat; power-ups | Network prefabs, power-up prefabs | `Sandbox/Onur` |

Louis owns the repo and keeps `main` healthy. Architecture lead: _to confirm at the first meeting_.

## 7. Connections between systems (draft: agree at the first meeting)

Each one is a small public method or event that one person writes and another calls.

| Connection | Written by | Used by | API |
|---|---|---|---|
| Snowball takes snow from the ground | Elise | Daniel | `SnowField.TakeSnow(position, radius)` → amount taken |
| Geyser melts nearby snowballs | Daniel | Elise | `Snowball.Melt()` |
| A snowman was built | Daniel (raises) | Louis | event `OnSnowmanBuilt(ownerId)` |
| A player fell in the water | Elise (raises) | Daniel, Louis | event `OnPlayerRingOut(player)` |
| Power-ups affect other systems | Daniel / Elise | Onur | `Snowball.SetOwner(...)`, `SnowField.Freeze(area, seconds)`, `Snowman.SetOwner(...)`, `Snowman.Destroy()` |
| Player identity and scarf color | Onur | everyone | player id + color (red, blue, green, yellow) |

Writing code that converts to Fusion easily: keep input reading separate from movement, keep game
state in a few clear fields (size, owner, frozen, score), and avoid `static` singletons that hold state.

## 8. Scenes

Only two real scenes. The 7 GDD screens are UI panels, not scenes.

| Scene | Contains | Owner |
|---|---|---|
| `Scenes/Main/Menu.unity` | Home, Options, Join, Lobby (panels shown/hidden) | Louis |
| `Scenes/Main/Game.unity` | Island, players, snowballs + Countdown, HUD, Rankings as UI prefabs | Elise (scene), Louis (UI prefabs) |
| `Scenes/Sandbox/<Name>.unity` | Anything | each person |

Build order (File → Build Profiles → Scene List): `Menu` first, then `Game`.

## 9. UI conventions

Designs come from the Figma file (final page: *Final UI – Fireside cabin*).

- **uGUI** (Canvas + Image + Button) with **TextMeshPro** text, font **Baloo 2** (Medium / Bold / ExtraBold) in `Art/UI/Fonts/`.
- Canvas Scaler: *Scale With Screen Size*, reference **1512 × 982**, match **Height** (1).
- Export sprites from Figma as **PNG @2x**. Import as *Sprite (2D and UI)*, Mesh Type *Full Rect*, compression off or high quality.
- Use **9-slice** (Sprite Editor borders + Image Type *Sliced*) for panels and buttons so they stretch cleanly.
- Keep text out of button images: put the label in a TMP child so it can change and be translated.
- Each screen is a prefab in `Prefabs/UI/`, so `Menu.unity` and `Game.unity` only hold instances.

## 10. Key dates

| Date | Milestone |
|---|---|
| 6 Nov 2026 | Everyone can clone, open, push |
| 17 Nov | Test exam 1 |
| 18 Nov | Demo freeze on `main` |
| 20 Nov | Preliminary presentation: offline prototype |
| 1 & 4 Dec | Photon Fusion classes |
| 15 Dec | Test exam 2 (feature freeze before) |
| 22 Dec | Final delivery: online 4-player match, menus, sound, GDD v2 |
