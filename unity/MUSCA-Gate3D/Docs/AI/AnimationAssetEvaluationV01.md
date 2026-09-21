# MUSCA // Animation Asset Evaluation v0.1

Date: 2026-09-21
Target editor: Unity 6000.6.1f1
Purpose: replace the temporary procedural puppet motion with real humanoid clips while preserving MUSCA gameplay authority.

## Decision

Use a layered approach:

1. keep CharacterController / combat logic authoritative;
2. add Humanoid Animator + Blend Trees for locomotion;
3. use in-place clips first, driven by gameplay speed/direction;
4. add dedicated dodge/evade clips without transferring gameplay authority to root motion until measured;
5. use Animation Rigging for feet, weapon alignment and targeted upper-body corrections;
6. keep current procedural presentation only as fallback until each humanoid layer passes human feel testing.

## Best first import candidates

### Human Basic Motions FREE — Kevin Iglesias

Status: preferred locomotion baseline.

Current store facts observed 2026-09-21:

- free;
- latest version 2.4.2;
- latest release 2026-04-30;
- original Unity version 6000.0.59;
- Built-in / URP / HDRP listed compatible;
- keywords explicitly include walk, run, sprint, jump, humanoid, Mecanim and retargeting.

Use for:

- idle;
- walk/run/sprint;
- jump/fall/land candidates;
- first locomotion Blend Tree;
- possible roll/slide candidates after inspecting package contents.

Source:
https://marketplace.unity.com/packages/3d/animations/human-basic-motions-free-154271

### Human Melee Animations FREE — Kevin Iglesias

Status: preferred free combat baseline.

Current store facts observed 2026-09-21:

- free;
- latest version 2.0.2;
- latest release 2026-06-19;
- original Unity version 6000.0.59;
- Built-in / URP / HDRP listed compatible;
- explicitly tagged humanoid, Mecanim and retargeting.

Use for:

- attack timing reference;
- hit/recovery/body commitment reference;
- first replacement for purely procedural strike posing where clips fit.

Source:
https://marketplace.unity.com/packages/3d/animations/human-melee-animations-free-165785

## Dodge-specific option

### Roll And Dash Animation — Raisecreation

Status: secondary paid candidate, not first dependency.

Observed:

- $10;
- latest version 1.0;
- release 2025-01-23;
- original Unity version 2022.3.29;
- keywords include roll, dash, dodge and character movement.

Reason not to buy/import first:

Human Basic Motions FREE may already provide enough roll/slide material for the first humanoid pass. Evaluate the free pack contents before adding a paid dependency.

Source:
https://assetstore.unity.com/packages/3d/animations/roll-and-dash-animation-307761

## Kael / spear option

### Spear MocapAnimPack — RIB Studio

Status: reference / later evaluation, not approved dependency.

Observed:

- $49.99;
- version 1.0;
- release 2022-10-12;
- original Unity version 2021.2.7;
- keywords include spear, pike, halberd, fighting, attack and movement.

Risk:

The pack is substantially older than our Unity 6.6 project. Humanoid FBX retargeting may still work, but compatibility has not been verified in this repository. Do not purchase solely because the theme matches Kael.

Source:
https://assetstore.unity.com/packages/3d/animations/spear-mocapanimpack-234022

## Official Unity rigging layer

### Animation Rigging

Status: recommended official package once the humanoid Animator layer is introduced.

Unity 6.0 documentation states:

- package: `com.unity.animation.rigging`;
- released version for Unity 6000.0: **1.4.0**;
- implemented using Unity Animation C# Jobs.

Intended MUSCA uses:

- two-bone foot IK / foot placement;
- spear-hand alignment;
- upper-body aim/telegraph corrections;
- additive procedural corrections after authored clips rather than replacing authored clips.

Source:
https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.animation.rigging.html

## Mixamo

Status: fallback/source-expansion option, not repository dependency yet.

Use only for gaps not covered by the two Unity-6-compatible free packs. Downloaded FBX clips would still need:

- Humanoid rig validation;
- Avatar mapping;
- loop/in-place settings;
- retargeting check on the chosen Mira/Kael skeleton;
- license/provenance record for each imported clip.

## Import gate

Third-party Asset Store bytes are not committed until they are legitimately acquired through the owner's Unity account / Asset Store entitlement.

Required before commit:

- exact asset/version recorded;
- license source recorded;
- imported FBX clips inspected;
- Humanoid Avatar valid;
- no demo scripts/controllers silently become gameplay authority;
- only necessary package content retained when license permits;
- locomotion/combat behavior still controlled by MUSCA code;
- EditMode + runtime QA rerun.

## Proposed implementation order

1. Import Human Basic Motions FREE.
2. Create Mira Humanoid Avatar and Animator.
3. Build 2D locomotion Blend Tree from forward/back/left/right clips.
4. Keep CharacterController authoritative and drive Animator parameters from measured velocity.
5. Replace procedural jump/dodge poses with clips one at a time.
6. Import Human Melee Animations FREE and replace player/Kael attack presentation selectively.
7. Add Animation Rigging for foot placement and spear alignment.
8. Evaluate a dedicated spear pack only if generic humanoid melee + rigging cannot give Kael a convincing silhouette.

## Claim boundary

This document records current external candidates and a technical integration plan. No third-party animation package has yet been imported into the MUSCA repository, and no claim is made that any candidate feels correct in-game before human playtest.
