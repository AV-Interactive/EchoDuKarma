# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Echo du Karma** is a 3D RPG built with **Godot 4.6** and **C# (.NET)**. It features a 3D world with 2D billboard sprites (HD-2D aesthetic), turn-based combat, NPC dialogues, and progression systems driven by CSV data files.

- Engine: Godot 4.6, GL Compatibility renderer
- Language: C# (namespace `EchoduKarma`)
- Physics: Jolt Physics (3D)
- Resolution: 1920×1080

## Building & Running

This project has no CLI build commands — it must be opened and run from the **Godot 4.6 editor**.

To compile the C# solution manually (for error checking):
```bash
dotnet build "Echo du Karma.sln"
```

There are no automated tests in this project.

## Architecture

### Autoloaded Singletons (Global/)
Three autoloads are registered in `project.godot` and accessible statically from anywhere:

- **`GameManager`** — Central orchestrator. Holds `CurrentPlayer`, `ListEnemiesBattle`, handles scene transitions, and dispatches post-dialogue events via an `_eventLibrary` dictionary (keys: `BATTLE`, `TELEPORT`, `CHANGE_SCENE`, `GOLD`, `ITEM`, `LEVEL_UP`).
- **`DialogueSystem`** — Loads dialogue CSV per zone, manages `DialogueLine` chains, emits `DialogueRequested`, `ChoiceSelected`, and `ActionTriggered` signals.
- **`Bestiary`** — Loads `Datas/Bestiary/bestiary.csv` at startup, provides `GetEnemy(name)` returning `EnemyStats`.

### Battle System (Scripts/Battle/)
The battle flow is a **state machine** in `BattleManager`:
`Setup → Selection → Action → Evaluation → Victory/Defeat`

- `BattleManager` orchestrates turns, spawning, damage calculation, and scene transitions.
- `IBattler` interface is implemented by both `Player` and `Enemy`, enabling polymorphic combat logic.
- `CameraDirector` manages three named `Camera3D` shots (`Neutral`, `PlayerAttack`, `EnemyAttack`) with fade transitions.
- `BattleHud` (CanvasLayer) communicates back to `BattleManager` via the `ActionSelected` signal.
- Battle is triggered by `GameManager.StartBattle()`, which populates `ListEnemiesBattle` from the Bestiary, then calls `GetTree().ChangeSceneToFile("res://Maps/Battles/Basic.tscn")`.

### Player & Stats
- `Player` (CharacterBody3D) implements `IBattler` and delegates all stat storage to a child `StatHandler` node.
- `StatHandler` reads a CSV file (set via `[Export] DataFilePath`) to populate a `Dictionary<int, Stats>` keyed by level.
- `SkillManager.LoadSkills()` reads `Datas/Persos/skills.csv` and returns all skills. `Player._Ready()` then filters them by class (currently hardcoded to `"Magus"`).

### Initiative & combat rounds (`Scripts/Data/CombatInitiative.cs`, `BattleManager`)
- Each **round**: enemies plan (IA) → player chooses → sort by initiative (desc) → execute all.
- Formulas: skill = `AgiEffective + skill.Speed`; physical/defend = `AgiEffective`; flee = `AgiEffective + 5`; enemy skill = `Agi + skill.Speed` (bestiary `Skills` → `skills.csv`); enemy melee = `Agi` only.
- See `_GDD/GDD_systeme_initiative.md` and `_GDD/GDD_systeme_ia_ennemis.md`.

### Enemy AI (`Scripts/Data/EnemyTurnPlanner.cs`)
- Plans **skill**, **melee**, or **defend** per round from `IA` + `Skills` + HP/MP.
- Patterns: `Aggressive`, `Defensive`, `Normal` — see `_GDD/GDD_systeme_ia_ennemis.md`.

### Game design docs (`_GDD/`)
Full gameplay reference synced with code: start at `_GDD/GDD_INDEX.md` (combat, karma, elements, initiative, CSV tables, economy, quests).

### Elemental Combat (`Scripts/Data/ElementCombat.cs`)
- Cycle: **Fire → Earth → Air → Water → Fire** (strong ×1.5, weak ×0.75 per cycle check).
- **Affinity synergy**: skill element matches battler affinity → ×1.25.
- **Two cycle checks** on offense: skill vs target affinity, and attacker affinity vs target affinity (multiplicative). Applies to magic and physical melee (player and enemies).
- Data: `heroes.csv` (`Affinity`), `bestiary.csv` (`Affinity`), `skills.csv` (`Élément`). See `_GDD/GDD_systeme_elementaire.md`.

### Data / CSV Convention
All game data lives in `Datas/` as CSV files:
- `Datas/Bestiary/bestiary.csv` — enemy stats + `Affinity` + `Skills` (`sort1|sort2`)
- `Datas/Persos/heroes.csv` — hero `Classe`, `Affinity`
- `Datas/Persos/skills.csv` — skills (11 columns, class filter on col 9, `Élément` column)
- `Datas/Persos/equipments.csv` — equipment data
- `Datas/Persos/<ClassName>/progression-*.csv` — per-class level stat tables (loaded by StatHandler)
- `Datas/Progress/<ZoneName>/dialogues.csv` — zone dialogues loaded on scene enter via `MapLoader`

Dialogue CSV columns: `ID, TYPE (TEXT/CHOIX), PNJ, TEXTE, CONDITION ACCES, ACTION POST DIALOGUE, LIEN SUIVANT`

Actions in dialogue use the format `ACTION_KEY:arg1:arg2`, parsed by `GameManager.OnActionTriggered()`.

### Dialogue Flow
`MapLoader` (placed in each map scene) calls `DialogueSystem.LoadZoneDialogues(ZoneName)` on `_Ready`.
NPC interaction → `Npc.AdvanceDialogue()` → `DialogueSystem.RequestDialogue()` → `DialogueRequested` signal → UI (`Dialogue.cs`).
For CHOICE nodes, `DialogueSystem.SelectChoice(nextId)` advances the chain.

### World (Scripts/World/)
- `GrassSpawner` / `PropSpawner` — procedural 3D prop placement.
- `CameraFollow3D` — follows the player in 3D, also exposes `MapMin`/`MapMax`/`BorderMargin` used by `Player` to clamp its position.

## Key Conventions

- **Singleton pattern**: All autoloads use `public static T Instance { get; private set; }` set in `_Ready()`.
- **Signal-based decoupling**: Systems communicate via Godot signals rather than direct calls (e.g., `BattleManager.BattleEnded`, `BattleHud.ActionSelected`, `DialogueSystem.ActionTriggered`).
- **CSV as source of truth**: Never hardcode stats or dialogue — always extend the appropriate CSV.
- **3D projection for UI**: Use `camera.UnprojectPosition(node.GlobalPosition)` to place 2D UI elements (damage numbers, target cursor) over 3D entities.
- **Billboard sprites**: Entities use `Sprite3D` with `BillboardMode.Enabled` for the HD-2D look. Enemy textures are loaded from `Assets/Actors/Enemies/<enemyname>.png` (lowercase).
- **`CallDeferred`**: Used heavily for initialization that requires the scene tree to be fully ready (e.g., `InitializeBattle`, `ConnectToSignals`).
