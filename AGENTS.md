# Agent Guidelines for Defunc

This repository contains the source code and assets for **Defunc**, a 2D time travel platformer developed in Unity 6 for ACM Game Jam 2026.

All AI agents, coding assistants, and automated tooling operating within this repository must adhere strictly to the guidelines and constraints outlined below.

---

## ⚠️ Mandatory Permission Gates (Strict Restrictions)

Agents **MUST** ask for explicit user confirmation before executing any of the following operations:

1. **Git Merge Operations:**
   - Any `git merge` command (including merging branches, merge commits, or `git pull` operations that execute a merge).
   - Agents must detail the source and target branches/commits and request explicit permission before invoking the merge.

2. **File Deletion Commands:**
   - Any command, script, or tool action that deletes, removes, or unlinks files or directories (e.g., `Remove-Item`, `rm`, `del`, `git rm`, `git clean`, or API-level deletions).
   - Agents must list the exact paths targeted for deletion and obtain explicit confirmation before proceeding.

---

## Project Overview & Technical Stack

- **Engine:** Unity 6 (`6000.6.9f1`)
- **Render Pipeline:** Universal Render Pipeline (URP 2D)
- **Primary Language:** C# (.NET / Unity C# runtime)
- **Key Mechanics:** 2D platformer movement and instantaneous time-travel / world-state switching.
- **Key Reference Docs:**
  - [player_movement_setup.md](Documentation/player_movement_setup.md)
  - [time_travel_mechanics.md](Documentation/time_travel_mechanics.md)

---

## Unity & Asset Guidelines

1. **Unity `.meta` File Integrity:**
   - Every file and directory inside `Assets/` is paired with an associated `.meta` file containing Unity GUIDs.
   - When adding new assets, ensure corresponding `.meta` files are created or generated properly by Unity.
   - Never move, rename, or delete an asset without updating/handling its `.meta` file.
   - Never corrupt or arbitrarily regenerate existing `.meta` files as this breaks asset references across scenes, prefabs, and scriptable objects.

2. **Ignored & Cache Directories:**
   - Do not manually edit, stage, or commit files within Unity temporary/cache folders:
     - `Library/`
     - `Temp/`
     - `Logs/`
     - `UserSettings/`
     - `Build/` / `Builds/`

3. **C# Scripting Standards:**
   - Place player and gameplay scripts in organized subfolders under `Assets/Scripts/` (e.g., [PlayerMovement.cs](Assets/Scripts/Player/PlayerMovement.cs)).
   - Use `[SerializeField] private` for Inspector-exposed fields rather than public fields where appropriate.
   - Cache component references in `Awake()` or `Start()` to minimize unnecessary `GetComponent()` calls in `Update()` / `FixedUpdate()`.
   - Adhere to Unity's standard naming conventions (PascalCase for classes/methods/public fields, camelCase for local variables and private fields with optional `_` prefix).

4. **Documentation Requirements:**
   - All newly created scripts, tools, and systems must have associated documentation created in the `Documentation/` folder.
   - Documentation should detail component setup, inspector configuration, usage instructions, and any relevant technical architecture (referencing existing guides in `Documentation/`).

---

## Git & Change Management

- Keep changes modular, focused, and well-documented.
- Never force push (`git push -f` or `--force`) without explicit instruction.
- Follow git hygiene: check `git status` and `git diff` before and after modifying files.
