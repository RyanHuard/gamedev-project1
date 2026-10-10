# The Hollow Tournament

A 3D Mordhau-style directional melee game made in Unity 6 for a Halloween-themed project. A living knight fights through a dead tournament's grounds to duel the Headless Horseman.

## Status

This is an early framework commit: the combat/FSM scripting layer (player, skeleton enemies, the Horseman boss, directional swing/block/parry/kick, projectile-parry interactables, lock-on camera targeting) is implemented in `HollowTournament/Assets/Scripts/`, but scene assembly, animation import (Mixamo), models, and audio have not been wired in yet. See `GAME_SCRIPT.md`'s iteration log for what's been validated so far.

## Open the project

Open `HollowTournament/` in Unity Hub (Unity 6000.6.5f1 or compatible Unity 6 LTS). Input uses Unity's legacy Input Manager (no extra packages required): WASD to move, left mouse to swing (move the mouse during the wind-up to pick a direction), right mouse to block/parry, E to kick.

## Docs

- [HIGH_CONCEPT.md](HIGH_CONCEPT.md) - concept, teaching goals, rules, game modes, expected player, background.
- [GAME_SCRIPT.md](GAME_SCRIPT.md) - level beats, Rules of Play design schemas, FSM diagrams, iteration log.
