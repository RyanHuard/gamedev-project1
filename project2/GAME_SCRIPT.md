# The Hollow Tournament - Game Script

Companion document to `HIGH_CONCEPT.md`. This document covers level-by-level beats, the grad-level design schemas and FSM requirements, and the iteration log. See `HollowTournament/` for the Unity project.

## Level Beats (single level, ~6-8 minutes)

| Section | Teaches | Twist / New Use |
|---|---|---|
| Courtyard | Directional swing on armored scarecrow dummies | Dummies rotate their armored side, forcing the player to read and react instead of memorizing a pattern |
| Gatehouse | Block vs. a basic skeleton | A second skeleton joins, forcing the player to hold block while turning to track the bigger threat |
| Armory | Parry vs. an armored skeleton that only takes riposte damage | A witch lobs pumpkin bombs; the same parry timing is reused to send a bomb back at her instead of a sword |
| Chapel | Kick to break a shield skeleton's guard | A kicked-loose skull can be kicked a second time into a bell (opens the gate) or into a crowd of skeletons (staggers the group) |
| Arena | All four abilities together | Headless Horseman boss, three phases (below) |

### Boss Fight: The Headless Horseman

1. **Mounted charges** - the Horseman rides the player down with a couched lance. The player must parry the lance (same 0.2s-before-impact timing as every other parry in the game) to unhorse him. This is the joust moment promised in the concept.
2. **On-foot duel** - standard directional attacks mixed with feints (a wind-up that cancels before the swing lands) that punish a player who parries on reflex instead of on the actual read.
3. **Flaming pumpkin head** - at low health the Horseman throws his own head. The player parries the thrown head back at him to finish the fight, reusing the pumpkin-bomb parry skill from the Armory on the final, highest-stakes throw of the game.

Every ability taught gets at least one reuse in a context that isn't "hit the dummy/skeleton with it": swing is reused via the boss's directional duel, block is reused as the chassis parry is built on top of, parry is reused on two different projectiles and a lance, and kick is reused as an environmental tool (bell, crowd) before ever appearing again as a guard-break on the shield skeleton and the boss.

## Design Schemas (Rules of Play)

Per Salen & Zimmerman's *Rules of Play*, a game can be read through three schemas - **Rules** (the game as a formal system), **Play** (the game as lived experience), and **Culture** (the game as a cultural artifact in a larger context). Below is how The Hollow Tournament's final design connects to each.

### Rules schema (formal system)

The Hollow Tournament is, formally, a rock-paper-scissors-with-timing system layered on top of a positional read:

- **State:** player HP, player stamina, enemy HP, enemy guard-side, and a shared "who currently has initiative" flag implied by wind-up/recover windows.
- **Actions:** swing-left, swing-right, swing-overhead, block, parry (a sub-case of block defined purely by timing), kick, move.
- **Resolution procedure:** `TryReceiveDamage` (see `Assets/Scripts/Combat/CombatTypes.cs`) is the single formal arbiter - every hit in the game, whether a sword swing, a kicked skull, or a thrown pumpkin, resolves through the same `IDamageable` contract, so the formal rules are identical regardless of the attack's visual dressing.
- **Win/lose conditions:** the level is won when the Horseman's `HealthComponent` reaches 0 in the Head Throw phase; it is lost (locally) whenever the player's `HealthComponent` reaches 0, restarting from the last checkpoint.
- **Formal elegance constraint:** guard side (`GuardSide`) and swing direction (`SwingDirection`) are deliberately the same three-value enum shape (Left/Right/Overhead) so that "did this attack match the defender's open side" is one equality check, not a special-cased table per enemy type. Armored and Shield variants don't add new formal states - they add a gate function (`TryReceiveDamage`) in front of the same resolution procedure, which is why new enemy "rules" never required a new FSM.

### Play schema (lived experience)

Formally identical actions are made to feel different moment to moment:

- Reading a stationary dummy (Courtyard) is a calm, nearly puzzle-like experience; the same read against two circling skeletons (Gatehouse) becomes a tense, attention-splitting experience, even though the underlying block/parry math hasn't changed.
- The parry window is tuned tight (~0.2s) specifically so landing one feels like a skill moment worth a satisfying sound cue, per the brief's note that "playtesters will tell you it matters" - the experiential payoff of a parry has to outweigh the formal risk of a mistimed block.
- The boss's Feint state exists purely for the play schema: formally it's a no-op wind-up, but experientially it converts the player's confidence from phase 2 back into uncertainty, which is the intended emotional arc of a tournament "final boss showing off."

### Culture schema (game as cultural artifact)

- The game explicitly reframes Halloween horror iconography (skeletons, a headless rider, a haunted castle) as sport and comedy rather than threat - skeletons "rattle apart" instead of gruesomely dying, and the Horseman is written as a showboating champion, not a reaper. This is a deliberate cultural move: it borrows the *recognizability* of Halloween horror conventions while opting out of the genre's usual tone, which is why the assignment's "theming, not necessarily horror" framing is handled through costume and event design (pumpkins exploding into candy, a literal tournament structure) rather than through atmosphere or fear.
- The combat vocabulary (directional swings, parries, riposte) deliberately echoes a recognizable subgenre (Mordhau/For Honor-style melee) so a player arriving with that cultural context has an immediate read on the game's conventions, while the Courtyard section exists so a player without that context is not excluded.

## Finite State Machines

All three FSMs below are implemented as explicit state-object machines (`Assets/Scripts/Core/StateMachine.cs`) rather than Animator-only state graphs, so transition logic lives in testable C# instead of being implicit in animation wiring. Animator parameters are still driven from each state's `Enter`/`Tick` for visual feedback.

### Player FSM (`Assets/Scripts/Player/PlayerStates.cs`)

```
                     ┌─────────────┐
      ┌─────────────▶│  Locomotion │◀─────────────────────┐
      │               └──────┬──────┘                      │
      │        mouse1 down   │                              │
      │                      ▼                              │
      │               ┌─────────────┐                       │
      │               │   WindUp    │  (samples mouse        │
      │               │             │   movement for          │
      │               └──────┬──────┘   direction)            │
      │        mouse1 up     │                              │
      │                      ▼                              │
      │               ┌─────────────┐                       │
      │               │   Release   │  (hitbox active,       │
      │               │             │   direction locked)    │
      │               └──────┬──────┘                        │
      │          timer       │                              │
      │                      ▼                              │
      │               ┌─────────────┐    timer               │
      └───────────────│   Recover   │───────────────────────┘
                       └─────────────┘

  Locomotion ──mouse2 down──▶ Block ──(hit within 0.2s)──▶ [parry] ──▶ Riposte ──▶ Locomotion
                                │ mouse2 up                                ▲ timer
                                ▼                                          │
                           Locomotion ◀────────────────────────────────────┘

  Locomotion ──E──▶ Kick ──timer──▶ Locomotion

  ANY STATE ──took unguarded hit──▶ Staggered ──timer──▶ Locomotion
  ANY STATE ──HP reaches 0──▶ Dead  (terminal)
```

### Skeleton FSM (`Assets/Scripts/Enemies/SkeletonStates.cs`)

```
Idle ──player in range──▶ Approach ──reached stop distance──▶ Circle
                                                                 │
                                        decision timer elapses  │
                              ┌──────────────────────────────────┤
                              ▼                                  ▼
                           WindUp                              Block
                              │ timer                            │ timer / released
                              ▼                                  ▼
                           Attack ──timer──▶ Recover ──timer──▶ Circle

  ANY STATE ──parried or kicked──▶ Staggered ──timer──▶ Circle
  ANY STATE ──HP reaches 0──▶ Dead (terminal, triggers OnSkullPopped)
```

Variant gating (does not change the diagram, changes what reaches `HealthComponent.ApplyDamage` inside `TryReceiveDamage`):
- **Basic:** any unblocked hit damages normally.
- **Armored:** only `DamageType.Riposte` damages; ordinary swings are absorbed with no effect.
- **Shield:** damage is blocked by default until `Kick` sets `ShieldBroken = true`; `Circle`'s `Enter` re-arms the shield for the next exchange.

### Headless Horseman FSM (`Assets/Scripts/Boss/HorsemanStates.cs`)

```
Phase 1 - Mounted:
  Charge ──reached end of lane──▶ Turn ──timer──▶ Charge (lane reversed)
  Charge ──lance parried──▶ Unhorsed ──timer──▶ [enter Phase 2]

Phase 2 - Dismounted duel:
  DuelWindUp ──timer──▶ (feint roll) ─┬─▶ DuelAttack ──timer──▶ DuelRecover
                                       └─▶ Feint ──timer──▶ DuelAttack ──timer──▶ DuelRecover

  DuelRecover ──(health above threshold)──▶ DuelWindUp  (loop)
  DuelRecover ──(random roll)──▶ DuelBlock ──released/timer──▶ DuelWindUp
  DuelRecover ──(health <= 1/3 of Dismounted max)──▶ [enter Phase 3]
  ANY Phase-2 state ──parried──▶ Staggered ──timer──▶ DuelWindUp

Phase 3 - Head throw:
  HeadThrowWindUp ──timer──▶ (spawn projectile) ──▶ HeadThrowRecover
  HeadThrowRecover ──cooldown elapses──▶ HeadThrowWindUp (loop until resolved)
  [projectile parried back and lands] ──▶ Defeated (terminal)

ANY STATE ──HP reaches 0 (HealthComponent.OnDied)──▶ Defeated (terminal)
```

## Iteration Log

Per-iteration descriptions are written here as each is pushed to GitHub, matching the commit that introduced it, per the assignment's "description of changes and git pushes match" criterion.

### Iteration 1 - Framework (this commit)

Initial combat/FSM framework before any scene assembly or playtesting: the shared `StateMachine<T>` core, the `IDamageable`/`GuardComponent`/`StaminaSystem`/`HealthComponent` combat stack, the Player/Skeleton/Horseman FSMs described above, the directional-swing mouse reader, lock-on targeting script, and the kickable-skull/pumpkin-bomb/pumpkin-head interactables that back the "reuse the ability in a new way" beats. No scene, animation, or audio integration yet - this iteration establishes the rules layer the rest of the build will be played and tuned against. Subsequent iterations will describe concrete playtesting findings (timing feel on the parry window, stamina costs, boss phase pacing) and the specific numeric/behavioral changes made in response, once the level is playable end to end.
