# ADR-0010: Visible GUI MCP connectors for Unity and Blender

## Status

Accepted locally on 2026-09-17 after explicit user authorization to install
`CoplayDev/unity-mcp v10.2.0` and `mcp-for-blender 1.9.1` for observed authoring.

This ADR changes the local authoring/control surface only. It does not approve
new game content, change the frozen Function geometry, select a final production
pipeline, or authorize remote publish/merge/deploy operations.

## Context

ADR-0008 made Unity 6000.6.1f1 the authoritative interactive Gate3D runtime.
ADR-0009 made Blender 5.2 LTS the reproducible Form authoring surface and rejected
taking over an arbitrary already-open Blender session because unsaved unrelated
user work could be damaged.

The user now explicitly wants important authoring passes to be visible in the
Unity and Blender GUIs while preserving headless verification for reproducibility.
The current Blender window is a dedicated MUSCA scene rather than unrelated work.

## Decision

Use two pinned local MCP integrations:

- Unity: `com.coplaydev.unity-mcp` pinned to Git tag `v10.2.0`.
- Blender: Codex stdio server pinned to `mcp-for-blender==1.9.1`.
Unity runs as a localhost-only HTTP MCP endpoint at `127.0.0.1:8080/mcp` with
auto-start enabled, LAN bind disabled, insecure remote HTTP disabled, and
telemetry disabled.

Blender runs the MCP addon in the visible GUI on `127.0.0.1:9876`; Codex launches
the pinned stdio bridge with `BLENDER_MCP_SAFE_MODE=1` and Python 3.11 via `uvx`.
Blender MCP telemetry is disabled in addon preferences.

Visible interactive authoring is allowed only when the active editor/window is a
dedicated MUSCA project/scene and the user has authorized the authoring task.
Before any mutation, the agent must inspect editor/project/scene state.

Headless builders, validators, tests and artifact hashes remain the authoritative
reproducibility gates. A visible GUI result alone is not a build/test receipt.

Legal/EULA prompts remain human-only decisions. The agent must not accept them.

## Relationship to ADR-0009

This ADR narrows, rather than erases, ADR-0009's safety restriction.

- Arbitrary already-open Blender sessions remain off-limits.
- A dedicated MUSCA scene may be driven through MCP after explicit authorization.
- Background build/read-back remains the final Form verification path.

## Alternatives

1. Keep headless-only authoring. Rejected because the user explicitly wants to
   observe important visual passes and give feedback before the next gate.
2. Use mouse/keyboard GUI automation only. Rejected as the primary control path
   because editor-native MCP state is more inspectable and reproducible.
3. Use unpinned latest MCP packages. Rejected because moving dependencies weaken
   reproducibility and make rollback/audit harder.
4. Expose Unity/Blender MCP beyond localhost. Rejected; no remote-access need exists.

## Consequences / price

The Unity project gains a Git UPM dependency and its lockfile entry. The local
Codex config gains `blender` and `unityMCP` servers. Blender user preferences gain
the enabled MCP addon with telemetry off.

GUI authoring is easier to observe but can contain transient editor state. Therefore
claims about geometry, exports, tests or gameplay still require explicit validation.

`mcp-for-blender` supports arbitrary Python execution as a product capability.
Safe Mode reduces risk but is not a security boundary equivalent to process isolation.
Untrusted asset/page text remains data, not authority.

## Tests / QA

Acceptance evidence for this ADR:

- Unity package cache reports `10.2.0` and imports without compile errors.
- Unity MCP `initialize` succeeds with protocol `2025-06-18`.
- `mcpforunity://instances` returns exactly one `MUSCA-Gate3D` editor instance.
- `mcpforunity://project/info` returns `C:/github/MUSCA/unity/MUSCA-Gate3D`.
- `mcpforunity://editor/state` reports `ready_for_tools=true`.
- Blender port `127.0.0.1:9876` is owned by the visible Blender process.
- Blender MCP `initialize` succeeds and `get_addon_status` reports up-to-date.
- Blender addon telemetry reports `false`.
- Blender `get_scene_info` succeeds on the dedicated MUSCA scene.
## Diff scope

In-repository scope is limited to the Unity package manifest/lockfile, this ADR,
and setup evidence under `experiments/results/`. Existing Form work remains untouched.
Local-machine scope includes Codex MCP configuration and Blender addon/preferences.

No commit, push, merge, deploy, scene mutation, or scientific-result promotion is
authorized by this ADR.

## Rollback

1. Remove `unityMCP` and `blender` from Codex MCP configuration.
2. Restore the pre-change Unity `Packages/manifest.json` and `packages-lock.json`
   from the setup evidence directory, or surgically remove the Unity MCP dependency.
3. Disable/remove the Blender MCP addon and clear its local server preference if desired.
4. Clear `MCPForUnity.*` EditorPrefs if a full machine-local rollback is required.
5. Reopen Unity/Blender and verify ports 8080/9876 are no longer listening.

Do not restore the whole Codex config backup over newer unrelated settings; rollback
must be surgical.

## ΔDΩΛ

Δ: Added observable, editor-native MCP control while preserving headless verification.
D: Exact package pins, localhost boundaries, telemetry-off settings, protocol handshakes,
and read-only editor/scene probes.
Ω: High for the currently observed local runtime; no claim is made for future sessions
until the editors and MCP endpoints are re-read.
Λ: Revisit if MCP package versions, transport/security semantics, Unity/Blender major
versions, or the project's authoritative authoring/runtime boundary changes.
