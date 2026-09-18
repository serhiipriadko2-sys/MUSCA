"""Local visual shell for the synthetic MUSCA gate puzzle.

The GUI is a presentation layer over Simulation and GateSession. It does not
receive the hidden gate layout and does not issue frame-level motor commands.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from .backends import ScriptedBackend
from .contracts import ContractError, Goal, MAX_INTENT_TICKS, SensorMode
from .puzzle import GateSession
from .simulation import Simulation
from .world import GridWorld


@dataclass(frozen=True)
class VisualSnapshot:
    stage: str
    mode: str
    tick: int
    signal: int | None
    signal_history: tuple[int, ...]
    interpretation: tuple[str, ...]
    cells_remaining: int | None = None
    scan_available: bool = False
    outcome: str | None = None


class VisualPuzzleController:
    """Player-safe state machine shared by tests and the Tk presentation."""

    def __init__(self, mode: SensorMode, layout: str, *, max_ticks: int = 16) -> None:
        if not isinstance(mode, SensorMode):
            raise ContractError("visual mode must be a SensorMode")
        if type(max_ticks) is not int or not 1 <= max_ticks <= 256:
            raise ContractError("max_ticks must be an integer between 1 and 256")
        self.mode = mode
        self.max_ticks = max_ticks
        self._layout = layout
        self.simulation = Simulation(
            world=GridWorld(hazards=frozenset()),
            backend=ScriptedBackend(),
            sensor_mode=mode,
        )
        self.gate: GateSession | None = None
        self.stage = "navigation"
        self.navigation_result: dict[str, Any] | None = None
        self._error: str | None = None

    def _signal_values(self) -> tuple[int, ...]:
        values: list[int] = []
        for frame in self.simulation.trace:
            for event in frame["events"]:
                if event["kind"] == "signal" and type(event["value"]) is int:
                    values.append(event["value"])
        return tuple(values)

    def snapshot(self) -> VisualSnapshot:
        frame = self.simulation.trace[-1]
        values = self._signal_values()
        gate_view = self.gate.view() if self.gate is not None else None
        interpretation = tuple(frame["interpretation"])
        cells: int | None = None
        scan_available = False
        outcome: str | None = None
        if gate_view is not None:
            interpretation = tuple(gate_view["interpretation"])
            cells = gate_view["cells_remaining"]
            scan_available = gate_view["scan_available"]
            outcome = gate_view["end_reason"]
        elif self.navigation_result is not None:
            outcome = self.navigation_result["end_reason"]
        if self._error:
            interpretation = (*interpretation, f"[INTERP] {self._error}")
        return VisualSnapshot(
            stage=self.stage,
            mode=self.mode.value,
            tick=self.simulation.world.tick,
            signal=values[-1] if values else None,
            signal_history=values,
            interpretation=interpretation,
            cells_remaining=cells,
            scan_available=scan_available,
            outcome=outcome,
        )

    def _finish_navigation(self, reason: str) -> None:
        self.navigation_result = self.simulation.result(reason)
        if self.navigation_result["end_reason"] == "beacon":
            self.gate = GateSession(self.mode, self._layout)
            self.stage = "gate"
        else:
            self.stage = "result"

    def navigation_choice(self, command: str) -> bool:
        if self.stage != "navigation":
            return False
        if command == "quit":
            self._finish_navigation("user_quit")
            return True
        if command not in {"go", "wait"}:
            self._error = "Неизвестное действие; состояние мира не изменилось."
            return False
        self._error = None
        try:
            if command == "wait":
                self.simulation.submit_goal(Goal.HOLD, ttl=4)
            elif not self.simulation.bridge.modulation(self.simulation.world.tick).approach_signal:
                self.simulation.submit_goal(Goal.SEEK_SIGNAL, ttl=MAX_INTENT_TICKS)
            previous_kinds = {event["kind"] for event in self.simulation.trace[-1]["events"]}
            for _ in range(min(4, self.max_ticks - self.simulation.world.tick)):
                frame = self.simulation.step()
                kinds = {event["kind"] for event in frame["events"]}
                newly_warned = "chemical_warning" in kinds and "chemical_warning" not in previous_kinds
                if self.simulation.finished or newly_warned:
                    break
                previous_kinds = kinds
        except ContractError as exc:
            self._error = f"Намерение отклонено: {exc}."
            return False
        if self.simulation.finished:
            self._finish_navigation("tick_limit")
        elif self.simulation.world.tick >= self.max_ticks:
            self._finish_navigation("tick_limit")
        return True

    def gate_choice(self, command: str) -> bool:
        if self.stage != "gate" or self.gate is None:
            return False
        self._error = None
        accepted = self.gate.choose(command)
        if not accepted:
            self._error = "Действие недоступно; запас и наблюдения не изменились."
            return False
        if self.gate.finished:
            self.stage = "result"
        return True

    def quit(self) -> None:
        if self.stage == "navigation":
            self.navigation_choice("quit")
        elif self.stage == "gate" and self.gate is not None and not self.gate.finished:
            self.gate_choice("quit")

    def results(self) -> tuple[dict[str, Any], dict[str, Any]]:
        navigation = self.navigation_result or self.simulation.result("user_quit")
        gate = self.gate.result() if self.gate is not None else {"end_reason": "not_reached"}
        return navigation, gate
