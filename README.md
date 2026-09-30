# Dragon Battle Arena

A small 2.5D top-down dragon battle game developed in Unity as part of a **Junior Unity Developer Technical Assessment**.

The project features a player-controlled dragon battling an AI-controlled dragon in a small arena, with three distinct abilities, health and cooldown systems, hit feedback, VFX, SFX, AI combat behavior, death animations, and a complete win/loss flow.

---

## Gameplay Video


Uploading GamePlayVideo.mp4…



[Watch the Gameplay Video](https://youtu.be/7RpiYfHHxME?si=LHeyKaAkSZB4POX5)

The video demonstrates the player movement, all three abilities, AI combat, health/cooldown UI, combat feedback, and the Winner Screen.

---

## Engine & Setup

- **Unity:** 6000.0.39f1 LTS
- **Render Pipeline:** Universal Render Pipeline (URP)
- **Input:** Unity New Input System
- **Perspective:** 2.5D top-down

---

## Controls

### Movement

- **Left Mouse Click** — Move the player dragon

### Abilities

- **1** — Fire Attack
- **2** — Tail Attack
- **3** — Fly / Fireball Attack

The controls are implemented using Unity's **New Input System**.

---

## Gameplay

The player fights an AI-controlled dragon inside a bounded arena.

### Fire Attack

A ranged fire-breath attack that deals damage to the enemy when the attack successfully connects.

- Ranged attack
- Damage-based
- Cooldown
- Attack animation
- Fire VFX
- Sound effect
- Hit feedback

### Tail Attack

A close-range melee attack using the dragon's tail.

- Close-range damage
- Knockback interaction
- Cooldown
- Attack animation
- VFX
- Sound effect
- Hit feedback

### Fly / Fireball Attack

The dragon performs a flying attack and launches a fireball projectile toward the target.

- Projectile-based attack
- Damage on impact
- Cooldown
- Attack animation
- Fireball VFX
- Projectile impact effect
- Sound effects
- Hit feedback

Each ability has its own cooldown and visual UI indicator.

---

## AI

The enemy dragon is controlled by a simple distance-based combat state system.

The AI can:

- Detect the player
- Chase the player when required
- Choose abilities based on distance
- Use Fire, Tail, and Fly attacks
- Respect ability cooldowns
- React to successful attacks
- Stop combat when the battle ends

The goal was to keep the AI readable and simple while still making the enemy actively participate in the battle.

---

## Health & Battle System

Both dragons have independent health systems.

When damage is successfully applied:

- Health is reduced
- Hit feedback is triggered
- Appropriate VFX/SFX can be played

When a dragon reaches zero health, the battle is resolved.

Possible results:

- **Player Win**
- **AI Win**
- **Draw**

The defeated dragon plays its death animation and the result screen is displayed.

A **Restart** option is provided to start the battle again.

---

## UI

The battle UI includes:

- Player health bar
- AI health bar
- Dragon portraits
- Ability icons
- Ability cooldown indicators
- Winner Screen
- Restart button

---

## VFX & SFX

The project includes combat feedback such as:

- Fire attack VFX
- Fireball projectile VFX
- Projectile impact VFX
- Hit effects
- Tail knockback feedback
- Attack sound effects
- Hit sound effects
- Death animations
- Cooldown feedback

These were used to make the combat interactions clearer and more responsive.

---

## Assets & Resources

Free assets and resources were used for the project.

### Asset Sources

- **AnimeTree** — Dragon / environment assets
- **Bublik** — Dragon / game assets
- **FreeDragons** — Dragon assets
- **Unity Technologies** — Particle System / Unity resources
- **Mixkit** — Sound effects

Third-party assets remain the property of their respective creators and publishers and are used according to their applicable free-use and licensing terms.

---

## How to Run

1. Open the project using **Unity 6000.0.39f1 LTS**.
2. Open the main battle scene.
3. Press **Play**.
4. Use **Left Mouse Click** to move the player dragon.
5. Use **1, 2, and 3** to use the three abilities.

A playable Windows build is provided separately.

---

## Project Structure

The project is organized into separate systems for:

- Player movement
- Player combat
- AI behavior and combat
- Health and damage
- Ability cooldowns
- Hit feedback
- Attack VFX
- Projectile and impact VFX
- Sound effects
- Death animations
- Battle result handling
- UI and cooldown indicators

---

## AI Usage Note

AI-assisted tools were used during development as a support tool for implementation, debugging, research, and iteration.

The AI-assisted workflow was used for:

- Exploring implementation approaches
- Generating and refining C# code patterns
- Debugging Unity-related issues
- Troubleshooting gameplay systems
- Iterating on existing implementations

The generated suggestions were reviewed, adapted, integrated into the Unity project, and tested during development. The final setup, component configuration, Animator configuration, Animation Events, VFX, UI, balancing, and gameplay integration were verified in Unity.

A separate `AI_USAGE_NOTE.md` file is included in the repository with additional information about the AI-assisted development process.

---

## Credits

This project was created as part of a **Junior Unity Developer Technical Assessment**.

Thanks to the creators and publishers of the free assets and resources used in the project.
