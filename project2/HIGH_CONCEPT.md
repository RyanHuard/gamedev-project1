# The Hollow Tournament - High Concept Document

CSCI 5499B/5999B - Project 2 (Halloween theme)

## Concept

Every Halloween night, the dead hold a knightly tournament in the ruins of a castle they once defended in life. You are a living knight who wandered in by accident, and the skeleton knights are delighted to finally have fresh competition. You fight your way across the tournament grounds - courtyard, gatehouse, armory, and chapel - to earn a match against the tournament's reigning champion, the Headless Horseman.

The tone is goofy rather than scary: skeletons rattle apart and reassemble, a kicked skull can ring a bell like a bowling pin, pumpkins used as bombs pop into candy on explosion, and the Horseman is a showboating champion who taunts before every charge. Halloween theming (jack-o'-lanterns, bone piles, autumn ruins, bats, a harvest-moon sky) is present throughout, but nothing here is built to be frightening.

## What You Are Learning / What the Game Is Teaching

The Hollow Tournament teaches a four-part melee combat vocabulary, one verb per section of the level, and then immediately asks the player to recombine everything they've learned in a new context:

1. **Directional swing** - mouse-movement-driven attack direction (left / right / overhead), taught against armored scarecrow dummies that rotate their open side.
2. **Block** - a held defensive stance that costs stamina, taught against a basic skeleton, then stress-tested against two skeletons at once.
3. **Parry** - a precisely-timed block (first ~0.2s of a block, before the hit lands) that staggers the attacker and opens a free counter-hit (riposte). Taught against an armored skeleton that is otherwise immune to damage, then immediately reused on a thrown projectile (the witch's pumpkin bomb) instead of a melee swing.
4. **Kick** - a guard-breaking strike, taught against a shield skeleton, then reused as an environmental tool: the popped-loose skull can be kicked into a bell to open a gate, or into a crowd of skeletons to stagger all of them at once.

Mechanically, the lesson under the lesson is: **read the opponent's open side, and every tool you have (sword or environment) can be redirected back at whatever creates an opening.** The boss fight is built explicitly to prove this - the player must parry a lance, then a sword, then a thrown flaming pumpkin head, using the same parry timing against three different presentations of "an incoming attack."

## Rules

- The player has a health pool and a stamina pool. Stamina gates blocking (continuous drain while held) and kicking (a flat cost per kick); it regenerates when neither is active.
- Attacks are thrown by moving the mouse during a wind-up and releasing the attack button; the net direction of mouse movement at release selects Left, Right, or Overhead.
- Enemies telegraph a wind-up before every attack and guard one side at a time; an attack that matches an enemy's guarded side is blocked (or, for shield/armored variants, does nothing) while an attack to the open side connects.
- Blocking absorbs incoming damage at a stamina cost; blocking with zero stamina breaks guard instead of absorbing the hit.
- Starting a block within approximately 0.2 seconds of an incoming hit is a parry instead of a block: the attacker staggers for a fixed duration and takes no stamina cost, and the player may immediately land one free, guaranteed-hit riposte.
- A kick ignores guard entirely, always staggers (breaking a shield skeleton's guard specifically), and on a skeleton kill or kick, knocks a physical skull object loose that can be kicked a second time to trigger environment objects (bells, crowds).
- Armored skeletons take no damage from ordinary swings; the only damage that affects them is a riposte landed immediately after parrying them.
- Thrown projectiles (pumpkin bombs, the Horseman's flaming head) can be parried exactly like a melee swing - starting a block in the ~0.2s before impact redirects the projectile back at its thrower instead of resolving against the player.
- The Horseman fight has three phases that must be cleared in order: Mounted (parry the lance charge to unhorse him), Dismounted (standard duel rules, with feints that bait an early parry), and Head Throw (parry the thrown head back at him to win). Defeating the Horseman ends the level.
- The player dies and the level restarts from the last checkpoint if health reaches zero at any point.

## Game Modes

The Hollow Tournament has a single game mode: a linear single-player story level ("the tournament grounds") played start to finish in one sitting, roughly 6-8 minutes long. There is no difficulty select, no replay-for-score loop, and no multiplayer mode - the vertical slice is the whole game. (If this were extended past Project 2, a "New Tournament+" replay mode with remixed guard patterns would be the natural next game mode, but it is out of scope for this submission.)

## Expected Player

The expected player is someone with prior experience in 3D action/melee games (e.g., Mordhau, For Honor, Soulslike titles) who recognizes directional-combat conventions quickly, but the game does not assume they have played those specific titles - the Courtyard section is built to onboard a complete newcomer to directional swinging in under a minute. The player is expected to have a mouse and keyboard (or equivalent) and basic 3D-camera literacy (WASD + mouse-look). No reading comprehension beyond short on-screen prompts is required, and no reflex requirement exceeds what a first-time player can hit after one or two failed attempts - every teaching encounter is intentionally low-stakes (dummies, a single weak skeleton) before stakes rise.

## Background Information

- **Setting:** A ruined castle that was the site of a real tournament in life. Every Halloween, the knights who died there rise to hold the tournament again, because that is the only night the dead remember how to be anything other than dead. They are not malicious - to them, this is sport, not violence, which is why a defeated skeleton rattles apart instead of gruesomely dying, and why the Horseman taunts rather than threatens.
- **The Headless Horseman's role:** He is not a folkloric threat haunting the living; he is the tournament's reigning, undefeated champion, and he is thrilled that a living knight finally showed up to give him a real match. His "flaming pumpkin head" is as much his signature showboating prop as it is a weapon - it's the Halloween version of a champion's cape toss.
- **Why the skeletons have variants:** The armory skeleton wears parade armor left over from the tournament's glory days (hence why only a clean riposte punches through it), and the chapel's shield skeleton is a former gate-guard, which is why breaking its guard with a kick is thematically "breaking the gate open" before the player can later use a skull to open the literal gate.
- **Nothing in the Background section above is explained in-level** - it exists so a player who wonders "why does this skeleton suddenly take damage" or "why is the final boss headless and on fire" has a consistent in-world answer, per the assignment's request for background the instructor would need but the game itself doesn't spell out.
