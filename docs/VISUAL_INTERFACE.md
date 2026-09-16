# MUSCA Visual Interface — local exploratory shell

Status: implemented candidate for the next gameplay iteration. It is **not** part
of frozen GATE-P01 v0.1 and must not replace that interface mid-collection.

Decision record: [ADR-0006](adr/ADR-0006-local-visual-interface.md).

## Run

```powershell
py -3.14 -m musca --visual --puzzle --mode light_chemical --layout a --output experiments/results/visual-gate.json
```

`--layout` is evaluator/facilitator configuration. The visual window never displays
its value or the hidden neutral reagent before permitted evidence is obtained.

## What the player sees

1. A signal path whose intensity changes while MUSCA approaches the source.
2. Separate FACT / HYP / UNKNOWN / INTERP statements.
3. The gate with amber and cobalt reagent cards and their light charge.
4. A two-cell resource indicator.
5. `SCAN` only when the `light_chemical` mode permits it.
6. A visual outcome panel after the player's decision.

## Architecture boundary

The UI is presentation only:

```text
HUMAN clicks a semantic action
        ↓
VisualPuzzleController
        ↓
Simulation / GateSession
        ↓
Bridge / MuscaBackend / World
```

The UI does not accept `north/south/east/west` and does not bypass Bridge.
`VisualPuzzleController.snapshot()` intentionally contains no layout field.

The final evaluator receipt may contain diagnostic layout metadata after the fact,
matching the existing terminal receipt boundary. Do not show the receipt to the
player before the session is complete.

## Current implementation boundary

- Windows host verified with CPython 3.14.6 and Tk 8.6.
- No third-party Python packages were added.
- Tk is a local prototype bridge, not a production game engine decision.
- Audio, 3D, animation pipeline, controller/gamepad support and packaging are not implemented.
- Linux/macOS GUI behavior is unverified.
