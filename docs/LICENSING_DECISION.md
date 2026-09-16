# MUSCA licensing decision note

Status: `DECISION SUPPORT / NO LICENSE SELECTED`
Date: 2026-09-16

## 1. Current state

The MUSCA repository currently has no project license selected. GitHub's official documentation states that without a license, default copyright applies; publishing a public repository allows GitHub users to view and fork it under the platform terms but does not make the code open source or generally grant rights to reproduce, distribute or create derivative works.

Therefore adding a LICENSE file is not clerical cleanup. It is an owner/governance decision.

## 2. Separate rights layers

Do not apply one mental label such as "MUSCA is MIT" to every artifact. At minimum separate:

1. MUSCA source code written for this project.
2. Third-party source code and libraries.
3. Connectome/neuroscience datasets.
4. Scientific paper text/figures and archived experiment output.
5. Game art, audio, lore, character designs and branding.
6. User/playtest data.

Each layer can have a different license or privacy regime.

## 3. Observed third-party boundaries

### Shiu computational model code

`philshiu/Drosophila_brain_model` is reported by GitHub as MIT licensed.
### FlyWire public-release data

FlyWire's current [public-release guideline](https://flywire.ai/guidelines) states `CC BY-NC 4.0` (rechecked 2026-09-16). That page explicitly discusses v783; the applicable terms and attribution chain of a historical v630 artifact still need to be traced for its intended use. The `NC` restriction is material for a future commercial game. Do not assume that an MIT license on an accompanying code repository removes restrictions attached to underlying connectome data.

Whether a learned/distilled controller is a derivative of licensed data is a fact-specific legal question. This note does not answer it.

### MaleCNS v1.0

The official MaleCNS download page states that the Male CNS is licensed under CC-BY. This may make it an attractive later research source for a commercially compatible pipeline, but scientific suitability and provenance still have to be established; it is not a drop-in replacement for reproducing the Shiu/FlyWire-v630 paper.

## 4. MUSCA source-code choices

Possible owner decisions include:

### A. Keep default copyright for now

Use while the research/game architecture is still changing and before deciding what should be open.

Price: outside contribution/reuse rights remain unclear or restrictive; public visibility is not open-source permission.

### B. MIT for MUSCA code

Simple permissive grant with attribution and warranty disclaimer.

Price: no explicit patent grant language comparable to Apache-2.0; permissive downstream reuse, including commercial reuse, is expected.

### C. Apache-2.0 for MUSCA code

Permissive license with explicit patent terms and more notice machinery.
Price: more text/obligations and still does not solve third-party dataset licensing.

### D. Split licensing

For example, open-source research/runtime tooling while retaining proprietary rights for game assets, narrative, brand and production server code.

This often maps better to a research-plus-game project than pretending every byte is the same product.

## 5. Recommended decision process

Before choosing:

1. Decide whether MUSCA is intended to invite external code contributions now.
2. Decide whether a future commercial game should be allowed to reuse the research/runtime code freely.
3. Identify any patent-strategy concerns.
4. Inventory third-party code/data licenses independently.
5. Keep game art/lore/trademarks outside the software-license assumption.
6. Obtain qualified legal review before commercial distribution that depends on CC-BY-NC connectome-derived material.

## 6. Current safe boundary

Until the owner chooses a license:

- do not label MUSCA "open source";
- do not copy third-party datasets into the game repository merely because they are publicly downloadable;
- keep research datasets out of distributable game builds;
- store license/provenance metadata in dataset manifests;
- do not claim commercial clearance for a connectome-derived controller without separate analysis.

## Decision status
`OPEN / OWNER DECISION REQUIRED`

No LICENSE file was added by this note.
